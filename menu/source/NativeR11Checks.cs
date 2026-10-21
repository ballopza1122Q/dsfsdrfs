using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using COW.GamePlay;
using VI=UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager;
unsafe static partial class MizzeRuntimeTest
{
    static void R11Setup(out Player local,out Player target)
    {
        Run("get_UIModelRole");Driver=GameObject.Find("__mizze_driver").Added;Frame(EventType.Repaint);
        License(Record(Get<string>("MzDeviceHash")));Set("MzLeaseDeadline",100000f);Set("MzFov",350f);Set("MzRange",150f);Set("MzHeadChance",100f);
        Camera.main=new Camera {gameObject=new GameObject("r11-camera")};local=Enemy();local.Local=true;target=Enemy();target.Visual=Bone(.2f,-.5f,10);
        COW.GameFacade.Local=local;COW.GameFacade.Match=new JMAGGLCNGIG {Players=new ArrayList {target}};Physics.Blocked=false;Physics.Query=null;
        Array.Clear(Get<bool[]>("MzSwitch"),0,16);Time.Now=3000;Time.Frame=70000;Set("MzNextRefresh",100000f);
    }
    static void NativeR11NegativeControl()
    {
        Player local,target;R11Setup(out local,out target);Get<bool[]>("MzSwitch")[15]=true;
        local.Weapon=new OverrideGun {Owner=local,OverrideReportedOwner=true,ReportedOwner=Enemy()};R8Reset(local);R8Frame(3001,false);
        Check(Get<int>("MzAutoState")==10&&VI.Downs==0,"R10 reproduces user waiting state for a stable local-inventory gun with another owner proxy");
        Console.WriteLine("PASS: negative control: R10 blocks a stable local-inventory weapon on its reported owner proxy.");
    }
    static void R11Before(Camera camera){var callback=Camera.onPreCull;if(callback!=null)callback(camera);}
    static void R11After(Camera camera){var callback=Camera.onPostRender;if(callback!=null)callback(camera);}
    static void NativeR11Checks(string[] args)
    {
        int before=Checks;Player local,target;R11Setup(out local,out target);var sw=Get<bool[]>("MzSwitch");sw[15]=true;float at=3000;
        foreach(int ownerMode in new[]{0,1,2}){
            var weapon=new OverrideGun {Owner=local,OverrideReportedOwner=true,ReportedOwner=ownerMode==0?null:Enemy(),FailReportedOwner=ownerMode==2};local.Weapon=weapon;
            R8Reset(local);R8Frame(at+=2,false);Check(VI.Downs==1&&weapon.OwnerReads==0&&Get<int>("MzAutoState")==5,"stable local inventory submits input without owner-proxy comparison "+ownerMode);
            R8Frame(at+.02f);Check(weapon.Starts==1&&weapon.Ammo==4999&&Get<int>("MzAutoState")==6,"following native Update consumes local inventory input "+ownerMode);
        }
        R8Reset(local);local.Local=false;R8Frame(at+=2,false);Check(VI.Downs==0,"a foreign player cannot submit local HUD input");local.Local=true;
        local.Weapon=new OverrideGun {Owner=local};int reads=0;local.WeaponRead=delegate(){if(++reads==2)local.Weapon=new OverrideGun {Owner=local};};R8Frame(at+=2,false);local.WeaponRead=null;
        Check(VI.Downs==0&&Get<int>("MzAutoState")==10,"mid-read inventory swap is rejected");R8Frame(at+=.1f,false);R8Frame(at+.02f);Check(local.Weapon.Starts==1,"stable next weapon resumes without manual primer");
        R8Reset(local);local.Weapon=new OverrideGun {Owner=local,Ammo=0,OverrideReportedOwner=true};R8Frame(at+=2,false);R8Frame(at+.02f);
        Check(VI.Downs==1&&local.Weapon.Starts==0&&Get<int>("MzAutoState")!=6,"removing proxy gate does not claim success without engine firing");
        sw[15]=false;Run("MzResetAim");

        COW.GameFacade.Match=null;
        Console.WriteLine("PASS: R11 "+(Checks-before)+" inventory checks; owner proxy absence/mismatch, inventory swaps and truthful observed firing. Render coverage moved to R12 snapshots.");
    }
}
