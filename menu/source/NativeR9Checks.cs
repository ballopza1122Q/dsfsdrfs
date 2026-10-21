using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static void NativeR9RecoveryChecks(string[] args)
    {
        int before=Checks;COW.GameFacade.Match=null;COW.GameFacade.Local=null;
        Run("get_UIModelRole");var driver=GameObject.Find("__mizze_driver");
        Check(driver!=null&&driver.Persistent&&driver.Added!=null,"cold lobby creates persistent driver while optional input types are blocked");Driver=driver.Added;
        Frame(EventType.Repaint);Check(BadgeVisible(),"cold lobby renders badge without match, player or optional input");
        Check(Get<int>("MzInputBindAttempts")==0,"startup never attempts optional input binding");
        Set("MzInputName","Unavailable.Native.Input");
        for(int i=0;i<10;i++){
            Click(Get<float>("MzBubbleX")+44,Get<float>("MzBubbleY")+22);Check(Get<bool>("MzOpen"),"unavailable input does not prevent badge opening "+i);
            Click(Get<float>("MzPanelX")+349,Get<float>("MzPanelY")+34);Check(!Get<bool>("MzOpen")&&GUIUtility.hotControl==0,"unavailable input does not prevent close/release "+i);
        }
        float bx=Get<float>("MzBubbleX"),by=Get<float>("MzBubbleY");
        Frame(EventType.MouseDown,bx+44,by+22);Frame(EventType.MouseDrag,bx+124,by+82);Frame(EventType.MouseUp,bx+124,by+82);
        Check(Math.Abs(Get<float>("MzBubbleX")-bx-80)<.01&&!Get<bool>("MzOpen"),"badge remains draggable without optional input");
        Check(Get<int>("MzInputBindAttempts")==0,"opening, closing and dragging do not trigger optional input resolution");
        License(Record(Get<string>("MzDeviceHash")));Set("MzLeaseDeadline",100000f);Set("MzFov",350f);Set("MzRange",150f);Set("MzHeadChance",100f);
        Camera.main=new Camera {gameObject=new GameObject("r9-camera")};var local=Enemy();local.Local=true;local.Weapon=new HBIBDMMOOOK {Owner=local};
        COW.GameFacade.Local=local;COW.GameFacade.Match=new JMAGGLCNGIG {Players=new ArrayList {Enemy()}};
        var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[15]=true;Time.Now=100;Time.Frame++;
        Run("MzRunAim");Check(Get<int>("MzAutoState")==8&&!Get<bool>("MzAutoHeld")&&Get<object>("MzAutoButton")==null,"missing optional API isolates Autofire to waiting with no held input");
        int attempts=Get<int>("MzInputBindAttempts");for(int i=0;i<30;i++){Time.Now=100+i/60f;Time.Frame++;Run("MzRunAim");}
        Check(Get<int>("MzInputBindAttempts")==attempts,"missing API uses bounded retry instead of per-frame reflection");
        Frame(EventType.Repaint);Check(BadgeVisible(),"badge still renders after failed Autofire binding inside match");
        Click(Get<float>("MzBubbleX")+44,Get<float>("MzBubbleY")+22);Check(Get<bool>("MzOpen"),"badge opens inside match after optional API failure");
        Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzTabAnim",1f);Frame(EventType.Repaint);Get<float[]>("MzScroll")[1]=166;Get<float[]>("MzScrollTarget")[1]=166;Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Text=="Chờ nút bắn của game"),"menu reports isolated input waiting rather than disappearing");
        NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r9-recovery.png"));
        // Recovery needs no reload: a later successful retry can bind all five APIs.
        Set("MzInputName",null);Set("MzInputBindNext",0f);Time.Now=103;Time.Frame++;Run("MzRunAim");
        Check(Get<int>("MzAutoRequests")==1&&Get<bool>("MzAutoHeld"),"later API availability recovers via cached lazy binding");
        sw[15]=false;Run("MzResetAim");Check(!Get<bool>("MzAutoHeld")&&GUIUtility.hotControl==0,"recovered input releases without disturbing GUI capture");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r9-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,optionalTypesBlockedAtLoad=true,coldLobbyBadge=true,menuAfterInputFailure=true,lazyRetryRecovery=true,deviceVerified=false}));
        Console.WriteLine("PASS: R9 "+(Checks-before)+" recovery checks; cold lobby, badge/menu/drag with optional input absent, bounded retry and later binding recovery.");
    }
}
