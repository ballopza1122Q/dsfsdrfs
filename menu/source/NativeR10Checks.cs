using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using COW.GamePlay;
using VI=UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager;
unsafe static partial class MizzeRuntimeTest
{
    static void NativeR10NegativeControl()
    {
        Run("get_UIModelRole");
        Check(!(bool)Run("MzAutoGun",new OverrideGun()),"R9 rejects legitimate inherited firearm override");
        Console.WriteLine("PASS: negative control: R9 rejects an overridden firearm before submitting input.");
    }
    static void NativeR10Checks(string[] args)
    {
        int before=Checks;Run("get_UIModelRole");Driver=GameObject.Find("__mizze_driver").Added;Frame(EventType.Repaint);
        License(Record(Get<string>("MzDeviceHash")));Set("MzLeaseDeadline",100000f);Set("MzFov",350f);Set("MzRange",150f);Set("MzHeadChance",100f);
        Camera.main=new Camera {gameObject=new GameObject("r10-camera")};var local=Enemy();local.Local=true;var target=Enemy();
        COW.GameFacade.Local=local;COW.GameFacade.Match=new JMAGGLCNGIG {Players=new ArrayList {target}};Physics.Blocked=false;Physics.Query=null;
        var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[15]=true;float at=2000;Time.Frame=50000;
        foreach(int kind in new[]{0,1,2,4,6,8,10,13,28}){
            var weapon=new OverloadedGun {Owner=local,Kind=(OfflineWeaponKind)kind};local.Weapon=weapon;R8Reset(local);at+=2;
            R8Frame(at,false);Check(VI.Downs==1&&Get<int>("MzAutoState")==5&&weapon.Starts==0,"valid overridden/overloaded firearm kind submits input "+kind);
            R8Frame(at+.02f);Check(weapon.Starts==1&&weapon.Ammo==4999&&Get<int>("MzAutoState")==6,"native Update consumes overridden firearm input "+kind);
            sw[15]=false;R8Frame(at+.03f,false);Check(!VI.Button.Held&&weapon.Stops==0,"release remains HUD input, never direct weapon stop "+kind);sw[15]=true;
        }
        var cached=Get<System.Reflection.MethodInfo>("MzAutoKindMethod");Check(cached!=null&&cached.Name=="JBHDCDGJDPK","only native weapon-kind getter is cached, no fire/stop declaring-type inspection");
        foreach(int kind in new[]{3,5,7,9,11,12,14,15,16,17,18,19,20,21,22,23,24,25,26,27,29,30,31,32,33,34,35,36,255,-1}){
            local.Weapon=new OverloadedGun {Owner=local,Kind=(OfflineWeaponKind)kind};R8Reset(local);R8Frame(at+=2,false);
            Check(VI.Downs==0&&!Get<bool>("MzAutoHeld"),"unsupported or unready category cannot submit firearm input "+kind);
            Check(Get<int>("MzAutoState")==((kind==23||kind<0)?14:11),"specific category status "+kind);
        }
        var pending=new OverrideGun {Owner=local,Kind=OfflineWeaponKind.NotReady};local.Weapon=pending;R8Reset(local);R8Frame(at+=2,false);
        Check(Get<int>("MzAutoState")==14,"unready native category is reported as missing weapon data");pending.Kind=OfflineWeaponKind.Rifle;R8Frame(at+=.1f,false);
        Check(VI.Downs==1&&Get<int>("MzAutoState")==5,"same weapon recovers when native data becomes ready; no permanent negative cache");
        pending.Kind=OfflineWeaponKind.Grenade;R8Frame(at+=.01f,false);Check(!VI.Button.Held&&Get<int>("MzAutoState")==11,"same-instance mode switch to non-firearm releases owned input");
        pending.Kind=OfflineWeaponKind.Rifle;pending.FailKind=true;R8Frame(at+=.1f,false);Check(Get<int>("MzAutoState")==14&&VI.Downs==1,"metadata read exception waits without posting or owning input");
        Frame(EventType.Repaint);Check(BadgeVisible(),"weapon classification failure cannot remove menu badge");
        pending.FailKind=false;R8Frame(at+=.1f,false);Check(VI.Downs==2,"metadata read retry resumes without swapping or reloading weapon");
        R8Reset(local);local.Weapon=null;R8Frame(at+=2,false);Check(Get<int>("MzAutoState")==2&&VI.Downs==0,"no weapon has separate state");
        local.Weapon=new OverrideGun {Owner=local};int reads=0;local.WeaponRead=delegate(){if(++reads==2)local.Weapon=new OverrideGun {Owner=local};};R8Frame(at+=2,false);local.WeaponRead=null;Check(Get<int>("MzAutoState")==10&&VI.Downs==0,"inventory changing between reads has separate state");
        local.Weapon=new OverrideGun {Owner=local,Interval=0};R8Frame(at+=2,false);Check(Get<int>("MzAutoState")==12&&VI.Downs==0,"invalid cadence has separate state");
        Check(System.Object.ReferenceEquals(cached,Get<System.Reflection.MethodInfo>("MzAutoKindMethod")),"metadata getter cache remains one MethodInfo across swaps/retries");
        sw[15]=false;Run("MzResetAim");COW.GameFacade.Match=null;VI.Reset();
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r10-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,nativeWeaponKindGetter="JBHDCDGJDPK",supportedKinds=new[]{0,1,2,4,6,8,10,13,28},overridesAndOverloads=true,pendingMetadataRecovery=true,categoryChangeRelease=true,deviceVerified=false}));
        Console.WriteLine("PASS: R10 "+(Checks-before)+" checks; derived/overloaded firearms, enum categories, same-weapon readiness recovery, ownership/cadence status and menu isolation.");
    }
}
