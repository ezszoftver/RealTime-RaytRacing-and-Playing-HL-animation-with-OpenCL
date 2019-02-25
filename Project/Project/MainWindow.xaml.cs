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
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            m_Scene = new OpenCLRenderer.Scene();
            m_Scene.CreateDevice();

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
                m_Scene.SetMatrix(iMatrixId, Matrix4.CreateTranslation(new Vector3(100,200,300)));
            
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
                //iId = m_Scene.GenObject();
                //OpenCLRenderer.BVHObject staticObject = m_Scene.CreateStaticObject(triangles, Matrix4.CreateTranslation(new Vector3(100, 200, 300)));
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
            m_Scene.RunRefitTreeShader();
            m_Scene.SetCamera(new Vector3(0, 0, 10), new Vector3(0, 0, 0), new Vector3(0, 1, 0), (float)Math.PI / 4.0f, 100.0f);
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

            m_Scene.Resize(iWidth, iHeight);
        }
    }
}
