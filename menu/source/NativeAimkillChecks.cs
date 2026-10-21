using System;
using System.IO;
using System.Linq;
using UnityEngine;
using COW.GamePlay;
using VI=UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager;
unsafe static partial class MizzeRuntimeTest
{
    static void AimkillFrame(float dt=.3f){Time.Now+=dt;Time.Frame++;Run("MzRunAim");}
    static void NativeAimkillChecks(string[] args)
    {
        int before=Checks;Player local,target;R11Setup(out local,out target);VI.Reset();
        var gun=new HBIBDMMOOOK {Owner=local,Kind=OfflineWeaponKind.Rifle};local.Weapon=gun;var aim=local.FDMIEDDNCEC;
        aim.DKPDBLLJEAA=LKOOALMKJND.Body;aim.CLOLKHNMNEK=37;aim.OLGEMBBDBID=7;aim.CFIPDNFBKHK=99;
        Set("MzAimkillEnabled",true);Set("MzAimkillFrame",-1);Run("MzResetAim");
        AimkillFrame();
        Check(gun.NativeHitCalls==1&&gun.NativeDamageCalls==1&&gun.LastNativeDamage==24,"Aimkill invokes the real named full native hit entry with weapon-calculated damage, not an input press or renamed Silent-only path");
        Check(gun.LastNativeTarget==target.gameObject&&gun.LastNativeKind==LKOOALMKJND.Head&&gun.LastNativeSpecial==0,"native entry receives exact game enum/int16 hit record fields for the selected actor");
        Check(gun.NativeBehaviourNull&&gun.LastNativeFactor==0&&gun.LastNativeDistance>0,"native invocation preserves the verified slot223 null-object/zero-float calling convention and actual distance");
        Check(aim.DKPDBLLJEAA==LKOOALMKJND.Body&&aim.CLOLKHNMNEK==37&&aim.OLGEMBBDBID==7&&aim.CFIPDNFBKHK==99&&aim.JHAINOBIFMA==null,"borrowed native aim record is restored, including non-boolean original enum and short");
        Check(VI.Downs==0&&gun.Starts==0&&local.AimCalls==0,"direct Aimkill makes no simulated trigger or camera-rotation calls");
        Check(Get<object[]>("MzAimkillHitArgs").All(x=>x==null||x is float)&&Get<object[]>("MzAimkillDamageArgs")[0]==null&&!Get<bool>("MzAimkillDispatch"),"reflection scratch does not retain actor/aim objects after native dispatch");
        Run("MzRunAim");Check(gun.NativeHitCalls==1,"duplicate callbacks in one frame cannot dispatch twice");AimkillFrame(.1f);Check(gun.NativeHitCalls==1,"sub-interval frames cannot flood native hit calls");
        gun.NativeReturn=0;AimkillFrame();Check(gun.NativeHitCalls==2&&Get<int>("MzAimkillState")==14&&target.HP==100,"native zero return is reported as unconfirmed, never inventing a kill or HP reduction");
        local.Firing=true;AimkillFrame();Check(gun.NativeHitCalls==2&&Get<int>("MzAimkillState")==3,"manual shooting prevents a second independent damage pipeline");local.Firing=false;
        var wall=new Collider {gameObject=new GameObject("aimkill-wall")};Physics.TestHits=new[]{new RaycastHit {collider=wall,distance=1}};AimkillFrame();Check(gun.NativeHitCalls==2&&Get<int>("MzAimkillState")==10,"solid cover rejects native dispatch");Physics.TestHits=null;
        target.Downed=true;AimkillFrame();Check(gun.NativeHitCalls==2,"knocked enemy excluded before native dispatch");target.Downed=false;
        gun.Owner=target;AimkillFrame();Check(gun.NativeHitCalls==2&&Get<int>("MzAimkillState")==7,"foreign weapon owner is rejected through the exact native owner field");gun.Owner=local;
        gun.NativeDamage=0;AimkillFrame();Check(gun.NativeHitCalls==2&&Get<int>("MzAimkillState")==12,"zero weapon-calculated damage is not replaced with a made-up amount");gun.NativeDamage=24;
        gun.FailNativeDamage=true;AimkillFrame();Check(gun.NativeHitCalls==2&&Get<int>("MzAimkillState")==15,"damage calculation exception fails closed");gun.FailNativeDamage=false;AimkillFrame(1.1f);
        int calls=gun.NativeHitCalls;gun.NativeCalculated=delegate{local.Weapon=new HBIBDMMOOOK {Owner=local};};AimkillFrame();
        Check(gun.NativeHitCalls==calls&&Get<int>("MzAimkillState")==7&&aim.DKPDBLLJEAA==LKOOALMKJND.Body,"weapon swap during calculation prevents dispatch on stale weapon and restores record");gun.NativeCalculated=null;local.Weapon=gun;
        gun.NativeCalculated=delegate{Set("MzLeaseDeadline",0f);};AimkillFrame();Check(gun.NativeHitCalls==calls,"license revocation during calculation prevents subsequent native dispatch");gun.NativeCalculated=null;Set("MzLeaseDeadline",100000f);
        gun.FailNativeHit=true;AimkillFrame();Check(Get<int>("MzAimkillState")==15&&aim.OLGEMBBDBID==7&&aim.DKPDBLLJEAA==LKOOALMKJND.Body&&Get<CGKJLKPMGDJ>("MzChangedAim")==null,"native invocation exception restores all owned record fields");gun.FailNativeHit=false;
        Set("MzHeadChance",0f);gun.NativeReturn=1;AimkillFrame(1.1f);Check(gun.LastNativeKind==LKOOALMKJND.Body&&gun.LastNativeTarget==target.gameObject,"zero headshot chance routes actual native record through the verified body enum/collider");
        gun.Kind=OfflineWeaponKind.ShotGun;calls=gun.NativeHitCalls;AimkillFrame();Check(gun.NativeHitCalls==calls&&Get<int>("MzAimkillState")==5,"unsupported multi-pellet/projectile categories are not guessed as supported");gun.Kind=OfflineWeaponKind.Rifle;
        int mesh=Mesh.Created,texture=Texture2D.Created;var scratch=Get<object[]>("MzAimkillHitArgs");for(int i=0;i<120;i++)AimkillFrame(.01f);
        Check(gun.NativeHitCalls-calls<=5&&Mesh.Created==mesh&&Texture2D.Created==texture&&System.Object.ReferenceEquals(scratch,Get<object[]>("MzAimkillHitArgs")),"steady callbacks reuse scratch storage and respect a bounded native dispatch cadence");
        Set("MzAimkillEnabled",false);Run("MzResetAim");calls=gun.NativeHitCalls;AimkillFrame();Check(gun.NativeHitCalls==calls,"OFF stops native calls and releases borrowed state");
        Set("MzAimkillEnabled",true);Set("MzAimkillHit",null);Set("MzAimkillBindAfter",Time.Now+10);AimkillFrame();Check(gun.NativeHitCalls==calls&&Get<int>("MzAimkillState")==6,"unavailable optional native API does not fall back to fake success");
        Set("MzChooseLanguage",false);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzTabAnim",1f);Frame(EventType.Repaint);
        Get<float[]>("MzScroll")[1]=482;Get<float[]>("MzScrollTarget")[1]=482;Frame(EventType.Repaint);
        Check(GUI.Calls.Any(x=>x.Text=="Aimkill · Native (thử nghiệm)"),"native experimental control is visible in the scrollable Aim menu");
        NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-aimkill-menu.png"));Set("MzLanguage",1);Frame(EventType.Repaint);
        Check(GUI.Calls.Any(x=>x.Text=="Aimkill · Native (experimental)"),"new experimental control is translated in English mode");Set("MzLanguage",0);
        Set("MzAimkillEnabled",false);Run("MzResetAim");COW.GameFacade.Match=null;Physics.TestHits=null;
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-aimkill-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,actualVm=true,nativeApiDoubles=true,fullHitEntry=true,weaponDerivedDamage=true,correctNativeHitTypes=true,noRawPacketInjection=true,physicalIPhoneTested=false,serverDamageAcceptanceVerified=false}));
        Console.WriteLine("PASS: Aimkill "+(Checks-before)+" checks; actual IFix VM with named native API doubles, strict ownership, native-calculated amount, ABI types, cover, cooldown and restoration. Server damage acceptance is UNVERIFIED.");
    }
}
