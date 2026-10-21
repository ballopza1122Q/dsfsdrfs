using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static CGKJLKPMGDJ MzChangedAim;
        public static Player MzAimOwner;
        public static GameObject MzAppliedObject;
        public static Collider MzAppliedCollider;
        public static GameObject MzOldAimObject;
        public static Collider MzOldAimCollider;
        public static Vector3 MzOldHpc,MzOldDoj,MzOldDirection,MzOldOrigin,MzOldOnm,MzAppliedPoint,MzAppliedDirection,MzAppliedOrigin;
        public static LKOOALMKJND MzOldDkp,MzAppliedHitType;
        public static short MzOldClo;
        public static bool MzOldEfm,MzOldCeo;
        public static long MzChanceSeed;
        public static int MzChanceFrame;
        public static bool MzChanceAccepted;
        // One head/body choice per game frame. Repeated getter reads cannot
        // re-roll the same frame; a body outcome is an explicit body hit route.
        public static bool MzSilentHeadAllowed()
        {
            float chance=Mathf.Clamp(MzHeadChance,0,100);if(chance>=100)return true;if(chance<=0)return false;int frame=Time.frameCount;
            if(frame!=MzChanceFrame){MzChanceFrame=frame;if(MzChanceSeed==0)MzChanceSeed=1;MzChanceSeed=(MzChanceSeed*48271)%2147483647;MzChanceAccepted=MzChanceSeed%100<chance;}return MzChanceAccepted;
        }
        public static bool MzEnemy(Player p){return p!=null&&!p.IsLocalPlayer()&&p.CurHP>0&&!p.IsDieing&&!p.IsLocalTeammate(false)&&!p.IsLocalTeammate(true)&&p.gameObject!=null&&p.gameObject.activeInHierarchy&&p.RootTransform!=null&&p.GetHeadTF()!=null;}
        public static bool MzProject(Camera cam,Vector3 world,out Vector2 screen){screen=new Vector2();if(!MzPoint(world))return false;Vector3 p=cam.WorldToScreenPoint(MzDrawingEsp?world+MzEspVisualShift:world);if(!MzPoint(p)||p.z<=0)return false;screen.x=p.x;screen.y=(MzBuilding?MzEspHeight:Screen.height)-p.y;return true;}
        public static void MzWorldLine(Camera cam,Vector3 a,Vector3 b,float width){Vector2 x,y;if(MzProject(cam,a,out x)&&MzProject(cam,b,out y))MzLine(x.x,x.y,y.x,y.y,width);}
        public static void MzBone(Camera cam,Transform a,Transform b){if(a!=null&&b!=null)MzWorldLine(cam,a.position,b.position,1.5f);}
        public static void MzBox(float x,float y,float w,float h)
        {
            if(MzSwitch[2]){float cw=w*.25f,ch=h*.2f;MzLine(x,y,x+cw,y,1.5f);MzLine(x,y,x,y+ch,1.5f);MzLine(x+w-cw,y,x+w,y,1.5f);MzLine(x+w,y,x+w,y+ch,1.5f);MzLine(x,y+h,x+cw,y+h,1.5f);MzLine(x,y+h-ch,x,y+h,1.5f);MzLine(x+w-cw,y+h,x+w,y+h,1.5f);MzLine(x+w,y+h-ch,x+w,y+h,1.5f);}
            else {MzLine(x,y,x+w,y,1.5f);MzLine(x,y+h,x+w,y+h,1.5f);MzLine(x,y,x,y+h,1.5f);MzLine(x+w,y,x+w,y+h,1.5f);}
        }
        // Fixed screen-coordinate scratch space: no Player/Transform survives a sample.
        public static float[] MzProjectedX,MzProjectedY;
        public static bool[] MzProjectedValid;
        public static string[] MzDistanceLabels;
        public static void MzProjectionScratch()
        {
            if(MzProjectedX!=null)return;MzProjectedX=new float[11];MzProjectedY=new float[11];MzProjectedValid=new bool[11];
        }
        public static void MzProjectSlot(Camera camera,int slot,Vector3 world)
        {
            Vector2 point;bool valid=MzProject(camera,world,out point);MzProjectedValid[slot]=valid;if(valid){MzProjectedX[slot]=point.x;MzProjectedY[slot]=point.y;}
        }
        public static void MzBoneSlot(Camera camera,int slot,Transform bone)
        {
            MzProjectedValid[slot]=false;if(MzDrawingEsp&&MzLagEspSlot>=0&&MzLagBoneValid!=null){int frozen=MzLagEspSlot*11+slot;if(MzLagBoneValid[frozen])MzProjectSlot(camera,slot,MzLagBones[frozen]);return;}if(bone!=null)MzProjectSlot(camera,slot,bone.position);
        }
        public static void MzProjectedEdge(int first,int second)
        {
            var valid=MzProjectedValid;if(!valid[first]||!valid[second])return;var x=MzProjectedX;var y=MzProjectedY;MzLine(x[first],y[first],x[second],y[second],1.5f);
        }
        public static string MzDistanceLabel(float distance)
        {
            if(MzDistanceLabels==null)MzDistanceLabels=new string[501];int value=(int)Mathf.Clamp(distance,0,500);var labels=MzDistanceLabels;
            if(labels[value]==null)labels[value]=value.ToString()+" m";return labels[value];
        }
        public static void MzBox3D(Camera cam,Vector3 root,Vector3 head)
        {
            MzProjectionScratch();float half=Mathf.Clamp((head.y-root.y)*.22f,.15f,1);
            MzProjectSlot(cam,0,new Vector3(root.x-half,root.y,root.z-half));MzProjectSlot(cam,1,new Vector3(root.x+half,root.y,root.z-half));
            MzProjectSlot(cam,2,new Vector3(root.x+half,root.y,root.z+half));MzProjectSlot(cam,3,new Vector3(root.x-half,root.y,root.z+half));
            MzProjectSlot(cam,4,new Vector3(root.x-half,head.y,root.z-half));MzProjectSlot(cam,5,new Vector3(root.x+half,head.y,root.z-half));
            MzProjectSlot(cam,6,new Vector3(root.x+half,head.y,root.z+half));MzProjectSlot(cam,7,new Vector3(root.x-half,head.y,root.z+half));
            MzProjectedEdge(0,1);MzProjectedEdge(1,2);MzProjectedEdge(2,3);MzProjectedEdge(3,0);MzProjectedEdge(4,5);MzProjectedEdge(5,6);MzProjectedEdge(6,7);MzProjectedEdge(7,4);MzProjectedEdge(0,4);MzProjectedEdge(1,5);MzProjectedEdge(2,6);MzProjectedEdge(3,7);
        }
        public static void MzSkeleton(Camera cam,Player player,Vector2 head,Vector2 root)
        {
            MzProjectionScratch();MzProjectedX[0]=head.x;MzProjectedY[0]=head.y;MzProjectedX[1]=root.x;MzProjectedY[1]=root.y;MzProjectedValid[0]=true;MzProjectedValid[1]=true;
            MzBoneSlot(cam,2,player.NeckBone);MzBoneSlot(cam,3,player.ArmBoneLeft);MzBoneSlot(cam,4,player.HandBoneLeft);MzBoneSlot(cam,5,player.ArmBoneRight);MzBoneSlot(cam,6,player.HandBoneRight);
            MzBoneSlot(cam,7,player.LegBoneLeft);MzBoneSlot(cam,8,player.FootBoneLeft);MzBoneSlot(cam,9,player.LegBoneRight);MzBoneSlot(cam,10,player.FootBoneRight);
            MzProjectedEdge(0,2);MzProjectedEdge(2,1);MzProjectedEdge(2,3);MzProjectedEdge(3,4);MzProjectedEdge(2,5);MzProjectedEdge(5,6);MzProjectedEdge(1,7);MzProjectedEdge(7,8);MzProjectedEdge(1,9);MzProjectedEdge(9,10);
        }
        public static void MzSampleEsp()
        {
            MzEspCacheInit();MzEspCacheClear();MzSampledPlayers=0;MzLagEspSlot=-1;
            try{
                var camera=MzEspCamera;var match=MzEspMatch;if(camera==null||match==null||!MzSwitch[0])return;
                if(!MzSwitch[1]&&!MzSwitch[4]&&!MzSwitch[5]&&!MzSwitch[6]&&!MzSwitch[7]&&!MzSwitch[8]&&!MzSwitch[9]&&!MzSwitch[10])return;
                Vector3 origin=camera.transform.position;if(!MzPoint(origin))return;var players=match.ILPNGCMEFFI();if(players==null)return;
                int count=players.Count;if(count>128)count=128;
                for(int i=0;i<count&&MzEspCacheCount<(MzBatchReady?16:3);i++){
                    if(i>0&&Time.realtimeSinceStartup-MzEspSampleStarted>.012f)break;
                    try{
                        if(i>=players.Count)break;var player=players[i] as Player;if(!MzEnemy(player))continue;
                        Vector3 root=player.RootTransform.position,head=player.GetHeadTF().position;if(!MzPoint(root)||!MzPoint(head))continue;float distance=Vector3.Distance(root,origin);if(!MzFinite(distance)||distance>MzRange)continue;
                        MzLagEspSlot=MzLagFind(player);if(MzLagEspSlot>=0){root=MzLagBones[MzLagEspSlot*11+1];head=MzLagHeads[MzLagEspSlot];}
                        Vector2 feet,top;if(!MzProject(camera,root,out feet)||!MzProject(camera,head,out top))continue;
                        float h=feet.y-top.y,w=h*.5f,x=(feet.x+top.x)*.5f-w*.5f;if(!MzFinite(h)||h<=4||h>Screen.height*2||x+w<0||x>Screen.width||feet.y<0||top.y>Screen.height)continue;
                        int actor=MzEspCacheCount;MzEspCacheActor(player,actor,root,head,player.CurHP,distance);MzEspCacheCount++;MzSampledPlayers=MzEspCacheCount;
                    }catch(Exception){}
                }
            }catch(Exception){}finally{MzLagEspSlot=-1;}
        }
        public static Player MzPickTarget()
        {
            MzDropTarget();if(!MzOnMainThread())return null;
            var cam=Camera.main;var match=COW.GameFacade.CurrentMatch();if(cam==null||match==null)return null;var cameraTransform=cam.transform;if(cameraTransform==null)return null;
            Vector3 origin=cameraTransform.position;if(!MzPoint(origin))return null;var list=match.ILPNGCMEFFI();if(list==null)return null;
            Player best=null;float radius=MzEffectiveFov(),bestDistance=radius*radius,rangeSquared=MzRange*MzRange,start=Time.realtimeSinceStartup;
            int count=list.Count;if(count>128)count=128;int first=MzAimCursor;if(first<0||first>=count)first=0;float cx=Screen.width*.5f,cy=Screen.height*.5f;
            for(int visited=0;visited<count;visited++){
                if(visited>0&&Time.realtimeSinceStartup-start>.003f)break;int i=first+visited;if(i>=count)i-=count;MzAimCursor=i+1;if(MzAimCursor>=count)MzAimCursor=0;
                try{
                    if(i>=list.Count)break;var p=list[i] as Player;if(p==null||p.IsLocalPlayer())continue;
                    var obj=p.gameObject;if(obj==null||!obj.activeInHierarchy||p.CurHP<=0||p.IsDieing||p.IsLocalTeammate(false)||p.IsLocalTeammate(true))continue;
                    var root=p.RootTransform;var bone=MzSwitch[11]&&MzAimNeck&&!MzSwitch[12]?p.NeckBone:p.GetHeadTF();if(root==null||bone==null)continue;Vector3 position=root.position,point=bone.position;if(!MzPoint(position)||!MzPoint(point))continue;
                    float rx=position.x-origin.x,ry=position.y-origin.y,rz=position.z-origin.z,range=rx*rx+ry*ry+rz*rz;if(!MzFinite(range)||range>rangeSquared)continue;
                    Vector2 screen;if(!MzProject(cam,MzLagSelectPoint(p,point),out screen))continue;float dx=screen.x-cx,dy=screen.y-cy,distance=dx*dx+dy*dy;
                    if(distance<bestDistance){bestDistance=distance;best=p;MzAimPoint=point;MzAimScreen=screen;}
                }catch(Exception){}
            }
            if(best!=null){MzAimTarget=best;MzAimMatch=match;MzAimCamera=cam;MzAimOrigin=origin;MzAimStamp=Time.realtimeSinceStartup;MzAimLagSlot=MzLagFind(best);}return best;
        }
        public static void MzRunAim()
        {
            if(!MzOnMainThread()||MzAimRunning||MzSilentRunning)return;MzAimRunning=true;
            try {
                MzAutoInputTick();
                if(!MzInitialized||(!MzSwitch[11]&&!MzSwitch[12]&&!MzSwitch[15]&&!MzAimkillEnabled&&!MzLagScope())){MzResetAim();return;}
                var local=COW.GameFacade.GetLocalPlayerOrObServer();if(local==null||!local.IsLocalPlayer()){MzResetAim();return;}var localObject=local.gameObject;
                if(localObject==null||!localObject.activeInHierarchy||local.RootTransform==null||local.CurHP<=0||local.IsDieing){MzResetAim();return;}
                bool firing=local.IsFiring();if(!firing&&!MzSwitch[12]&&!MzSwitch[15]&&!MzAimkillEnabled&&!MzLagScope()){MzResetAim();return;}
                int frame=Time.frameCount;if(MzAimFrame==frame)return;MzAimFrame=frame;MzRestoreSilent();
                // Keep a fresh silent snapshot while idle: the first weapon getter
                // can precede this frame's OnGUI after the trigger is pressed.
                float idleAge=Time.realtimeSinceStartup-MzAimStamp;
                if(!firing&&!MzSwitch[15]&&!MzAimkillEnabled&&!MzLagScope()&&MzAimTarget!=null&&idleAge>=0&&idleAge<.04f&&System.Object.ReferenceEquals(COW.GameFacade.CurrentMatch(),MzAimMatch)&&Camera.main==MzAimCamera&&!MzAimTarget.IsDieing&&MzAimTarget.CurHP>0)return;
                if(MzPickTarget()==null){MzAutoState=1;MzAimkillState=2;MzStopAuto();return;}
                if(MzAimkillEnabled){MzStopAuto();MzAimkillTick(local);return;}
                if(MzSwitch[15]){MzAutoTick(local);return;}else MzStopAuto();
                if(MzLagAimBridge()){MzLagDispatch=true;try{MzSilent(local,local.FDMIEDDNCEC);}finally{MzLagDispatch=false;}}
                if(!firing)return;
                if(MzSwitch[14]){MzTint(1,.76f,.3f,1);MzLine(Screen.width*.5f,Screen.height*.5f,MzAimScreen.x,MzAimScreen.y,1.5f);}
                if(MzSwitch[11]){Vector3 direction=MzAimPoint-MzAimOrigin;float length=direction.x*direction.x+direction.y*direction.y+direction.z*direction.z;if(MzPoint(direction)&&MzFinite(length)&&length>.0001f&&MzAimTarget!=null)local.SetAimRotation(Quaternion.LookRotation(direction),true);}
            }catch(Exception){MzResetAim();}finally{MzAimRunning=false;}
        }
        public static bool MzSame(Vector3 a,Vector3 b){return a.x==b.x&&a.y==b.y&&a.z==b.z;}
        public static bool MzSilentApplied(CGKJLKPMGDJ a)
        {
            return a!=null&&System.Object.ReferenceEquals(a.JHAINOBIFMA,MzAppliedObject)&&System.Object.ReferenceEquals(a.JINDNOFABKB,MzAppliedCollider)&&MzSame(a.HPCOJHGFEEG,MzAppliedPoint)&&MzSame(a.DOJDNHCGFGK,MzAppliedPoint)&&MzSame(a.HLDECMJFKJK,MzAppliedDirection)&&MzSame(a.EKPLMDDKOGB,MzAppliedOrigin)&&MzSame(a.ONMKMEGMHOL,MzAppliedOrigin)&&a.DKPDBLLJEAA==MzAppliedHitType&&!a.EFMBNPNFGHM&&!a.CEOKOILBODN&&a.CLOLKHNMNEK==0;
        }
        public static void MzRestoreSilent()
        {
            try {
                var a=MzChangedAim;
                // Restore each field only while it is still our value. A native
                // rewrite of one field must not leave all other owned values stuck.
                if(a!=null&&MzAimOwner!=null&&System.Object.ReferenceEquals(MzAimOwner.FDMIEDDNCEC,a)){
                    if(System.Object.ReferenceEquals(a.JHAINOBIFMA,MzAppliedObject))a.JHAINOBIFMA=MzOldAimObject!=null?MzOldAimObject:null;
                    if(System.Object.ReferenceEquals(a.JINDNOFABKB,MzAppliedCollider))a.JINDNOFABKB=MzOldAimCollider!=null?MzOldAimCollider:null;
                    if(MzSame(a.HPCOJHGFEEG,MzAppliedPoint))a.HPCOJHGFEEG=MzOldHpc;if(MzSame(a.DOJDNHCGFGK,MzAppliedPoint))a.DOJDNHCGFGK=MzOldDoj;
                    if(MzSame(a.HLDECMJFKJK,MzAppliedDirection))a.HLDECMJFKJK=MzOldDirection;if(MzSame(a.EKPLMDDKOGB,MzAppliedOrigin))a.EKPLMDDKOGB=MzOldOrigin;if(MzSame(a.ONMKMEGMHOL,MzAppliedOrigin))a.ONMKMEGMHOL=MzOldOnm;
                    if(a.DKPDBLLJEAA==MzAppliedHitType)a.DKPDBLLJEAA=MzOldDkp;if(!a.EFMBNPNFGHM)a.EFMBNPNFGHM=MzOldEfm;if(!a.CEOKOILBODN)a.CEOKOILBODN=MzOldCeo;if(a.CLOLKHNMNEK==0)a.CLOLKHNMNEK=MzOldClo;
                }
            }catch(Exception){}finally{MzChangedAim=null;MzAimOwner=null;MzOldAimObject=null;MzOldAimCollider=null;MzAppliedObject=null;MzAppliedCollider=null;MzSilentFrame=-1;}
        }
        public static void MzSilent(Player local,CGKJLKPMGDJ result)
        {
            // The weapon getter may run many times or from worker threads. Never
            // discover targets or call camera APIs here. Consume one short-lived
            // snapshot prepared by the driver's main-thread repaint instead.
            if(!MzInitialized||!MzOnMainThread()||MzSilentRunning||(MzAimRunning&&!MzAutoDispatch&&!MzLagDispatch&&!MzAimkillDispatch)||MzRendering)return;
            if(!MzSwitch[12]&&!MzSwitch[15]&&!MzLagScope()&&!MzAimkillDispatch&&MzChangedAim==null)return;MzSilentRunning=true;
            try {
                if(local==null||!local.IsLocalPlayer())return;var localObject=local.gameObject;bool lag=MzLagAimBridge();
                if(!MzAuthorized()||(!MzSwitch[12]&&!MzSwitch[15]&&!lag&&!MzAimkillDispatch)||result==null||localObject==null||!localObject.activeInHierarchy||local.RootTransform==null||local.CurHP<=0||local.IsDieing){MzRestoreSilent();MzDropTarget();return;}
                if(!MzAutoDispatch&&!MzAimkillDispatch&&!local.IsFiring()&&!lag){MzRestoreSilent();return;}
                bool preferHead=MzSilentHeadAllowed();
                int frame=Time.frameCount;var target=MzAimTarget;float age=Time.realtimeSinceStartup-MzAimStamp;
                if(age<0||age>.1f||frame-MzAimFrame>2||target==null||!System.Object.ReferenceEquals(COW.GameFacade.CurrentMatch(),MzAimMatch)){MzRestoreSilent();MzDropTarget();return;}
                var obj=target.gameObject;var head=target.GetHeadTF();
                if(obj==null||!obj.activeInHierarchy||head==null||target.CurHP<=0||target.IsDieing||target.IsLocalTeammate(false)||target.IsLocalTeammate(true)){MzRestoreSilent();MzDropTarget();return;}
                if(MzSilentFrame==frame&&MzSilentWasHead==preferHead&&MzAppliedCollider!=null&&(preferHead?target.HeadCollider==MzAppliedCollider:MzAppliedCollider.enabled)&&System.Object.ReferenceEquals(result,MzChangedAim)&&MzSilentApplied(result))return;MzRestoreSilent();
                Collider collider;Vector3 point;if(preferHead){collider=target.HeadCollider;point=MzLagLivePoint(target,head.position);}else if(!MzBodyHit(target,out collider,out point))return;
                Vector3 origin=local.AimStartPostion,delta=point-origin;float length=delta.x*delta.x+delta.y*delta.y+delta.z*delta.z;if(collider==null||!MzPoint(point)||!MzPoint(origin)||!MzFinite(length)||length<=.0001f)return;Vector3 direction=Vector3.Normalize(delta);if(!MzPoint(direction))return;MzSilentWasHead=preferHead;
                MzChangedAim=result;MzAimOwner=local;MzAppliedObject=obj;MzAppliedCollider=collider;MzOldAimObject=result.JHAINOBIFMA;MzOldAimCollider=result.JINDNOFABKB;MzOldHpc=result.HPCOJHGFEEG;MzOldDoj=result.DOJDNHCGFGK;MzOldDirection=result.HLDECMJFKJK;MzOldOrigin=result.EKPLMDDKOGB;MzOldOnm=result.ONMKMEGMHOL;MzOldDkp=result.DKPDBLLJEAA;MzOldEfm=result.EFMBNPNFGHM;MzOldCeo=result.CEOKOILBODN;MzOldClo=result.CLOLKHNMNEK;MzAppliedPoint=point;MzAppliedOrigin=origin;MzAppliedDirection=direction;
                result.JHAINOBIFMA=obj;result.JINDNOFABKB=collider;result.HPCOJHGFEEG=point;result.DOJDNHCGFGK=point;result.HLDECMJFKJK=direction;result.EKPLMDDKOGB=origin;result.ONMKMEGMHOL=origin;MzAppliedHitType=preferHead?LKOOALMKJND.Head:LKOOALMKJND.Body;result.DKPDBLLJEAA=MzAppliedHitType;result.EFMBNPNFGHM=false;result.CEOKOILBODN=false;result.CLOLKHNMNEK=0;MzSilentFrame=frame;
            }catch(Exception){MzRestoreSilent();MzDropTarget();}finally{MzSilentRunning=false;}
        }
    }
}
