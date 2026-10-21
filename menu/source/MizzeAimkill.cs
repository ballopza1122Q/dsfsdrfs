using System;
using System.Reflection;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static bool MzAimkillEnabled,MzAimkillDispatch;
        public static int MzAimkillState,MzAimkillFrame,MzAimkillCalls,MzAimkillResult;
        public static float MzAimkillNext,MzAimkillBindAfter;
        public static MethodInfo MzAimkillHit,MzAimkillDamage;
        public static FieldInfo MzAimkillOwnerField;
        public static object[] MzAimkillHitArgs,MzAimkillDamageArgs;
        // TH 1.132.1 metadata slot223 and real ARM64 caller 0x5C5A654:
        // CIHHEOKFADJ(aimInfo, null, 0f). This full native entry runs
        // CHBHFOIAMDE before LHNJLFOINDO; native code constructs DamageInfo
        // and its check list. Do not call the hit-only helper, forge packets,
        // cast raw structs, or synthesize a damage amount in this patch.
        public static bool MzAimkillBind()
        {
            if(MzAimkillHit!=null)return true;float now=Time.realtimeSinceStartup;if(now<MzAimkillBindAfter)return false;MzAimkillBindAfter=now+2;
            try{
                var type=typeof(HBIBDMMOOOK);var hit=type.GetMethod("CIHHEOKFADJ",BindingFlags.Instance|BindingFlags.Public);
                var damage=type.GetMethod("JEMMELJLKCN",BindingFlags.Instance|BindingFlags.Public);
                var ownerType=type.Assembly.GetType("COW.GamePlay.CDMMCIIFHGM");if(hit==null||damage==null||ownerType==null||hit.ReturnType!=typeof(int)||damage.ReturnType!=typeof(int))return false;
                var owner=ownerType.GetField("<FDEPGLKBBKP>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
                var hp=hit.GetParameters();var dp=damage.GetParameters();
                if(owner==null||owner.FieldType!=typeof(Player)||hp.Length!=3||hp[0].ParameterType!=typeof(CGKJLKPMGDJ)||hp[1].ParameterType.FullName!="UnityEngine.MonoBehaviour"||hp[2].ParameterType!=typeof(float)||dp.Length!=1||dp[0].ParameterType!=typeof(float))return false;
                MzAimkillHitArgs=new object[3];MzAimkillHitArgs[2]=0f;MzAimkillDamageArgs=new object[1];
                MzAimkillOwnerField=owner;MzAimkillDamage=damage;MzAimkillHit=hit;return true;
            }catch(Exception){return false;}
        }
        public static void MzAimkillClearArgs()
        {
            if(MzAimkillHitArgs!=null)MzAimkillHitArgs[0]=null;
            if(MzAimkillDamageArgs!=null)MzAimkillDamageArgs[0]=null;
            MzAimkillDispatch=false;
        }
        public static void MzAimkillTick(Player local)
        {
            if(!MzAimkillEnabled)return;if(!MzOnMainThread()||!MzAuthorized()){MzAimkillState=0;return;}
            int frame=Time.frameCount;if(MzAimkillFrame==frame)return;MzAimkillFrame=frame;
            CGKJLKPMGDJ info=null;int oldDamage=0,damage=0;float oldDistance=0,distance=0;bool changed=false;
            try{
                var target=MzAimTarget;var match=COW.GameFacade.CurrentMatch();
                if(local==null||!local.IsLocalPlayer()||local.gameObject==null||!local.gameObject.activeInHierarchy||local.CurHP<=0||local.IsDieing||match==null){MzAimkillState=1;return;}
                if(target==null||!MzEnemy(target)){MzAimkillState=2;return;}
                // Never double-dispatch alongside a real manual burst.
                if(local.IsFiring()){MzAimkillState=3;return;}
                var weapon=local.GetWeaponOnHand();if(weapon==null){MzAimkillState=4;return;}
                if(!MzAutoGun(weapon)||!(MzAutoKind==0||MzAutoKind==1||MzAutoKind==2||MzAutoKind==6)){MzAimkillState=5;return;}
                if(!MzAimkillBind()){MzAimkillState=6;return;}
                if((MzAimkillOwnerField.GetValue(weapon) as Player)!=local){MzAimkillState=7;return;}
                float now=Time.realtimeSinceStartup;if(now<MzAimkillNext){MzAimkillState=8;return;}
                float interval=weapon.LDKCJEMDAMC();if(!MzFinite(interval)||interval<=0){MzAimkillState=9;return;}if(interval<.25f)interval=.25f;
                if(!MzAutoClear(local,target)){MzAimkillState=10;return;}
                info=local.FDMIEDDNCEC;if(info==null){MzAimkillState=11;return;}
                MzAimkillDispatch=true;MzSilent(local,info);
                if(!System.Object.ReferenceEquals(MzChangedAim,info)||!MzSilentApplied(info)){MzAimkillState=11;return;}
                distance=Vector3.Distance(MzAppliedOrigin,MzAppliedPoint);if(!MzFinite(distance)||distance<=0||distance>MzRange){MzAimkillState=11;return;}
                // Slot230 calculates the actual weapon's damage for this distance.
                // ARM64 callers write its result to aimInfo.OLGEMBBDBID (+0x58).
                MzAimkillDamageArgs[0]=distance;object calculated=MzAimkillDamage.Invoke(weapon,MzAimkillDamageArgs);
                if(calculated==null||(damage=(int)calculated)<=0){MzAimkillState=12;return;}
                if(!MzAuthorized()||local.IsFiring()||!System.Object.ReferenceEquals(COW.GameFacade.CurrentMatch(),match)||!System.Object.ReferenceEquals(local.GetWeaponOnHand(),weapon)||(MzAimkillOwnerField.GetValue(weapon) as Player)!=local||!MzEnemy(target)||!MzSilentApplied(info)){MzAimkillState=7;return;}
                oldDamage=info.OLGEMBBDBID;oldDistance=info.CFIPDNFBKHK;changed=true;
                info.OLGEMBBDBID=damage;info.CFIPDNFBKHK=distance;MzAimkillNext=now+interval;
                MzAimkillHitArgs[0]=info;MzAimkillResult=0;
                object result=MzAimkillHit.Invoke(weapon,MzAimkillHitArgs);MzAimkillCalls++;
                if(result!=null)MzAimkillResult=(int)result;
                // A local native return is not a server damage acknowledgement.
                MzAimkillState=MzAimkillResult>0?13:14;
            }catch(Exception){MzAimkillState=15;MzAimkillNext=Time.realtimeSinceStartup+1;}
            finally{
                if(changed&&info!=null){if(info.OLGEMBBDBID==damage)info.OLGEMBBDBID=oldDamage;if(info.CFIPDNFBKHK==distance)info.CFIPDNFBKHK=oldDistance;}
                MzRestoreSilent();MzAimkillClearArgs();
            }
        }
        public static void MzAimkillRow(float x,float y)
        {
            MzRowVisual(x,y,MzAimkillEnabled?1:0,"Aimkill · Native (thử nghiệm)",0);
            if(MzTap(x,y,354,40)){MzAimkillEnabled=!MzAimkillEnabled;if(MzAimkillEnabled)MzSwitch[15]=false;MzResetAim();MzAimkillNext=0;MzAimkillFrame=-1;MzAimkillState=0;}
        }
        public static void MzAimkillStatus(float x,float y)
        {
            if(!MzAimkillEnabled)return;int state=MzAimkillState;
            string text=state==1?"Chờ trận và người chơi":state==2?"Chờ mục tiêu":state==3?"Đang bắn tay":state==4?"Chưa cầm súng":state==5?"Aimkill chưa hỗ trợ loại súng này":state==6?"API Aimkill chưa khớp bản game":state==7?"Ngữ cảnh súng đã thay đổi":state==8?"Chờ nhịp Aimkill":state==9?"Nhịp súng không hợp lệ":state==10?"Mục tiêu bị che":state==11?"Chờ dữ liệu trúng đích":state==12?"Sát thương native chưa hợp lệ":state==13?"Native đã xử lý · chưa xác nhận server":state==14?"Native chưa báo trúng đích":state==15?"Lỗi gọi native · đang chờ thử lại":"Chờ Aimkill";
            MzTint(.85f,.76f,.65f,MzOpenAnim);MzCenter(x,y,354,24,text);
        }
    }
}
