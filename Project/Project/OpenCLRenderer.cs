using GlmSharp;
using OpenCL.Net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenCLRenderer
{
    
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
        public Triangle m_Triangle;
        public BBox m_BBox;
        public int m_iLeft;
        public int m_iRight;
    }

    
    struct Matrix
    {
        public vec4 m_Row0;
        public vec4 m_Row1;
        public vec4 m_Row2;
        public vec4 m_Row3;

        public vec4 m_Column0;
        public vec4 m_Column1;
        public vec4 m_Column2;
        public vec4 m_Column3;
    }

    
    struct BVHNodeOffset
    {
        public int m_iOffset;
        public int m_iNumBVHNodes;
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
                foreach (Device newDevice in Cl.GetDeviceIDs(platform, DeviceType.Gpu | DeviceType.Accelerator, out error))
                {
                    if (error != ErrorCode.Success) { continue; }
                    if (Cl.GetDeviceInfo(newDevice, DeviceInfo.ImageSupport, out error).CastTo<Bool>() == Bool.False) { continue; }

                    Context newContext = Cl.CreateContext(null, 1, new Device[] { newDevice }, null, IntPtr.Zero, out error);
                    if (error != ErrorCode.Success)
                    {
                        continue;
                    }

                    InfoBuffer info = Cl.GetDeviceInfo(newDevice, DeviceInfo.Name, out error);

                    // print name
                    //System.Console.WriteLine(info.ToString());

                    OpenCLDevice newOpenCLDevice = new OpenCLDevice();
                    newOpenCLDevice.m_Context = newContext;
                    newOpenCLDevice.m_Device = newDevice;
                    newOpenCLDevice.m_strName = info.ToString();
                    m_listOpenCLDevices.Add(newOpenCLDevice);
                }
            }

            // have cl device ?
            if (0 == m_listOpenCLDevices.Count)
            {
                throw new Exception("Scene: Not find OpenCL GPU device!");
            }

            // buils cl script
            foreach (OpenCLDevice clDevice in m_listOpenCLDevices)
            {
                using (OpenCL.Net.Program program = Cl.CreateProgramWithSource(clDevice.m_Context, 1, new[] { OpenCLScript.GetText() }, null, out error))
                {
                    error = Cl.BuildProgram(program, 1, new[] { clDevice.m_Device }, string.Empty, null, IntPtr.Zero);
                    if (Cl.GetProgramBuildInfo(program, clDevice.m_Device, ProgramBuildInfo.Status, out error).CastTo<BuildStatus>() != BuildStatus.Success)
                    {
                        string strBuildInfo = Cl.GetProgramBuildInfo(program, clDevice.m_Device, ProgramBuildInfo.Log, out error).ToString();
                        throw new Exception(strBuildInfo);
                    }

                    // VertexShader
                    clDevice.kernelVertexShader = Cl.CreateKernel(program, "Main_VertexShader", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_VertexShader");
                    }

                    // RefitTree
                    clDevice.kernelRefitTree_Level2 = Cl.CreateKernel(program, "Main_RefitTree_Level2", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level2");
                    }

                    clDevice.kernelRefitTree_Level3 = Cl.CreateKernel(program, "Main_RefitTree_Level3", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level3");
                    }

                    clDevice.kernelRefitTree_Level4 = Cl.CreateKernel(program, "Main_RefitTree_Level4", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level4");
                    }

                    clDevice.kernelRefitTree_Level5 = Cl.CreateKernel(program, "Main_RefitTree_Level5", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level5");
                    }

                    clDevice.kernelRefitTree_Level6 = Cl.CreateKernel(program, "Main_RefitTree_Level6", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level6");
                    }

                    clDevice.kernelRefitTree_Level7 = Cl.CreateKernel(program, "Main_RefitTree_Level7", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level7");
                    }

                    clDevice.kernelRefitTree_Level8 = Cl.CreateKernel(program, "Main_RefitTree_Level8", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level8");
                    }

                    clDevice.kernelRefitTree_Level9 = Cl.CreateKernel(program, "Main_RefitTree_Level9", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level9");
                    }

                    clDevice.kernelRefitTree_Level10 = Cl.CreateKernel(program, "Main_RefitTree_Level10", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level10");
                    }

                    clDevice.kernelRefitTree_Level11 = Cl.CreateKernel(program, "Main_RefitTree_Level11", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level11");
                    }

                    clDevice.kernelRefitTree_Level12 = Cl.CreateKernel(program, "Main_RefitTree_Level12", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level12");
                    }

                    clDevice.kernelRefitTree_Level13 = Cl.CreateKernel(program, "Main_RefitTree_Level13", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level13");
                    }

                    clDevice.kernelRefitTree_Level14 = Cl.CreateKernel(program, "Main_RefitTree_Level14", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level14");
                    }

                    clDevice.kernelRefitTree_Level15 = Cl.CreateKernel(program, "Main_RefitTree_Level15", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level15");
                    }

                    clDevice.kernelRefitTree_Level16 = Cl.CreateKernel(program, "Main_RefitTree_Level16", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level16");
                    }

                    clDevice.kernelRefitTree_Level17 = Cl.CreateKernel(program, "Main_RefitTree_Level17", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level17");
                    }

                    clDevice.kernelRefitTree_Level18 = Cl.CreateKernel(program, "Main_RefitTree_Level18", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level18");
                    }

                    clDevice.kernelRefitTree_Level19 = Cl.CreateKernel(program, "Main_RefitTree_Level19", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level19");
                    }

                    clDevice.kernelRefitTree_Level20 = Cl.CreateKernel(program, "Main_RefitTree_Level20", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level20");
                    }

                    clDevice.kernelRefitTree_Level21 = Cl.CreateKernel(program, "Main_RefitTree_Level21", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level21");
                    }

                    clDevice.kernelRefitTree_Level22 = Cl.CreateKernel(program, "Main_RefitTree_Level22", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level22");
                    }

                    clDevice.kernelRefitTree_Level23 = Cl.CreateKernel(program, "Main_RefitTree_Level23", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level23");
                    }

                    clDevice.kernelRefitTree_Level24 = Cl.CreateKernel(program, "Main_RefitTree_Level24", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level24");
                    }

                    clDevice.kernelRefitTree_Level25 = Cl.CreateKernel(program, "Main_RefitTree_Level25", out error);
                    if (error != ErrorCode.Success)
                    {
                        throw new Exception("Cl.CreateKernel: Main_RefitTree Level25");
                    }

                    clDevice.cmdQueue = Cl.CreateCommandQueue(clDevice.m_Context, clDevice.m_Device, (CommandQueueProperties)0, out error);
                }
            }
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

        public void SetMatrix(int iId, mat4 mMatrix)
        {
            m_mtxMutex.WaitOne();
            Matrix newMatrix = new Matrix();

            newMatrix.m_Row0 = mMatrix.Row0;
            newMatrix.m_Row1 = mMatrix.Row1;
            newMatrix.m_Row2 = mMatrix.Row2;
            newMatrix.m_Row3 = mMatrix.Row3;

            newMatrix.m_Column0 = mMatrix.Column0;
            newMatrix.m_Column1 = mMatrix.Column1;
            newMatrix.m_Column2 = mMatrix.Column2;
            newMatrix.m_Column3 = mMatrix.Column3;

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

            fMinDistance = Math.Min(fMinDistance, vec3.Distance(new vec3(tri1.m_A.m_Vx, tri1.m_A.m_Vy, tri1.m_A.m_Vz), new vec3(tri2.m_A.m_Vx, tri2.m_A.m_Vy, tri2.m_A.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(new vec3(tri1.m_A.m_Vx, tri1.m_A.m_Vy, tri1.m_A.m_Vz), new vec3(tri2.m_B.m_Vx, tri2.m_B.m_Vy, tri2.m_B.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(new vec3(tri1.m_A.m_Vx, tri1.m_A.m_Vy, tri1.m_A.m_Vz), new vec3(tri2.m_C.m_Vx, tri2.m_C.m_Vy, tri2.m_C.m_Vz)));

            fMinDistance = Math.Min(fMinDistance, vec3.Distance(new vec3(tri1.m_B.m_Vx, tri1.m_B.m_Vy, tri1.m_B.m_Vz), new vec3(tri2.m_A.m_Vx, tri2.m_A.m_Vy, tri2.m_A.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(new vec3(tri1.m_B.m_Vx, tri1.m_B.m_Vy, tri1.m_B.m_Vz), new vec3(tri2.m_B.m_Vx, tri2.m_B.m_Vy, tri2.m_B.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(new vec3(tri1.m_B.m_Vx, tri1.m_B.m_Vy, tri1.m_B.m_Vz), new vec3(tri2.m_C.m_Vx, tri2.m_C.m_Vy, tri2.m_C.m_Vz)));

            fMinDistance = Math.Min(fMinDistance, vec3.Distance(new vec3(tri1.m_C.m_Vx, tri1.m_C.m_Vy, tri1.m_C.m_Vz), new vec3(tri2.m_A.m_Vx, tri2.m_A.m_Vy, tri2.m_A.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(new vec3(tri1.m_C.m_Vx, tri1.m_C.m_Vy, tri1.m_C.m_Vz), new vec3(tri2.m_B.m_Vx, tri2.m_B.m_Vy, tri2.m_B.m_Vz)));
            fMinDistance = Math.Min(fMinDistance, vec3.Distance(new vec3(tri1.m_C.m_Vx, tri1.m_C.m_Vy, tri1.m_C.m_Vz), new vec3(tri2.m_C.m_Vx, tri2.m_C.m_Vy, tri2.m_C.m_Vz)));

            return fMinDistance;
        }

        static float GetDistance_BBox_BBox(BBox bbox1, BBox bbox2)
        {
            vec3 center1 = (new vec3(bbox1.minx, bbox1.miny, bbox1.minz) + new vec3(bbox1.maxx, bbox1.maxy, bbox1.maxz)) / 2.0f;
            vec3 center2 = (new vec3(bbox2.minx, bbox2.miny, bbox2.minz) + new vec3(bbox2.maxx, bbox2.maxy, bbox2.maxz)) / 2.0f;

            vec3 halfSize1 = new vec3(bbox1.maxx, bbox1.maxy, bbox1.maxz) - center1;
            vec3 halfSize2 = new vec3(bbox2.maxx, bbox2.maxy, bbox2.maxz) - center2;

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

        public BVHObject CreateStaticObject(List<Triangle> triangles, mat4 matTransform)
        {
            List<Triangle> newTriangles = new List<Triangle>();

            foreach (Triangle oldTri in triangles)
            {
                Triangle newTri = new Triangle();

                Vertex vertexA = new Vertex();
                vec3 AV = new vec3(matTransform * new vec4(oldTri.m_A.m_Vx, oldTri.m_A.m_Vy, oldTri.m_A.m_Vz, 1.0f));
                vertexA.m_Vx = AV.x;
                vertexA.m_Vy = AV.y;
                vertexA.m_Vz = AV.z;
                vec3 AN = new vec3(matTransform * new vec4(oldTri.m_A.m_Nx, oldTri.m_A.m_Ny, oldTri.m_A.m_Nz, 0.0f));
                vertexA.m_Nx = AN.x;
                vertexA.m_Ny = AN.y;
                vertexA.m_Nz = AN.z;
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
                vec3 BV = new vec3(matTransform * new vec4(oldTri.m_B.m_Vx, oldTri.m_B.m_Vy, oldTri.m_B.m_Vz, 1.0f));
                vertexB.m_Vx = BV.x;
                vertexB.m_Vy = BV.y;
                vertexB.m_Vz = BV.z;
                vec3 BN = new vec3(matTransform * new vec4(oldTri.m_B.m_Nx, oldTri.m_B.m_Ny, oldTri.m_B.m_Nz, 0.0f));
                vertexB.m_Nx = BN.x;
                vertexB.m_Ny = BN.y;
                vertexB.m_Nz = BN.z;
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
                vec3 CV = new vec3(matTransform * new vec4(oldTri.m_C.m_Vx, oldTri.m_C.m_Vy, oldTri.m_C.m_Vz, 1.0f));
                vertexC.m_Vx = CV.x;
                vertexC.m_Vy = CV.y;
                vertexC.m_Vz = CV.z;
                vec3 CN = new vec3(matTransform * new vec4(oldTri.m_C.m_Nx, oldTri.m_C.m_Ny, oldTri.m_C.m_Nz, 0.0f));
                vertexC.m_Nx = CN.x;
                vertexC.m_Ny = CN.y;
                vertexC.m_Nz = CN.z;
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

            m_mtxMutex.ReleaseMutex();

            return newObject;
        }

        public BVHObject CreateDynamicObject(List<Triangle> triangles)
        {
            List<BVHNode> newBVH = CreateBVH(triangles);

            m_mtxMutex.WaitOne();

            BVHObject newObject = new BVHObject();
            newObject.m_iType = (int)BVHObjectType.Dynamic;
            newObject.m_listBVHNodes = newBVH;

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
                leaf1.m_BBox = GenBBox(tri1, tri1);

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

                    parent.m_BBox = GenBBox(node1.m_BBox, node2.m_BBox);

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

                    parent.m_BBox = GenBBox(node1.m_BBox, node1.m_BBox);

                    outBuffer.Add(parent);
                }
            }

            // root frissitese
            tree[0] = outBuffer[0];
            outBuffer.RemoveAt(0);

            return tree;
        }

        public void Commit()
        {
            m_mtxMutex.WaitOne();

            // opencl object eloallitasa
            // offset-ek eloallitasa
            List<BVHNodeOffset> listBVHNodesOffsets = new List<BVHNodeOffset>();
            // all bvh one big list
            List<BVHNode> listAllBVHNodes = new List<BVHNode>();
            List<int> listAllBVHNodesType = new List<int>();

            foreach (BVHObject bvhObject in m_listObjects)
            {
                // offset
                BVHNodeOffset newOffset = new BVHNodeOffset();
                newOffset.m_iOffset = listBVHNodesOffsets.Count;
                newOffset.m_iNumBVHNodes = bvhObject.m_listBVHNodes.Count;
                listBVHNodesOffsets.Add(newOffset);

                // all list
                listAllBVHNodes.AddRange(bvhObject.m_listBVHNodes);
                foreach (BVHNode node in bvhObject.m_listBVHNodes) { listAllBVHNodesType.Add(bvhObject.m_iType); }
            }

            // bufferek letrehozasa, device-onkent
            foreach (OpenCLDevice clDevice in m_listOpenCLDevices)
            {
                ErrorCode error;

                // texturak betoltese
                clDevice.clInput_Materials = Cl.CreateBuffer(clDevice.m_Context, MemFlags.CopyHostPtr | MemFlags.ReadOnly, m_listMaterials.ToArray(), out error);
                if (error != ErrorCode.Success) { throw new Exception("Cl.CreateBuffer: Materials"); }

                clDevice.clInput_TexturesData = Cl.CreateBuffer(clDevice.m_Context, MemFlags.CopyHostPtr | MemFlags.ReadOnly, m_listTexturesData.ToArray(), out error);
                if (error != ErrorCode.Success) { throw new Exception("Cl.CreateBuffer: TexturesData"); }

                // matrixok betoltese
                clDevice.clInput_MatricesData = Cl.CreateBuffer(clDevice.m_Context, MemFlags.CopyHostPtr | MemFlags.ReadOnly, m_listMatrices.ToArray(), out error);
                if (error != ErrorCode.Success) { throw new Exception("Cl.CreateBuffer: MatricesData"); }

                // all bvh nodes
                clDevice.m_iNumObjects = listBVHNodesOffsets.Count;
                clDevice.m_iNumBVHNodes = listAllBVHNodes.Count;

                clDevice.clInput_BVHNodeOffsetsData = Cl.CreateBuffer(clDevice.m_Context, MemFlags.CopyHostPtr | MemFlags.ReadOnly, listBVHNodesOffsets.ToArray(), out error);
                if (error != ErrorCode.Success) { throw new Exception("Cl.CreateBuffer: BVHNodeOffsetsData"); }

                clDevice.clInput_AllBVHNodesType = Cl.CreateBuffer(clDevice.m_Context, MemFlags.CopyHostPtr | MemFlags.ReadOnly, listAllBVHNodesType.ToArray(), out error);
                if (error != ErrorCode.Success) { throw new Exception("Cl.CreateBuffer: Input AllBVHNodesType"); }

                clDevice.clInput_AllBVHNodes = Cl.CreateBuffer(clDevice.m_Context, MemFlags.CopyHostPtr | MemFlags.ReadOnly, listAllBVHNodes.ToArray(), out error);
                if (error != ErrorCode.Success) { throw new Exception("Cl.CreateBuffer: Input AllBVHNodes"); }

                clDevice.clInputOutput_AllBVHNodes = Cl.CreateBuffer<BVHNode>(clDevice.m_Context, MemFlags.CopyHostPtr | MemFlags.ReadWrite, listAllBVHNodes.ToArray(), out error);
                if (error != ErrorCode.Success) { throw new Exception("Cl.CreateBuffer: Output AllBVHNodes"); }
            }

            listBVHNodesOffsets.Clear();
            listAllBVHNodes.Clear();
            listAllBVHNodesType.Clear();

            m_mtxMutex.ReleaseMutex();
        }

        public void RunVertexShader()
        {
            m_mtxMutex.WaitOne();

            Parallel.For(0, m_listOpenCLDevices.Count, index =>
            {
                ErrorCode error;
                OpenCLDevice device = m_listOpenCLDevices[index];

                Cl.SetKernelArg(device.kernelVertexShader, 0, device.clInput_AllBVHNodesType);
                Cl.SetKernelArg(device.kernelVertexShader, 1, device.clInput_AllBVHNodes);
                Cl.SetKernelArg(device.kernelVertexShader, 2, device.clInput_MatricesData);
                Cl.SetKernelArg(device.kernelVertexShader, 3, device.clInputOutput_AllBVHNodes);

                Event clevent;
                int iCount = device.m_iNumBVHNodes;
                IntPtr intptrCount = new IntPtr(iCount);
                error = Cl.EnqueueNDRangeKernel(device.cmdQueue, device.kernelVertexShader, 1, new IntPtr[] { new IntPtr(0) }, new IntPtr[] { intptrCount }, null, 0, null, out clevent);
                Cl.Finish(device.cmdQueue);
                if (error != ErrorCode.Success) { throw new Exception("RunVertexShader: Cl.EnqueueNDRangeKernel"); }
            });

            m_mtxMutex.ReleaseMutex();
        }

        public void RunRefitTree()
        {
            m_mtxMutex.WaitOne();

            Parallel.For(0, m_listOpenCLDevices.Count, index =>
            {
                //ErrorCode error;
                //OpenCLDevice device = m_listOpenCLDevices[index];

                //Cl.SetKernelArg(device.kernelRefitTree, 0, device.clInput_BVHNodeOffsetsData);
                //Cl.SetKernelArg(device.kernelRefitTree, 1, device.clInputOutput_AllBVHNodes);
                //
                //Event clevent;
                //int iCount = device.m_iNumObjects;
                //IntPtr intptrCount = new IntPtr(iCount);
                //error = Cl.EnqueueNDRangeKernel(device.cmdQueue, device.kernelRefitTree, 1, new IntPtr[] { new IntPtr(0) }, new IntPtr[] { intptrCount }, null, 0, null, out clevent);
                //Cl.Finish(device.cmdQueue);
                //if (error != ErrorCode.Success) { throw new Exception("RunRefitTree: Cl.EnqueueNDRangeKernel"); }

                // level2
                ;
                // level3
                ;
                // level4
                ;
                // level5
                ;
                // level6
                ;
                // level7
                ;
                // level8
                ;
                // level9
                ;
                // level10
                ;
                // level11
                ;
                // level12
                ;
                // level13
                ;
                // level14
                ;
                // level15
                ;
                // level16
                ;
                // level17
                ;
                // level18
                ;
                // level19
                ;
                // level20
                ;
                // level21
                ;
                // level22
                ;
                // level23
                ;
                // level24
                ;
                // level25
                ;
            });

            m_mtxMutex.ReleaseMutex();
        }

        bool IsVisited(BVHNode node)
        {
            if (   node.m_BBox.minx == 0
                && node.m_BBox.miny == 0
                && node.m_BBox.minz == 0
                && node.m_BBox.maxx == 0
                && node.m_BBox.maxy == 0
                && node.m_BBox.maxz == 0
                ) { return false; }
            return true;
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

        enum BVHObjectType
        {
            Static = 1,
            Dynamic = 2
        }

        Mutex m_mtxMutex = new Mutex();

        List<Material> m_listMaterials = new List<Material>();
        List<Matrix> m_listMatrices = new List<Matrix>();
        List<BVHObject> m_listObjects = new List<BVHObject>();
        
        List<OpenCLDevice> m_listOpenCLDevices = new List<OpenCLDevice>();

        List<byte> m_listTexturesData = new List<byte>();
    }

    class OpenCLDevice
    {
        public Context m_Context;
        public Device m_Device;
        public CommandQueue cmdQueue;
        public Kernel kernelVertexShader;
        public Kernel kernelRefitTree_Level2;
        public Kernel kernelRefitTree_Level3;
        public Kernel kernelRefitTree_Level4;
        public Kernel kernelRefitTree_Level5;
        public Kernel kernelRefitTree_Level6;
        public Kernel kernelRefitTree_Level7;
        public Kernel kernelRefitTree_Level8;
        public Kernel kernelRefitTree_Level9;
        public Kernel kernelRefitTree_Level10;
        public Kernel kernelRefitTree_Level11;
        public Kernel kernelRefitTree_Level12;
        public Kernel kernelRefitTree_Level13;
        public Kernel kernelRefitTree_Level14;
        public Kernel kernelRefitTree_Level15;
        public Kernel kernelRefitTree_Level16;
        public Kernel kernelRefitTree_Level17;
        public Kernel kernelRefitTree_Level18;
        public Kernel kernelRefitTree_Level19;
        public Kernel kernelRefitTree_Level20;
        public Kernel kernelRefitTree_Level21;
        public Kernel kernelRefitTree_Level22;
        public Kernel kernelRefitTree_Level23;
        public Kernel kernelRefitTree_Level24;
        public Kernel kernelRefitTree_Level25;

        public string m_strName;

        // textures
        public IMem<Material> clInput_Materials;
        public IMem<byte> clInput_TexturesData;

        // matrices
        public IMem<Matrix> clInput_MatricesData;

        // objects
        public int m_iNumObjects;
        public IMem<BVHNodeOffset> clInput_BVHNodeOffsetsData;

        public int m_iNumBVHNodes;
        public IMem<int> clInput_AllBVHNodesType;
        public IMem<BVHNode> clInput_AllBVHNodes;
        public IMem<BVHNode> clInputOutput_AllBVHNodes;
    }
}
