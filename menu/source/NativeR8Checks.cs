using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using COW.GamePlay;
using VI=UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager;
unsafe static partial class MizzeRuntimeTest
{
    static void R8Frame(float now,bool game=true)
    {
        Time.Now=now;Time.Frame++;if(game)VI.GameUpdate(COW.GameFacade.Local);Run("MzRunAim");
    }
    static void R8Reset(Player local)
    {
        Run("MzResetAim");VI.Reset();Set("MzAutoReleased",false);Set("MzAutoRetry",0f);Set("MzAutoRequests",0);Set("MzAutoObserved",0);local.Firing=false;
    }
    static void NativeR8Checks(string[] args)
    {
        int before=Checks;var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[15]=true;
        Time.Now=1000;Time.Frame=900000;Set("MzLeaseDeadline",100000f);Set("MzFov",350f);Set("MzRange",150f);Set("MzHeadChance",100f);
        Camera.main=new Camera {gameObject=new GameObject("r8-camera")};var local=Enemy();local.Local=true;local.Origin=new Vector3(0,.9f,1);var target=Enemy();var match=new JMAGGLCNGIG {Players=new ArrayList {target}};COW.GameFacade.Match=match;COW.GameFacade.Local=local;
        var weapon=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local,Interval=.1f};local.Weapon=weapon;Physics.Blocked=false;Physics.Query=null;R8Reset(local);
        VI.Registered=false;R8Frame(1000,false);Check(VI.Downs==0&&weapon.Starts==0&&Get<int>("MzAutoState")==8,"missing Action1 registration reports waiting instead of fake firing success");
        VI.Registered=true;R8Frame(1001,false);Check(VI.Downs==1&&weapon.Starts==0&&!local.Firing&&Get<int>("MzAutoState")==5,"HUD press publishes input but cannot itself count as a game shot");
        R8Frame(1001.0167f);Check(weapon.Starts==1&&weapon.Ammo==4999&&Get<int>("MzAutoObserved")>0&&Get<int>("MzAutoState")==6,"following native Update consumes press and enters real simulated firing state");
        R8Frame(1001.0334f);Check(!VI.Button.Held&&VI.Ups==1,"pulse survives two native updates then releases");R8Frame(1001.05f);Check(!local.Firing&&Get<int>("MzAutoState")==9,"button release is consumed by next update and status distinguishes cooldown");
        R8Frame(1001.36f);Check(VI.Downs==2&&weapon.Starts==1,"next pulse also waits for native input consumption");R8Frame(1001.3767f);Check(weapon.Starts==2,"second automatic shot needs no manual primer");
        Physics.Blocked=true;Physics.First=new Collider {gameObject=new GameObject("r8-wall")};R8Frame(1001.39f,false);Check(!VI.Button.Held&&VI.Ups==2&&Get<int>("MzAutoState")==3,"solid cover releases the virtual trigger and reports blocked");Physics.Blocked=false;R8Frame(1001.41f);R8Frame(1001.43f);Check(VI.Downs==3,"exposure resumes after a complete release update");
        sw[15]=false;R8Frame(1001.44f,false);Check(!VI.Button.Held&&Get<object>("MzAutoButton")==null,"feature OFF releases only its owned virtual press");sw[15]=true;
        R8Reset(local);VI.Button.Held=true;int ups=VI.Ups;R8Frame(1002);sw[15]=false;R8Frame(1002.02f,false);Check(VI.Downs==0&&VI.Ups==ups&&VI.Button.Held,"pre-existing manual input is never acquired or released");sw[15]=true;
        R8Reset(local);R8Frame(1003,false);var oldButton=VI.Button;VI.Button=new VirtualButton {Held=true};sw[15]=false;R8Frame(1003.02f,false);Check(VI.Ups==0&&VI.Button.Held&&Get<object>("MzAutoButton")==null,"replaced scene input backend cannot receive old button release");sw[15]=true;
        R8Reset(local);R8Frame(1004,false);var next=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local};local.Weapon=next;int downs=VI.Downs;R8Frame(1004.02f,false);Check(VI.Ups==1&&VI.Downs==downs&&!VI.Button.Held&&weapon.Stops==0,"weapon swap releases input without calling stale native weapon stop");R8Frame(1004.04f);Check(VI.Downs==downs,"release edge gets its own game Update");R8Frame(1004.06f);R8Frame(1004.08f);Check(next.Starts==1,"new weapon fires through its native input state after swap");
        target.Downed=true;R8Frame(1004.1f,false);Check(!VI.Button.Held&&Get<HBIBDMMOOOK>("MzAutoWeapon")==null,"knocked target immediately releases input");target.Downed=false;
        R8Reset(local);local.Origin=new Vector3(40,20,30);R8Frame(1005,false);Check(Vector3.Distance(Physics.LastOrigin,Camera.main.transform.position)<.001f,"cover query uses reference camera origin rather than potentially uninitialized weapon origin");local.Origin=new Vector3(0,.9f,1);
        R8Reset(local);local.Weapon=new ForeignFireWeapon {Owner=local};R8Frame(1006,false);Check(VI.Downs==0&&Get<int>("MzAutoState")==11,"grenade category cannot acquire firearm input");
        local.Weapon=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local,FailReportedOwner=true};R8Frame(1007,false);Check(VI.Downs==1&&local.Weapon.OwnerReads==0,"local inventory remains authoritative when owner proxy is unavailable");
        foreach(float invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity}){R8Reset(local);local.Weapon=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local,Interval=invalid};R8Frame(1008,false);Check(VI.Downs==0,"invalid native interval cannot create input "+invalid);}
        R8Reset(local);local.Weapon=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local};VI.FailDown=true;R8Frame(1009,false);Check(!VI.Button.Held&&!Get<bool>("MzAutoHeld")&&!Get<bool>("MzAutoDispatch")&&Get<int>("MzAutoState")==7,"failed press releases ownership and reports bridge error");VI.FailDown=false;R8Frame(1009.1f);Check(VI.Downs==0&&Get<int>("MzAutoState")==7,"failed input uses retry backoff and retains error reason");R8Frame(1009.6f);R8Frame(1009.62f);Check(local.Weapon.Starts==1,"input retry recovers through native Update");
        R8Reset(local);local.Weapon=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local};Physics.Query=delegate(){COW.GameFacade.Match=new JMAGGLCNGIG();};R8Frame(1010,false);Physics.Query=null;Check(VI.Downs==0,"match replacement inside physics callback cannot publish stale input");COW.GameFacade.Match=match;
        R8Reset(local);R8Frame(1011,false);local.Downed=true;R8Frame(1011.01f,false);Check(!VI.Button.Held,"local knock releases pending input");local.Downed=false;
        R8Reset(local);R8Frame(1012,false);Set("MzLeaseDeadline",0f);Frame(EventType.Repaint);Check(!VI.Button.Held,"authorization expiry in native GUI releases pending input");Set("MzLeaseDeadline",100000f);
        R8Reset(local);local.Weapon=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local,Ammo=0};R8Frame(1013,false);R8Frame(1013.02f);Check(local.Weapon.Starts==0&&Get<int>("MzAutoObserved")==0&&Get<int>("MzAutoState")!=6,"zero ammo does not claim game firing merely because input was sent");
        R8Reset(local);local.Weapon=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local};VI.OnDown=delegate(){Run("get_LastAimingInfoFromWeapon",local);Run("MzRunAim");};R8Frame(1014,false);Check(!Get<bool>("MzAutoDispatch")&&!Get<bool>("MzAimRunning")&&!Get<bool>("MzSilentRunning"),"nested input callbacks cannot leave guards held");VI.OnDown=null;
        foreach(float interval in new[]{.1f,.8f})foreach(int fps in new[]{30,60,120}){
            R8Reset(local);weapon=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local,Interval=interval};local.Weapon=weapon;float start=1100+fps+interval,lastAt=0;int last=0;
            for(int i=0;i<fps*4;i++){float at=start+(float)i/fps;R8Frame(at);if(VI.Downs!=last){Check(last==0||at-lastAt>=Math.Max(interval,.04f)-.0002f,"input cadence at "+fps+"fps interval="+interval);last=VI.Downs;lastAt=at;}}
            Check(weapon.Starts>=4&&weapon.Ammo<5000,"native updates actually consumed repeated pulses at "+fps+"fps interval="+interval);int count=VI.Downs;R8Frame(start+10);R8Frame(start+10);Check(VI.Downs<=count+1,"stalled frame never emits a catch-up burst");
        }
        R8Reset(local);local.Weapon=new HBIBDMMOOOK {Kind=OfflineWeaponKind.HandGun,Owner=local};VI.Registered=false;sw[13]=true;Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzTabAnim",1f);Get<float[]>("MzScroll")[1]=166;Get<float[]>("MzScrollTarget")[1]=166;Time.Frame++;Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Text=="Chờ nút bắn của game"),"native AIM shows actual waiting reason instead of silently falling back to Silent");NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r8-autofire.png"));
        sw[15]=false;Run("MzResetAim");COW.GameFacade.Match=null;VI.Reset();
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r8-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,input="Action1",gameUpdateConsumptionTested=true,statusDistinguishesRequestsFromFiring=true,newGameHooks=0,deviceVerified=false}));
        Console.WriteLine("PASS: R8 "+(Checks-before)+" checks; HUD Action1 pulses, next-Update consumption/ammo, release ordering, manual/backend ownership, no fake success, cover, swap and cadence.");
    }
}
