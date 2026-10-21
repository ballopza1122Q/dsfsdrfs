using System;
using UnityEngine;
using System.Reflection;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static HBIBDMMOOOK MzAutoWeapon,MzAutoCheckedWeapon;
        public static Player MzAutoOwner;
        public static JMAGGLCNGIG MzAutoMatch;
        public static bool MzAutoDispatch,MzAutoTypeValid;
        public static MethodInfo MzAutoKindMethod;
        public static int MzAutoKind;
        public static float MzAutoKindBindNext;
        public static float MzAutoNext,MzAutoRetry,MzAutoPressedAt;
        public static object MzAutoButton;
        public static MethodInfo MzInputExists,MzInputGet,MzInputReference,MzInputDown,MzInputUp;
        public static object[] MzInputArgs;
        public static string MzInputName;
        public static float MzInputBindNext;
        public static int MzInputBindAttempts;
        // Optional input types and methods must never be patch-loader dependencies.
        // Resolve only after the user enables Autofire; failure leaves GUI intact.
        public static bool MzBindInput()
        {
            if(MzInputDown!=null)return true;float now=Time.realtimeSinceStartup;if(now<MzInputBindNext)return false;
            MzInputBindNext=now+2;MzInputBindAttempts++;
            try{
                string name=MzInputName;if(name==null)name="UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager";
                var type=typeof(Player).Assembly.GetType(name);if(type==null)return false;
                var exists=type.GetMethod("ButtonExists");var get=type.GetMethod("GetButton");var reference=type.GetMethod("VirtualButtonReference");var down=type.GetMethod("SetButtonDown");var up=type.GetMethod("SetButtonUp");
                if(exists==null||get==null||reference==null||down==null||up==null)return false;
                MzInputArgs=new object[1];MzInputArgs[0]="Action1";MzInputExists=exists;MzInputGet=get;MzInputReference=reference;MzInputUp=up;MzInputDown=down;return true;
            }catch(Exception){return false;}
        }
        public static object MzInputCall(MethodInfo method){return method.Invoke(null,MzInputArgs);}
        public static int MzAutoPressedFrame,MzAutoState,MzAutoRequests,MzAutoObserved;
        public static bool MzAutoHeld,MzAutoReleased;
        public static bool MzAutoContinuous;
        public static float MzAutoInputUntil;
        public static RaycastHit[] MzAutoHits;
        public static int MzAutoCoverState;
        public static int MzAutoReleasedFrame;
        // UIHudFireController's constructor selects Action1; its base OnBtnDown
        // and OnBtnUp call this exact input manager. No new game hooks are added.
        public static void MzReleaseAutoButton()
        {
            var button=MzAutoButton;bool held=MzAutoHeld;MzAutoHeld=false;MzAutoButton=null;MzAutoContinuous=false;MzAutoInputUntil=0;
            if(!held||button==null)return;MzAutoReleased=true;MzAutoReleasedFrame=Time.frameCount;
            try{if(System.Object.ReferenceEquals(MzInputCall(MzInputReference),button))MzInputCall(MzInputUp);}catch(Exception){}
        }
        public static void MzAutoInputTick()
        {
            if(!MzOnMainThread()||!MzAutoHeld)return;
            // A press made after Update must survive the next game Update. Give
            // every pulse two update opportunities, then release even on no target.
            if(MzAutoContinuous){if(Time.realtimeSinceStartup>=MzAutoInputUntil)MzReleaseAutoButton();return;}
            if(Time.frameCount-MzAutoPressedFrame>=2||Time.realtimeSinceStartup-MzAutoPressedAt>=.12f)MzReleaseAutoButton();
        }
        // HUD input is consumed by the game's weapon implementation. A legitimate
        // firearm may override its fire/stop methods; DeclaringType is not its kind.
        // Read the native weapon category lazily, without adding an enum dependency
        // to the patch loader. Re-read data so an initially unready weapon recovers.
        public static bool MzAutoGun(HBIBDMMOOOK weapon)
        {
            MzAutoCheckedWeapon=weapon;MzAutoTypeValid=false;MzAutoKind=-1;if(weapon==null)return false;
            try{
                var method=MzAutoKindMethod;if(method==null){float now=Time.realtimeSinceStartup;if(now<MzAutoKindBindNext)return false;MzAutoKindBindNext=now+2;method=typeof(HBIBDMMOOOK).GetMethod("JBHDCDGJDPK");if(method==null)return false;MzAutoKindMethod=method;}
                var value=method.Invoke(weapon,null);if(value==null)return false;int kind=Convert.ToInt32(value);MzAutoKind=kind;
                // ELNCBDCJNLC from TH 1.132.1: Rifle, Sniper, HandGun, ShotGun,
                // SMG, CrossBow, ExternalMachineGun, HeavyWeapon, UGCSprayRifle.
                MzAutoTypeValid=kind==0||kind==1||kind==2||kind==4||kind==6||kind==8||kind==10||kind==13||kind==28;
                return MzAutoTypeValid;
            }catch(Exception){return false;}
        }
        public static void MzStopAuto()
        {
            if(!MzOnMainThread())return;MzReleaseAutoButton();MzAutoPoseClear();if(MzAutoHits!=null)Array.Clear(MzAutoHits,0,MzAutoHits.Length);MzAutoWeapon=null;MzAutoOwner=null;MzAutoMatch=null;MzAutoNext=0;
        }
        public static bool MzAutoClear(Player local,Player target)
        {
            MzAutoCoverState=3;var head=target.GetHeadTF();var root=target.RootTransform;if(head==null||root==null)return false;
            Vector3 origin=MzAimOrigin,point=head.position,delta=point-origin;float distance=Vector3.Distance(point,origin);
            if(!MzPoint(origin)||!MzPoint(point)||!MzFinite(distance)||distance<=.001f)return false;
            if(MzAutoHits==null)MzAutoHits=new RaycastHit[16];else Array.Clear(MzAutoHits,0,MzAutoHits.Length);int count=Physics.RaycastNonAlloc(origin,Vector3.Normalize(delta),MzAutoHits,distance,-5,QueryTriggerInteraction.Ignore);
            if(count<0||count>=16){MzAutoCoverState=15;return false;}float wall=distance+1,targetAt=distance+1;
            for(int i=0;i<count;i++){
                var hit=MzAutoHits[i];var collider=hit.collider;float at=hit.distance;if(collider==null||!MzFinite(at)||at<0){MzAutoCoverState=15;return false;}
                var transform=collider.transform;if(transform==null){MzAutoCoverState=15;return false;}
                var localRoot=local.RootTransform;var localObject=local.gameObject;
                if(collider==local.HeadCollider||(localRoot!=null&&transform.IsChildOf(localRoot))||(localObject!=null&&transform.IsChildOf(localObject.transform)))continue;
                var targetObject=target.gameObject;bool own=collider==target.HeadCollider||transform.IsChildOf(root)||(targetObject!=null&&transform.IsChildOf(targetObject.transform));
                if(own){if(at<targetAt)targetAt=at;}else if(at<wall)wall=at;
            }
            return wall>distance||targetAt<wall;
        }
        public static void MzAutoTick(Player local)
        {
            try{
                MzAutoInputTick();
                if(!MzSwitch[15]||!MzAuthorized()||!MzOnMainThread()||local==null||!local.IsLocalPlayer()||local.IsDieing){MzAutoState=0;MzStopAuto();return;}
                var target=MzAimTarget;var match=COW.GameFacade.CurrentMatch();var weapon=local.GetWeaponOnHand();float now=Time.realtimeSinceStartup;
                if(MzAutoWeapon!=null&&(!System.Object.ReferenceEquals(weapon,MzAutoWeapon)||!System.Object.ReferenceEquals(local,MzAutoOwner)||!System.Object.ReferenceEquals(match,MzAutoMatch)))MzStopAuto();
                if(target==null||!MzEnemy(target)){MzAutoState=1;MzStopAuto();return;}
                if(weapon==null){MzAutoState=2;MzStopAuto();return;}
                // The native local inventory is authoritative. DHNEEGMCLNO goes
                // through a separate owner/proxy route and need not return this
                // exact managed reference. No native shot is dispatched on it.
                if(!System.Object.ReferenceEquals(local.GetWeaponOnHand(),weapon)){MzAutoState=10;MzStopAuto();return;}
                if(!MzAutoGun(weapon)){MzAutoState=MzAutoKind<0||MzAutoKind==23?14:11;MzStopAuto();return;}
                if(!MzAutoClear(local,target)){MzAutoState=MzAutoCoverState;MzStopAuto();return;}
                if(!MzBindInput()||!(bool)MzInputCall(MzInputExists)){MzAutoState=8;MzStopAuto();return;}
                bool firing=local.IsFiring();if(MzAutoWeapon==null&&(firing||(bool)MzInputCall(MzInputGet))){MzAutoState=4;return;}
                if(firing){MzAutoObserved++;MzAutoState=6;}
                if(now<MzAutoRetry)return;
                float interval=weapon.LDKCJEMDAMC();if(!MzFinite(interval)||interval<=0){MzAutoState=12;MzStopAuto();return;}if(interval<.04f)interval=.04f;
                if(!System.Object.ReferenceEquals(COW.GameFacade.CurrentMatch(),match)||!System.Object.ReferenceEquals(local.GetWeaponOnHand(),weapon)||!MzEnemy(target)||local.IsDieing){MzStopAuto();return;}
                Vector3 direction=MzAimPoint-MzAimOrigin;float length=direction.x*direction.x+direction.y*direction.y+direction.z*direction.z;if(!MzPoint(direction)||!MzFinite(length)||length<=.0001f){MzStopAuto();return;}
                local.SetAimRotation(Quaternion.LookRotation(direction),true);
                if(!System.Object.ReferenceEquals(COW.GameFacade.CurrentMatch(),match)||!System.Object.ReferenceEquals(local.GetWeaponOnHand(),weapon)||!MzEnemy(target)||local.IsDieing){MzStopAuto();return;}
                MzAutoDispatch=true;MzSilent(local,local.FDMIEDDNCEC);
                if(MzAutoHeld){
                    if(MzAutoContinuous){MzAutoInputUntil=now+.15f;if(MzAutoPoseCount>0)MzAutoPoseUntil=now+.5f;if(!firing&&now-MzAutoPressedAt>Mathf.Clamp(interval*2,.25f,2))MzReleaseAutoButton();}
                    if(!firing)MzAutoState=5;return;
                }
                if(now<MzAutoNext||(MzAutoReleased&&Time.frameCount-MzAutoReleasedFrame<=1)){if(!firing)MzAutoState=9;return;}
                var button=MzInputCall(MzInputReference);if(button==null){MzAutoState=8;return;}
                // Leave a new manual press alone between our pulses.
                if(!MzAutoHeld&&(bool)MzInputCall(MzInputGet)){MzAutoState=4;return;}
                MzAutoOwner=local;MzAutoWeapon=weapon;MzAutoMatch=match;MzAutoNext=now+interval;MzAutoDispatch=true;
                MzAutoButton=button;MzAutoHeld=true;MzAutoPressedFrame=Time.frameCount;MzAutoPressedAt=now;
                MzAutoContinuous=MzAutoKind==0||MzAutoKind==6||MzAutoKind==10||MzAutoKind==13||MzAutoKind==28;MzAutoInputUntil=now+.15f;
                MzAutoPosePrepare(local);MzSilent(local,local.FDMIEDDNCEC);MzInputCall(MzInputDown);MzAutoRequests++;MzAutoState=5;
            }catch(Exception){MzStopAuto();MzAutoState=7;MzAutoRetry=Time.realtimeSinceStartup+.5f;}
            finally{MzAutoDispatch=false;}
        }
        public static void MzAutoStatus(float x,float y)
        {
            if(!MzSwitch[15])return;int state=MzAutoState;
            string text=state==1?"Chờ mục tiêu":state==2?"Chưa cầm súng":state==3?"Mục tiêu bị che":state==4?"Đang bắn tay":state==5?"Đã gửi cò bắn":state==6?"Game đang bắn":state==7?"Lỗi cầu nối bắn":state==8?"Chờ nút bắn của game":state==9?"Chờ nhịp súng":state==10?"Đang đổi súng":state==11?"Vũ khí chưa hỗ trợ":state==12?"Nhịp súng chưa hợp lệ":state==14?"Chờ dữ liệu súng":state==15?"Đường ngắm chưa xác định":"Đang chờ";
            MzTint(.65f,.86f,.83f,MzOpenAnim);MzCenter(x,y,354,24,text);
        }
    }
}
