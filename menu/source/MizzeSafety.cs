using System;
using System.Threading;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static int MzMainThread;
        public static bool MzGuiRunning,MzAimRunning,MzSilentRunning,MzLagDispatch;
        public static int MzAimFrame,MzAimCursor,MzAimLagSlot;
        public static float MzAimStamp;
        public static Player MzAimTarget;
        public static JMAGGLCNGIG MzAimMatch;
        public static Camera MzAimCamera;
        public static Vector3 MzAimPoint,MzAimOrigin;
        public static Vector2 MzAimScreen;
        public static int MzSilentFrame;
        public static long MzHeapFloor,MzHeapBytes;
        public static float MzHeapNext,MzHeapCollectAfter;
        public static int MzMemoryPressure;
        public static long MzHeapCollectBase;
        public static float MzMemoryEspDelay;
        public static bool MzIdleCleared,MzIdleReleased;
        public static float MzIdleSince;
        public static int MzHeapCollections;
        public static void MzDestroyOwned(UnityEngine.Object resource)
        {
            if(resource!=null)try{UnityEngine.Object.Destroy(resource);}catch(Exception){}
        }
        public static void MzReleaseEspResources()
        {
            // These objects are created by Mizze. Never destroy Camera.main,
            // actors, shared whiteTexture or a material owned by the game.
            MzResetAim();if(MzRects!=null)MzClearCommands();MzEspCacheClear();MzResetCounts();
            MzDestroyOwned(MzMesh);MzDestroyOwned(MzMaterial);MzDestroyOwned(MzRing);MzDestroyOwned(MzHeadRing);
            MzDestroyOwned(MzHeartMesh);MzDestroyOwned(MzHeartMaterial);MzDestroyOwned(MzHeartAtlas);
            MzMesh=null;MzMaterial=null;MzRing=null;MzHeadRing=null;MzWhite=null;
            MzHeartMesh=null;MzHeartMaterial=null;MzHeartAtlas=null;
            MzRects=null;MzColors=null;MzMatrices=null;MzTexts=null;MzTextures=null;MzRotated=null;MzSolids=null;
            MzVertices=null;MzVertexColors=null;MzIndices=null;MzQuadCounts=null;
            MzEspWorld=null;MzEspWorldValid=null;MzEspHp=null;MzEspMaxHp=null;MzEspNames=null;MzEspDistances=null;MzEspBots=null;MzHeartVertices=null;MzHeartUvs=null;MzHeartColors=null;MzHeartIndices=null;
            MzFullNames=null;MzShortNames=null;MzAutoCheckedWeapon=null;MzAutoTypeValid=false;MzEspMatch=null;MzEspCamera=null;
            MzCommandCount=0;MzQuadCount=0;MzMeshUploaded=-1;MzMeshCapacity=0;MzBatchReady=false;MzBatchDirty=true;MzBatchRetry=0;
            MzHeartCount=0;MzHeartUploaded=-1;MzHeartCapacity=0;MzHeartRetry=0;MzTextureRetry=0;MzNextEsp=0;
        }
        public static void MzIdleMemory(bool active,float now)
        {
            if(active){MzIdleCleared=false;MzIdleReleased=false;return;}
            if(!MzIdleCleared){MzResetAim();if(MzRects!=null)MzClearCommands();MzEspCacheClear();MzResetCounts();MzEspMatch=null;MzEspCamera=null;MzFullNames=null;MzShortNames=null;MzIdleSince=now;MzIdleCleared=true;}
            if(!MzIdleReleased&&(now-MzIdleSince>=5)){MzReleaseEspResources();MzIdleReleased=true;}
        }
        public static long MzReadHeap()
        {
            // Unity's native counter is present in the supplied TH player.
            // Retain the managed fallback for runtimes returning no profiler data.
            try{long bytes=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();if(bytes>0)return bytes;}catch(Exception){}
            return GC.GetTotalMemory(false);
        }
        public static bool MzOnMainThread(){return MzMainThread!=0&&Thread.CurrentThread.ManagedThreadId==MzMainThread;}
        public static void MzDropTarget(){MzBodyTarget=null;MzBodyCollider=null;MzBodyFrame=-1;MzAimLagSlot=-1;MzAimTarget=null;MzAimCamera=null;MzAimMatch=null;MzAimStamp=0;}
        public static void MzResetAim(){MzStopAuto();MzAimkillClearArgs();MzAutoCheckedWeapon=null;MzAutoTypeValid=false;MzRestoreSilent();MzDropTarget();MzAimFrame=-1;MzSilentFrame=-1;MzAimCursor=0;}
        // Heap usage includes the game. A large retained game heap is not itself
        // a reason to disable Mizze. Reduce sampling work while growth is high.
        public static void MzHeapObserve(long bytes)
        {
            MzHeapBytes=bytes;if(bytes<=0)return;if(MzHeapFloor==0||bytes<MzHeapFloor)MzHeapFloor=bytes;
            long growth=bytes-MzHeapFloor;
            if(growth>=67108864)MzMemoryPressure=2;else if(growth>=33554432)MzMemoryPressure=1;
            else if(growth<16777216)MzMemoryPressure=0;
            MzMemoryEspDelay=MzMemoryPressure==2?.10f:MzMemoryPressure==1?.06f:.025f;
        }
        public static void MzMemoryTick()
        {
            if(!MzOnMainThread())return;
            float now=Time.realtimeSinceStartup;if(now<MzHeapNext)return;MzHeapNext=now+.5f;
            try{
                MzHeapObserve(MzReadHeap());
                bool active=MzAuthorized()&&(MzSwitch[0]||MzSwitch[11]||MzSwitch[12]||MzSwitch[13]||MzSwitch[15]||MzAimkillEnabled||MzAimlagEnabled)&&COW.GameFacade.CurrentMatch()!=null;
                MzIdleMemory(active,now);
                if(MzHeapCollectBase==0||MzHeapBytes<MzHeapCollectBase)MzHeapCollectBase=MzHeapBytes;
                if(MzHeapBytes-MzHeapCollectBase>=16777216&&now>=MzHeapCollectAfter){
                    MzHeapCollectAfter=now+10;MzHeapCollections++;GC.Collect();MzHeapObserve(MzReadHeap());MzHeapCollectBase=MzHeapBytes;
                }
            }catch(Exception){MzHeapNext=now+5;}
        }
    }
}
