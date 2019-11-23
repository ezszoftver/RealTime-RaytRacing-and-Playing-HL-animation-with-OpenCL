using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            m_Scene = new OpenCLRenderer.Scene();
            List<string> listDevices = m_Scene.GetDevices();
            m_Scene.Dispose();
            m_Scene = null;
            
            ContextMenu contextMenu = new ContextMenu();
            foreach (string strDevice in listDevices)
            {
                MenuItem item = new MenuItem();
                item.Header = strDevice;
                item.Click += Item_Click;
            
                contextMenu.Items.Add(item);
            }
            ContextMenu = contextMenu;
        }

        private void Item_Click(object sender, RoutedEventArgs e)
        {
            labelMessage.IsEnabled = false;

            if (null != m_Timer)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            if (null != m_Scene)
            {
                m_Scene.Dispose();
                m_Scene = null;
            }

            string strDeviceName = (sender as MenuItem).Header.ToString();
            m_Scene = new OpenCLRenderer.Scene();

            string strVertexShader =
@"
Vertex VertexShader(Vertex in, __global Matrix4x4 *in_Matrices)
{
    Vertex out;
    
    out = in;

    if (1 == in.numMatrices)
    {
        float3 v1 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId1], ToFloat4(in.vx, in.vy, in.vz, 1.0f)), in.weight1);
        out.vx = v1.x;
        out.vy = v1.y;
        out.vz = v1.z;

        float3 n1 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId1], ToFloat4(in.nx, in.ny, in.nz, 0.0f)), in.weight1);
        float3 n = normalize(n1);
        out.nx = n.x;
        out.ny = n.y;
        out.nz = n.z;
    }
    else if (2 == in.numMatrices)
    {
        float3 v1 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId1], ToFloat4(in.vx, in.vy, in.vz, 1.0f)), in.weight1);
        float3 v2 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId2], ToFloat4(in.vx, in.vy, in.vz, 1.0f)), in.weight2);
        out.vx = v1.x + v2.x;
        out.vy = v1.y + v2.y;
        out.vz = v1.z + v2.z;

        float3 n1 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId1], ToFloat4(in.nx, in.ny, in.nz, 0.0f)), in.weight1);
        float3 n2 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId2], ToFloat4(in.nx, in.ny, in.nz, 0.0f)), in.weight2);
        float3 n = normalize(n1 + n2);
        out.nx = n.x;
        out.ny = n.y;
        out.nz = n.z;
    }
    else if (3 == in.numMatrices)
    {
        float3 v1 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId1], ToFloat4(in.vx, in.vy, in.vz, 1.0f)), in.weight1);
        float3 v2 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId2], ToFloat4(in.vx, in.vy, in.vz, 1.0f)), in.weight2);
        float3 v3 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId3], ToFloat4(in.vx, in.vy, in.vz, 1.0f)), in.weight3);
        out.vx = v1.x + v2.x + v3.x;
        out.vy = v1.y + v2.y + v3.y;
        out.vz = v1.z + v2.z + v3.z;

        float3 n1 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId1], ToFloat4(in.nx, in.ny, in.nz, 0.0f)), in.weight1);
        float3 n2 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId2], ToFloat4(in.nx, in.ny, in.nz, 0.0f)), in.weight2);
        float3 n3 = scale4(Mult_Matrix4x4Float4(in_Matrices[in.matrixId3], ToFloat4(in.nx, in.ny, in.nz, 0.0f)), in.weight3);
        float3 n = normalize(n1 + n2 + n3);
        out.nx = n.x;
        out.ny = n.y;
        out.nz = n.z;
    }

    return out;
}
";

            string @strRayShader =
@"

bool RayShader(Hits *hits, Rays *rays, Vector3 camPos, Vector3 camAt, __global Material *materials, __global unsigned char *textureDatas, __global unsigned char *out, int in_Width, int in_Height, int pixelx, int pixely)
{
    float3 light1;
    light1.x = +1000.0f;
    light1.y = +1000.0f;
    light1.z = +1000.0f;

    float3 light2;
    light2.x = -1000.0f;
    light2.y = +1000.0f;
    light2.z = +1000.0f;

    float3 light3;
    light3.x = 0.0f;
    light3.y = +1000.0f;
    light3.z = +1000.0f;
    
    float3 cam_pos;
    cam_pos.x = camPos.x;
    cam_pos.y = camPos.y;
    cam_pos.z = camPos.z;

    if (hits->id == 0)
    {
        Hit hit = hits->hit[hits->id][0];
        if (hit.isCollision == 0) { return true; }

        Ray newRay1; // light1
        newRay1.posx = light1.x;
        newRay1.posy = light1.y;
        newRay1.posz = light1.z;
        float3 dir1 = normalize(hit.pos - light1);
        newRay1.dirx = dir1.x;
        newRay1.diry = dir1.y;
        newRay1.dirz = dir1.z;
        newRay1.length = 5000.0f;

        Ray newRay2; // light 2
        newRay2.posx = light2.x;
        newRay2.posy = light2.y;
        newRay2.posz = light2.z;
        float3 dir2 = normalize(hit.pos - light2);
        newRay2.dirx = dir2.x;
        newRay2.diry = dir2.y;
        newRay2.dirz = dir2.z;
        newRay2.length = 5000.0f;

        Ray newRay3; // light3
        newRay3.posx = light3.x;
        newRay3.posy = light3.y;
        newRay3.posz = light3.z;
        float3 dir3 = normalize(hit.pos - light3);
        newRay3.dirx = dir3.x;
        newRay3.diry = dir3.y;
        newRay3.dirz = dir3.z;
        newRay3.length = 5000.0f;

        Ray newRay4; // reflection
        float3 pos = hit.pos + hit.normal * 0.01;
        newRay4.posx = pos.x;
        newRay4.posy = pos.y;
        newRay4.posz = pos.z;
        float3 dir4 = reflect(normalize(hit.pos - cam_pos), hit.normal);
        newRay4.dirx = dir4.x;
        newRay4.diry = dir4.y;
        newRay4.dirz = dir4.z;
        newRay4.length = 5000.0f;

        rays->id = 1;
        rays->count[rays->id] = 4;
        rays->ray[rays->id][0] = newRay1;
        rays->ray[rays->id][1] = newRay2;
        rays->ray[rays->id][2] = newRay3;
        rays->ray[rays->id][3] = newRay4;

        return false;
    }
    
    if (hits->id == 1)
    {
        Hit hit1 = hits->hit[0][0];
        if (hit1.isCollision == 0) { return true; }

        float diffuseIntensity = 0.0f;

        Hit hit2 = hits->hit[hits->id][0];
        if (hit2.isCollision == 1)
        {
            float length2 = length(light1 - hit2.pos);
            float length1 = length(light1 - hit1.pos);
            
            if ((length2 + 0.005f) > length1)
            {
                float3 dir = normalize(hit1.pos - light1);
                diffuseIntensity += max(dot(-dir, hit2.normal), 0.0f);// + max(dot(-dir2, hit.normal), 0.0f);
            }
        }

        Hit hit3 = hits->hit[hits->id][1];
        if (hit3.isCollision == 1)
        {
            {
                float length2 = length(light2 - hit3.pos);
                float length1 = length(light2 - hit1.pos);
        
                if ((length2 + 0.005f) > length1)
                {
                    float3 dir = normalize(hit1.pos - light2);
                    diffuseIntensity += max(dot(-dir, hit3.normal), 0.0f);
                }
            }
        }

        Hit hit4 = hits->hit[hits->id][2];
        if (hit4.isCollision == 1)
        {
            {
                float length2 = length(light3 - hit4.pos);
                float length1 = length(light3 - hit1.pos);
        
                if ((length2 + 0.005f) > length1)
                {
                    float3 dir = normalize(hit1.pos - light3);
                    diffuseIntensity += max(dot(-dir, hit4.normal), 0.0f);
                }
            }
        }

        

        Color textureColor = Tex2DDiffuse(materials, textureDatas, hit1.materialId, hit1.st);

        // diffuse
        Color diffuseColor;
        diffuseColor.red   = (int)(((float)textureColor.red  ) * diffuseIntensity);
        diffuseColor.green = (int)(((float)textureColor.green) * diffuseIntensity);
        diffuseColor.blue  = (int)(((float)textureColor.blue ) * diffuseIntensity);
        diffuseColor.alpha = 255;

        // reflection
        Hit hit0 = hits->hit[0][0];
        Hit hit5 = hits->hit[hits->id][3];
        if (hit5.isCollision == 1 && hit0.objectId == 0)
        {
            Color reflectionColor = Tex2DDiffuse(materials, textureDatas, hit5.materialId, hit5.st);

            float reflectionIntensity = 0.5;
            diffuseColor.red   += (int)(((float)reflectionColor.red  ) * reflectionIntensity);
            diffuseColor.green += (int)(((float)reflectionColor.green) * reflectionIntensity);
            diffuseColor.blue  += (int)(((float)reflectionColor.blue ) * reflectionIntensity);
            diffuseColor.alpha = 255;

            WriteTexture(out, in_Width, in_Height, ToFloat2(pixelx, pixely), diffuseColor);

            return true;
        }

        WriteTexture(out, in_Width, in_Height, ToFloat2(pixelx, pixely), diffuseColor);

        return true;
    }

    return true;
}
";

            m_Scene.CreateDevice(strDeviceName, @strVertexShader, @strRayShader);

            //Mutex mtxMutex = new Mutex();

            Random rand = new Random();

            {
                // load from obj file
                string strDirectory = @".\";
                OBJLoader objLoader = new OBJLoader();
                
                //mtxMutex.WaitOne();
                objLoader.LoadFromFile(@strDirectory, @"Talaj.obj");
                //mtxMutex.ReleaseMutex();

                // convert to triangle list
                //int iMatrixId = m_Scene.GenMatrix();
                //m_Scene.SetMatrix(iMatrixId, Matrix4.Identity);
                int iMatrixId = 0;


                List<OpenCLRenderer.Triangle> triangles = new List<OpenCLRenderer.Triangle>();
                foreach (OBJLoader.Material material in objLoader.materials)
                {
                    string strDiffuseTextureName = @strDirectory + @material.texture_filename;
                    string strSpecularTextureName = @strDirectory + @"Specular.bmp";
                    string strNormalTextureName = @strDirectory + @"Normal.bmp";
                
                    int iMaterialId = m_Scene.GenMaterial();
                    m_Scene.SetMaterial(iMaterialId, @strDiffuseTextureName, @strSpecularTextureName, @strNormalTextureName);
                
                    for (int i = 0; i < material.indices.Count; i += 3)
                    {
                        Vector3 vA = objLoader.vertices[material.indices[i + 0].id_vertex];
                        Vector3 vB = objLoader.vertices[material.indices[i + 1].id_vertex];
                        Vector3 vC = objLoader.vertices[material.indices[i + 2].id_vertex];
                
                        Vector3 nA = objLoader.normals[material.indices[i + 0].id_normal];
                        Vector3 nB = objLoader.normals[material.indices[i + 1].id_normal];
                        Vector3 nC = objLoader.normals[material.indices[i + 2].id_normal];
                
                        Vector2 tA = objLoader.text_coords[material.indices[i + 0].id_textcoord];
                        Vector2 tB = objLoader.text_coords[material.indices[i + 1].id_textcoord];
                        Vector2 tC = objLoader.text_coords[material.indices[i + 2].id_textcoord];
                
                        OpenCLRenderer.Vertex vertexA = new OpenCLRenderer.Vertex();
                        vertexA.m_Vx = vA.X;
                        vertexA.m_Vy = vA.Y;
                        vertexA.m_Vz = vA.Z;
                        vertexA.m_Nx = nA.X;
                        vertexA.m_Ny = nA.Y;
                        vertexA.m_Nz = nA.Z;
                        vertexA.m_TCx = tA.X;
                        vertexA.m_TCy = tA.Y;
                        vertexA.m_iNumMatrices = 1;
                        vertexA.m_iMatrixId1 = iMatrixId;
                        vertexA.m_fWeight1 = 1.0f;
                        vertexA.m_iMatrixId2 = -1;
                        vertexA.m_fWeight2 = 0.0f;
                        vertexA.m_iMatrixId3 = -1;
                        vertexA.m_fWeight3 = 0.0f;
                
                        OpenCLRenderer.Vertex vertexB = new OpenCLRenderer.Vertex();
                        vertexB.m_Vx = vB.X;
                        vertexB.m_Vy = vB.Y;
                        vertexB.m_Vz = vB.Z;
                        vertexB.m_Nx = nB.X;
                        vertexB.m_Ny = nB.Y;
                        vertexB.m_Nz = nB.Z;
                        vertexB.m_TCx = tB.X;
                        vertexB.m_TCy = tB.Y;
                        vertexB.m_iNumMatrices = 1;
                        vertexB.m_iMatrixId1 = iMatrixId;
                        vertexB.m_fWeight1 = 1.0f;
                        vertexB.m_iMatrixId2 = -1;
                        vertexB.m_fWeight2 = 0.0f;
                        vertexB.m_iMatrixId3 = -1;
                        vertexB.m_fWeight3 = 0.0f;
                
                        OpenCLRenderer.Vertex vertexC = new OpenCLRenderer.Vertex();
                        vertexC.m_Vx = vC.X;
                        vertexC.m_Vy = vC.Y;
                        vertexC.m_Vz = vC.Z;
                        vertexC.m_Nx = nC.X;
                        vertexC.m_Ny = nC.Y;
                        vertexC.m_Nz = nC.Z;
                        vertexC.m_TCx = tC.X;
                        vertexC.m_TCy = tC.Y;
                        vertexC.m_iNumMatrices = 1;
                        vertexC.m_iMatrixId1 = iMatrixId;
                        vertexC.m_fWeight1 = 1.0f;
                        vertexC.m_iMatrixId2 = -1;
                        vertexC.m_fWeight2 = 0.0f;
                        vertexC.m_iMatrixId3 = -1;
                        vertexC.m_fWeight3 = 0.0f;
                
                        OpenCLRenderer.Triangle newTriangle = new OpenCLRenderer.Triangle();
                        newTriangle.m_A = vertexA;
                        newTriangle.m_B = vertexB;
                        newTriangle.m_C = vertexC;
                        newTriangle.m_iMaterialId = iMaterialId;
                
                        triangles.Add(newTriangle);
                    }
                }
                
                int iId;
                iId = m_Scene.GenObject();
                OpenCLRenderer.BVHObject staticObject = m_Scene.CreateStaticObject(triangles, Matrix4.CreateScale(7.0f,3.0f,7.0f));
                m_Scene.SetObject(iId, staticObject);
                objLoader.Release();
                triangles.Clear();

                // load from obj file
                strDirectory = @".\";
                smd = new SMDLoader();
                mesh = new Mesh();

                //mtxMutex.WaitOne();

                // HL1
                smd.LoadReference(@strDirectory, @"Goblin_Reference.smd", mesh);
                smd.AddAnimation(@strDirectory, @"Goblin_Anim.smd", "Anim1", 30.0f);

                // HL2
                //smd.LoadReference(@strDirectory, @"Antlion_guard_reference.smd", mesh);
                //smd.AddAnimation(@strDirectory, @"Antlion_idle.smd", "Anim1", 30.0f);

                smd.SetAnimation("Anim1");

                iMatrixOffset = m_Scene.NumMatrices();
                for (int i = 0; i < mesh.transforms.Count; i++)
                {
                    iMatrixId = m_Scene.GenMatrix();
                    m_Scene.SetMatrix(iMatrixId, smd.GetMatrix(i) * Matrix4.CreateRotationX(-1.57f) * Matrix4.CreateRotationY(3.14f));
                }

                //int iMatrixId = m_Scene.GenMatrix();
                //m_Scene.SetMatrix(iMatrixId, Matrix4.CreateRotationX(-1.57f) * Matrix4.CreateRotationY(3.14f));

                //mtxMutex.ReleaseMutex();

                triangles = new List<OpenCLRenderer.Triangle>();
                foreach (Mesh.Material material in mesh.materials)
                {
                    string strDiffuseTextureName = @strDirectory + @material.texture_name;
                    string strSpecularTextureName = @strDirectory + @"Specular.bmp";
                    string strNormalTextureName = @strDirectory + @"Normal.bmp";
                    
                    int iMaterialId = m_Scene.GenMaterial();
                    m_Scene.SetMaterial(iMaterialId, @strDiffuseTextureName, @strSpecularTextureName, @strNormalTextureName);

                    for (int i = 0; i < material.vertices.Count(); i += 3)
                    {
                        // add triangle
                        Mesh.Vertex meshVertexA = material.vertices[i + 0];
                        Mesh.Vertex meshVertexB = material.vertices[i + 1];
                        Mesh.Vertex meshVertexC = material.vertices[i + 2];

                        Vector3 vA = meshVertexA.vertex;
                        Vector3 vB = meshVertexB.vertex;
                        Vector3 vC = meshVertexC.vertex;

                        // none normal vector
                        Vector3 nA = meshVertexA.normal;
                        Vector3 nB = meshVertexB.normal;
                        Vector3 nC = meshVertexC.normal;

                        Vector2 tA = meshVertexA.textcoords;
                        Vector2 tB = meshVertexB.textcoords;
                        Vector2 tC = meshVertexC.textcoords;

                        OpenCLRenderer.Vertex vertexA = new OpenCLRenderer.Vertex();
                        vertexA.m_Vx = vA.X;
                        vertexA.m_Vy = vA.Y;
                        vertexA.m_Vz = vA.Z;
                        vertexA.m_Nx = nA.X;
                        vertexA.m_Ny = nA.Y;
                        vertexA.m_Nz = nA.Z;
                        vertexA.m_TCx = tA.X;
                        vertexA.m_TCy = tA.Y;
                        vertexA.m_iNumMatrices = meshVertexA.matrices.Count;
                        for(int j = 0; j < vertexA.m_iNumMatrices; j++)
                        {
                            if (j == 0)
                            {
                                vertexA.m_iMatrixId1 = iMatrixOffset + meshVertexA.matrices[j].matrix_id;
                                vertexA.m_fWeight1 = meshVertexA.matrices[j].weight;
                            }
                            if (j == 1)
                            {
                                vertexA.m_iMatrixId2 = iMatrixOffset + meshVertexA.matrices[j].matrix_id;
                                vertexA.m_fWeight2 = meshVertexA.matrices[j].weight;
                            }
                            if (j == 2)
                            {
                                vertexA.m_iMatrixId3 = iMatrixOffset + meshVertexA.matrices[j].matrix_id;
                                vertexA.m_fWeight3 = meshVertexA.matrices[j].weight;
                            }
                        }

                        OpenCLRenderer.Vertex vertexB = new OpenCLRenderer.Vertex();
                        vertexB.m_Vx = vB.X;
                        vertexB.m_Vy = vB.Y;
                        vertexB.m_Vz = vB.Z;
                        vertexB.m_Nx = nB.X;
                        vertexB.m_Ny = nB.Y;
                        vertexB.m_Nz = nB.Z;
                        vertexB.m_TCx = tB.X;
                        vertexB.m_TCy = tB.Y;
                        vertexB.m_iNumMatrices = meshVertexB.matrices.Count;
                        for (int j = 0; j < vertexB.m_iNumMatrices; j++)
                        {
                            if (j == 0)
                            {
                                vertexB.m_iMatrixId1 = iMatrixOffset + meshVertexB.matrices[j].matrix_id;
                                vertexB.m_fWeight1 = meshVertexB.matrices[j].weight;
                            }
                            if (j == 1)
                            {
                                vertexB.m_iMatrixId2 = iMatrixOffset + meshVertexB.matrices[j].matrix_id;
                                vertexB.m_fWeight2 = meshVertexB.matrices[j].weight;
                            }
                            if (j == 2)
                            {
                                vertexB.m_iMatrixId3 = iMatrixOffset + meshVertexB.matrices[j].matrix_id;
                                vertexB.m_fWeight3 = meshVertexB.matrices[j].weight;
                            }
                        }

                        OpenCLRenderer.Vertex vertexC = new OpenCLRenderer.Vertex();
                        vertexC.m_Vx = vC.X;
                        vertexC.m_Vy = vC.Y;
                        vertexC.m_Vz = vC.Z;
                        vertexC.m_Nx = nC.X;
                        vertexC.m_Ny = nC.Y;
                        vertexC.m_Nz = nC.Z;
                        vertexC.m_TCx = tC.X;
                        vertexC.m_TCy = tC.Y;
                        vertexC.m_iNumMatrices = meshVertexC.matrices.Count;
                        for (int j = 0; j < vertexC.m_iNumMatrices; j++)
                        {
                            if (j == 0)
                            {
                                vertexC.m_iMatrixId1 = iMatrixOffset + meshVertexC.matrices[j].matrix_id;
                                vertexC.m_fWeight1 = meshVertexC.matrices[j].weight;
                            }
                            if (j == 1)
                            {
                                vertexC.m_iMatrixId2 = iMatrixOffset + meshVertexC.matrices[j].matrix_id;
                                vertexC.m_fWeight2 = meshVertexC.matrices[j].weight;
                            }
                            if (j == 2)
                            {
                                vertexC.m_iMatrixId3 = iMatrixOffset + meshVertexC.matrices[j].matrix_id;
                                vertexC.m_fWeight3 = meshVertexC.matrices[j].weight;
                            }
                        }

                        OpenCLRenderer.Triangle newTriangle = new OpenCLRenderer.Triangle();
                        newTriangle.m_A = vertexA;
                        newTriangle.m_B = vertexB;
                        newTriangle.m_C = vertexC;
                        newTriangle.m_iMaterialId = iMaterialId;
                        
                        triangles.Add(newTriangle);
                    }
                }

                //int iId;
                iId = m_Scene.GenObject();
                OpenCLRenderer.BVHObject dynamicObject = m_Scene.CreateDynamicObject(triangles);
                m_Scene.SetObject(iId, dynamicObject);
                triangles.Clear();
            }
            
            m_Scene.Commit();

            Image_SizeChanged(null, null);

            m_Timer = new DispatcherTimer();
            m_Timer.Tick += Timer_Tick;
            m_Timer.Interval = TimeSpan.FromMilliseconds(0);
            
            m_ElapsedTime = m_CurrentTime = DateTime.Now;
            m_fSec = 0.0f;
            m_fFullTime = 0.0f;
            m_Timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // delta time
            m_ElapsedTime = m_CurrentTime;
            m_CurrentTime = DateTime.Now;
            m_fDeltaTime = (float)(m_CurrentTime - m_ElapsedTime).TotalSeconds;
            

            // FPS
            FPS++;
            m_fSec += m_fDeltaTime;
            if (m_fSec >= 1.0f)
            {
                this.Title = "FPS: " + FPS;
                m_fSec = 0.0f;
                FPS = 0;
            }

            // smd update
            time += m_fDeltaTime;
            while (time >= smd.GetFullTime())
            {
                time -= smd.GetFullTime();
            }
            smd.SetTime(time, mesh);
            for (int i = 0; i < mesh.transforms.Count; i++)
            {
                m_Scene.SetMatrix(iMatrixOffset + i, mesh.transforms[i] * Matrix4.CreateRotationX(-1.57f) * Matrix4.CreateRotationY(3.14f));
            }

            m_Scene.UpdateMatrices();
            m_Scene.RunTriangleShader();
            m_Scene.RunRefitTreeShader();

            m_fFullTime += m_fDeltaTime;
            float fSpeed = 0.5f;
            m_Scene.SetCamera(new Vector3(-45.0f * (float)Math.Cos(m_fFullTime * fSpeed), 30, -45.0f * (float)Math.Sin(m_fFullTime * fSpeed)), new Vector3(0, 5, 0), new Vector3(0, 1, 0), (float)Math.PI / 4.0f, 1000.0f);
            m_Scene.RunRayShader(127, 127, 255, 255);

            image.Source = m_Scene.GetWriteableBitmap();
        }

        OpenCLRenderer.Scene m_Scene = null;
        DispatcherTimer m_Timer = null;
        int FPS = 0;
        DateTime m_ElapsedTime;
        DateTime m_CurrentTime;
        float m_fDeltaTime;
        float m_fSec;
        float m_fFullTime;

        SMDLoader smd;
        Mesh mesh;
        float time = 0.0f;
        int iMatrixOffset;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != m_Timer)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            if (null != m_Scene)
            {
                m_Scene.Dispose();
                m_Scene = null;
            }
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (null == m_Scene) { return; }

            int iWidth = (int)image.ActualWidth;
            int iHeight = (int)image.ActualHeight;

            if (iWidth == 0 || iHeight == 0) { return; }
            m_Scene.Resize(iWidth, iHeight);
        }

        
    }
}
