using System;
using System.Reflection;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static bool MzNoReloadEnabled,MzRapidEnabled;
        public static MethodInfo MzAttributesGet,MzAmmoGet,MzAmmoSet,MzRateGet,MzRateSet;
        public static FieldInfo MzAttributesOwnerField,MzReloadField;
        public static object MzWeaponAttributes;
        public static Player MzFeatureOwner;
        public static JMAGGLCNGIG MzFeatureMatch;
        public static bool[] MzFeatureOwned,MzFeatureOriginal;
        public static float MzRateOriginal,MzRateWritten,MzWeaponBindAfter,MzWeaponNext;
        public static object[] MzWeaponArgs;
        public static int MzWeaponFeatureState,MzWeaponFeatureFrame;
        public static bool MzWeaponBinding()
        {
            if(MzRateSet!=null)return true;float now=Time.realtimeSinceStartup;if(now<MzWeaponBindAfter)return false;MzWeaponBindAfter=now+2;
            try{
                var getter=typeof(Player).GetMethod("get_Attributes");if(getter==null)return false;var type=getter.ReturnType;
                if(type.FullName!="COW.GamePlay.PlayerAttributes")return false;
                var owner=type.GetField("BDOEGBMMEOK",BindingFlags.Instance|BindingFlags.NonPublic);
                var reload=type.GetField("ShootNoReload",BindingFlags.Instance|BindingFlags.Public);
                MethodInfo ammoGet=type.GetMethod("get_IsAmmoFree"),ammoSet=type.GetMethod("set_IsAmmoFree");
                MethodInfo rateGet=type.GetMethod("get_FireIntervalScale"),rateSet=type.GetMethod("set_FireIntervalScale");
                if(owner==null||reload==null||ammoGet==null||ammoSet==null||rateGet==null||rateSet==null||reload.FieldType!=typeof(bool))return false;
                MzAttributesGet=getter;MzAttributesOwnerField=owner;MzReloadField=reload;MzAmmoGet=ammoGet;MzAmmoSet=ammoSet;MzRateGet=rateGet;MzRateSet=rateSet;
                MzFeatureOwned=new bool[3];MzFeatureOriginal=new bool[2];MzWeaponArgs=new object[1];return true;
            }catch(Exception){return false;}
        }
        public static bool MzFeatureIdentity(object attributes,Player owner)
        {
            var component=attributes as Component;return owner!=null&&component!=null&&component.gameObject!=null&&MzAttributesOwnerField!=null&&(MzAttributesOwnerField.GetValue(attributes) as Player)==owner;
        }
        public static void MzWeaponForget()
        {
            MzWeaponAttributes=null;MzFeatureOwner=null;MzFeatureMatch=null;if(MzFeatureOwned!=null)for(int i=0;i<3;i++)MzFeatureOwned[i]=false;
        }
        public static bool MzFeatureBool(object attributes,int index,bool enabled)
        {
            if(!enabled&&!MzFeatureOwned[index])return true;
            bool value=(bool)(index==0?MzReloadField.GetValue(attributes):MzAmmoGet.Invoke(attributes,null));
            if(!enabled){if(!MzFeatureOwned[index])return true;if(!value||value==MzFeatureOriginal[index]){MzFeatureOwned[index]=false;return true;}value=MzFeatureOriginal[index];}
            else {if(value)return true;MzFeatureOriginal[index]=value;value=true;}
            if(index==0)MzReloadField.SetValue(attributes,value);else {MzWeaponArgs[0]=value;MzAmmoSet.Invoke(attributes,MzWeaponArgs);}
            MzFeatureOwned[index]=enabled;return true;
        }
        public static bool MzRateSame(float a,float b){return Math.Abs(a-b)<=.0001f*Math.Max(1,Math.Abs(b));}
        public static bool MzFeatureRate(object attributes,bool enabled)
        {
            if(!enabled&&!MzFeatureOwned[2])return true;
            float value=(float)MzRateGet.Invoke(attributes,null);if(!MzFinite(value)||value<.05f||value>16)return false;
            if(!enabled){if(!MzFeatureOwned[2])return true;if(!MzRateSame(value,MzRateWritten)){MzFeatureOwned[2]=false;return true;}value=MzRateOriginal;}
            else {if(!MzFeatureOwned[2]||!MzRateSame(value,MzRateWritten))MzRateOriginal=value;value=Math.Max(.05f,MzRateOriginal*.5f);if(MzRateSame(value,(float)MzRateGet.Invoke(attributes,null)))return true;}
            MzWeaponArgs[0]=value;MzRateSet.Invoke(attributes,MzWeaponArgs);MzRateWritten=value;MzFeatureOwned[2]=enabled;return true;
        }
        public static bool MzWeaponRestore()
        {
            if(MzWeaponAttributes==null)return true;
            try{if(!MzFeatureIdentity(MzWeaponAttributes,MzFeatureOwner)){MzWeaponForget();return true;}}catch(Exception){return false;}
            bool ok=true;
            for(int i=0;i<2;i++)try{if(!MzFeatureBool(MzWeaponAttributes,i,false))ok=false;}catch(Exception){ok=false;}
            try{if(!MzFeatureRate(MzWeaponAttributes,false))ok=false;}catch(Exception){ok=false;}
            if(ok)MzWeaponForget();return ok;
        }
        public static void MzWeaponFeaturesTick()
        {
            if(!MzOnMainThread())return;bool enabled=MzAuthorized()&&(MzNoReloadEnabled||MzRapidEnabled);
            if(!enabled&&MzWeaponAttributes==null){MzWeaponFeatureState=0;return;}
            int frame=Time.frameCount;if(MzWeaponFeatureFrame==frame&&Time.realtimeSinceStartup<MzWeaponNext)return;MzWeaponFeatureFrame=frame;MzWeaponNext=Time.realtimeSinceStartup+.05f;
            if(!enabled){MzWeaponFeatureState=MzWeaponRestore()?0:6;return;}
            try{
                var match=COW.GameFacade.CurrentMatch();var local=COW.GameFacade.GetLocalPlayerOrObServer();
                if(match==null||local==null||!local.IsLocalPlayer()||local.gameObject==null||!local.gameObject.activeInHierarchy||local.CurHP<=0||local.IsDieing){MzWeaponFeatureState=MzWeaponRestore()?1:6;return;}
                if(!MzWeaponBinding()){MzWeaponFeatureState=4;return;}
                var attributes=MzAttributesGet.Invoke(local,null);if(attributes==null){MzWeaponFeatureState=3;return;}
                if(!MzFeatureIdentity(attributes,local)){MzWeaponFeatureState=3;return;}
                if(!System.Object.ReferenceEquals(attributes,MzWeaponAttributes)||local!=MzFeatureOwner){
                    bool transfer=local==MzFeatureOwner&&System.Object.ReferenceEquals(match,MzFeatureMatch);bool rate=transfer&&MzFeatureOwned[2];float original=MzRateOriginal,written=MzRateWritten;
                    bool reload=transfer&&MzFeatureOwned[0]&&(bool)MzReloadField.GetValue(attributes),ammo=transfer&&MzFeatureOwned[1]&&(bool)MzAmmoGet.Invoke(attributes,null);
                    bool reloadOriginal=MzFeatureOriginal[0],ammoOriginal=MzFeatureOriginal[1];rate=rate&&MzRateSame((float)MzRateGet.Invoke(attributes,null),written);
                    if(!MzWeaponRestore()){MzWeaponFeatureState=6;return;}
                    MzWeaponAttributes=attributes;MzFeatureOwner=local;MzFeatureMatch=match;
                    if(reload){MzFeatureOwned[0]=true;MzFeatureOriginal[0]=reloadOriginal;}if(ammo){MzFeatureOwned[1]=true;MzFeatureOriginal[1]=ammoOriginal;}
                    if(rate){MzFeatureOwned[2]=true;MzRateOriginal=original;MzRateWritten=written;}
                }else MzFeatureMatch=match;
                if(!System.Object.ReferenceEquals(COW.GameFacade.CurrentMatch(),match)||COW.GameFacade.GetLocalPlayerOrObServer()!=local||!System.Object.ReferenceEquals(MzAttributesGet.Invoke(local,null),attributes)){MzWeaponFeatureState=3;return;}
                bool first=MzFeatureBool(attributes,0,MzNoReloadEnabled),second=MzFeatureBool(attributes,1,MzNoReloadEnabled),third=MzFeatureRate(attributes,MzRapidEnabled);
                MzWeaponFeatureState=first&&second&&third?2:5;
            }catch(Exception){MzWeaponFeatureState=6;}
        }
        public static void MzWeaponFeatureRow(float x,float y,bool rapid)
        {
            bool enabled=rapid?MzRapidEnabled:MzNoReloadEnabled;MzRowVisual(x,y,enabled?1:0,rapid?"Bắn nhanh (2×)":"Không nạp đạn",0);
            if(MzTap(x,y,354,40)){if(rapid)MzRapidEnabled=!enabled;else MzNoReloadEnabled=!enabled;MzWeaponNext=0;MzWeaponFeatureFrame=-1;MzWeaponFeaturesTick();}
        }
        public static void MzWeaponFeatureStatus(float x,float y)
        {
            if(!MzNoReloadEnabled&&!MzRapidEnabled)return;
            string text=MzWeaponFeatureState==2?"Thông số súng đã áp dụng":MzWeaponFeatureState==1?"Chờ trận và người chơi":MzWeaponFeatureState==3?"Chờ thuộc tính người chơi":MzWeaponFeatureState==4?"API súng chưa sẵn sàng":MzWeaponFeatureState==5?"Hệ số súng không hợp lệ":MzWeaponFeatureState==6?"Chưa áp dụng được thông số súng":"Đang chờ";
            MzTint(.65f,.86f,.83f,MzOpenAnim);MzCenter(x,y,354,24,text);
        }
    }
}
