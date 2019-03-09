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
    float m11;
    float m12;
    float m13;
    float m14;

    float m21;
    float m22;
    float m23;
    float m24;

    float m31;
    float m32;
    float m33;
    float m34;

    float m41;
    float m42;
    float m43;
    float m44;
}
Matrix4x4;

float2 ToFloat2(float x, float y)
{
    float2 ret;
    ret.x = x;
    ret.y = y;
    return ret;
}

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

    ret.x = dot(ToFloat4(T.m11, T.m12, T.m13, T.m14), v1);
    ret.y = dot(ToFloat4(T.m21, T.m22, T.m23, T.m24), v1);
    ret.z = dot(ToFloat4(T.m31, T.m32, T.m33, T.m34), v1);
    ret.w = dot(ToFloat4(T.m41, T.m42, T.m43, T.m44), v1);

    return ret;
}

float4 Mult_Matrix4x4Float4(Matrix4x4 T, float4 v)
{
    float4 ret;

    ret.x = dot(ToFloat4(T.m11, T.m12, T.m13, T.m14), v);
    ret.y = dot(ToFloat4(T.m21, T.m22, T.m23, T.m24), v);
    ret.z = dot(ToFloat4(T.m31, T.m32, T.m33, T.m34), v);
    ret.w = dot(ToFloat4(T.m41, T.m42, T.m43, T.m44), v);

    return ret;
}

Matrix4x4 Mult_Matrix4x4Matrix4x4(Matrix4x4 T2, Matrix4x4 T1)
{
    Matrix4x4 ret;

    float4 T1row1 = ToFloat4(T1.m11, T1.m12, T1.m13, T1.m14);
    float4 T1row2 = ToFloat4(T1.m21, T1.m22, T1.m23, T1.m24);
    float4 T1row3 = ToFloat4(T1.m31, T1.m32, T1.m33, T1.m34);
    float4 T1row4 = ToFloat4(T1.m41, T1.m42, T1.m43, T1.m44);

    float4 T2column1 = ToFloat4(T2.m11, T2.m21, T2.m31, T2.m41);
    float4 T2column2 = ToFloat4(T2.m12, T2.m22, T2.m32, T2.m42);
    float4 T2column3 = ToFloat4(T2.m13, T2.m23, T2.m33, T2.m43);
    float4 T2column4 = ToFloat4(T2.m14, T2.m24, T2.m34, T2.m44);

    float m11 = dot(T1row1, T2column1);
    float m12 = dot(T1row1, T2column2);
    float m13 = dot(T1row1, T2column3);
    float m14 = dot(T1row1, T2column4);
    
    float m21 = dot(T1row2, T2column1);
    float m22 = dot(T1row2, T2column2);
    float m23 = dot(T1row2, T2column3);
    float m24 = dot(T1row2, T2column4);
    
    float m31 = dot(T1row3, T2column1);
    float m32 = dot(T1row3, T2column2);
    float m33 = dot(T1row3, T2column3);
    float m34 = dot(T1row3, T2column4);
    
    float m41 = dot(T1row4, T2column1);
    float m42 = dot(T1row4, T2column2);
    float m43 = dot(T1row4, T2column3);
    float m44 = dot(T1row4, T2column4);

    ret.m11 = m11;
    ret.m12 = m12;
    ret.m13 = m13;
    ret.m14 = m14;

    ret.m21 = m21;
    ret.m22 = m22;
    ret.m23 = m23;
    ret.m24 = m24;

    ret.m31 = m31;
    ret.m32 = m32;
    ret.m33 = m33;
    ret.m34 = m34;

    ret.m41 = m41;
    ret.m42 = m42;
    ret.m43 = m43;
    ret.m44 = m44;

    return ret;
}

Matrix4x4 Inverse_Matrix4x4(Matrix4x4 T)
{
    float m[16] = 
        {  
            T.m11, T.m12, T.m13, T.m14,
            T.m21, T.m22, T.m23, T.m24,
            T.m31, T.m32, T.m33, T.m34,
            T.m41, T.m42, T.m43, T.m44
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

    Matrix4x4 ret;

    ret.m11 = invOut[0];  ret.m12 = invOut[1];  ret.m13 = invOut[2];  ret.m14 = invOut[3];
    ret.m21 = invOut[4];  ret.m22 = invOut[5];  ret.m23 = invOut[6];  ret.m24 = invOut[7];
    ret.m31 = invOut[8];  ret.m32 = invOut[9];  ret.m33 = invOut[10]; ret.m34 = invOut[11];
    ret.m41 = invOut[12]; ret.m42 = invOut[13]; ret.m43 = invOut[14]; ret.m44 = invOut[15];

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
    float3 pos;
    float3 normal;
    float t;
    int materialId;
    int isCollision;
}
Hit;

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
    float normalx;
    float normaly;
    float normalz;
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
    float centerx;
    float centery;
    float centerz;
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

typedef struct
{
    unsigned long offset;
    int width;
    int height;
}
Texture;
    
typedef struct
{
    Texture diffuseTexture;
    Texture specularTexture;
    Texture normalTexture;
}
Material;

typedef struct 
{
    unsigned char red;
    unsigned char green;
    unsigned char blue;
    unsigned char alpha;
}
Color;

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
    
    bbox.centerx = (fMinX + fMaxX) / 2.0f;
    bbox.centery = (fMinY + fMaxY) / 2.0f;
    bbox.centerz = (fMinZ + fMaxZ) / 2.0f;

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

    bbox.centerx = (fMinX + fMaxX) / 2.0f;
    bbox.centerx = (fMinY + fMaxY) / 2.0f;
    bbox.centerx = (fMinZ + fMaxZ) / 2.0f;

    return bbox;
}

float3 scale4(float4 point, float scale)
{
	float3 ret;
	ret.x = point.x * scale;
	ret.y = point.y * scale;
	ret.z = point.z * scale;
	return ret;
}

float3 scale3(float3 point, float scale)
{
	float3 ret;
	ret.x = point.x * scale;
	ret.y = point.y * scale;
	ret.z = point.z * scale;
	return ret;
}

float2 scale2(float2 point, float scale)
{
	float2 ret;
	ret.x = point.x * scale;
	ret.y = point.y * scale;
	return ret;
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

    float4 v = Mult_Matrix4x4Float4(in_Matrices[in.matrixId1], ToFloat4(in.vx, in.vy, in.vz, 1.0f));
    out.vx = v.x;
    out.vy = v.y;
    out.vz = v.z;
    

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

            // normal
            float3 va = ToFloat3(outBVHNode.triangle.a.vx, outBVHNode.triangle.a.vy, outBVHNode.triangle.a.vz);
            float3 vb = ToFloat3(outBVHNode.triangle.b.vx, outBVHNode.triangle.b.vy, outBVHNode.triangle.b.vz);
            float3 vc = ToFloat3(outBVHNode.triangle.c.vx, outBVHNode.triangle.c.vy, outBVHNode.triangle.c.vz);
            float3 normal = normalize(cross(vb - va, vc - va));
            outBVHNode.triangle.normalx = normal.x;
            outBVHNode.triangle.normaly = normal.y;
            outBVHNode.triangle.normalz = normal.z;

            // level 1
            outBVHNode.bbox = GenBBox_Tri(outBVHNode.triangle);
        }
        else
        {
            outBVHNode = inBVHNode;

            // level 2 - X
            //outBVHNode.bbox.minx = 0;
            //outBVHNode.bbox.miny = 0;
            //outBVHNode.bbox.minz = 0;
            //outBVHNode.bbox.maxx = 0;
            //outBVHNode.bbox.maxy = 0;
            //outBVHNode.bbox.maxz = 0;
            //
            //outBVHNode.bbox.centerx = 0;
            //outBVHNode.bbox.centery = 0;
            //outBVHNode.bbox.centerz = 0;
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

__kernel void Main_CameraRays(Vector3 in_Pos, Vector3 in_Up, Vector3 in_Dir, Vector3 in_Right, float in_Angle, float in_ZFar, int in_Width, int in_Height, __global Ray *inout_Rays, __global float *inout_DepthTexture)
{
    int pixelx = get_global_id(0);
    int pixely = get_global_id(1);
    
    int id = (in_Width * pixely) + pixelx;

    float3 pos = ToFloat3(in_Pos.x, in_Pos.y, in_Pos.z);
    float3 up = ToFloat3(in_Up.x, in_Up.y, in_Up.z);
    float3 dir = ToFloat3(in_Dir.x, in_Dir.y, in_Dir.z);
    float3 right = ToFloat3(in_Right.x, in_Right.y, in_Right.z);

    float stepPerPixel = tan(in_Angle) / ((float)in_Height / 2.0f);

    int movePixelX = pixelx - (in_Width / 2);
	int movePixelY = pixely - (in_Height / 2);

    float3 moveUp = scale3(up, movePixelY * stepPerPixel);
    float3 moveRight = scale3(right, movePixelX * stepPerPixel);

    float3 dir2 = normalize(dir + moveUp + moveRight);

    Ray ray;
    ray.posx = pos.x;
    ray.posy = pos.y;
    ray.posz = pos.z;
    ray.dirx = dir2.x;
    ray.diry = dir2.y;
    ray.dirz = dir2.z;
    ray.length = in_ZFar;

    inout_DepthTexture[id] = in_ZFar;
    inout_Rays[id] = ray;
}

float3 Ray_GetPoint(Ray *ray, float t)
{
    return ( ToFloat3(ray->posx, ray->posy, ray->posz) + ToFloat3(ray->dirx * t, ray->diry * t, ray->dirz * t) );
}

Hit Intersect_RayTriangle(Ray *ray, Triangle *tri)
{
    Hit ret;
    ret.isCollision = 0;

    float3 a = ToFloat3(tri->a.vx, tri->a.vy, tri->a.vz);
    float3 b = ToFloat3(tri->b.vx, tri->b.vy, tri->b.vz);
    float3 c = ToFloat3(tri->c.vx, tri->c.vy, tri->c.vz);
    float3 normal = ToFloat3(tri->normalx, tri->normaly, tri->normalz);
    float cost = dot(ToFloat3(ray->dirx, ray->diry, ray->dirz), normal);
	if (fabs(cost) <= 0.0001) 
		return ret;
    
	float t = dot(a - ToFloat3(ray->posx, ray->posy, ray->posz), normal) / cost;
	if(t < 0.0001) 
		return ret;
    
	float3 ip = Ray_GetPoint(ray, t);
    
	float c1 = dot(cross(b - a, ip - a), normal);
	float c2 = dot(cross(c - b, ip - b), normal);
	float c3 = dot(cross(a - c, ip - c), normal);
	if (c1 >= 0 && c2 >= 0 && c3 >= 0) 
    {
		ret.isCollision = 1;
        ret.pos = ip;
        ret.normal = normal;
        ret.t = t;
        ret.materialId = tri->materialId;
        return ret;
    }
		
	return ret;
}

int Intersect_RayBBox(Ray *ray, BBox *bbox) 
{
    Vector3 lb;
    lb.x = bbox->minx;
    lb.y = bbox->miny;
    lb.z = bbox->minz;

    Vector3 rt;
    rt.x = bbox->maxx;
    rt.y = bbox->maxy;
    rt.z = bbox->maxz;

    Vector3 dirfrac;
    dirfrac.x = 1.0 / ray->dirx;
    dirfrac.y = 1.0 / ray->diry;
    dirfrac.z = 1.0 / ray->dirz;
    
    float t1 = (lb.x - ray->posx) * dirfrac.x;
    float t2 = (rt.x - ray->posx) * dirfrac.x;
    float t3 = (lb.y - ray->posy) * dirfrac.y;
    float t4 = (rt.y - ray->posy) * dirfrac.y;
    float t5 = (lb.z - ray->posz) * dirfrac.z;
    float t6 = (rt.z - ray->posz) * dirfrac.z;
    
    float tmin = max(max(min(t1, t2), min(t3, t4)), min(t5, t6));
    float tmax = min(min(max(t1, t2), max(t3, t4)), max(t5, t6));
    
    // if tmax < 0, ray (line) is intersecting AABB, but the whole AABB is behind us
    if (tmax < 0)
    {
        return 0;
    }
    
    // if tmin > tmax, ray doesn't intersect AABB
    if (tmin > tmax)
    {
        return 0;
    }
    
    return 1;
}

void WriteTexture(__global unsigned char *texture, int width, int height, float2 pixel, Color color)
{
    int id = (width * (int)pixel.y * 4) + ((int)pixel.x * 4);
    
    texture[id + 0] = color.blue;
    texture[id + 1] = color.green;
    texture[id + 2] = color.red;
    texture[id + 3] = color.alpha;
}

Color ReadTexture(__global unsigned char *texture, int width, int height, float2 pixel)
{
    int id = (width * (int)pixel.y * 4) + ((int)pixel.x * 4);
    
    Color color;
    color.blue  = texture[id + 0];
    color.green = texture[id + 1];
    color.red   = texture[id + 2];
    color.alpha = texture[id + 3];

    return color;
}

Color Tex2D(__global unsigned char *texture, int width, int height, float3 A, float3 B, float3 C, float2 tA, float2 tB, float2 tC, float3 P)
{
    float3 u = B - A;
    float3 v = C - A;
    float3 w = P - A;

    float s = ((dot(u, v) * dot(w, v)) - (dot(v, v) * dot(w, u))) / ((dot(u, v) * dot(u, v)) - (dot(u, u) * dot(v, v)));
    float t = ((dot(u, v) * dot(w, u)) - (dot(u, u) * dot(w, v))) / ((dot(u, v) * dot(u, v)) - (dot(u, u) * dot(v, v)));

    // repeat texture, on
    tA.x = fmod(tA.x, 1.0f); if (tA.x < 0.0f) { tA.x += 1.0f; }
    tA.y = fmod(tA.y, 1.0f); if (tA.y < 0.0f) { tA.y += 1.0f; }
    tB.x = fmod(tB.x, 1.0f); if (tB.x < 0.0f) { tB.x += 1.0f; }
    tB.y = fmod(tB.y, 1.0f); if (tB.y < 0.0f) { tB.y += 1.0f; }
    tC.x = fmod(tC.x, 1.0f); if (tC.x < 0.0f) { tC.x += 1.0f; }
    tC.y = fmod(tC.y, 1.0f); if (tC.y < 0.0f) { tC.y += 1.0f; }

    float2 tu = tB - tA;
    float2 tv = tC - tA;

    float2 pixel = tA + scale2(tu, s) + scale2(tv, t);

    pixel.x = ((float)pixel.x * (float)width);
    pixel.y = ((float)pixel.y * (float)height);

    Color ret = ReadTexture(texture, width, height, pixel);
    return ret;
}

__kernel void Main_RayShader(__global Ray *in_Rays, __global BVHNode *in_BVHNodes, __global int *in_BeginObjects, int in_NumBeginObjects, __global float *inout_DepthTexture, int in_Width, int in_Height, float red, float green, float blue, float alpha, __global Material *materials, __global unsigned char *textureDatas, __global unsigned char *out_Texture)
{
    int pixelx = get_global_id(0);
    int pixely = get_global_id(1);
    int id = (in_Width * pixely) + pixelx;

    Ray ray = in_Rays[id];

    // clear
    Color background;
    background.red = 127;
    background.green = 127;
    background.blue = 255;
    background.alpha = 255;
    WriteTexture(out_Texture, in_Width, in_Height, (float2)(pixelx, pixely), background);

    for (int i = 0; i < in_NumBeginObjects; i++)
    {
        int rootId = in_BeginObjects[i];
     
        int stack[100];
        int top = -1;

        top++;
        stack[top] = rootId;

        while(top != -1)
        {
            BVHNode temp_node = in_BVHNodes[stack[top]];
            top--;

            if (temp_node.left == -1 && temp_node.right == -1) // ha haromszog
            {
                // haromszog-ray utkozesvizsgalat
                Hit hit = Intersect_RayTriangle(&ray, &temp_node.triangle);

                if (hit.isCollision == 1 && hit.t < inout_DepthTexture[id])
                {
                    inout_DepthTexture[id] = hit.t;

                    Material material = materials[hit.materialId];
                    unsigned int offset = material.diffuseTexture.offset;
                    int width = material.diffuseTexture.width;
                    int height = material.diffuseTexture.height;
                    __global unsigned char *texture = &(textureDatas[offset]);

                    float3 A = ToFloat3(temp_node.triangle.a.vx, temp_node.triangle.a.vy, temp_node.triangle.a.vz);
                    float3 B = ToFloat3(temp_node.triangle.b.vx, temp_node.triangle.b.vy, temp_node.triangle.b.vz);
                    float3 C = ToFloat3(temp_node.triangle.c.vx, temp_node.triangle.c.vy, temp_node.triangle.c.vz);
                    float3 P = hit.pos;
                    float2 tA = ToFloat2(temp_node.triangle.a.tx, temp_node.triangle.a.ty);
                    float2 tB = ToFloat2(temp_node.triangle.b.tx, temp_node.triangle.b.ty);
                    float2 tC = ToFloat2(temp_node.triangle.c.tx, temp_node.triangle.c.ty);

                    Color color = Tex2D(texture, width, height, A, B, C, tA, tB, tC, P);
                    WriteTexture(out_Texture, in_Width, in_Height, (float2)(pixelx, pixely), color);
                }
            }
            else if (1 == Intersect_RayBBox(&ray, &(temp_node.bbox))) // ha box
            {
                if (temp_node.left != -1) { top++; stack[top] = temp_node.left; }
                if (temp_node.right != -1) { top++; stack[top] = temp_node.right; }
            }
        }
    }
}

";
        }
    }
}
