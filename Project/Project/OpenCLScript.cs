using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCLRenderer
{
    public static class OpenCLScript
    {
        static string m_strVertexShader = @"";
        public static void SetVertexShader(string @strVertexShader)
        {
            m_strVertexShader = strVertexShader;
        }

        static string m_strRayShader = @"";
        public static void SetRayShader(string @strRayShader)
        {
            m_strRayShader = strRayShader;
        }

        public static string GetText()
        {
            return
@"
//#pragma OPENCL EXTENSION cl_khr_fp16 : enable

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
    float2 st;
    int isCollision;
    int objectId;
}
Hit;

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
    float area;
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
    int red;
    int green;
    int blue;
    int alpha;
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

float3 reflect(float3 dir, float3 n)
{
    float3 ret = dir - scale3(n, 2.0 * dot(dir, n));
    return ret;
}

typedef struct
{
    float x;
    float y;
    float z;
}
Vector3;

" + @m_strVertexShader + @"

__kernel void Main_TriangleShader(__global BVHNodeType *in_BVHNodeTypes, __global BVHNode *in_BVHNodes, __global Matrix4x4 *in_Matrices, __global BVHNode *inout_BVHNodes)
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
            float3 normal = normalize(cross( vb - va, vc - va ));
            outBVHNode.triangle.normalx = normal.x;
            outBVHNode.triangle.normaly = normal.y;
            outBVHNode.triangle.normalz = normal.z;
            // area
            outBVHNode.triangle.area = length(cross((vb - va), (vc - va))) / 2.0f;

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

__kernel void Main_CameraRays(Vector3 in_Pos, Vector3 in_Up, Vector3 in_Dir, Vector3 in_Right, float in_Angle, float in_ZFar, int in_Width, int in_Height, __global Ray *inout_Rays)
{
    int pixelx = get_global_id(0);
    int pixely = get_global_id(1);

    if (pixelx >= in_Width || pixely >= in_Height)
    {
        return;
    }
    
    int id = (in_Width * pixely) + pixelx;

    float3 pos   = ToFloat3(in_Pos.x, in_Pos.y, in_Pos.z);
    float3 up    = ToFloat3(in_Up.x, in_Up.y, in_Up.z);
    float3 dir   = ToFloat3(in_Dir.x, in_Dir.y, in_Dir.z);
    float3 right = ToFloat3(in_Right.x, in_Right.y, in_Right.z);

    float stepPerPixel = tan(in_Angle) / ((float)in_Height);

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

    inout_Rays[id] = ray;
}

float3 Ray_GetPoint(Ray ray, float t)
{
    return ( ToFloat3(ray.posx, ray.posy, ray.posz) + ToFloat3(ray.dirx * t, ray.diry * t, ray.dirz * t) );
}

Hit Intersect_RayTriangle(Ray ray, Triangle tri)
{

    Hit ret;
    ret.isCollision = 0;
    ret.objectId = -1;
    
    float3 A      = ToFloat3(tri.a.vx, tri.a.vy, tri.a.vz);
    float3 B      = ToFloat3(tri.b.vx, tri.b.vy, tri.b.vz);
    float3 C      = ToFloat3(tri.c.vx, tri.c.vy, tri.c.vz);
    float3 nA     = ToFloat3(tri.a.nx, tri.a.ny, tri.a.nz);
    float3 nB     = ToFloat3(tri.b.nx, tri.b.ny, tri.b.nz);
    float3 nC     = ToFloat3(tri.c.nx, tri.c.ny, tri.c.nz);
    float2 tA     = ToFloat2(tri.a.tx, tri.a.ty);
    float2 tB     = ToFloat2(tri.b.tx, tri.b.ty);
    float2 tC     = ToFloat2(tri.c.tx, tri.c.ty);

    float3 normal = ToFloat3(tri.normalx, tri.normaly, tri.normalz);
    
    float cost = dot(ToFloat3(ray.dirx, ray.diry, ray.dirz), normal);
	if (fabs(cost) <= 0.001f) 
		return ret;
    
	float t = dot(A - ToFloat3(ray.posx, ray.posy, ray.posz), normal) / cost;
	if(t < 0.001f) 
		return ret;
    
	float3 P = Ray_GetPoint(ray, t);
    
    float3 edge1 = C - B; 
    float3 vp1 = P - B; 
    float area1 = length(cross(edge1, vp1)) / 2.0f;
    float u = area1 / tri.area;

    float3 edge2 = A - C;
    float3 vp2 = P - C; 
    float area2 = length(cross(edge2, vp2)) / 2.0f;
    float v = area2 / tri.area;

    float3 edge3 = B - A;
    float3 vp3 = P - A; 
    float area3 = length(cross(edge3, vp3)) / 2.0f;
    float w = area3 / tri.area;

    if ((u + v + w) > 1.001) { return ret; }

    ret.isCollision = 1;
    ret.pos = P;
    ret.normal = normalize((u * nA) + (v * nB) + ((1 - u - v) * nC));
    ret.t = t;
    ret.materialId = tri.materialId;
    ret.st = (u * tA) + (v * tB) + ((1 - u - v) * tC);

    return ret;
}

float Intersect_RayBBox(Ray ray, BBox bbox)
{
    float t[10];
    t[1] = (bbox.minx - ray.posx) / ray.dirx;
    t[2] = (bbox.maxx - ray.posx) / ray.dirx;
    t[3] = (bbox.miny - ray.posy) / ray.diry;
    t[4] = (bbox.maxy - ray.posy) / ray.diry;
    t[5] = (bbox.minz - ray.posz) / ray.dirz;
    t[6] = (bbox.maxz - ray.posz) / ray.dirz;
    t[7] = max(max(min(t[1], t[2]), min(t[3], t[4])), min(t[5], t[6]));
    t[8] = min(min(max(t[1], t[2]), max(t[3], t[4])), max(t[5], t[6]));
    t[9] = (t[8] < 0 || t[7] > t[8]) ? -1.0f : t[7];
    return t[9];
}

void WriteTexture(__global unsigned char *texture, int width, int height, float2 pixel, Color color)
{
    int id = (width * (int)pixel.y * 4) + ((int)pixel.x * 4);
    
    // clip
    int blue  = color.blue;  if (blue  < 0) { blue  = 0; } else if (blue  > 255) { blue  = 255; }
    int green = color.green; if (green < 0) { green = 0; } else if (green > 255) { green = 255; }
    int red   = color.red;   if (red   < 0) { red   = 0; } else if (red   > 255) { red   = 255; }
    int alpha = color.alpha; if (alpha < 0) { alpha = 0; } else if (alpha > 255) { alpha = 255; }

    texture[id + 0] = blue;
    texture[id + 1] = green;
    texture[id + 2] = red;
    texture[id + 3] = alpha;
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

Color ColorBlending(Color color1, Color color2, float t)
{
    float red = ((float)color1.red * (1.0f - t)) + ((float)color2.red * t);
    float green = ((float)color1.green * (1.0f - t)) + ((float)color2.green * t);
    float blue = ((float)color1.blue * (1.0f - t)) + ((float)color2.blue * t);
    float alpha = 255.0f;

    Color ret;
    ret.red = (unsigned char)red;
    ret.green = (unsigned char)green;
    ret.blue = (unsigned char)blue;
    ret.alpha = (unsigned char)alpha;
    return ret;
}

Color Tex2DDiffuse(__global Material *materials, __global unsigned char *textureDatas, int materialId, float2 st)
{
    Material material = materials[materialId];
    unsigned int offset = material.diffuseTexture.offset;
    int width = material.diffuseTexture.width;
    int height = material.diffuseTexture.height;
    __global unsigned char *texture = &(textureDatas[offset]);

    // repeat on
    while (st.x < 0.0f) { st.x += 1.0f; } 
    while (st.y < 0.0f) { st.y += 1.0f; } 
    while (st.x > 1.0f) { st.x -= 1.0f; }
    while (st.y > 1.0f) { st.y -= 1.0f; }

    float2 pixel;
    pixel.x = ((float)st.x * (float)width);
    pixel.y = ((float)st.y * (float)height);

    Color ret = ReadTexture(texture, width, height, pixel);
    return ret;
}

typedef struct
{
    int id;
    int count[32];
    Ray ray[5][32];
}
Rays;

typedef struct
{
    int id;
    int count[32];
    Hit hit[5][32];
}
Hits;

" + @m_strRayShader + @"

__kernel void Main_RayShader(__global Ray *in_Rays, __global BVHNode *in_BVHNodes, __global int *in_BeginObjects, int in_NumBeginObjects, int in_Width, int in_Height, unsigned char red, unsigned char green, unsigned char blue, unsigned char alpha, __global Material *materials, __global unsigned char *textureDatas, __global unsigned char *out_Texture, Vector3 camPos, Vector3 camAt)
{
    int pixelx = get_global_id(0);
    int pixely = get_global_id(1);

    if (pixelx >= in_Width || pixely >= in_Height)
    {
        return;
    }


    int id = (in_Width * pixely) + pixelx;

    Rays rays;
    rays.id = 0;
    rays.count[rays.id] = 1;
    rays.ray[rays.id][0] = in_Rays[id];
    
    Hits hits;
    hits.id = rays.id;
    hits.count[rays.id] = 1;

    // clear
    Color background;
    background.red = red;
    background.green = green;
    background.blue = blue;
    background.alpha = alpha;
    WriteTexture(out_Texture, in_Width, in_Height, ToFloat2(pixelx, pixely), background);

    bool isEnd = false;

    do
    {
        for(int ray_id = 0; ray_id < rays.count[rays.id]; ray_id++)
        {
            hits.id = rays.id;
            Ray ray = rays.ray[rays.id][ray_id];
            hits.count[rays.id] = rays.count[rays.id];
            hits.hit[rays.id][ray_id].isCollision = 0;
            hits.hit[rays.id][ray_id].t = 1000000.0f;
            hits.hit[rays.id][ray_id].objectId = -1;
            
            for (int i = 0; i < in_NumBeginObjects; i++)
            {
                int isSearching = 1;

                int rootId = in_BeginObjects[i];
             
                int stack[100];
                int top = 0;
            
                stack[top] = rootId;
                top++;
            
                while(isSearching == 1)
                {
                    top--;
                    if (top < 0) { isSearching = 0; continue; }
                    BVHNode temp_node = in_BVHNodes[stack[top]];
            
                    if (temp_node.left == -1 && temp_node.right == -1) // ha haromszog
                    {
                        // haromszog-ray utkozesvizsgalat
                        Hit hit = Intersect_RayTriangle(ray, temp_node.triangle);
                        hit.objectId = i;
            
                        if (hit.isCollision == 1)
                        {
                            if (hit.t < hits.hit[rays.id][ray_id].t)
                            {
                                hits.hit[rays.id][ray_id] = hit;
                            }
                        }
                    }
                    
                    float left_distance = -1.0f;
                    if (temp_node.left != -1) 
                    {
                        BVHNode node = in_BVHNodes[temp_node.left];
                        left_distance = Intersect_RayBBox(ray, node.bbox);
                    }

                    float right_distance = -1.0f;
                    if (temp_node.right != -1) 
                    {
                        BVHNode node = in_BVHNodes[temp_node.right];
                        right_distance = Intersect_RayBBox(ray, node.bbox);
                    }

                    if (left_distance > -0.01f && right_distance > -0.01f)
                    {
                        if (left_distance < right_distance)
                        {
                            stack[top] = temp_node.left; 
                            top++;

                            stack[top] = temp_node.right;
                            top++;
                        } 
                        else
                        {
                            stack[top] = temp_node.right;
                            top++;

                            stack[top] = temp_node.left;
                            top++;
                        }
                    }
                    else if (left_distance > -0.01f)
                    {
                        stack[top] = temp_node.left;
                        top++;
                    }
                    else if (right_distance > -0.01f)
                    {
                        stack[top] = temp_node.right;
                        top++;
                    }
                }
            }
        }

        isEnd = RayShader(&hits, &rays, camPos, camAt, materials, textureDatas, out_Texture, in_Width, in_Height, pixelx, pixely);

        //if(true == RayShader(&hits, &rays, materials, textureDatas, out_Texture, in_Width, in_Height, pixelx, pixely))
        //{
        //    return;
        //}
    } 
    while(isEnd == false);
}
";
        }
    }
}
