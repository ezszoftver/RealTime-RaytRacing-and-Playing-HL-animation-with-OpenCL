using GlmSharp;
using OpenCL.Net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCLRenderer
{
    struct Vertex
    {
        public vec3 m_V;
        public vec3 m_N;
        public vec2 m_TC;
        public byte m_iNumMatrices;
        public Int32 m_iMatrixId1;
        public Int32 m_iMatrixId2;
        public Int32 m_iMatrixId3;
        public float m_fWeight1;
        public float m_fWeight2;
        public float m_fWeight3;
    }

    struct Triangle
    {
        public Vertex m_A;
        public Vertex m_B;
        public Vertex m_C;
        public Int32 m_iMaterialId;
    }

    struct BBox
    {
        public vec3 min;
        public vec3 max;
    }

    struct BVHNode
    {
        public Triangle m_Triangle;
        public BBox m_BBox;
        public Int32 m_iLeft;
        public Int32 m_iRight;
    }

    struct Material
    {
        public IMem m_clDiffuse;
        public IMem m_clSpecular;
        public IMem m_clNormal;
    }

    struct Matrix
    {
        public mat4 m_mMatrix;
        public Int32 m_iParentId;
    }

    struct BVHObject
    {
        public Int32 iOffset;
        public Int32 iCount;
        public byte iType;

        public Int32 iIsRefitTree;
    }

    class Scene
    {
        public Scene()
        {
            ErrorCode error;

            Platform[] platforms = Cl.GetPlatformIDs(out error);
            if (error != ErrorCode.Success)
            {
                throw new Exception("Cl.GetPlatformIDs");
            }

            foreach (Platform platform in platforms)
            {
                foreach (Device device in Cl.GetDeviceIDs(platform, DeviceType.Gpu, out error))
                {
                    if (error != ErrorCode.Success) { continue; }
                    if (Cl.GetDeviceInfo(device, DeviceInfo.ImageSupport, out error).CastTo<Bool>() == Bool.False) { continue; }

                    m_Context = Cl.CreateContext(null, 1, new Device[] { device }, null, IntPtr.Zero, out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateContext");
                    }

                    return;
                }
            }

            throw new Exception("Scene: Not find OpenCL GPU device!");
        }

        // Matrix
        public Int32 GenMatrix()
        {
            m_mtxMutex.WaitOne();
            Int32 iId = m_listMatrices.Count;

            Matrix newMatrix = new Matrix();

            m_listMatrices.Add(newMatrix);
            m_mtxMutex.ReleaseMutex();
            return iId;
        }
        public void SetMatrix(Int32 iId, mat4 mMatrix, Int32 iParentId = -1)
        {
            m_mtxMutex.WaitOne();
            Matrix newMatrix = new Matrix();

            newMatrix.m_mMatrix = mMatrix;
            newMatrix.m_iParentId = iParentId;

            m_listMatrices[iId] = newMatrix;
            m_mtxMutex.ReleaseMutex();
        }

        // Material
        public Int32 GenMaterial()
        {
            m_mtxMutex.WaitOne();
            Int32 iId = m_listMaterials.Count;

            Material newMaterial = new Material();

            m_listMaterials.Add(newMaterial);
            m_mtxMutex.ReleaseMutex();
            return iId;
        }
        public void SetMaterial(Int32 iId, string @strDiffuseFileName, string @strSpecularFileName, string @strNormalFileName)
        {
            m_mtxMutex.WaitOne();
            Material newMaterial = new Material();

            newMaterial.m_clDiffuse  = CreateOpenCLTextureFromFile(@strDiffuseFileName);
            newMaterial.m_clSpecular = CreateOpenCLTextureFromFile(@strSpecularFileName);
            newMaterial.m_clNormal   = CreateOpenCLTextureFromFile(@strNormalFileName);

            m_listMaterials[iId] = newMaterial;
            m_mtxMutex.ReleaseMutex();
        }
        IMem CreateOpenCLTextureFromFile(string @strFileName)
        {
            ErrorCode error;

            Bitmap bitmap = new Bitmap(@strFileName);
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            Rectangle rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            OpenCL.Net.ImageFormat format = new OpenCL.Net.ImageFormat(ChannelOrder.RGBA, ChannelType.Unorm_Int8);
            IMem clTexture = Cl.CreateImage2D(m_Context, MemFlags.CopyHostPtr | MemFlags.ReadOnly, format, new IntPtr(bitmap.Width), new IntPtr(bitmap.Height), new IntPtr(0), bitmapData.Scan0, out error);

            if (error != ErrorCode.Success)
            {
                throw new Exception("Cl.CreateImage2D: " + strFileName);
            }

            bitmap.Dispose();
            bitmap = null;

            return clTexture;
        }

        //// BVH to World
        //public Int32 Add(List<BVHNode> newBVH)
        //{
        //    m_mtxMutex.WaitOne();
        //    Int32 iId = m_listObjects.Count;
        //
        //    BVHObject newObject = new BVHObject();
        //    newObject.iOffset = m_listBVHNodes.Count;
        //    newObject.iCount = newBVH.Count;
        //    m_listObjects.Add(newObject);
        //    RefitTree(iId, true);
        //
        //    m_listBVHNodes.AddRange(newBVH);
        //    m_mtxMutex.ReleaseMutex();
        //    return iId;
        //}
        static float GetDistance_Triangle_Triangle(Triangle tri1, Triangle tri2)
        {
            float fMinDistance = float.MaxValue;

            fMinDistance = Math.Min(fMinDistance, vec3.Distance(tri1.m_A.m_V, tri2.m_A.m_V));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(tri1.m_A.m_V, tri2.m_B.m_V));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(tri1.m_A.m_V, tri2.m_C.m_V));

            fMinDistance = Math.Min(fMinDistance, vec3.Distance(tri1.m_B.m_V, tri2.m_A.m_V));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(tri1.m_B.m_V, tri2.m_B.m_V));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(tri1.m_B.m_V, tri2.m_C.m_V));

            fMinDistance = Math.Min(fMinDistance, vec3.Distance(tri1.m_C.m_V, tri2.m_A.m_V));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(tri1.m_C.m_V, tri2.m_B.m_V));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(tri1.m_C.m_V, tri2.m_C.m_V));

            return fMinDistance;
        }

        static float GetDistance_BBox_BBox(BBox bbox1, BBox bbox2)
        {
            vec3 center1 = (bbox1.min + bbox1.max) / 2.0f;
            vec3 center2 = (bbox2.min + bbox2.max) / 2.0f;

            vec3 halfSize1 = bbox1.max - center1;
            vec3 halfSize2 = bbox2.max - center2;

            float x = Math.Abs(center2.x - center1.x) - halfSize1.x - halfSize2.x;
            float y = Math.Abs(center2.y - center1.y) - halfSize1.y - halfSize2.y;
            float z = Math.Abs(center2.z - center1.z) - halfSize1.z - halfSize2.z;

            float fLength = (float)Math.Sqrt(x * x + y * y + z * z);

            // ha osszeernek, akor negativ tavolsag
            if (x < 0.0f && y < 0.0f && z < 0.0f) { fLength = -fLength; }

            return fLength;
        }

        static BBox GenBBox(Triangle tri1, Triangle tri2)
        {
            float fMinX = float.MaxValue;
            float fMinY = float.MaxValue;
            float fMinZ = float.MaxValue;

            fMinX = Math.Min(fMinX, tri1.m_A.m_V.x);
            fMinX = Math.Min(fMinX, tri1.m_B.m_V.x);
            fMinX = Math.Min(fMinX, tri1.m_C.m_V.x);
            fMinX = Math.Min(fMinX, tri2.m_A.m_V.x);
            fMinX = Math.Min(fMinX, tri2.m_B.m_V.x);
            fMinX = Math.Min(fMinX, tri2.m_C.m_V.x);

            fMinY = Math.Min(fMinY, tri1.m_A.m_V.y);
            fMinY = Math.Min(fMinY, tri1.m_B.m_V.y);
            fMinY = Math.Min(fMinY, tri1.m_C.m_V.y);
            fMinY = Math.Min(fMinY, tri2.m_A.m_V.y);
            fMinY = Math.Min(fMinY, tri2.m_B.m_V.y);
            fMinY = Math.Min(fMinY, tri2.m_C.m_V.y);

            fMinZ = Math.Min(fMinZ, tri1.m_A.m_V.z);
            fMinZ = Math.Min(fMinZ, tri1.m_B.m_V.z);
            fMinZ = Math.Min(fMinZ, tri1.m_C.m_V.z);
            fMinZ = Math.Min(fMinZ, tri2.m_A.m_V.z);
            fMinZ = Math.Min(fMinZ, tri2.m_B.m_V.z);
            fMinZ = Math.Min(fMinZ, tri2.m_C.m_V.z);

            float fMaxX = float.MinValue;
            float fMaxY = float.MinValue;
            float fMaxZ = float.MinValue;

            fMaxX = Math.Max(fMaxX, tri1.m_A.m_V.x);
            fMaxX = Math.Max(fMaxX, tri1.m_B.m_V.x);
            fMaxX = Math.Max(fMaxX, tri1.m_C.m_V.x);
            fMaxX = Math.Max(fMaxX, tri2.m_A.m_V.x);
            fMaxX = Math.Max(fMaxX, tri2.m_B.m_V.x);
            fMaxX = Math.Max(fMaxX, tri2.m_C.m_V.x);

            fMaxY = Math.Max(fMaxY, tri1.m_A.m_V.y);
            fMaxY = Math.Max(fMaxY, tri1.m_B.m_V.y);
            fMaxY = Math.Max(fMaxY, tri1.m_C.m_V.y);
            fMaxY = Math.Max(fMaxY, tri2.m_A.m_V.y);
            fMaxY = Math.Max(fMaxY, tri2.m_B.m_V.y);
            fMaxY = Math.Max(fMaxY, tri2.m_C.m_V.y);

            fMaxZ = Math.Max(fMaxZ, tri1.m_A.m_V.z);
            fMaxZ = Math.Max(fMaxZ, tri1.m_B.m_V.z);
            fMaxZ = Math.Max(fMaxZ, tri1.m_C.m_V.z);
            fMaxZ = Math.Max(fMaxZ, tri2.m_A.m_V.z);
            fMaxZ = Math.Max(fMaxZ, tri2.m_B.m_V.z);
            fMaxZ = Math.Max(fMaxZ, tri2.m_C.m_V.z);

            BBox bbox = new BBox();
            bbox.min = new vec3(fMinX, fMinY, fMinZ);
            bbox.max = new vec3(fMaxX, fMaxY, fMaxZ);

            return bbox;
        }

        static BBox GenBBox(BBox bbox1, BBox bbox2)
        {
            float fMinX = float.MaxValue;
            float fMinY = float.MaxValue;
            float fMinZ = float.MaxValue;

            fMinX = Math.Min(fMinX, bbox1.min.x);
            fMinX = Math.Min(fMinX, bbox1.max.x);
            fMinX = Math.Min(fMinX, bbox2.min.x);
            fMinX = Math.Min(fMinX, bbox2.max.x);

            fMinY = Math.Min(fMinY, bbox1.min.y);
            fMinY = Math.Min(fMinY, bbox1.max.y);
            fMinY = Math.Min(fMinY, bbox2.min.y);
            fMinY = Math.Min(fMinY, bbox2.max.y);

            fMinZ = Math.Min(fMinZ, bbox1.min.z);
            fMinZ = Math.Min(fMinZ, bbox1.max.z);
            fMinZ = Math.Min(fMinZ, bbox2.min.z);
            fMinZ = Math.Min(fMinZ, bbox2.max.z);

            float fMaxX = float.MinValue;
            float fMaxY = float.MinValue;
            float fMaxZ = float.MinValue;

            fMaxX = Math.Max(fMaxX, bbox1.min.x);
            fMaxX = Math.Max(fMaxX, bbox1.max.x);
            fMaxX = Math.Max(fMaxX, bbox2.min.x);
            fMaxX = Math.Max(fMaxX, bbox2.max.x);

            fMaxY = Math.Max(fMaxY, bbox1.min.y);
            fMaxY = Math.Max(fMaxY, bbox1.max.y);
            fMaxY = Math.Max(fMaxY, bbox2.min.y);
            fMaxY = Math.Max(fMaxY, bbox2.max.y);

            fMaxZ = Math.Max(fMaxZ, bbox1.min.z);
            fMaxZ = Math.Max(fMaxZ, bbox1.max.z);
            fMaxZ = Math.Max(fMaxZ, bbox2.min.z);
            fMaxZ = Math.Max(fMaxZ, bbox2.max.z);

            BBox bbox = new BBox();
            bbox.min = new vec3(fMinX, fMinY, fMinZ);
            bbox.max = new vec3(fMaxX, fMaxY, fMaxZ);

            return bbox;
        }

        public Int32 CreateStaticObject(List<Triangle> triangles, mat4 matTransform)
        {
            List<Triangle> newTriangles = new List<Triangle>();

            foreach (Triangle oldTri in triangles)
            {
                Triangle newTri = new Triangle();

                Vertex vertexA = new Vertex();
                vertexA.m_V = new vec3(matTransform * new vec4(oldTri.m_A.m_V, 1.0f));
                vertexA.m_N = new vec3(matTransform * new vec4(oldTri.m_A.m_N, 0.0f));
                vertexA.m_TC = new vec2(oldTri.m_A.m_TC);
                vertexA.m_iNumMatrices = 0;
                vertexA.m_iMatrixId1 = -1;
                vertexA.m_fWeight1 = 0.0f;
                vertexA.m_iMatrixId2 = -1;
                vertexA.m_fWeight2 = 0.0f;
                vertexA.m_iMatrixId3 = -1;
                vertexA.m_fWeight3 = 0.0f;

                Vertex vertexB = new Vertex();
                vertexB.m_V = new vec3(matTransform * new vec4(oldTri.m_B.m_V, 1.0f));
                vertexB.m_N = new vec3(matTransform * new vec4(oldTri.m_B.m_N, 0.0f));
                vertexB.m_TC = new vec2(oldTri.m_B.m_TC);
                vertexB.m_iNumMatrices = 0;
                vertexB.m_iMatrixId1 = -1;
                vertexB.m_fWeight1 = 0.0f;
                vertexB.m_iMatrixId2 = -1;
                vertexB.m_fWeight2 = 0.0f;
                vertexB.m_iMatrixId3 = -1;
                vertexB.m_fWeight3 = 0.0f;

                Vertex vertexC = new Vertex();
                vertexC.m_V = new vec3(matTransform * new vec4(oldTri.m_C.m_V, 1.0f));
                vertexC.m_N = new vec3(matTransform * new vec4(oldTri.m_C.m_N, 0.0f));
                vertexC.m_TC = new vec2(oldTri.m_C.m_TC);
                vertexC.m_iNumMatrices = 0;
                vertexC.m_iMatrixId1 = -1;
                vertexC.m_fWeight1 = 0.0f;
                vertexC.m_iMatrixId2 = -1;
                vertexC.m_fWeight2 = 0.0f;
                vertexC.m_iMatrixId3 = -1;
                vertexC.m_fWeight3 = 0.0f;

                newTri.m_A = vertexA;
                newTri.m_B = vertexB;
                newTri.m_C = vertexC;
                newTri.m_iMaterialId = oldTri.m_iMaterialId;

                newTriangles.Add(newTri);
            }

            List<BVHNode> newBVH = CreateBVH(newTriangles);
            newTriangles.Clear();

            m_mtxMutex.WaitOne();
            Int32 iId = m_listObjects.Count;
            
            BVHObject newObject = new BVHObject();
            newObject.iOffset = m_listBVHNodes.Count;
            newObject.iCount = newBVH.Count;
            newObject.iType = (byte)BVHObjectType.Static;
            m_listObjects.Add(newObject);
            RefitTree(iId, false);
            
            m_listBVHNodes.AddRange(newBVH);
            m_mtxMutex.ReleaseMutex();

            return iId;
        }

        public Int32 CreateDynamicObject(List<Triangle> triangles, Int32 iMatrixId)
        {
            List<Triangle> newTriangles = new List<Triangle>();

            foreach (Triangle oldTri in triangles)
            {
                Triangle newTri = new Triangle();

                Vertex vertexA = new Vertex();
                vertexA.m_V = new vec3(oldTri.m_A.m_V);
                vertexA.m_N = new vec3(oldTri.m_A.m_N);
                vertexA.m_TC = new vec2(oldTri.m_A.m_TC);
                vertexA.m_iNumMatrices = 1;
                vertexA.m_iMatrixId1 = iMatrixId;
                vertexA.m_fWeight1 = 1.0f;
                vertexA.m_iMatrixId2 = -1;
                vertexA.m_fWeight2 = 0.0f;
                vertexA.m_iMatrixId3 = -1;
                vertexA.m_fWeight3 = 0.0f;

                Vertex vertexB = new Vertex();
                vertexB.m_V = new vec3(oldTri.m_B.m_V);
                vertexB.m_N = new vec3(oldTri.m_B.m_N);
                vertexB.m_TC = new vec2(oldTri.m_B.m_TC);
                vertexB.m_iNumMatrices = 1;
                vertexB.m_iMatrixId1 = iMatrixId;
                vertexB.m_fWeight1 = 1.0f;
                vertexB.m_iMatrixId2 = -1;
                vertexB.m_fWeight2 = 0.0f;
                vertexB.m_iMatrixId3 = -1;
                vertexB.m_fWeight3 = 0.0f;

                Vertex vertexC = new Vertex();
                vertexC.m_V = new vec3(oldTri.m_C.m_V);
                vertexC.m_N = new vec3(oldTri.m_C.m_N);
                vertexC.m_TC = new vec2(oldTri.m_C.m_TC);
                vertexC.m_iNumMatrices = 1;
                vertexC.m_iMatrixId1 = iMatrixId;
                vertexC.m_fWeight1 = 1.0f;
                vertexC.m_iMatrixId2 = -1;
                vertexC.m_fWeight2 = 0.0f;
                vertexC.m_iMatrixId3 = -1;
                vertexC.m_fWeight3 = 0.0f;

                newTri.m_A = vertexA;
                newTri.m_B = vertexB;
                newTri.m_C = vertexC;
                newTri.m_iMaterialId = oldTri.m_iMaterialId;

                newTriangles.Add(newTri);
            }

            List<BVHNode> newBVH = CreateBVH(newTriangles);
            newTriangles.Clear();

            m_mtxMutex.WaitOne();
            Int32 iId = m_listObjects.Count;

            BVHObject newObject = new BVHObject();
            newObject.iOffset = m_listBVHNodes.Count;
            newObject.iCount = newBVH.Count;
            newObject.iType = (byte)BVHObjectType.Dynamic;
            m_listObjects.Add(newObject);
            RefitTree(iId, false);

            m_listBVHNodes.AddRange(newBVH);
            m_mtxMutex.ReleaseMutex();

            return iId;
        }

        public Int32 CreateAnimatedObject(List<Triangle> triangles)
        {
            List<BVHNode> newBVH = CreateBVH(triangles);

            m_mtxMutex.WaitOne();
            Int32 iId = m_listObjects.Count;

            BVHObject newObject = new BVHObject();
            newObject.iOffset = m_listBVHNodes.Count;
            newObject.iCount = newBVH.Count;
            newObject.iType = (byte)BVHObjectType.Animated;
            m_listObjects.Add(newObject);
            RefitTree(iId, false);

            m_listBVHNodes.AddRange(newBVH);
            m_mtxMutex.ReleaseMutex();

            return iId;
        }

        public static List<BVHNode> CreateBVH(List<Triangle> triangles0)
        {
            List<BVHNode> tree = new List<BVHNode>();

            // gyoker a 0. indexen. ez majd a legvegen lesz beallitva
            BVHNode root = new BVHNode();
            tree.Add(root);

            List<Triangle> triangles = new List<Triangle>();
            triangles.AddRange(triangles0);

            List<BVHNode> outBuffer = new List<BVHNode>();

            // elso szint: haromszogek tavolsaga
            while (triangles.Count > 1)
            {
                Triangle tri1 = triangles[0];

                float fMinDistance = float.MaxValue;
                int id = 0;

                for (int i = 1; i < triangles.Count; i++)
                {
                    float fCurrentDistance = GetDistance_Triangle_Triangle(tri1, triangles[i]);

                    if (fCurrentDistance < fMinDistance)
                    {
                        fMinDistance = fCurrentDistance;
                        id = i;
                    }
                }

                Triangle tri2 = triangles[id];
                triangles.RemoveAt(id);
                triangles.RemoveAt(0);

                BVHNode leaf1 = new BVHNode();
                leaf1.m_Triangle = tri1;
                leaf1.m_iLeft = -1;
                leaf1.m_iRight = -1;
                BVHNode leaf2 = new BVHNode();
                leaf2.m_Triangle = tri2;
                leaf2.m_iLeft = -1;
                leaf2.m_iRight = -1;

                BVHNode parent = new BVHNode();
                parent.m_iLeft = tree.Count;
                tree.Add(leaf1);
                parent.m_iRight = tree.Count;
                tree.Add(leaf2);

                parent.m_BBox = GenBBox(tri1, tri2);
                outBuffer.Add(parent);
            }

            if (triangles.Count == 1)
            {
                Triangle tri1 = triangles[0];

                triangles.RemoveAt(0);

                BVHNode leaf1 = new BVHNode();
                leaf1.m_Triangle = tri1;
                leaf1.m_iLeft = -1;
                leaf1.m_iRight = -1;

                BVHNode parent = new BVHNode();
                parent.m_iLeft = tree.Count;
                tree.Add(leaf1);
                parent.m_iRight = -1;

                parent.m_BBox = GenBBox(tri1, tri1);
                outBuffer.Add(parent);
            }

            // tovabbi bboxok epitese, mig 1-et nam kapunk
            while (outBuffer.Count > 1)
            {
                List<BVHNode> inBuffer = new List<BVHNode>();
                inBuffer.AddRange(outBuffer);

                outBuffer.Clear();

                while (inBuffer.Count > 1)
                {
                    BVHNode node1 = inBuffer[0];

                    float fMinDistance = float.MaxValue;
                    int id = 0;

                    for (int i = 1; i < inBuffer.Count; i++)
                    {
                        float fCurrentDistance = GetDistance_BBox_BBox(node1.m_BBox, inBuffer[i].m_BBox);

                        if (fCurrentDistance < fMinDistance)
                        {
                            fMinDistance = fCurrentDistance;
                            id = i;
                        }
                    }

                    BVHNode node2 = inBuffer[id];
                    inBuffer.RemoveAt(id);
                    inBuffer.RemoveAt(0);

                    BVHNode parent = new BVHNode();
                    parent.m_iLeft = tree.Count;
                    tree.Add(node1);
                    parent.m_iRight = tree.Count;
                    tree.Add(node2);

                    outBuffer.Add(parent);
                }

                if (inBuffer.Count == 1)
                {
                    BVHNode node1 = inBuffer[0];

                    inBuffer.RemoveAt(0);

                    BVHNode parent = new BVHNode();
                    parent.m_iLeft = tree.Count;
                    tree.Add(node1);
                    parent.m_iRight = -1;

                    outBuffer.Add(parent);
                }
            }

            // root frissitese
            tree[0] = outBuffer[0];
            outBuffer.RemoveAt(0);

            return tree;
        }

        public void RefitTree(Int32 iId, bool bIsTrue)
        {
            m_mtxMutex.WaitOne();
            BVHObject bvhObject = m_listObjects[iId];
            bvhObject.iIsRefitTree = bIsTrue ? 1 : 0;
            m_listObjects[iId] = bvhObject;
            m_mtxMutex.ReleaseMutex();
        }

        public void Commit()
        {
            m_mtxMutex.WaitOne();
            ;
            m_mtxMutex.ReleaseMutex();
        }

        enum BVHObjectType
        {
            Static = 1,
            Dynamic = 2,
            Animated = 3
        }

        Mutex m_mtxMutex = new Mutex();
        List<Material> m_listMaterials = new List<Material>();
        List<Matrix> m_listMatrices = new List<Matrix>();
        List<BVHObject> m_listObjects = new List<BVHObject>();
        List<BVHNode> m_listBVHNodes = new List<BVHNode>();

        Context m_Context;
    }
}
