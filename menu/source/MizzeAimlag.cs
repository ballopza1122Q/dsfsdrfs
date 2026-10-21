using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        // Retain the original visual rig during camera render, then restore it.
        // No mesh baking, replacement shaders, renderer hiding or scale writes.
        public static bool MzAimlagEnabled,MzLagBound,MzLagBusy;
        public static int MzLagState,MzLagCount,MzLagSerial,MzLagRenderFrame,MzLagVertices,MzLagBakes;
        public static float MzLagScopeUntil,MzLagBindAfter,MzLagAnim;
        public static Player MzLagLocal;
        public static JMAGGLCNGIG MzLagMatch;
        public static Camera MzLagRenderCamera;
        public static Camera.CameraCallback MzLagBeforeHandler,MzLagAfterHandler;
        public static Player[] MzLagPlayers;
        public static GameObject[] MzLagObjects;
        public static Transform[] MzLagVisuals;
        public static Vector3[] MzLagAnchors,MzLagHeads,MzLagBones;
        public static bool[] MzLagBoneValid;
        public static int[] MzLagParts,MzLagSeen,MzLagDrawnFrame;
        public static Vector3 MzEspVisualShift;
        public static int MzLagEspSlot;
        public static void MzLagBuffers()
        {
            if(MzLagPlayers!=null)return;
            MzLagPlayers=new Player[16];MzLagObjects=new GameObject[16];MzLagVisuals=new Transform[16];MzLagAnchors=new Vector3[16];MzLagHeads=new Vector3[16];
            MzLagBones=new Vector3[176];MzLagBoneValid=new bool[176];MzLagParts=new int[16];MzLagSeen=new int[16];MzLagDrawnFrame=new int[16];
            MzRigBuffers();
        }
        public static bool MzLagScope(){return MzAimlagEnabled&&MzLagMatch!=null&&Time.realtimeSinceStartup<MzLagScopeUntil;}
        public static int MzLagFind(Player p)
        {
            if(!MzLagScope()||MzLagPlayers==null||p==null)return -1;
            for(int i=0;i<16;i++)if(MzLagPlayers[i]==p&&MzLagParts[i]>0&&MzLagDrawnFrame[i]>0&&Time.frameCount-MzLagDrawnFrame[i]<=2&&p.gameObject==MzLagObjects[i]&&p.get_BoneRootTransform()==MzLagVisuals[i])return i;
            return -1;
        }
        public static bool MzLagAimBridge(){int slot=MzAimLagSlot;return MzLagScope()&&slot>=0&&slot<16&&MzLagPlayers!=null&&MzLagPlayers[slot]==MzAimTarget&&MzLagParts[slot]>0&&MzLagDrawnFrame[slot]>0&&Time.frameCount-MzLagDrawnFrame[slot]<=2;}
        public static Vector3 MzLagSelectPoint(Player p,Vector3 live){int slot=MzLagFind(p);return slot>=0?MzLagHeads[slot]:live;}
        public static Vector3 MzLagShift(Player p){int slot=MzLagFind(p);return slot>=0?MzLagAnchors[slot]-p.RootTransform.position:new Vector3();}
        public static bool MzLagInFov(Camera camera,Vector3 point)
        {
            Vector2 screen;if(!MzProject(camera,point,out screen))return false;float x=screen.x-Screen.width*.5f,y=screen.y-Screen.height*.5f,r=MzEffectiveFov();return x*x+y*y<r*r;
        }
        public static void MzLagRestore()
        {
            MzLagRenderCamera=null;if(MzLagPlayers!=null)for(int i=0;i<16;i++)MzRigRestore(i);
            MzAutoPoseRestore();
        }
        public static void MzLagDrop(int slot)
        {
            MzRigDrop(slot);
            for(int b=0;b<11;b++)MzLagBoneValid[slot*11+b]=false;
            MzLagPlayers[slot]=null;MzLagObjects[slot]=null;MzLagVisuals[slot]=null;MzLagParts[slot]=0;MzLagSeen[slot]=0;MzLagDrawnFrame[slot]=-1;
        }
        public static void MzLagClear()
        {
            MzLagRestore();if(MzLagPlayers!=null)for(int i=0;i<16;i++)MzLagDrop(i);
            MzLagCount=0;MzLagVertices=0;MzLagMatch=null;MzLagLocal=null;MzLagScopeUntil=0;MzLagEspSlot=-1;MzEspVisualShift=new Vector3();
        }
        public static void MzLagDetach()
        {
            try{if(MzLagBeforeHandler!=null)Camera.onPreCull-=MzLagBeforeHandler;}catch(Exception){}
            try{if(MzLagAfterHandler!=null)Camera.onPostRender-=MzLagAfterHandler;}catch(Exception){}
            MzLagBound=false;
        }
        public static void MzAimlagTick()
        {
            if(!MzOnMainThread())return;
            if(!MzAutoHideInitialized){MzAutoHideInitialized=true;MzHideAutoAnimation=true;}
            if(MzLagRenderCamera!=null)MzLagRestore();
            if(!MzAuthorized()){MzLagClear();MzAutoPoseClear();MzLagDetach();MzLagState=0;return;}
            try{
                var match=COW.GameFacade.CurrentMatch();var local=COW.GameFacade.GetLocalPlayerOrObServer();
                if(match==null||local==null||!local.IsLocalPlayer()||local.CurHP<=0||local.IsDieing||local.gameObject==null||!local.gameObject.activeInHierarchy){MzLagClear();MzAutoPoseClear();MzLagDetach();MzLagState=1;return;}
                if(!System.Object.ReferenceEquals(match,MzLagMatch)||local!=MzLagLocal){MzLagClear();MzLagDetach();MzLagMatch=match;MzLagLocal=local;}
                float now=Time.realtimeSinceStartup;bool scoped=MzAimlagEnabled&&local.get_IsSighting();if(scoped)MzLagScopeUntil=now+.18f;
                bool lag=MzAimlagEnabled&&MzLagScopeUntil>now;
                if(!lag&&MzLagPlayers!=null){for(int i=0;i<16;i++)if(MzLagParts[i]>0)MzLagDrop(i);MzLagCount=0;}
                bool hide=MzHideAutoAnimation&&MzSwitch[15]&&MzAutoPoseCount>0&&now<MzAutoPoseUntil;
                if(!lag&&!hide){MzLagRestore();MzLagDetach();MzLagState=MzAimlagEnabled?2:0;return;}
                if(lag)MzLagBuffers();
                if(!MzLagBound){
                    if(now<MzLagBindAfter)return;MzLagBindAfter=now+5;
                    if(MzLagBeforeHandler==null)MzLagBeforeHandler=new Camera.CameraCallback(MzLagBefore);
                    if(MzLagAfterHandler==null)MzLagAfterHandler=new Camera.CameraCallback(MzLagAfter);
                    Camera.onPreCull+=MzLagBeforeHandler;Camera.onPostRender+=MzLagAfterHandler;MzLagBound=true;MzLagBindAfter=0;
                }
                MzLagState=lag?(MzLagCount>0?4:3):2;
            }catch(Exception){MzLagClear();MzAutoPoseClear();MzLagDetach();MzLagState=5;}
        }
        public static Transform MzLagBone(Player p,int bone)
        {
            return bone==0?p.GetHeadTF():bone==1?p.RootTransform:bone==2?p.NeckBone:bone==3?p.ArmBoneLeft:bone==4?p.HandBoneLeft:bone==5?p.ArmBoneRight:bone==6?p.HandBoneRight:bone==7?p.LegBoneLeft:bone==8?p.FootBoneLeft:bone==9?p.LegBoneRight:p.FootBoneRight;
        }
        public static bool MzLagCapture(Player p,int slot){return MzRigCapture(p,slot);}
        public static bool MzLagDraw(int slot,Camera camera){return MzRigApply(slot);}
        public static void MzLagBefore(Camera camera)
        {
            if(MzLagBusy||!MzOnMainThread()||camera==null||camera!=Camera.main)return;MzLagBusy=true;
            try{
                MzLagRestore();var local=COW.GameFacade.GetLocalPlayerOrObServer();var match=COW.GameFacade.CurrentMatch();
                if(!MzAuthorized()||local==null||local!=MzLagLocal||!System.Object.ReferenceEquals(match,MzLagMatch)||local.CurHP<=0||local.IsDieing){MzLagClear();MzAutoPoseClear();MzLagDetach();return;}
                bool lag=MzLagScope(),hide=MzHideAutoAnimation&&MzSwitch[15]&&MzAutoPoseCount>0&&Time.realtimeSinceStartup<MzAutoPoseUntil;
                if(!lag&&!hide){MzLagClear();MzLagDetach();return;}MzLagRenderCamera=camera;MzLagRenderFrame=Time.frameCount;
                if(hide)MzAutoPoseApply(local);
                if(!lag)return;var list=match.ILPNGCMEFFI();if(list==null)return;MzLagSerial++;if(MzLagSerial<=0)MzLagSerial=1;int count=list.Count;if(count>128)count=128;int held=0,captures=0,failures=0;
                for(int n=0;n<count&&held<16;n++){
                    try{
                        if(n>=list.Count)break;var p=list[n] as Player;if(!MzEnemy(p))continue;var visual=p.get_BoneRootTransform();if(visual==null||visual==p.RootTransform)continue;
                        var root=p.RootTransform;float range=Vector3.Distance(root.position,camera.transform.position);if(!MzFinite(range)||range>MzRange)continue;
                        int slot=-1,empty=-1;for(int i=0;i<16;i++){if(MzLagPlayers[i]==p){slot=i;break;}if(empty<0&&MzLagPlayers[i]==null)empty=i;}
                        if(slot>=0&&MzLagSeen[slot]==MzLagSerial)continue;
                        if(slot>=0&&(MzLagVisuals[slot]!=visual||MzLagObjects[slot]!=p.gameObject||!MzLagInFov(camera,MzLagHeads[slot]))){MzLagDrop(slot);slot=-1;}
                        if(slot<0){if(captures>=1||Time.realtimeSinceStartup<MzLagCaptureAfter||!MzLagInFov(camera,p.GetHeadTF().position))continue;if(empty<0)for(int i=0;i<16;i++)if(MzLagPlayers[i]==null){empty=i;break;}if(empty<0)continue;slot=empty;captures++;if(!MzLagCapture(p,slot)){failures++;continue;}}
                        MzLagSeen[slot]=MzLagSerial;if(MzLagDraw(slot,camera))held++;else {MzLagDrop(slot);failures++;}
                    }catch(Exception){}
                }
                for(int i=0;i<16;i++)if(MzLagSeen[i]!=MzLagSerial&&MzLagPlayers[i]!=null)MzLagDrop(i);MzLagCount=held;MzLagState=held>0?4:failures>0?6:3;
            }catch(Exception){MzLagRestore();MzLagBindAfter=Time.realtimeSinceStartup+1;MzLagState=5;}finally{MzLagBusy=false;}
        }
        public static void MzLagAfter(Camera camera){if(MzOnMainThread()&&camera==MzLagRenderCamera)MzLagRestore();}
        public static void MzAimlagRow(float x,float y){MzRenderFeatureRow(x,y,false);}
        public static void MzRenderFeatureRow(float x,float y,bool hide)
        {
            bool enabled=hide?MzHideAutoAnimation:MzAimlagEnabled;float a=hide?MzHideAnim:MzLagAnim;if(MzUiPaint)a=MzTween(a,enabled?1:0,Mathf.Clamp(Time.unscaledDeltaTime*14,0,1));if(hide)MzHideAnim=a;else MzLagAnim=a;
            MzRowVisual(x,y,a,hide?"Ẩn hoạt ảnh Autofire":"Aimlag khi mở ngắm",0);
            if(MzTap(x,y,354,40)){if(hide){MzHideAutoAnimation=!enabled;if(enabled)MzAutoPoseClear();}else {MzAimlagEnabled=!enabled;if(enabled)MzLagClear();}MzNextEsp=0;}
        }
        public static void MzAimlagStatus(float x,float y)
        {
            if(!MzAimlagEnabled)return;string text=MzLagState==1?"Chờ trận":MzLagState==2?"Mở ngắm để giữ toàn thân":MzLagState==3?"Chờ địch trong FOV":MzLagState==4?"Đang giữ toàn thân trong FOV":MzLagState==5?"Chưa nối được render":MzLagState==6?"Model chưa sẵn sàng":"Đang chờ";
            MzTint(.65f,.86f,.83f,MzOpenAnim);MzCenter(x,y,354,24,text);
        }
    }
}
