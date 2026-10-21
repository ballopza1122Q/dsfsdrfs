using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static Texture2D MzRing,MzHeadRing;
        public static Texture2D MzWhite;
        public static Rect[] MzRects;
        public static Color[] MzColors;
        public static Matrix4x4[] MzMatrices;
        public static string[] MzTexts;
        public static Texture[] MzTextures;
        public static bool[] MzRotated;
        public static bool[] MzSolids;
        public static int MzCommandCount,MzEspWidth,MzEspHeight,MzSampledPlayers;
        public static bool MzBuilding,MzRendering;
        public static float MzNextEsp,MzEspSampleStarted;
        public static float MzTextureRetry;
        public static JMAGGLCNGIG MzEspMatch;
        public static Camera MzEspCamera;
        public static float MzFovMaximum()
        {
            float value=Screen.height*.5f-8,wide=Screen.width*.5f-8;if(value>wide)value=wide;if(value>480)value=480;if(value<20)value=20;return value;
        }
        public static float MzEffectiveFov(){return Mathf.Clamp(MzFov,20,MzFovMaximum());}
        public static Texture2D MzCreateRing(int size,float thickness)
        {
            return MzPng(MzRingData(size<=32),FilterMode.Bilinear);
        }
        public static void MzRendererInit()
        {
            if(Time.realtimeSinceStartup<MzTextureRetry)return;
            MzBatchInit();if(MzWhite==null)MzWhite=Texture2D.whiteTexture;if(MzRects==null){MzRects=new Rect[512];MzColors=new Color[512];MzMatrices=new Matrix4x4[512];MzTexts=new string[512];MzTextures=new Texture[512];MzRotated=new bool[512];MzSolids=new bool[512];}
            try{if(MzRing==null){MzRing=MzCreateRing(256,1.5f);MzNextEsp=0;}if(MzHeadRing==null){MzHeadRing=MzCreateRing(32,3);MzNextEsp=0;}MzTextureRetry=0;}catch(Exception){MzTextureRetry=Time.realtimeSinceStartup+5;throw;}
        }
        public static void MzClearCommands()
        {
            MzEspGeometryValid=false;MzTrimCommands(0);MzQuadCount=0;MzBatchDirty=true;MzHeartCount=0;MzHeartDirty=true;
        }
        public static void MzTrimCommands(int count)
        {
            for(int i=count;i<MzCommandCount;i++){MzTexts[i]=null;MzTextures[i]=null;}MzCommandCount=count;MzBatchDirty=true;
        }
        public static void MzCommand(Rect rect,Matrix4x4 matrix,bool rotated,string text,Texture texture)
        {
            if(MzCommandCount>=512)return;int i=MzCommandCount++;MzRects[i]=rect;MzColors[i]=MzBuilding?MzBatchColor:GUI.color;MzMatrices[i]=matrix;MzRotated[i]=rotated;MzTexts[i]=text;MzTextures[i]=texture;MzSolids[i]=text==null&&System.Object.ReferenceEquals(texture,MzWhite);
        }
        public static void MzDrawCommands()
        {
            var matrix=GUI.matrix;var tint=GUI.color;bool previous=false;var identity=Matrix4x4.identity;
            var rects=MzRects;var colors=MzColors;var matrices=MzMatrices;var turns=MzRotated;var texts=MzTexts;var textures=MzTextures;var solids=MzSolids;int count=MzCommandCount;bool batched=MzDrawBatch();int fallback=0;
            // A failed mesh cannot replay stale duplicate geometry. Rebuild the bounded
            // GUI fallback on the next sample instead, without allocating twice.
            if(!batched&&MzQuadCount>0){MzNextEsp=0;return;}
            try{MzEspStyles();var style=MzEspStyle;for(int i=0;i<count;i++){
                if(batched&&solids[i])continue;
                bool rotated=turns[i];if(rotated||previous)GUI.matrix=rotated?matrices[i]:identity;previous=rotated;GUI.color=colors[i];
                if(texts[i]!=null)GUI.Label(rects[i],texts[i],style);else {var texture=textures[i];if(solids[i]&&fallback++>=96)continue;if(texture!=null)GUI.DrawTexture(rects[i],texture);}
            }}finally{GUI.matrix=matrix;GUI.color=tint;}
        }
        public static void MzRender()
        {
            if(MzRendering||!MzOnMainThread())return;MzRendering=true;
            try{
                if(!MzSwitch[0]&&!MzSwitch[13]){if(MzRects!=null)MzClearCommands();MzEspCacheClear();MzEspMatch=null;MzEspCamera=null;MzNextEsp=0;MzResetCounts();return;}
                var match=COW.GameFacade.CurrentMatch();var camera=Camera.main;
                if(match==null||camera==null){if(MzRects!=null)MzClearCommands();MzEspCacheClear();MzEspMatch=null;MzEspCamera=null;MzNextEsp=0;MzResetCounts();MzResetAim();return;}
                MzRendererInit();if(MzRing==null||MzHeadRing==null)return;float now=Time.realtimeSinceStartup;
                bool changed=!System.Object.ReferenceEquals(match,MzEspMatch)||camera!=MzEspCamera||MzEspWidth!=Screen.width||MzEspHeight!=Screen.height;
                if(changed){MzClearCommands();MzEspCacheClear();MzNextEsp=0;MzResetCounts();MzResetAim();MzEspMatch=match;MzEspCamera=camera;MzEspWidth=Screen.width;MzEspHeight=Screen.height;}
                if(MzSwitch[0]){
                    if(MzSwitch[5])MzHeartsInit();
                    if(now>=MzNextEsp){MzEspSampleStarted=now;
                        try{MzSampleEsp();}finally{float cost=Time.realtimeSinceStartup-now;bool detailed=MzSwitch[3]||MzSwitch[7]||MzSwitch[9];MzNextEsp=now+(cost>.012f?.08f:cost>.006f||detailed||MzSampledPlayers>6?.045f:.025f);if(MzNextEsp<now+MzMemoryEspDelay)MzNextEsp=now+MzMemoryEspDelay;}
                    }
                    MzEspDrawCache(camera);MzDrawCommands();MzDrawHearts();MzCountHud();
                }else {MzClearCommands();MzEspCacheClear();MzResetCounts();}
                // FOV is one square, unrotated draw. Its outline cannot run out of a
                // per-enemy segment budget and scales equally on both axes.
                if(MzSwitch[13]){GUI.color=MzFovColor;MzCircle(Screen.width*.5f,Screen.height*.5f,MzEffectiveFov(),1.5f);}
            }catch(Exception){MzBuilding=false;MzDrawingEsp=false;MzEspCacheClear();MzResetCounts();if(MzRects!=null)MzClearCommands();MzNextEsp=Time.realtimeSinceStartup+.25f;}
            finally{MzRendering=false;}
        }
    }
}
