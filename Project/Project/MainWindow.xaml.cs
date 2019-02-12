using GlmSharp;
using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Mutex mtxMutex = new Mutex();

            Parallel.For(0, 1, index =>
            {
                // load from obj file
                string strDirectory = @".\";
                OBJLoader objLoader = new OBJLoader();

                mtxMutex.WaitOne();
                objLoader.LoadFromFile(@strDirectory, @"Model.obj");
                mtxMutex.ReleaseMutex();

                // convert to triangle list
                int iMatrixId = m_Scene.GenMatrix();
                m_Scene.SetMatrix(iMatrixId, mat4.Translate(new vec3(100,200,300)));

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
                        vec3 vA = objLoader.vertices[material.indices[i + 0].id_vertex];
                        vec3 vB = objLoader.vertices[material.indices[i + 1].id_vertex];
                        vec3 vC = objLoader.vertices[material.indices[i + 2].id_vertex];

                        vec3 nA = objLoader.normals[material.indices[i + 0].id_normal];
                        vec3 nB = objLoader.normals[material.indices[i + 1].id_normal];
                        vec3 nC = objLoader.normals[material.indices[i + 2].id_normal];

                        vec2 tA = objLoader.text_coords[material.indices[i + 0].id_textcoord];
                        vec2 tB = objLoader.text_coords[material.indices[i + 1].id_textcoord];
                        vec2 tC = objLoader.text_coords[material.indices[i + 2].id_textcoord];

                        OpenCLRenderer.Vertex vertexA = new OpenCLRenderer.Vertex();
                        vertexA.m_Vx = vA.x;
                        vertexA.m_Vy = vA.y;
                        vertexA.m_Vz = vA.z;
                        vertexA.m_Nx = nA.x;
                        vertexA.m_Ny = nA.y;
                        vertexA.m_Nz = nA.z;
                        vertexA.m_TCx = tA.x;
                        vertexA.m_TCy = tA.y;
                        vertexA.m_iNumMatrices = 1;
                        vertexA.m_iMatrixId1 = iMatrixId;
                        vertexA.m_fWeight1 = 1.0f;
                        vertexA.m_iMatrixId2 = -1;
                        vertexA.m_fWeight2 = 0.0f;
                        vertexA.m_iMatrixId3 = -1;
                        vertexA.m_fWeight3 = 0.0f;

                        OpenCLRenderer.Vertex vertexB = new OpenCLRenderer.Vertex();
                        vertexB.m_Vx = vB.x;
                        vertexB.m_Vy = vB.y;
                        vertexB.m_Vz = vB.z;
                        vertexB.m_Nx = nB.x;
                        vertexB.m_Ny = nB.y;
                        vertexB.m_Nz = nB.z;
                        vertexB.m_TCx = tB.x;
                        vertexB.m_TCy = tB.y;
                        vertexB.m_iNumMatrices = 1;
                        vertexB.m_iMatrixId1 = iMatrixId;
                        vertexB.m_fWeight1 = 1.0f;
                        vertexB.m_iMatrixId2 = -1;
                        vertexB.m_fWeight2 = 0.0f;
                        vertexB.m_iMatrixId3 = -1;
                        vertexB.m_fWeight3 = 0.0f;

                        OpenCLRenderer.Vertex vertexC = new OpenCLRenderer.Vertex();
                        vertexC.m_Vx = vC.x;
                        vertexC.m_Vy = vC.y;
                        vertexC.m_Vz = vC.z;
                        vertexC.m_Nx = nC.x;
                        vertexC.m_Ny = nC.y;
                        vertexC.m_Nz = nC.z;
                        vertexC.m_TCx = tC.x;
                        vertexC.m_TCy = tC.y;
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
                //iId = m_Scene.GenObject();
                //OpenCLRenderer.BVHObject staticObject = m_Scene.CreateStaticObject(triangles, mat4.Identity);
                //m_Scene.SetObject(iId, staticObject);

                iId = m_Scene.GenObject();
                OpenCLRenderer.BVHObject dynamicObject = m_Scene.CreateDynamicObject(triangles);
                m_Scene.SetObject(iId, dynamicObject);

                triangles.Clear();
                objLoader.Release();
            });
            
            m_Scene.Commit();

            m_Timer = new DispatcherTimer();
            m_Timer.Tick += Timer_Tick;
            m_Timer.Interval = TimeSpan.FromMilliseconds(0);

            m_ElapsedTime = m_CurrentTime = DateTime.Now;
            m_fSec = 0.0f;
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
                this.Title = FPS + " FPS";
                m_fSec = 0.0f;
                FPS = 0;
            }

            m_Scene.RunVertexShader();
            m_Scene.RunRefitTree();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (null != m_Timer)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        OpenCLRenderer.Scene m_Scene = null;

        DispatcherTimer m_Timer = null;
        int FPS = 0;
        DateTime m_ElapsedTime;
        DateTime m_CurrentTime;
        float m_fDeltaTime;
        float m_fSec;

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int iWidth = (int)(sender as Grid).ActualWidth;
            int iHeight = (int)(sender as Grid).ActualHeight;
            m_Scene.Resize(iWidth, iHeight);
        }
    }
}
