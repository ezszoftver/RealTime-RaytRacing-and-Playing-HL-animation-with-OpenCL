using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCLRenderer
{
    public static class OpenCLScript
    {
        public static string GetText()
        {
            return
@"
#pragma OPENCL EXTENSION cl_khr_fp16 : enable

typedef struct
{
    float4 row0;
    float4 row1;
    float4 row2;
    float4 row3;
    
    float4 column0;
    float4 column1;
    float4 column2;
    float4 column3;
}
Matrix4x4;

float3 ToFloat3(float x, float y, float z)
{
    float3 ret;
    ret.x = x;
    ret.y = y;
    ret.z = z;
    return ret;
}

float4 ToFloat4(float x, float y, float z, float w)
{
    float4 ret;
    ret.x = x;
    ret.y = y;
    ret.z = z;
    ret.w = w;
    return ret;
}

float4 Mult_Matrix4x4Float3(Matrix4x4 T, float3 v, float w)
{
    float4 ret;

    float4 v1 = ToFloat4(v.x, v.y, v.z, w);

    ret.x = dot(T.row0, v1);
    ret.y = dot(T.row1, v1);
    ret.z = dot(T.row2, v1);
    ret.w = dot(T.row3, v1);

    return ret;
}

float4 Mult_Matrix4x4Float4(Matrix4x4 T, float4 v)
{
    float4 ret;

    ret.x = dot(T.row0, v);
    ret.y = dot(T.row1, v);
    ret.z = dot(T.row2, v);
    ret.w = dot(T.row3, v);

    return ret;
}

Matrix4x4 Mult_Matrix4x4Float(Matrix4x4 T, float scale)
{
    Matrix4x4 ret;

    ret.row0 = T.row0;
    ret.row1 = T.row1;
    ret.row2 = T.row2;
    ret.row3 = T.row3;

    ret.column0 = T.column0;
    ret.column1 = T.column1;
    ret.column2 = T.column2;
    ret.column3 = T.column3;

    ret.row0.x *= scale;
    ret.row1.y *= scale;
    ret.row2.z *= scale;

    ret.column0.x *= scale;
    ret.column1.y *= scale;
    ret.column2.z *= scale;

    return ret;
}

Matrix4x4 Mult_Matrix4x4Matrix4x4(Matrix4x4 T2, Matrix4x4 T1)
{
    Matrix4x4 ret;

    float m00 = dot(T1.row0, T2.column0);
    float m01 = dot(T1.row0, T2.column1);
    float m02 = dot(T1.row0, T2.column2);
    float m03 = dot(T1.row0, T2.column3);
    
    float m10 = dot(T1.row1, T2.column0);
    float m11 = dot(T1.row1, T2.column1);
    float m12 = dot(T1.row1, T2.column2);
    float m13 = dot(T1.row1, T2.column3);
    
    float m20 = dot(T1.row2, T2.column0);
    float m21 = dot(T1.row2, T2.column1);
    float m22 = dot(T1.row2, T2.column2);
    float m23 = dot(T1.row2, T2.column3);
    
    float m30 = dot(T1.row3, T2.column0);
    float m31 = dot(T1.row3, T2.column1);
    float m32 = dot(T1.row3, T2.column2);
    float m33 = dot(T1.row3, T2.column3);

    ret.row0.x = m00;
    ret.row0.y = m01;
    ret.row0.z = m02;
    ret.row0.w = m03;

    ret.row1.x = m10;
    ret.row1.y = m11;
    ret.row1.z = m12;
    ret.row1.w = m13;

    ret.row2.x = m20;
    ret.row2.y = m21;
    ret.row2.z = m22;
    ret.row2.w = m23;

    ret.row3.x = m30;
    ret.row3.y = m31;
    ret.row3.z = m32;
    ret.row3.w = m33;

    ret.column0 = m00;
    ret.column0 = m10;
    ret.column0 = m20;
    ret.column0 = m30;

    ret.column1 = m01;
    ret.column1 = m11;
    ret.column1 = m21;
    ret.column1 = m31;

    ret.column2 = m02;
    ret.column2 = m12;
    ret.column2 = m22;
    ret.column2 = m32;

    ret.column3 = m03;
    ret.column3 = m13;
    ret.column3 = m23;
    ret.column3 = m33;

    return ret;
}

Matrix4x4 Inverse_Matrix4x4(Matrix4x4 T)
{
    float m[16] = 
        {  
            T.row0.x, T.row0.y, T.row0.z, T.row0.w,
            T.row1.x, T.row1.y, T.row1.z, T.row1.w,
            T.row2.x, T.row2.y, T.row2.z, T.row2.w,
            T.row3.x, T.row3.y, T.row3.z, T.row3.w,
        };

    float inv[16], det;
    int i;

    inv[0] = m[5]  * m[10] * m[15] - 
             m[5]  * m[11] * m[14] - 
             m[9]  * m[6]  * m[15] + 
             m[9]  * m[7]  * m[14] +
             m[13] * m[6]  * m[11] - 
             m[13] * m[7]  * m[10];

    inv[4] = -m[4]  * m[10] * m[15] + 
              m[4]  * m[11] * m[14] + 
              m[8]  * m[6]  * m[15] - 
              m[8]  * m[7]  * m[14] - 
              m[12] * m[6]  * m[11] + 
              m[12] * m[7]  * m[10];

    inv[8] = m[4]  * m[9] * m[15] - 
             m[4]  * m[11] * m[13] - 
             m[8]  * m[5] * m[15] + 
             m[8]  * m[7] * m[13] + 
             m[12] * m[5] * m[11] - 
             m[12] * m[7] * m[9];

    inv[12] = -m[4]  * m[9] * m[14] + 
               m[4]  * m[10] * m[13] +
               m[8]  * m[5] * m[14] - 
               m[8]  * m[6] * m[13] - 
               m[12] * m[5] * m[10] + 
               m[12] * m[6] * m[9];

    inv[1] = -m[1]  * m[10] * m[15] + 
              m[1]  * m[11] * m[14] + 
              m[9]  * m[2] * m[15] - 
              m[9]  * m[3] * m[14] - 
              m[13] * m[2] * m[11] + 
              m[13] * m[3] * m[10];

    inv[5] = m[0]  * m[10] * m[15] - 
             m[0]  * m[11] * m[14] - 
             m[8]  * m[2] * m[15] + 
             m[8]  * m[3] * m[14] + 
             m[12] * m[2] * m[11] - 
             m[12] * m[3] * m[10];

    inv[9] = -m[0]  * m[9] * m[15] + 
              m[0]  * m[11] * m[13] + 
              m[8]  * m[1] * m[15] - 
              m[8]  * m[3] * m[13] - 
              m[12] * m[1] * m[11] + 
              m[12] * m[3] * m[9];

    inv[13] = m[0]  * m[9] * m[14] - 
              m[0]  * m[10] * m[13] - 
              m[8]  * m[1] * m[14] + 
              m[8]  * m[2] * m[13] + 
              m[12] * m[1] * m[10] - 
              m[12] * m[2] * m[9];

    inv[2] = m[1]  * m[6] * m[15] - 
             m[1]  * m[7] * m[14] - 
             m[5]  * m[2] * m[15] + 
             m[5]  * m[3] * m[14] + 
             m[13] * m[2] * m[7] - 
             m[13] * m[3] * m[6];

    inv[6] = -m[0]  * m[6] * m[15] + 
              m[0]  * m[7] * m[14] + 
              m[4]  * m[2] * m[15] - 
              m[4]  * m[3] * m[14] - 
              m[12] * m[2] * m[7] + 
              m[12] * m[3] * m[6];

    inv[10] = m[0]  * m[5] * m[15] - 
              m[0]  * m[7] * m[13] - 
              m[4]  * m[1] * m[15] + 
              m[4]  * m[3] * m[13] + 
              m[12] * m[1] * m[7] - 
              m[12] * m[3] * m[5];

    inv[14] = -m[0]  * m[5] * m[14] + 
               m[0]  * m[6] * m[13] + 
               m[4]  * m[1] * m[14] - 
               m[4]  * m[2] * m[13] - 
               m[12] * m[1] * m[6] + 
               m[12] * m[2] * m[5];

    inv[3] = -m[1] * m[6] * m[11] + 
              m[1] * m[7] * m[10] + 
              m[5] * m[2] * m[11] - 
              m[5] * m[3] * m[10] - 
              m[9] * m[2] * m[7] + 
              m[9] * m[3] * m[6];

    inv[7] = m[0] * m[6] * m[11] - 
             m[0] * m[7] * m[10] - 
             m[4] * m[2] * m[11] + 
             m[4] * m[3] * m[10] + 
             m[8] * m[2] * m[7] - 
             m[8] * m[3] * m[6];

    inv[11] = -m[0] * m[5] * m[11] + 
               m[0] * m[7] * m[9] + 
               m[4] * m[1] * m[11] - 
               m[4] * m[3] * m[9] - 
               m[8] * m[1] * m[7] + 
               m[8] * m[3] * m[5];

    inv[15] = m[0] * m[5] * m[10] - 
              m[0] * m[6] * m[9] - 
              m[4] * m[1] * m[10] + 
              m[4] * m[2] * m[9] + 
              m[8] * m[1] * m[6] - 
              m[8] * m[2] * m[5];

    det = m[0] * inv[0] + m[1] * inv[4] + m[2] * inv[8] + m[3] * inv[12];

    det = 1.0 / det;

    float invOut[16];
    for (i = 0; i < 16; i++)
        invOut[i] = inv[i] * det;

    float m00 = invOut[0];  float m01 = invOut[1];  float m02 = invOut[2];  float m03 = invOut[3];
    float m10 = invOut[4];  float m11 = invOut[5];  float m12 = invOut[6];  float m13 = invOut[7];
    float m20 = invOut[8];  float m21 = invOut[9];  float m22 = invOut[10]; float m23 = invOut[11];
    float m30 = invOut[12]; float m31 = invOut[13]; float m32 = invOut[14]; float m33 = invOut[15];

    Matrix4x4 ret;

    ret.row0.x = m00;
    ret.row0.y = m01;
    ret.row0.z = m02;
    ret.row0.w = m03;

    ret.row1.x = m10;
    ret.row1.y = m11;
    ret.row1.z = m12;
    ret.row1.w = m13;

    ret.row2.x = m20;
    ret.row2.y = m21;
    ret.row2.z = m22;
    ret.row2.w = m23;

    ret.row3.x = m30;
    ret.row3.y = m31;
    ret.row3.z = m32;
    ret.row3.w = m33;

    ret.column0 = m00;
    ret.column0 = m10;
    ret.column0 = m20;
    ret.column0 = m30;

    ret.column1 = m01;
    ret.column1 = m11;
    ret.column1 = m21;
    ret.column1 = m31;

    ret.column2 = m02;
    ret.column2 = m12;
    ret.column2 = m22;
    ret.column2 = m32;

    ret.column3 = m03;
    ret.column3 = m13;
    ret.column3 = m23;
    ret.column3 = m33;

    return ret;
}

typedef struct
{
    float posx;
    float posy;
    float posz;
    float dirx;
    float diry;
    float dirz;
    float length;
}
Ray;

typedef struct
{
    float x;
    float y;
    float z;
}
Vector3;

typedef struct
{
    float vx;
    float vy;
    float vz;
    float nx;
    float ny;
    float nz;
    float tx;
    float ty;
    int numMatrices;
    int matrixId1;
    int matrixId2;
    int matrixId3;
    float weight1;
    float weight2;
    float weight3;
}
Vertex;

typedef struct 
{
    Vertex a;
    Vertex b;
    Vertex c;
    int materialId;
}
Triangle;

typedef struct
{
    float minx;
    float miny;
    float minz;
    float maxx;
    float maxy;
    float maxz;
}
BBox;

typedef struct
{
    int id;
    Triangle triangle;
    BBox bbox;
    int left;
    int right;
}
BVHNode;

#define Static  1
#define Dynamic 2

typedef struct 
{
    int type;
}
BVHNodeType;

typedef struct 
{
    int offset;
    int count;
}
BVHNodeOffset;

BBox GenBBox_Tri(Triangle tri)
{
    float fMinX = +10000000.0f;
    float fMinY = +10000000.0f;
    float fMinZ = +10000000.0f;

    fMinX = min(fMinX, tri.a.vx);
    fMinX = min(fMinX, tri.b.vx);
    fMinX = min(fMinX, tri.c.vx);

    fMinY = min(fMinY, tri.a.vy);
    fMinY = min(fMinY, tri.b.vy);
    fMinY = min(fMinY, tri.c.vy);
    
    fMinZ = min(fMinZ, tri.a.vz);
    fMinZ = min(fMinZ, tri.b.vz);
    fMinZ = min(fMinZ, tri.c.vz);
    
    float fMaxX = -10000000.0f;
    float fMaxY = -10000000.0f;
    float fMaxZ = -10000000.0f;

    fMaxX = max(fMaxX, tri.a.vx);
    fMaxX = max(fMaxX, tri.b.vx);
    fMaxX = max(fMaxX, tri.c.vx);
    
    fMaxY = max(fMaxY, tri.a.vy);
    fMaxY = max(fMaxY, tri.b.vy);
    fMaxY = max(fMaxY, tri.c.vy);
    
    fMaxZ = max(fMaxZ, tri.a.vz);
    fMaxZ = max(fMaxZ, tri.b.vz);
    fMaxZ = max(fMaxZ, tri.c.vz);
    
    BBox bbox;
    bbox.minx = fMinX;
    bbox.miny = fMinY;
    bbox.minz = fMinZ;
    bbox.maxx = fMaxX;
    bbox.maxy = fMaxY;
    bbox.maxz = fMaxZ;

    return bbox;
}

BBox GenBBox_BBoxBBox(BBox bbox1, BBox bbox2)
{
    float fMinX = +10000000.0f;
    float fMinY = +10000000.0f;
    float fMinZ = +10000000.0f;

    fMinX = min(fMinX, bbox1.minx);
    fMinX = min(fMinX, bbox1.maxx);
    fMinX = min(fMinX, bbox2.minx);
    fMinX = min(fMinX, bbox2.maxx);

    fMinY = min(fMinY, bbox1.miny);
    fMinY = min(fMinY, bbox1.maxy);
    fMinY = min(fMinY, bbox2.miny);
    fMinY = min(fMinY, bbox2.maxy);

    fMinZ = min(fMinZ, bbox1.minz);
    fMinZ = min(fMinZ, bbox1.maxz);
    fMinZ = min(fMinZ, bbox2.minz);
    fMinZ = min(fMinZ, bbox2.maxz);

    float fMaxX = -10000000.0f;
    float fMaxY = -10000000.0f;
    float fMaxZ = -10000000.0f;

    fMaxX = max(fMaxX, bbox1.minx);
    fMaxX = max(fMaxX, bbox1.maxx);
    fMaxX = max(fMaxX, bbox2.minx);
    fMaxX = max(fMaxX, bbox2.maxx);

    fMaxY = max(fMaxY, bbox1.miny);
    fMaxY = max(fMaxY, bbox1.maxy);
    fMaxY = max(fMaxY, bbox2.miny);
    fMaxY = max(fMaxY, bbox2.maxy);

    fMaxZ = max(fMaxZ, bbox1.minz);
    fMaxZ = max(fMaxZ, bbox1.maxz);
    fMaxZ = max(fMaxZ, bbox2.minz);
    fMaxZ = max(fMaxZ, bbox2.maxz);

    BBox bbox;
    bbox.minx = fMinX;
    bbox.miny = fMinY;
    bbox.minz = fMinZ;
    bbox.maxx = fMaxX;
    bbox.maxy = fMaxY;
    bbox.maxz = fMaxZ;

    return bbox;
}

Vertex VertexShader(Vertex in, __global Matrix4x4 *in_Matrices)
{
    Vertex out;
    
    out.tx          = in.tx;
    out.ty          = in.ty;
    out.numMatrices = in.numMatrices;
    out.matrixId1   = in.matrixId1;
    out.matrixId2   = in.matrixId2;
    out.matrixId3   = in.matrixId3;
    out.weight1     = in.weight1;
    out.weight2     = in.weight2;
    out.weight3     = in.weight3;

    if (in.numMatrices == 0) 
    {
        out = in;
    }
    else if (in.numMatrices == 1) 
    {
        float4 v = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId1], in.weight1), ToFloat4(in.vx, in.vy, in.vz, 1.0f));
        out.vx = v.x;
        out.vy = v.y;
        out.vz = v.z;

        float4 n = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId1], in.weight1), ToFloat4(in.nx, in.ny, in.nz, 0.0f));
        out.nx = n.x;
        out.ny = n.y;
        out.nz = n.z;
    }
    else if (in.numMatrices == 2)
    {
        float4 v1 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId1], in.weight1), ToFloat4(in.vx, in.vy, in.vz, 1.0f));
        float4 v2 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId2], in.weight2), ToFloat4(in.vx, in.vy, in.vz, 1.0f));
        out.vx = v1.x + v2.x;
        out.vy = v1.y + v2.y;
        out.vz = v1.z + v2.z;
    
        float4 n1 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId1], in.weight1), ToFloat4(in.nx, in.ny, in.nz, 0.0f));
        float4 n2 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId2], in.weight2), ToFloat4(in.nx, in.ny, in.nz, 0.0f));
        out.nx = n1.x + n2.x;
        out.ny = n1.y + n2.y;
        out.nz = n1.z + n2.z;
    }
    else if (in.numMatrices == 3)
    {
        float4 v1 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId1], in.weight1), ToFloat4(in.vx, in.vy, in.vz, 1.0f)); 
        float4 v2 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId2], in.weight2), ToFloat4(in.vx, in.vy, in.vz, 1.0f));
        float4 v3 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId3], in.weight3), ToFloat4(in.vx, in.vy, in.vz, 1.0f));
        out.vx = v1.x + v2.x + v3.x;
        out.vy = v1.y + v2.y + v3.y;
        out.vx = v1.z + v2.z + v3.z;
        
        float4 n1 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId1], in.weight1), ToFloat4(in.nx, in.ny, in.nz, 0.0f));
        float4 n2 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId2], in.weight2), ToFloat4(in.nx, in.ny, in.nz, 0.0f));
        float4 n3 = Mult_Matrix4x4Float4(Mult_Matrix4x4Float(in_Matrices[in.matrixId3], in.weight3), ToFloat4(in.nx, in.ny, in.nz, 0.0f));
        out.nx = n1.x + n2.x + n3.x;
        out.ny = n1.y + n2.y + n3.y;
        out.nz = n1.z + n2.z + n3.z;
    }

    return out;
}

__kernel void Main_VertexShader(__global BVHNodeType *in_BVHNodeTypes, __global BVHNode *in_BVHNodes, __global Matrix4x4 *in_Matrices, __global BVHNode *inout_BVHNodes)
{
    int id = get_global_id(0);

    BVHNodeType bvhNodeType = in_BVHNodeTypes[id];
    BVHNode inBVHNode = in_BVHNodes[id];
    BVHNode outBVHNode;
    
    if (Static == bvhNodeType.type) 
    {
        outBVHNode = inBVHNode;
    }
    else if (Dynamic == bvhNodeType.type)
    {
        if (-1 == inBVHNode.left && -1 == inBVHNode.right)
        {
            outBVHNode = inBVHNode;
            outBVHNode.triangle.a = VertexShader(inBVHNode.triangle.a, in_Matrices);
            outBVHNode.triangle.b = VertexShader(inBVHNode.triangle.b, in_Matrices);
            outBVHNode.triangle.c = VertexShader(inBVHNode.triangle.c, in_Matrices);

            // level 1
            outBVHNode.bbox = GenBBox_Tri(outBVHNode.triangle);
        }
        else
        {
            outBVHNode = inBVHNode;

            // level 1
            outBVHNode.bbox.minx = 0;
            outBVHNode.bbox.miny = 0;
            outBVHNode.bbox.minz = 0;
            outBVHNode.bbox.maxx = 0;
            outBVHNode.bbox.maxy = 0;
            outBVHNode.bbox.maxz = 0;
        }
    }

    inout_BVHNodes[id] = outBVHNode;
}

__kernel void Main_RefitTree_LevelX(__global BVHNode *in_BVHNodes, __global BVHNode *inout_allBVHNodes)
{
    int id = get_global_id(0);

    BVHNode node = in_BVHNodes[id];

    if (-1 != node.left && -1 != node.right)
    {
        BBox bbox1 = inout_allBVHNodes[node.left].bbox;
        BBox bbox2 = inout_allBVHNodes[node.right].bbox;
        inout_allBVHNodes[node.id].bbox = GenBBox_BBoxBBox(bbox1, bbox2);
    }
    else if (-1 != node.left)
    {
        inout_allBVHNodes[node.id].bbox = inout_allBVHNodes[node.left].bbox;
    }
    else if (-1 != node.right)
    {
        inout_allBVHNodes[node.id].bbox = inout_allBVHNodes[node.right].bbox;
    }
}

float3 RotateAxisAngle(float3 axis, float3 v, float theta)
{
    float cos_theta = cos(theta);
    float sin_theta = sin(theta);

    float3 rotated = (v * cos_theta) + (cross(axis, v) * sin_theta) + (axis * dot(axis, v)) * (1 - cos_theta);

    return rotated;
}

__kernel void Main_CameraRays(Vector3 in_Pos, Vector3 in_At, Vector3 in_Up, Vector3 in_Dir, Vector3 in_Right, float stepAngle, float in_Angle, float in_ZFar, int in_Width, int in_Height, int origox, int origoy, __global Ray *inout_Rays)
{
    int pixelx = get_global_id(0);
    int pixely = get_global_id(1);

    int id = (in_Width * pixely) + pixelx;

    float3 pos = ToFloat3(in_Pos.x, in_Pos.y, in_Pos.z);
    float3 at = ToFloat3(in_At.x, in_At.y, in_At.z);
    float3 up = ToFloat3(in_Up.x, in_Up.y, in_Up.z);
    float3 dir = ToFloat3(in_Dir.x, in_Dir.y, in_Dir.z);
    float3 right = ToFloat3(in_Right.x, in_Right.y, in_Right.z);

    Ray ray;
    
    ray.posx = pos.x;
    ray.posy = pos.y;
    ray.posz = pos.z;

    int diffx = pixelx - origox;
    int diffy = pixely - origoy;
    
    float thetax = stepAngle * (float)diffx;
    float thetay = stepAngle * (float)diffy;

    float3 rotateUp = normalize(RotateAxisAngle(up, dir, thetax));
    float3 rotateUpAndRight = normalize(RotateAxisAngle(right, rotateUp, thetay));
    
    ray.dirx = rotateUpAndRight.x;
    ray.diry = rotateUpAndRight.y;
    ray.dirz = rotateUpAndRight.z;
    
    ray.length = in_ZFar;

    inout_Rays[id] = ray;
}

__kernel void Main_RayShader(__global Ray *in_Rays, __global BVHNode *in_BVHNodes, int in_Width, int in_Height, __global unsigned char *out_Texture)
{
    int pixelx = get_global_id(0);
    int pixely = get_global_id(1);
    int id = (in_Width * pixely * 4) + (pixelx * 4);
    
    unsigned char red   = 255;
    unsigned char green = 0;
    unsigned char blue  = 0;
    unsigned char alpha  = 255;

    out_Texture[id + 0] = blue;
    out_Texture[id + 1] = green;
    out_Texture[id + 2] = red;
    out_Texture[id + 3] = alpha;
}

";
        }
    }
}
