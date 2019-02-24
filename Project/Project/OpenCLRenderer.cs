using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

using OpenTK;
using Cloo;
using System.Windows;
using System.Windows.Media;

namespace OpenCLRenderer
{
    struct Float3
    {
        public float m_X;
        public float m_Y;
        public float m_Z;
    }

    struct Vertex
    {
        public float m_Vx;
        public float m_Vy;
        public float m_Vz;
        public float m_Nx;
        public float m_Ny;
        public float m_Nz;
        public float m_TCx;
        public float m_TCy;
        public int m_iNumMatrices;
        public int m_iMatrixId1;
        public int m_iMatrixId2;
        public int m_iMatrixId3;
        public float m_fWeight1;
        public float m_fWeight2;
        public float m_fWeight3;
    }

    struct Triangle
    {
        public Vertex m_A;
        public Vertex m_B;
        public Vertex m_C;
        public int m_iMaterialId;
    }

    struct BBox
    {
        public float minx;
        public float miny;
        public float minz;
        public float maxx;
        public float maxy;
        public float maxz;
    }

    struct BVHNode
    {
        public int m_iId;
        public Triangle m_Triangle;
        public BBox m_BBox;
        public int m_iLeft;
        public int m_iRight;
    }

    struct Matrix
    {
        public float m11;
        public float m12;
        public float m13;
        public float m14;

        public float m21;
        public float m22;
        public float m23;
        public float m24;

        public float m31;
        public float m32;
        public float m33;
        public float m34;

        public float m41;
        public float m42;
        public float m43;
        public float m44;
    }

    struct Texture
    {
        public ulong m_iOffsetTextreDatas;
        public int m_iWidth;
        public int m_iHeight;
    }

    
    struct Material
    {
        public Texture m_DiffuseTexture;
        public Texture m_SpecularTexture;
        public Texture m_NormalTexture;
    }

    struct BVHObject
    {
        public int m_iType;
        public List<BVHNode> m_listBVHNodes;
        public List< List<BVHNode> > m_LevelXBVHs;
    }

    struct Ray
    {
        public float posx;
        public float posy;
        public float posz;
        public float dirx;
        public float diry;
        public float dirz;
        public float length;
    }

    public static class Win32
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        public static extern void CopyMemory(IntPtr dest, IntPtr source, int Length);
    }

    class Scene : IDisposable
    {
        
        public Scene() { }

        public void CreateDevice()
        {
            ComputePlatform[] platforms = ComputePlatform.Platforms.ToArray();

            foreach (ComputePlatform platform in platforms)
            {
                ComputeContextPropertyList properties = new ComputeContextPropertyList(platform);

                ComputeContext newContext = new ComputeContext(ComputeDeviceTypes.Gpu, properties, null, IntPtr.Zero);

                ComputeDevice[] devices = newContext.Devices.ToArray();

                foreach (ComputeDevice device in devices)
                {
                    m_Context = newContext;
                    m_Device = device;

                    cmdQueue = new ComputeCommandQueue(m_Context, m_Device, ComputeCommandQueueFlags.None);
                    m_Program = new ComputeProgram(m_Context, OpenCLScript.GetText());

                    try
                    {
                        m_Program.Build(null, null, null, IntPtr.Zero);
                    }
                    catch (Exception e)
                    {
                        e.ToString();

                        string strText = m_Program.GetBuildLog(m_Device);
                        MessageBox.Show(strText, "Exception");
                        Application.Current.Shutdown();
                    }

                    // VertexShader
                    kernelVertexShader = m_Program.CreateKernel("Main_VertexShader");

                    // RefitTree
                    KernelRefitTree_LevelX = m_Program.CreateKernel("Main_RefitTree_LevelX");

                    // CameraRays
                    KernelCameraRays = m_Program.CreateKernel("Main_CameraRays");

                    // ClearScreen
                    KernelClearShader = m_Program.CreateKernel("Main_ClearShader");

                    // RayShader
                    KernelRayShader = m_Program.CreateKernel("Main_RayShader");
                    
                    // Resize
                    Resize(8, 8);

                    // ok
                    return;
                }

            }

            MessageBox.Show("Scene: Not find OpenCL GPU device!", "Exception");
            Application.Current.Shutdown();
        }

        // Matrix
        public int GenMatrix()
        {
            m_mtxMutex.WaitOne();
            int iId = m_listMatrices.Count;

            Matrix newMatrix = new Matrix();

            m_listMatrices.Add(newMatrix);
            m_mtxMutex.ReleaseMutex();
            return iId;
        }

        public void SetMatrix(int iId, Matrix4 mMatrix)
        {
            m_mtxMutex.WaitOne();
            Matrix newMatrix = new Matrix();

            newMatrix.m11 = mMatrix.M11;
            newMatrix.m12 = mMatrix.M12;
            newMatrix.m13 = mMatrix.M13;
            newMatrix.m14 = mMatrix.M14;

            newMatrix.m21 = mMatrix.M21;
            newMatrix.m22 = mMatrix.M22;
            newMatrix.m23 = mMatrix.M23;
            newMatrix.m24 = mMatrix.M24;

            newMatrix.m31 = mMatrix.M31;
            newMatrix.m32 = mMatrix.M32;
            newMatrix.m33 = mMatrix.M33;
            newMatrix.m34 = mMatrix.M34;

            newMatrix.m41 = mMatrix.M41;
            newMatrix.m42 = mMatrix.M42;
            newMatrix.m43 = mMatrix.M43;
            newMatrix.m44 = mMatrix.M44;

            m_listMatrices[iId] = newMatrix;
            m_mtxMutex.ReleaseMutex();
        }

        // Material
        public int GenMaterial()
        {
            m_mtxMutex.WaitOne();
            int iId = m_listMaterials.Count;

            Material newMaterial = new Material();

            m_listMaterials.Add(newMaterial);
            m_mtxMutex.ReleaseMutex();
            return iId;
        }

        public void SetMaterial(int iId, string @strDiffuseFileName, string @strSpecularFileName, string @strNormalFileName)
        {
            m_mtxMutex.WaitOne();
            Material newMaterial = new Material();

            newMaterial.m_DiffuseTexture  = CreateTextureFromFile(@strDiffuseFileName);
            newMaterial.m_NormalTexture = CreateTextureFromFile(@strSpecularFileName);
            newMaterial.m_SpecularTexture = CreateTextureFromFile(@strNormalFileName);

            m_listMaterials[iId] = newMaterial;
            m_mtxMutex.ReleaseMutex();
        }

        Texture CreateTextureFromFile(string @strFileName)
        {
            Bitmap bitmap = new Bitmap(@strFileName);
            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            Rectangle rectangle = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = bitmap.LockBits(rectangle, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Texture newTexture = new Texture();
            newTexture.m_iWidth = bitmap.Width;
            newTexture.m_iHeight = bitmap.Height;
            newTexture.m_iOffsetTextreDatas = (uint)m_listTexturesData.Count;
            // copy data
            int iSize = newTexture.m_iWidth * newTexture.m_iHeight * 4;
            byte[] datas = new byte[iSize];
            Marshal.Copy(bitmapData.Scan0, datas, 0, iSize);
            m_listTexturesData.AddRange(datas);

            datas = null;

            bitmap.UnlockBits(bitmapData);

            bitmap.Dispose();
            bitmap = null;

            return newTexture;
        }

        static float GetDistance_Triangle_Triangle(Triangle tri1, Triangle tri2)
        {
            float fMinDistance = float.MaxValue;

            fMinDistance = Math.Min(fMinDistance, Vector3.Distance(new Vector3(tri1.m_A.m_Vx, tri1.m_A.m_Vy, tri1.m_A.m_Vz), new Vector3(tri2.m_A.m_Vx, tri2.m_A.m_Vy, tri2.m_A.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, Vector3.Distance(new Vector3(tri1.m_A.m_Vx, tri1.m_A.m_Vy, tri1.m_A.m_Vz), new Vector3(tri2.m_B.m_Vx, tri2.m_B.m_Vy, tri2.m_B.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, Vector3.Distance(new Vector3(tri1.m_A.m_Vx, tri1.m_A.m_Vy, tri1.m_A.m_Vz), new Vector3(tri2.m_C.m_Vx, tri2.m_C.m_Vy, tri2.m_C.m_Vz)));

            fMinDistance = Math.Min(fMinDistance, Vector3.Distance(new Vector3(tri1.m_B.m_Vx, tri1.m_B.m_Vy, tri1.m_B.m_Vz), new Vector3(tri2.m_A.m_Vx, tri2.m_A.m_Vy, tri2.m_A.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, Vector3.Distance(new Vector3(tri1.m_B.m_Vx, tri1.m_B.m_Vy, tri1.m_B.m_Vz), new Vector3(tri2.m_B.m_Vx, tri2.m_B.m_Vy, tri2.m_B.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, Vector3.Distance(new Vector3(tri1.m_B.m_Vx, tri1.m_B.m_Vy, tri1.m_B.m_Vz), new Vector3(tri2.m_C.m_Vx, tri2.m_C.m_Vy, tri2.m_C.m_Vz)));

            fMinDistance = Math.Min(fMinDistance, Vector3.Distance(new Vector3(tri1.m_C.m_Vx, tri1.m_C.m_Vy, tri1.m_C.m_Vz), new Vector3(tri2.m_A.m_Vx, tri2.m_A.m_Vy, tri2.m_A.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, Vector3.Distance(new Vector3(tri1.m_C.m_Vx, tri1.m_C.m_Vy, tri1.m_C.m_Vz), new Vector3(tri2.m_B.m_Vx, tri2.m_B.m_Vy, tri2.m_B.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, Vector3.Distance(new Vector3(tri1.m_C.m_Vx, tri1.m_C.m_Vy, tri1.m_C.m_Vz), new Vector3(tri2.m_C.m_Vx, tri2.m_C.m_Vy, tri2.m_C.m_Vz)));

            return fMinDistance;
        }

        static float GetDistance_BBox_BBox(BBox bbox1, BBox bbox2)
        {
            Vector3 center1 = (new Vector3(bbox1.minx, bbox1.miny, bbox1.minz) + new Vector3(bbox1.maxx, bbox1.maxy, bbox1.maxz)) / 2.0f;
            Vector3 center2 = (new Vector3(bbox2.minx, bbox2.miny, bbox2.minz) + new Vector3(bbox2.maxx, bbox2.maxy, bbox2.maxz)) / 2.0f;

            Vector3 halfSize1 = new Vector3(bbox1.maxx, bbox1.maxy, bbox1.maxz) - center1;
            Vector3 halfSize2 = new Vector3(bbox2.maxx, bbox2.maxy, bbox2.maxz) - center2;

            float x = Math.Abs(center2.X - center1.X) - halfSize1.X - halfSize2.X;
            float y = Math.Abs(center2.Y - center1.Y) - halfSize1.Y - halfSize2.Y;
            float z = Math.Abs(center2.Z - center1.Z) - halfSize1.Z - halfSize2.Z;

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

            fMinX = Math.Min(fMinX, tri1.m_A.m_Vx);
            fMinX = Math.Min(fMinX, tri1.m_B.m_Vx);
            fMinX = Math.Min(fMinX, tri1.m_C.m_Vx);
            fMinX = Math.Min(fMinX, tri2.m_A.m_Vx);
            fMinX = Math.Min(fMinX, tri2.m_B.m_Vx);
            fMinX = Math.Min(fMinX, tri2.m_C.m_Vx);

            fMinY = Math.Min(fMinY, tri1.m_A.m_Vy);
            fMinY = Math.Min(fMinY, tri1.m_B.m_Vy);
            fMinY = Math.Min(fMinY, tri1.m_C.m_Vy);
            fMinY = Math.Min(fMinY, tri2.m_A.m_Vy);
            fMinY = Math.Min(fMinY, tri2.m_B.m_Vy);
            fMinY = Math.Min(fMinY, tri2.m_C.m_Vy);

            fMinZ = Math.Min(fMinZ, tri1.m_A.m_Vz);
            fMinZ = Math.Min(fMinZ, tri1.m_B.m_Vz);
            fMinZ = Math.Min(fMinZ, tri1.m_C.m_Vz);
            fMinZ = Math.Min(fMinZ, tri2.m_A.m_Vz);
            fMinZ = Math.Min(fMinZ, tri2.m_B.m_Vz);
            fMinZ = Math.Min(fMinZ, tri2.m_C.m_Vz);

            float fMaxX = float.MinValue;
            float fMaxY = float.MinValue;
            float fMaxZ = float.MinValue;

            fMaxX = Math.Max(fMaxX, tri1.m_A.m_Vx);
            fMaxX = Math.Max(fMaxX, tri1.m_B.m_Vx);
            fMaxX = Math.Max(fMaxX, tri1.m_C.m_Vx);
            fMaxX = Math.Max(fMaxX, tri2.m_A.m_Vx);
            fMaxX = Math.Max(fMaxX, tri2.m_B.m_Vx);
            fMaxX = Math.Max(fMaxX, tri2.m_C.m_Vx);

            fMaxY = Math.Max(fMaxY, tri1.m_A.m_Vy);
            fMaxY = Math.Max(fMaxY, tri1.m_B.m_Vy);
            fMaxY = Math.Max(fMaxY, tri1.m_C.m_Vy);
            fMaxY = Math.Max(fMaxY, tri2.m_A.m_Vy);
            fMaxY = Math.Max(fMaxY, tri2.m_B.m_Vy);
            fMaxY = Math.Max(fMaxY, tri2.m_C.m_Vy);

            fMaxZ = Math.Max(fMaxZ, tri1.m_A.m_Vz);
            fMaxZ = Math.Max(fMaxZ, tri1.m_B.m_Vz);
            fMaxZ = Math.Max(fMaxZ, tri1.m_C.m_Vz);
            fMaxZ = Math.Max(fMaxZ, tri2.m_A.m_Vz);
            fMaxZ = Math.Max(fMaxZ, tri2.m_B.m_Vz);
            fMaxZ = Math.Max(fMaxZ, tri2.m_C.m_Vz);

            BBox bbox = new BBox();
            bbox.minx = fMinX;
            bbox.miny = fMinY;
            bbox.minz = fMinZ;
            bbox.maxx = fMaxX;
            bbox.maxy = fMaxY;
            bbox.maxz = fMaxZ;

            return bbox;
        }

        static BBox GenBBox(BBox bbox1, BBox bbox2)
        {
            float fMinX = float.MaxValue;
            float fMinY = float.MaxValue;
            float fMinZ = float.MaxValue;

            fMinX = Math.Min(fMinX, bbox1.minx);
            fMinX = Math.Min(fMinX, bbox1.maxx);
            fMinX = Math.Min(fMinX, bbox2.minx);
            fMinX = Math.Min(fMinX, bbox2.maxx);

            fMinY = Math.Min(fMinY, bbox1.miny);
            fMinY = Math.Min(fMinY, bbox1.maxy);
            fMinY = Math.Min(fMinY, bbox2.miny);
            fMinY = Math.Min(fMinY, bbox2.maxy);

            fMinZ = Math.Min(fMinZ, bbox1.minz);
            fMinZ = Math.Min(fMinZ, bbox1.maxz);
            fMinZ = Math.Min(fMinZ, bbox2.minz);
            fMinZ = Math.Min(fMinZ, bbox2.maxz);

            float fMaxX = float.MinValue;
            float fMaxY = float.MinValue;
            float fMaxZ = float.MinValue;

            fMaxX = Math.Max(fMaxX, bbox1.minx);
            fMaxX = Math.Max(fMaxX, bbox1.maxx);
            fMaxX = Math.Max(fMaxX, bbox2.minx);
            fMaxX = Math.Max(fMaxX, bbox2.maxx);

            fMaxY = Math.Max(fMaxY, bbox1.miny);
            fMaxY = Math.Max(fMaxY, bbox1.maxy);
            fMaxY = Math.Max(fMaxY, bbox2.miny);
            fMaxY = Math.Max(fMaxY, bbox2.maxy);

            fMaxZ = Math.Max(fMaxZ, bbox1.minz);
            fMaxZ = Math.Max(fMaxZ, bbox1.maxz);
            fMaxZ = Math.Max(fMaxZ, bbox2.minz);
            fMaxZ = Math.Max(fMaxZ, bbox2.maxz);

            BBox bbox = new BBox();
            bbox.minx = fMinX;
            bbox.miny = fMinY;
            bbox.minz = fMinZ;
            bbox.maxx = fMaxX;
            bbox.maxy = fMaxY;
            bbox.maxz = fMaxZ;

            return bbox;
        }

        public int GenObject()
        {
            m_mtxMutex.WaitOne();

            int iId = m_listObjects.Count;
            BVHObject newObject = new BVHObject();
            m_listObjects.Add(newObject);

            m_mtxMutex.ReleaseMutex();

            return iId;
        }

        public void SetObject(int iId, BVHObject bvhObject)
        {
            m_mtxMutex.WaitOne();
            m_listObjects[iId] = bvhObject;
            m_mtxMutex.ReleaseMutex();
        }

        public BVHObject CreateStaticObject(List<Triangle> triangles, Matrix4 matTransform)
        {
            List<Triangle> newTriangles = new List<Triangle>();

            foreach (Triangle oldTri in triangles)
            {
                Triangle newTri = new Triangle();

                Vertex vertexA = new Vertex();
                Vector3 AV = new Vector3(matTransform * new Vector4(oldTri.m_A.m_Vx, oldTri.m_A.m_Vy, oldTri.m_A.m_Vz, 1.0f));
                vertexA.m_Vx = AV.X;
                vertexA.m_Vy = AV.Y;
                vertexA.m_Vz = AV.Z;
                Vector3 AN = new Vector3(matTransform * new Vector4(oldTri.m_A.m_Nx, oldTri.m_A.m_Ny, oldTri.m_A.m_Nz, 0.0f));
                vertexA.m_Nx = AN.X;
                vertexA.m_Ny = AN.Y;
                vertexA.m_Nz = AN.Z;
                vertexA.m_TCx = oldTri.m_A.m_TCx;
                vertexA.m_TCy = oldTri.m_A.m_TCy;
                vertexA.m_iNumMatrices = 0;
                vertexA.m_iMatrixId1 = -1;
                vertexA.m_fWeight1 = 0.0f;
                vertexA.m_iMatrixId2 = -1;
                vertexA.m_fWeight2 = 0.0f;
                vertexA.m_iMatrixId3 = -1;
                vertexA.m_fWeight3 = 0.0f;

                Vertex vertexB = new Vertex();
                Vector3 BV = new Vector3(matTransform * new Vector4(oldTri.m_B.m_Vx, oldTri.m_B.m_Vy, oldTri.m_B.m_Vz, 1.0f));
                vertexB.m_Vx = BV.X;
                vertexB.m_Vy = BV.Y;
                vertexB.m_Vz = BV.Z;
                Vector3 BN = new Vector3(matTransform * new Vector4(oldTri.m_B.m_Nx, oldTri.m_B.m_Ny, oldTri.m_B.m_Nz, 0.0f));
                vertexB.m_Nx = BN.X;
                vertexB.m_Ny = BN.Y;
                vertexB.m_Nz = BN.Z;
                vertexB.m_TCx = oldTri.m_B.m_TCx;
                vertexB.m_TCy = oldTri.m_B.m_TCy;
                vertexB.m_iNumMatrices = 0;
                vertexB.m_iMatrixId1 = -1;
                vertexB.m_fWeight1 = 0.0f;
                vertexB.m_iMatrixId2 = -1;
                vertexB.m_fWeight2 = 0.0f;
                vertexB.m_iMatrixId3 = -1;
                vertexB.m_fWeight3 = 0.0f;

                Vertex vertexC = new Vertex();
                Vector3 CV = new Vector3(matTransform * new Vector4(oldTri.m_C.m_Vx, oldTri.m_C.m_Vy, oldTri.m_C.m_Vz, 1.0f));
                vertexC.m_Vx = CV.X;
                vertexC.m_Vy = CV.Y;
                vertexC.m_Vz = CV.Z;
                Vector3 CN = new Vector3(matTransform * new Vector4(oldTri.m_C.m_Nx, oldTri.m_C.m_Ny, oldTri.m_C.m_Nz, 0.0f));
                vertexC.m_Nx = CN.X;
                vertexC.m_Ny = CN.Y;
                vertexC.m_Nz = CN.Z;
                vertexC.m_TCx = oldTri.m_C.m_TCx;
                vertexC.m_TCy = oldTri.m_C.m_TCy;
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
            
            BVHObject newObject = new BVHObject();
            newObject.m_iType = (int)BVHObjectType.Static;
            newObject.m_listBVHNodes = newBVH;
            newObject.m_LevelXBVHs = null;

            m_mtxMutex.ReleaseMutex();

            return newObject;
        }

        public BVHObject CreateDynamicObject(List<Triangle> triangles)
        {
            List<BVHNode> newBVH = CreateBVH(triangles);

            List< List<BVHNode> > levelXBVH = CopyBVHToLevelX(newBVH);

            m_mtxMutex.WaitOne();

            BVHObject newObject = new BVHObject();
            newObject.m_iType = (int)BVHObjectType.Dynamic;
            newObject.m_listBVHNodes = newBVH;
            newObject.m_LevelXBVHs = levelXBVH;

            m_mtxMutex.ReleaseMutex();

            return newObject;
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

                    if (fMinDistance < 0.00001f) { break; }
                }

                Triangle tri2 = triangles[id];
                triangles.RemoveAt(id);
                triangles.RemoveAt(0);

                BVHNode leaf1 = new BVHNode();
                leaf1.m_Triangle = tri1;
                leaf1.m_iLeft = -1;
                leaf1.m_iRight = -1;
                leaf1.m_BBox = GenBBox(tri1, tri1);
                BVHNode leaf2 = new BVHNode();
                leaf2.m_Triangle = tri2;
                leaf2.m_iLeft = -1;
                leaf2.m_iRight = -1;
                leaf2.m_BBox = GenBBox(tri2, tri2);

                leaf1.m_iId = tree.Count;
                tree.Add(leaf1);
                leaf2.m_iId = tree.Count;
                tree.Add(leaf2);

                BVHNode parent = new BVHNode();
                parent.m_iLeft = leaf1.m_iId;
                parent.m_iRight = leaf2.m_iId;

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
                leaf1.m_BBox = GenBBox(tri1, tri1);
                leaf1.m_iId = tree.Count;
                tree.Add(leaf1);

                BVHNode parent = new BVHNode();
                parent.m_iLeft = leaf1.m_iId;
                parent.m_iRight = -1;

                parent.m_BBox = GenBBox(tri1, tri1);
                outBuffer.Add(parent);
            }

            // tovabbi bboxok epitese, mig 1-et nem kapunk
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

                    node1.m_iId = tree.Count;
                    tree.Add(node1);
                    node2.m_iId = tree.Count;
                    tree.Add(node2);

                    BVHNode parent = new BVHNode();
                    parent.m_iLeft = node1.m_iId;
                    parent.m_iRight = node2.m_iId;

                    parent.m_BBox = GenBBox(node1.m_BBox, node2.m_BBox);

                    outBuffer.Add(parent);
                }

                if (inBuffer.Count == 1)
                {
                    BVHNode node1 = inBuffer[0];

                    inBuffer.RemoveAt(0);

                    node1.m_iId = tree.Count;
                    tree.Add(node1);

                    BVHNode parent = new BVHNode();
                    parent.m_iLeft = node1.m_iId;
                    parent.m_iRight = -1;

                    parent.m_BBox = GenBBox(node1.m_BBox, node1.m_BBox);

                    outBuffer.Add(parent);
                }
            }

            // root frissitese
            BVHNode goodRoot = outBuffer[0];
            outBuffer.RemoveAt(0);

            goodRoot.m_iId = 0;
            tree[0] = goodRoot;

            return tree;
        }

        public static void Preorder(List<List<BVHNode>> levelXBVH, List<BVHNode> list, BVHNode node, int iLevel)
        {
            // ha haromszog, akkor nem kell
            if (-1 == node.m_iLeft && -1 == node.m_iRight)
            {
                return;
            }

            // node eltarolasa
            levelXBVH[iLevel].Add(node);

            // egy szinttel lejjebb megyunk
            if (-1 != node.m_iLeft)
            {
                BVHNode leftNode = list[node.m_iLeft];
                Preorder(levelXBVH, list, leftNode, iLevel - 1);
            }

            if (-1 != node.m_iRight)
            {
                BVHNode rightNode = list[node.m_iRight];
                Preorder(levelXBVH, list, rightNode, iLevel - 1);
            }
        }

        static int GetMaxLevel(List<BVHNode> list, BVHNode node, int iCurrentLevel)
        {
            if (-1 == node.m_iLeft && -1 == node.m_iRight)
            {
                return iCurrentLevel;
            }

            int iLeftLevel = 0;
            if (-1 != node.m_iLeft)
            {
                BVHNode leftNode = list[node.m_iLeft];
                iLeftLevel = GetMaxLevel(list, leftNode, iCurrentLevel + 1);
            }

            int iRightLevel = 0;
            if (-1 != node.m_iRight)
            {
                BVHNode rightNode = list[node.m_iRight];
                iRightLevel = GetMaxLevel(list, rightNode, iCurrentLevel + 1);
            }

            if (iLeftLevel < iRightLevel) { return iRightLevel; }
            else { return iLeftLevel; }
        }

        static void LevelXList_Resize<T>(List<List<T>> levelX, int iMaxId)
        {
            while (levelX.Count < iMaxId)
            {
                levelX.Add(new List<T>());
            }
        }

        List<List<BVHNode>> CopyBVHToLevelX(List<BVHNode> newBVH)
        {
            List< List<BVHNode> > levelXBVH = new List< List<BVHNode> >();

            BVHNode root = newBVH[0];

            int iMaxLevel = GetMaxLevel(newBVH, root, 0);
            LevelXList_Resize(levelXBVH, iMaxLevel);
            
            Preorder(levelXBVH, newBVH, root, iMaxLevel - 1);

            return levelXBVH;
        }

        List<BVHNode> ConvertBVHListToGlobal(List<BVHNode> listBVH, int iOffset)
        {
            List<BVHNode> ret = new List<BVHNode>();

            foreach (BVHNode node in listBVH)
            {
                BVHNode globalNode = node;

                globalNode.m_iId += iOffset;
                if (-1 != globalNode.m_iLeft) { globalNode.m_iLeft += iOffset; }
                if (-1 != globalNode.m_iRight) { globalNode.m_iRight += iOffset; }

                ret.Add(globalNode);
            }

            return ret;
        }

        public void Commit()
        {
            m_mtxMutex.WaitOne();

            List<BVHNode> listAllBVHNodes = new List<BVHNode>();
            List<int> listAllBVHNodesType = new List<int>();
            List< List<BVHNode> > listAllLevelXBVHs = new List< List<BVHNode> >();
            List< List<int> > listAllLevelXBVHsOffsets = new List< List<int> >();

            foreach (BVHObject bvhObject in m_listObjects)
            {
                int iOffset = listAllBVHNodes.Count;

                List<BVHNode> listGlobalBVHNode = ConvertBVHListToGlobal(bvhObject.m_listBVHNodes, iOffset);

                // global bvh nodes
                listAllBVHNodes.AddRange(listGlobalBVHNode);

                // global types
                foreach (BVHNode node in bvhObject.m_listBVHNodes) { listAllBVHNodesType.Add(bvhObject.m_iType); }

                // Global level N
                if (bvhObject.m_iType == (int)BVHObjectType.Dynamic)
                {
                    LevelXList_Resize(listAllLevelXBVHs, bvhObject.m_LevelXBVHs.Count);
                    for (int i = 0; i < bvhObject.m_LevelXBVHs.Count; i++)
                    {
                        List<BVHNode> listGlobalLevelN = ConvertBVHListToGlobal(bvhObject.m_LevelXBVHs[i], iOffset);
                        listAllLevelXBVHs[i].AddRange(listGlobalLevelN);

                        // size
                        m_listRefitTree_LevelXSizes.Add(listGlobalLevelN.Count);
                    }
                }
            }

            // bufferek letrehozasa, device-onkent
            // texturak betoltese
            if (null != clInput_Materials) { clInput_Materials.Dispose(); clInput_Materials = null; }
            clInput_Materials = new ComputeBuffer<Material>(m_Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, m_listMaterials.ToArray());

            if (null != clInput_TexturesData) { clInput_TexturesData.Dispose(); clInput_TexturesData = null; }
            clInput_TexturesData = new ComputeBuffer<byte>(m_Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, m_listTexturesData.ToArray());

            // matrixok betoltese
            if (null != clInput_MatricesData) { clInput_MatricesData.Dispose(); clInput_MatricesData = null; }
            clInput_MatricesData = new ComputeBuffer<Matrix>(m_Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, m_listMatrices.ToArray());

            // global bvh nodes, count
            m_iNumBVHNodes = listAllBVHNodes.Count;

            // Global level N
            foreach (ComputeBuffer<BVHNode> clInput_RefitTree_LevelX in listCLInput_RefitTree_LevelX)
            {
                clInput_RefitTree_LevelX.Dispose();
            }
            listCLInput_RefitTree_LevelX.Clear();
            foreach (List<BVHNode> listLevelXBVHs in listAllLevelXBVHs)
            {
                // levels
                ComputeBuffer<BVHNode> clInput_RefitTree_LevelX = new ComputeBuffer<BVHNode>(m_Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, listLevelXBVHs.ToArray());
                listCLInput_RefitTree_LevelX.Add(clInput_RefitTree_LevelX);
            }

            // global types
            if (null != clInput_AllBVHNodesType) { clInput_AllBVHNodesType.Dispose(); clInput_AllBVHNodesType = null; }
            clInput_AllBVHNodesType = new ComputeBuffer<int>(m_Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, listAllBVHNodesType.ToArray());

            // start global nodes
            if (null != clInput_AllBVHNodes) { clInput_AllBVHNodes.Dispose(); clInput_AllBVHNodes = null; }
            clInput_AllBVHNodes = new ComputeBuffer<BVHNode>(m_Context, ComputeMemoryFlags.ReadOnly | ComputeMemoryFlags.CopyHostPointer, listAllBVHNodes.ToArray());

            // calculating global types
            if (null != clInputOutput_AllBVHNodes) { clInputOutput_AllBVHNodes.Dispose(); clInputOutput_AllBVHNodes = null; }
            clInputOutput_AllBVHNodes = new ComputeBuffer<BVHNode>(m_Context, ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer, listAllBVHNodes.ToArray());

            ;

            listAllBVHNodes.Clear();
            listAllBVHNodesType.Clear();
            // all levelX
            foreach (List<BVHNode> listLevelXBVHs in listAllLevelXBVHs)
            {
                listLevelXBVHs.Clear();
            }
            listAllLevelXBVHs.Clear();
            // all levelX offsets
            foreach (List<int> listLevelXBVHsOffsets in listAllLevelXBVHsOffsets)
            {
                listLevelXBVHsOffsets.Clear();
            }
            listAllLevelXBVHsOffsets.Clear();

            m_mtxMutex.ReleaseMutex();
        }

        public void RunVertexShader()
        {
            m_mtxMutex.WaitOne();

            kernelVertexShader.SetMemoryArgument(0, clInput_AllBVHNodesType);
            kernelVertexShader.SetMemoryArgument(1, clInput_AllBVHNodes);
            kernelVertexShader.SetMemoryArgument(2, clInput_MatricesData);
            kernelVertexShader.SetMemoryArgument(3, clInputOutput_AllBVHNodes);

            int iCount = m_iNumBVHNodes;

            ComputeEventList eventList = new ComputeEventList();
            cmdQueue.Execute(kernelVertexShader, null, new long[] { iCount }, null, eventList);
            cmdQueue.Finish();
            foreach (ComputeEventBase eventBase in eventList) { eventBase.Dispose(); }
            eventList.Clear();

            m_mtxMutex.ReleaseMutex();
        }

        public void RunRefitTreeShader()
        {
            m_mtxMutex.WaitOne();

            for (int i = 0; i < listCLInput_RefitTree_LevelX.Count; i++)
            {
                int iCount = m_listRefitTree_LevelXSizes[i];
                if (iCount == 0) { continue; }

                ComputeBuffer<BVHNode> clInput_RefitTree_LevelX = listCLInput_RefitTree_LevelX[i];

                KernelRefitTree_LevelX.SetMemoryArgument(0, clInput_RefitTree_LevelX);
                KernelRefitTree_LevelX.SetMemoryArgument(1, clInputOutput_AllBVHNodes);
                
                ComputeEventList eventList = new ComputeEventList();
                cmdQueue.Execute(KernelRefitTree_LevelX, null, new long[] { iCount }, null, eventList);
                cmdQueue.Finish();
                foreach (ComputeEventBase eventBase in eventList) { eventBase.Dispose(); }
                eventList.Clear();
            }

            m_mtxMutex.ReleaseMutex();
        }

        public void SetCamera(Vector3 pos, Vector3 at, Vector3 up, float angle, float zfar)
        {
            m_mtxMutex.WaitOne();

            // pos
            Float3 inPos;
            inPos.m_X = pos.X;
            inPos.m_Y = pos.Y;
            inPos.m_Z = pos.Z;

            // at
            Float3 inAt;
            inAt.m_X = at.X;
            inAt.m_Y = at.Y;
            inAt.m_Z = at.Z;

            // at
            up = up.Normalized();
            Float3 inUp;
            inUp.m_X = up.X;
            inUp.m_Y = up.Y;
            inUp.m_Z = up.Z;

            // dir
            Vector3 dir = (at - pos).Normalized();
            Float3 inDir;
            inDir.m_X = dir.X;
            inDir.m_Y = dir.Y;
            inDir.m_Z = dir.Z;

            // right
            Vector3 right = Vector3.Cross(dir, up).Normalized();
            Float3 inRight;
            inRight.m_X = right.X;
            inRight.m_Y = right.Y;
            inRight.m_Z = right.Z;

            // step
            float step = angle / ((float)m_iHeight / 2.0f);

            KernelCameraRays.SetValueArgument<Float3>(0, inPos);
            KernelCameraRays.SetValueArgument<Float3>(1, inAt);
            KernelCameraRays.SetValueArgument<Float3>(2, inUp);
            KernelCameraRays.SetValueArgument<Float3>(3, inDir);
            KernelCameraRays.SetValueArgument<Float3>(4, inRight);
            KernelCameraRays.SetValueArgument<float>(5, step);
            KernelCameraRays.SetValueArgument<float>(6, angle);
            KernelCameraRays.SetValueArgument<float>(7, zfar);
            KernelCameraRays.SetValueArgument<int>(8, m_iWidth);
            KernelCameraRays.SetValueArgument<int>(9, m_iHeight);
            KernelCameraRays.SetValueArgument<int>(10, m_iWidth / 2);
            KernelCameraRays.SetValueArgument<int>(11, m_iHeight / 2);
            KernelCameraRays.SetMemoryArgument(12, clInputOutput_Rays);
            
            ComputeEventList eventList = new ComputeEventList();
            cmdQueue.Execute(KernelCameraRays, null, new long[] { m_iWidth, m_iHeight }, null, eventList);
            cmdQueue.Finish();
            foreach (ComputeEventBase eventBase in eventList) { eventBase.Dispose(); }
            eventList.Clear();

            m_mtxMutex.ReleaseMutex();
        }

        public void RunClearScreenShader(byte iRed, byte iGreen, byte iBlue, byte iAlpha)
        {
            m_mtxMutex.WaitOne();

            KernelClearShader.SetValueArgument<int>(0, m_iWidth);
            KernelClearShader.SetValueArgument<int>(1, m_iHeight);
            KernelClearShader.SetValueArgument<byte>(2, iRed);
            KernelClearShader.SetValueArgument<byte>(3, iGreen);
            KernelClearShader.SetValueArgument<byte>(4, iBlue);
            KernelClearShader.SetValueArgument<byte>(5, iAlpha);
            KernelClearShader.SetMemoryArgument(6, clOutput_TextureBuffer);

            ComputeEventList eventList = new ComputeEventList();
            cmdQueue.Execute(KernelClearShader, null, new long[] { m_iWidth, m_iHeight }, null, eventList);
            cmdQueue.Finish();
            foreach (ComputeEventBase eventBase in eventList) { eventBase.Dispose(); }
            eventList.Clear();

            m_mtxMutex.ReleaseMutex();
        }

        public void RunRayShader()
        {
            m_mtxMutex.WaitOne();

            KernelRayShader.SetMemoryArgument(0, clInputOutput_Rays);
            KernelRayShader.SetMemoryArgument(1, clInputOutput_AllBVHNodes);
            KernelRayShader.SetValueArgument<int>(2, m_iWidth);
            KernelRayShader.SetValueArgument<int>(3, m_iHeight);
            KernelRayShader.SetMemoryArgument(4, clOutput_TextureBuffer);
            
            ComputeEventList eventList = new ComputeEventList();
            cmdQueue.Execute(KernelRayShader, null, new long[] { m_iWidth, m_iHeight }, null, eventList);
            cmdQueue.Finish();
            foreach (ComputeEventBase eventBase in eventList) { eventBase.Dispose(); }
            eventList.Clear();

            SysIntX3 offset = new SysIntX3(0, 0, 0);
            SysIntX3 region = new SysIntX3(m_iWidth, m_iHeight, 4);
            IntPtr source = cmdQueue.Map(clOutput_TextureBuffer, true, ComputeMemoryMappingFlags.Read, 0, m_iWidth * m_iHeight * 4, eventList);
            cmdQueue.Finish();
            foreach (ComputeEventBase eventBase in eventList) { eventBase.Dispose(); }
            eventList.Clear();
            
            writeableBitmap.Lock();
            Win32.CopyMemory(writeableBitmap.BackBuffer, source, m_iWidth * m_iHeight * 4);
            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, m_iWidth, m_iHeight));
            writeableBitmap.Unlock();

            m_mtxMutex.ReleaseMutex();
        }

        public WriteableBitmap GetWriteableBitmap()
        {
            return writeableBitmap;
        }

        static BBox GenBBox(Triangle tri)
        {
            float fMinX = float.MaxValue;
            float fMinY = float.MaxValue;
            float fMinZ = float.MaxValue;

            fMinX = Math.Min(fMinX, tri.m_A.m_Vx);
            fMinX = Math.Min(fMinX, tri.m_B.m_Vx);
            fMinX = Math.Min(fMinX, tri.m_C.m_Vx);

            fMinY = Math.Min(fMinY, tri.m_A.m_Vy);
            fMinY = Math.Min(fMinY, tri.m_B.m_Vy);
            fMinY = Math.Min(fMinY, tri.m_C.m_Vy);
            
            fMinZ = Math.Min(fMinZ, tri.m_A.m_Vz);
            fMinZ = Math.Min(fMinZ, tri.m_B.m_Vz);
            fMinZ = Math.Min(fMinZ, tri.m_C.m_Vz);
            
            float fMaxX = float.MinValue;
            float fMaxY = float.MinValue;
            float fMaxZ = float.MinValue;

            fMaxX = Math.Max(fMaxX, tri.m_A.m_Vx);
            fMaxX = Math.Max(fMaxX, tri.m_B.m_Vx);
            fMaxX = Math.Max(fMaxX, tri.m_C.m_Vx);
            
            fMaxY = Math.Max(fMaxY, tri.m_A.m_Vy);
            fMaxY = Math.Max(fMaxY, tri.m_B.m_Vy);
            fMaxY = Math.Max(fMaxY, tri.m_C.m_Vy);
            
            fMaxZ = Math.Max(fMaxZ, tri.m_A.m_Vz);
            fMaxZ = Math.Max(fMaxZ, tri.m_B.m_Vz);
            fMaxZ = Math.Max(fMaxZ, tri.m_C.m_Vz);
            
            BBox bbox = new BBox();
            bbox.minx = fMinX;
            bbox.miny = fMinY;
            bbox.minz = fMinZ;
            bbox.maxx = fMaxX;
            bbox.maxy = fMaxY;
            bbox.maxz = fMaxZ;

            return bbox;
        }

        public void Resize(int iWidth, int iHeight)
        {
            m_mtxMutex.WaitOne();

            m_iWidth = iWidth;
            m_iHeight = iHeight;

            int iSize = iWidth * iHeight;

            // rays
            if (null != clInputOutput_Rays) { clInputOutput_Rays.Dispose(); clInputOutput_Rays = null; }
            clInputOutput_Rays = new ComputeBuffer<Ray>(m_Context, ComputeMemoryFlags.ReadWrite, iSize);

            // texture
            if (null != clOutput_TextureBuffer) { clOutput_TextureBuffer.Dispose(); clOutput_TextureBuffer = null; }
            clOutput_TextureBuffer = new ComputeBuffer<byte>(m_Context, ComputeMemoryFlags.WriteOnly, iWidth * iHeight * 4);

            writeableBitmap = new WriteableBitmap(iWidth, iHeight, 96, 96, PixelFormats.Bgra32, null);

            m_mtxMutex.ReleaseMutex();
        }

        public void Dispose()
        {
            m_mtxMutex.WaitOne();

            m_listMaterials.Clear();
            m_listTexturesData.Clear();
            m_listMatrices.Clear();
            m_listObjects.Clear();
            m_listRefitTree_LevelXSizes.Clear();

            // bufferek letrehozasa, device-onkent
            // texturak betoltese
            if (null != clInput_Materials) { clInput_Materials.Dispose(); clInput_Materials = null; }
            
            if (null != clInput_TexturesData) { clInput_TexturesData.Dispose(); clInput_TexturesData = null; }
            
            // matrixok betoltese
            if (null != clInput_MatricesData) { clInput_MatricesData.Dispose(); clInput_MatricesData = null; }
            
            // Global level N
            foreach (ComputeBuffer<BVHNode> clInput_RefitTree_LevelX in listCLInput_RefitTree_LevelX)
            {
                clInput_RefitTree_LevelX.Dispose();
            }
            listCLInput_RefitTree_LevelX.Clear();
            
            // global types
            if (null != clInput_AllBVHNodesType) { clInput_AllBVHNodesType.Dispose(); clInput_AllBVHNodesType = null; }

            // start global nodes
            if (null != clInput_AllBVHNodes) { clInput_AllBVHNodes.Dispose(); clInput_AllBVHNodes = null; }
            
            // calculating global types
            if (null != clInputOutput_AllBVHNodes) { clInputOutput_AllBVHNodes.Dispose(); clInputOutput_AllBVHNodes = null; }

            // rendertarget
            if (null != clOutput_TextureBuffer) { clOutput_TextureBuffer.Dispose(); clOutput_TextureBuffer = null; }

            kernelVertexShader.Dispose();
            KernelRefitTree_LevelX.Dispose();
            KernelCameraRays.Dispose();
            KernelRayShader.Dispose();
            cmdQueue.Dispose();
            m_Program.Dispose();
            m_Context.Dispose();

            m_mtxMutex.ReleaseMutex();
        }

        enum BVHObjectType
        {
            Static = 1,
            Dynamic = 2
        }

        Mutex m_mtxMutex = new Mutex();

        List<Material> m_listMaterials = new List<Material>();
        List<byte> m_listTexturesData = new List<byte>();
        List<Matrix> m_listMatrices = new List<Matrix>();
        List<BVHObject> m_listObjects = new List<BVHObject>();
        List<int> m_listRefitTree_LevelXSizes = new List<int>();

        int m_iWidth;
        int m_iHeight;
        
        // opencl resources
        ComputeContext m_Context;
        ComputeProgram m_Program;
        ComputeDevice m_Device;
        ComputeCommandQueue cmdQueue;
        ComputeKernel kernelVertexShader;
        ComputeKernel KernelRefitTree_LevelX;
        ComputeKernel KernelCameraRays;
        ComputeKernel KernelClearShader;
        ComputeKernel KernelRayShader;

        // textures
        ComputeBuffer<Material> clInput_Materials = null;
        ComputeBuffer<byte> clInput_TexturesData = null;

        // matrices
        ComputeBuffer<Matrix> clInput_MatricesData = null;

        int m_iNumBVHNodes;
        ComputeBuffer<int> clInput_AllBVHNodesType = null;
        ComputeBuffer<BVHNode> clInput_AllBVHNodes = null;
        ComputeBuffer<BVHNode> clInputOutput_AllBVHNodes = null;
        List<ComputeBuffer<BVHNode>> listCLInput_RefitTree_LevelX = new List<ComputeBuffer<BVHNode>>();
        ComputeBuffer<Ray> clInputOutput_Rays = null;

        // rendertarget
        ComputeBuffer<byte> clOutput_TextureBuffer = null;
        WriteableBitmap writeableBitmap = null;
    }
}
