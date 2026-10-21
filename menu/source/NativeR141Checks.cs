using System;
using System.IO;
using System.Linq;
using UnityEngine;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static void WeaponTick(){Time.Frame++;Time.Now+=.06f;Run("MzWeaponFeaturesTick");}
    static void NativeR141Checks(string[] args)
    {
        int before=Checks;Player local,target;R11Setup(out local,out target);Set("MzLeaseDeadline",100000f);
        var attributes=new PlayerAttributes(local);local.Attributes=attributes;
        Set("MzNoReloadEnabled",true);WeaponTick();Check(attributes.ShootNoReload&&attributes.IsAmmoFree&&attributes.FireIntervalScale==1,"no reload enables the actual two native attribute flags without changing rate");
        Set("MzRapidEnabled",true);WeaponTick();Check(attributes.FireIntervalScale==.5f&&Get<int>("MzWeaponFeatureState")==2,"rapid fire halves the native interval multiplier");
        int writes=attributes.RateWrites,ammoWrites=attributes.AmmoWrites;
        for(int i=0;i<1000;i++)WeaponTick();Check(attributes.FireIntervalScale==.5f&&attributes.RateWrites==writes&&attributes.AmmoWrites==ammoWrites,"one thousand frames neither compound the multiplier nor repeat setter writes");
        attributes.FireIntervalScale=.8f;WeaponTick();Check(Math.Abs(attributes.FireIntervalScale-.4f)<.0001f,"native buff recalculation rebases once before applying the twofold multiplier");
        Set("MzRapidEnabled",false);WeaponTick();Check(Math.Abs(attributes.FireIntervalScale-.8f)<.0001f&&attributes.ShootNoReload,"turning rapid OFF restores the latest baseline and leaves independent no-reload enabled");
        Set("MzNoReloadEnabled",false);WeaponTick();Check(!attributes.ShootNoReload&&!attributes.IsAmmoFree&&Get<object>("MzWeaponAttributes")==null,"all OFF restores owned flags and drops player references");
        attributes.ShootNoReload=true;attributes.IsAmmoFree=true;Set("MzNoReloadEnabled",true);WeaponTick();Set("MzNoReloadEnabled",false);WeaponTick();Check(attributes.ShootNoReload&&attributes.IsAmmoFree,"preexisting native flags remain enabled after OFF");
        attributes.ShootNoReload=false;attributes.IsAmmoFree=false;Set("MzRapidEnabled",true);attributes.FireIntervalScale=1;WeaponTick();attributes.FireIntervalScale=.7f;Set("MzRapidEnabled",false);WeaponTick();Check(attributes.FireIntervalScale==.7f,"OFF never overwrites a rate changed by native code after our write");
        Set("MzNoReloadEnabled",true);Set("MzRapidEnabled",true);attributes.FireIntervalScale=1;WeaponTick();Set("MzLeaseDeadline",0f);WeaponTick();Check(!attributes.ShootNoReload&&!attributes.IsAmmoFree&&attributes.FireIntervalScale==1,"expired key restores feature-owned attributes");Set("MzLeaseDeadline",100000f);
        var foreign=new PlayerAttributes(target);local.Attributes=foreign;WeaponTick();Check(!foreign.ShootNoReload&&!foreign.IsAmmoFree&&foreign.FireIntervalScale==1&&Get<int>("MzWeaponFeatureState")==3,"foreign attribute owner is rejected before all writes");
        local.Attributes=null;WeaponTick();Check(Get<int>("MzWeaponFeatureState")==3,"missing component waits without preventing menu");local.Attributes=attributes;
        attributes.FailRateRead=true;WeaponTick();Check(Get<int>("MzWeaponFeatureState")==6,"transient native getter error remains scoped to feature status");attributes.FailRateRead=false;WeaponTick();Check(attributes.FireIntervalScale==.5f,"same component recovers after getter failure");
        attributes.FailRateWrite=true;Set("MzRapidEnabled",false);WeaponTick();Check(Get<bool[]>("MzFeatureOwned")[2]&&Get<int>("MzWeaponFeatureState")==6,"failed restore retains ownership for a later retry");attributes.FailRateWrite=false;WeaponTick();Check(attributes.FireIntervalScale==1,"OFF retry restores after a transient setter failure");
        Set("MzRapidEnabled",true);WeaponTick();var replacement=new PlayerAttributes(local);replacement.FireIntervalScale=.5f;replacement.ShootNoReload=true;replacement.IsAmmoFree=true;local.Attributes=replacement;WeaponTick();Check(attributes.FireIntervalScale==1&&replacement.FireIntervalScale==.5f,"same-player replacement restores old component and does not halve an inherited value twice");
        Set("MzRapidEnabled",false);Set("MzNoReloadEnabled",false);WeaponTick();Check(replacement.FireIntervalScale==1&&!replacement.ShootNoReload&&!replacement.IsAmmoFree,"replacement preserves the original rapid baseline through OFF");
        replacement.FireIntervalScale=float.NaN;Set("MzRapidEnabled",true);WeaponTick();Check(float.IsNaN(replacement.FireIntervalScale)&&Get<int>("MzWeaponFeatureState")==5,"nonfinite native interval multiplier is never written");Set("MzRapidEnabled",false);WeaponTick();replacement.FireIntervalScale=1;
        Set("MzRapidEnabled",true);Set("MzNoReloadEnabled",true);WeaponTick();local.HP=0;WeaponTick();Check(replacement.FireIntervalScale==1&&!replacement.ShootNoReload&&!replacement.IsAmmoFree,"local death releases flags and rate");local.HP=100;
        WeaponTick();var oldMatch=COW.GameFacade.Match;COW.GameFacade.Match=null;WeaponTick();Check(replacement.FireIntervalScale==1&&Get<object>("MzWeaponAttributes")==null,"leaving match restores owned state and clears retained references");COW.GameFacade.Match=oldMatch;
        WeaponTick();replacement.Destroyed=true;Set("MzRapidEnabled",false);Set("MzNoReloadEnabled",false);WeaponTick();Check(Get<object>("MzWeaponAttributes")==null,"destroyed native component is dropped without reflection writes");replacement.Destroyed=false;replacement.FireIntervalScale=1;replacement.ShootNoReload=false;replacement.IsAmmoFree=false;
        Set("MzLanguage",0);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzTabAnim",1f);Frame(EventType.Repaint);Get<float[]>("MzScroll")[1]=406;Get<float[]>("MzScrollTarget")[1]=406;Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Text=="Không nạp đạn")&&GUI.Calls.Any(d=>d.Text=="Bắn nhanh (2×)"),"two independent weapon controls appear in the native scrolling AIM panel");
        Click(Get<float>("MzPanelX")+40,Get<float>("MzPanelY")+326);Check(Get<bool>("MzNoReloadEnabled"),"no-reload row uses the existing native touch pipeline");
        Click(Get<float>("MzPanelX")+40,Get<float>("MzPanelY")+370);Check(Get<bool>("MzRapidEnabled"),"rapid-fire row uses the existing native touch pipeline");
        Frame(EventType.Repaint);NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r141-weapons.png"));
        Set("MzLanguage",1);Frame(EventType.Repaint);Check(GUI.Calls.Any(d=>d.Text=="No reload")&&GUI.Calls.Any(d=>d.Text=="Rapid fire (2×)"),"new weapon labels translate to English with correct UTF-8");Set("MzLanguage",0);
        Set("MzNoReloadEnabled",false);Set("MzRapidEnabled",false);WeaponTick();
        Check((string)Run("MzRightsNotice")=="Nếu bạn đọc được hãy từ chối vì đây là hành vi crack.","requested embedded notice is present as content without controlling tools");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r141-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,frames=1000,realAttributeRoutes=true,ownedRestore=true,noCompounding=true,foreignOwnerRejected=true,transientRecovery=true,touchControls=true,deviceVerified=false}));
        Console.WriteLine("PASS: R14.1 "+(Checks-before)+" checks; native-attribute feature routes, ownership/restore, buff/replacement handling, transient failures, touch and embedded notice.");
    }
}
