using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

namespace Project
{
    class Mesh
    {
        public class MatrixIdAndWeight
        {
            public MatrixIdAndWeight(int matrix_id, float weight)
            {
                this.weight = weight;
                this.matrix_id = matrix_id;
            }

            public int matrix_id;
            public float weight;
        }

        public class Vertex
        {
            public Vertex(Vector3 vertex, Vector3 normal, Vector2 textcoords)
            {
                this.vertex = vertex;
                this.normal = normal;
                this.textcoords = textcoords;
                this.matrices = new List<MatrixIdAndWeight>();
            }

            public Vertex(Vertex b)
            {
                vertex = new Vector3(b.vertex.X, b.vertex.Y, b.vertex.Z);
                normal = new Vector3(b.normal.X, b.normal.Y, b.normal.Z);
                textcoords = new Vector2(b.textcoords.X, b.textcoords.Y);
                matrices = new List<MatrixIdAndWeight>();
                foreach (MatrixIdAndWeight b_matrix in b.matrices)
                {
                    MatrixIdAndWeight matrix = new MatrixIdAndWeight(b_matrix.matrix_id, b_matrix.weight);
                    matrices.Add(matrix);
                }
            }

            public void AddMatrix(int matrix_id, float weight)
            {
                matrices.Add(new MatrixIdAndWeight(matrix_id, weight));
            }

            public int GetMaxWeightID()
            {
                int matrix_id = 0; float max_weight = 0.0f;

                for (int i = 0; i < matrices.Count; i++)
                {
                    if (matrices[i].weight > max_weight)
                    {
                        max_weight = matrices[i].weight;
                        matrix_id = matrices[i].matrix_id;
                    }
                }

                return matrix_id;
            }

            public Vector3 vertex;
            public Vector3 normal;
            public Vector2 textcoords;
            public List<MatrixIdAndWeight> matrices;
        }

        public class Material
        {
            public string texture_name;

            public List<Vertex> vertices;

            public Material(string texture_name)
            {
                vertices = new List<Vertex>();
                this.texture_name = texture_name;
            }
        }

        public List<Matrix4> inverse_transforms_reference;
        public List<Matrix4> transforms;
        public List<Material>  materials;
        public Vector3 min, max;
        public bool is_loaded;

        public string mtllib;
        public bool is_obj;
        public bool is_hl1;
        public bool is_hl2;

        public Mesh()
        {
            is_loaded = false;
            transforms = new List<Matrix4>();
            inverse_transforms_reference = new List<Matrix4>();
            materials = new List<Material>();
            min = new Vector3(0, 0, 0);
            max = new Vector3(0, 0, 0);
            mtllib = "";
            is_obj = false;
            is_hl1 = false;
            is_hl2 = false;
        }

        public Mesh(Mesh b)
        {
            is_loaded = b.is_loaded;
            mtllib = b.mtllib;
            is_obj = b.is_obj;
            is_hl1 = b.is_hl1;
            is_hl2 = b.is_hl2;

            transforms = new List<Matrix4>();
            foreach (Matrix4 b_m in b.transforms)
            {
                Matrix4 m = new Matrix4(b_m.Row0, b_m.Row1, b_m.Row2, b_m.Row3);
                transforms.Add(m);
            }

            inverse_transforms_reference = new List<Matrix4>();
            foreach (Matrix4 b_m in b.inverse_transforms_reference)
            {
                Matrix4 m = new Matrix4(b_m.Row0, b_m.Row1, b_m.Row2, b_m.Row3);
                inverse_transforms_reference.Add(m);
            }

            materials = new List<Material>();
            min = new Vector3(+1000000.0f, +1000000.0f, +1000000.0f);
            max = new Vector3(-1000000.0f, -1000000.0f, -1000000.0f);

            foreach (Material b_mat in b.materials)
            {
                Material mat = new Material(b_mat.texture_name);

                foreach (Vertex b_v in b_mat.vertices)
                {
                    Vertex v = new Vertex(b_v);

                    // min
                    if (v.vertex.X < min.X) { min.X = v.vertex.X; }
                    if (v.vertex.Y < min.Y) { min.Y = v.vertex.Y; }
                    if (v.vertex.Z < min.Z) { min.Z = v.vertex.Z; }
                    // max
                    if (max.X < v.vertex.X) { max.X = v.vertex.X; }
                    if (max.Y < v.vertex.Y) { max.Y = v.vertex.Y; }
                    if (max.Z < v.vertex.Z) { max.Z = v.vertex.Z; }

                    mat.vertices.Add(v);
                }

                materials.Add(mat);
            }
        }

        public int GetVerticesCount()
        {
            int count = 0;

            foreach (Material material in materials)
            {
                foreach (Vertex vertex in material.vertices)
                {
                    if (vertex == null) continue;
                    count++;
                }
            }

            return count;
        }

        public void Release(bool is_delete_textures)
        {
            foreach (Material material in materials)
            { 
                foreach (Vertex vertex in material.vertices)
                {
                    if (vertex == null) { continue; }

                    if (vertex.matrices != null)
                    {
                        vertex.matrices.Clear();
                        vertex.matrices = null;
                    }
                }

                material.vertices.Clear();
                material.vertices = null;
            }

            transforms.Clear();
            transforms = null;

            if (is_delete_textures) { materials.Clear(); }
        }
    }
}
