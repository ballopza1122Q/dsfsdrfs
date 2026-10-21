using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        // All twenty half-heart values share one small pixel atlas and one mesh.
        public static Texture2D MzHeartAtlas;
        public static Mesh MzHeartMesh;
        public static Material MzHeartMaterial;
        public static Vector3[] MzHeartVertices;
        public static Vector2[] MzHeartUvs;
        public static Color[] MzHeartColors;
        public static int[] MzHeartIndices;
        public static int MzHeartCount,MzHeartUploaded,MzHeartCapacity;
        public static bool MzHeartDirty,MzHeartTintDirty;
        public static float MzHeartRetry,MzHeartR,MzHeartG,MzHeartB;
        public static int MzHeartUnits(int hp,int maximum)
        {
            if(maximum<=0||hp<=0)return 0;
            float units=Mathf.Clamp((float)hp/maximum,0,1)*20;
            int result=(int)units;if(units>result)result++;return result;
        }
        public static void MzHeartsInit()
        {
            if(MzHeartVertices==null){MzHeartVertices=new Vector3[64];MzHeartUvs=new Vector2[64];MzHeartColors=new Color[64];MzHeartIndices=new int[96];for(int i=0;i<16;i++){int v=i*4,t=i*6;MzHeartIndices[t]=v;MzHeartIndices[t+1]=v+1;MzHeartIndices[t+2]=v+2;MzHeartIndices[t+3]=v;MzHeartIndices[t+4]=v+2;MzHeartIndices[t+5]=v+3;}}
            bool palette=MzHealthColor.r!=MzHeartR||MzHealthColor.g!=MzHeartG||MzHealthColor.b!=MzHeartB;
            if(MzHeartAtlas!=null&&MzHeartMesh!=null&&MzHeartMaterial!=null){if(palette)MzHeartTint();return;}
            if(Time.realtimeSinceStartup<MzHeartRetry)return;MzHeartRetry=Time.realtimeSinceStartup+5;
            Texture2D atlas=null;Mesh mesh=null;Material material=null;
            try{
                var shader=Shader.Find("Sprites/Default");if(shader==null)shader=Shader.Find("UI/Default");if(shader==null)return;
                atlas=MzPng(MzHeartData(),FilterMode.Point);
                material=new Material(shader);material.SetTexture("_MainTex",atlas);material.SetInt("_SrcBlend",5);material.SetInt("_DstBlend",10);material.SetInt("_Cull",0);material.SetInt("_ZWrite",0);material.SetInt("_ZTest",8);
                mesh=new Mesh();mesh.MarkDynamic();MzHeartTint();
                if(MzHeartMesh!=null)UnityEngine.Object.Destroy(MzHeartMesh);if(MzHeartMaterial!=null)UnityEngine.Object.Destroy(MzHeartMaterial);if(MzHeartAtlas!=null)UnityEngine.Object.Destroy(MzHeartAtlas);
                MzHeartAtlas=atlas;MzHeartMaterial=material;MzHeartMesh=mesh;MzHeartR=MzHealthColor.r;MzHeartG=MzHealthColor.g;MzHeartB=MzHealthColor.b;MzHeartUploaded=-1;MzHeartCapacity=0;MzHeartDirty=true;MzNextEsp=0;MzHeartRetry=0;
            }catch(Exception){if(mesh!=null)UnityEngine.Object.Destroy(mesh);if(material!=null)UnityEngine.Object.Destroy(material);if(atlas!=null)UnityEngine.Object.Destroy(atlas);}
        }
        public static void MzHeartTint()
        {
            var color=MzHealthColor;for(int i=0;i<64;i++)MzHeartColors[i]=color;MzHeartR=color.r;MzHeartG=color.g;MzHeartB=color.b;MzHeartTintDirty=true;
        }
        public static void MzHeartRow(float center,float top,float bodyHeight,int hp,int maximum)
        {
            if(MzHeartAtlas==null||MzHeartMesh==null||MzHeartMaterial==null||MzHeartCount>=16)return;
            int state=MzHeartUnits(hp,maximum);float scale=Mathf.Clamp(bodyHeight*.011f,.72f,1.05f),w=99*scale,h=9*scale;
            float x=Mathf.Clamp(center-w*.5f,3,Screen.width-w-3),y=top-9-h;if(y<2)return;
            int v=MzHeartCount++*4;var points=MzHeartVertices;points[v].x=x;points[v].y=y;points[v].z=0;points[v+1].x=x+w;points[v+1].y=y;points[v+1].z=0;points[v+2].x=x+w;points[v+2].y=y+h;points[v+2].z=0;points[v+3].x=x;points[v+3].y=y+h;points[v+3].z=0;
            float u=99f/128,vt=(256-state*11)/256f,vb=(247-state*11)/256f;var uv=MzHeartUvs;uv[v].x=0;uv[v].y=vt;uv[v+1].x=u;uv[v+1].y=vt;uv[v+2].x=u;uv[v+2].y=vb;uv[v+3].x=0;uv[v+3].y=vb;MzHeartDirty=true;
        }
        public static void MzDrawHearts()
        {
            if(MzHeartCount==0||MzHeartMesh==null||MzHeartMaterial==null||MzHeartAtlas==null)return;
            try{
                if(MzHeartDirty){
                    int capacity=MzHeartCapacity;if(capacity==0)capacity=1;while(capacity<MzHeartCount)capacity*=2;bool resized=capacity!=MzHeartCapacity;
                    int end=resized?capacity:MzHeartUploaded;var zero=new Vector3();for(int i=MzHeartCount*4;i<end*4;i++)MzHeartVertices[i]=zero;
                    if(resized)MzHeartMesh.Clear();MzHeartMesh.SetVertices(MzHeartVertices,0,capacity*4);MzHeartMesh.SetUVs(0,MzHeartUvs,0,capacity*4);
                    if(resized||MzHeartTintDirty){MzHeartMesh.SetColors(MzHeartColors,0,capacity*4);MzHeartTintDirty=false;}if(resized)MzHeartMesh.SetTriangles(MzHeartIndices,0,capacity*6,0,false,0);
                    MzHeartCapacity=capacity;MzHeartUploaded=MzHeartCount;MzHeartDirty=false;
                }
                if(MzHeartTintDirty){MzHeartMesh.SetColors(MzHeartColors,0,MzHeartCapacity*4);MzHeartTintDirty=false;}
                if(!MzHeartMaterial.SetPass(0)){MzHeartDisable();return;}GL.PushMatrix();try{GL.LoadPixelMatrix(0,Screen.width,Screen.height,0);Graphics.DrawMeshNow(MzHeartMesh,Matrix4x4.identity);}finally{GL.PopMatrix();}
            }catch(Exception){MzHeartDisable();}
        }
        public static void MzHeartDisable()
        {
            if(MzHeartMesh!=null)UnityEngine.Object.Destroy(MzHeartMesh);if(MzHeartMaterial!=null)UnityEngine.Object.Destroy(MzHeartMaterial);if(MzHeartAtlas!=null)UnityEngine.Object.Destroy(MzHeartAtlas);
            MzHeartMesh=null;MzHeartMaterial=null;MzHeartAtlas=null;MzHeartCount=0;MzHeartRetry=Time.realtimeSinceStartup+5;MzNextEsp=0;
        }
    }
}
