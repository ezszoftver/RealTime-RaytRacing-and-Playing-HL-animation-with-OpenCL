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

            Mutex mtxMutex = new Mutex();

            Parallel.For(0, 100, index =>
            {
                // load from obj file
                string strDirectory = @".\";
                OBJLoader objLoader = new OBJLoader();

                mtxMutex.WaitOne();
                objLoader.LoadFromFile(@strDirectory, @"Model.obj");
                mtxMutex.ReleaseMutex();

                // convert to triangle list
                Int32 iMatrixId = m_Scene.GenMatrix();
                m_Scene.SetMatrix(iMatrixId, mat4.Identity);

                List<OpenCLRenderer.Triangle> triangles = new List<OpenCLRenderer.Triangle>();
                foreach (OBJLoader.Material material in objLoader.materials)
                {
                    string strDiffuseTextureName = @strDirectory + @material.texture_filename;
                    string strSpecularTextureName = @strDirectory + @"Specular.bmp";
                    string strNormalTextureName = @strDirectory + @"Normal.bmp";

                    Int32 iMaterialId = m_Scene.GenMaterial();
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
                        vertexA.m_V = vA;
                        vertexA.m_N = nA;
                        vertexA.m_TC = tA;
                        vertexA.m_iNumMatrices = 1;
                        vertexA.m_iMatrixId1 = iMatrixId;
                        vertexA.m_fWeight1 = 1.0f;
                        vertexA.m_iMatrixId2 = -1;
                        vertexA.m_fWeight2 = 0.0f;
                        vertexA.m_iMatrixId3 = -1;
                        vertexA.m_fWeight3 = 0.0f;

                        OpenCLRenderer.Vertex vertexB = new OpenCLRenderer.Vertex();
                        vertexB.m_V = vB;
                        vertexB.m_N = nB;
                        vertexB.m_TC = tB;
                        vertexB.m_iNumMatrices = 1;
                        vertexB.m_iMatrixId1 = iMatrixId;
                        vertexB.m_fWeight1 = 1.0f;
                        vertexB.m_iMatrixId2 = -1;
                        vertexB.m_fWeight2 = 0.0f;
                        vertexB.m_iMatrixId3 = -1;
                        vertexB.m_fWeight3 = 0.0f;

                        OpenCLRenderer.Vertex vertexC = new OpenCLRenderer.Vertex();
                        vertexC.m_V = vC;
                        vertexC.m_N = nC;
                        vertexC.m_TC = tC;
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

                m_Scene.CreateStaticObject(triangles, mat4.Identity);
                m_Scene.CreateDynamicObject(triangles, iMatrixId);
                m_Scene.CreateAnimatedObject(triangles);

                triangles.Clear();
                objLoader.Release();
            });
            
            m_Scene.Commit();
        }

        OpenCLRenderer.Scene m_Scene = null;
    }
}
