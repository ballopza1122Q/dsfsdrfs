using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static void AutofireR7Soak(string[] args)
    {
        UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.Reset();Run("get_UIModelRole");Driver=GameObject.Find("__mizze_driver").Added;Frame(EventType.Repaint);License(Record(Get<string>("MzDeviceHash")));Set("MzLeaseDeadline",100000f);
        var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[0]=true;sw[1]=true;sw[7]=true;sw[10]=true;sw[12]=true;sw[15]=true;
        Camera.main=new Camera {gameObject=new GameObject("autofire-soak-camera")};var local=Enemy();local.Local=true;local.Origin=new Vector3(0,.9f,1);COW.GameFacade.Local=local;
        var target=Enemy();var match=new JMAGGLCNGIG {Players=new ArrayList {target}};COW.GameFacade.Match=match;local.Weapon=new HBIBDMMOOOK {Owner=local};Set("MzFov",350f);Set("MzRange",150f);Set("MzHeadChance",100f);
        GUI.Record=false;Time.Now=0;Time.Frame=0;Run("MzRunAim");Render();GC.Collect();long retained=GC.GetTotalMemory(true);int meshes=Mesh.Created,textures=Texture2D.Created,shots=0;AppDomain.MonitoringIsEnabled=true;long allocated=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
        const int frames=18000;for(int i=1;i<=frames;i++){
            Time.Now=i/30f;Time.Frame=i;target.Head.position=new Vector3((i%120-60)*.001f,1.2f,10);target.Downed=i%900<6;sw[15]=i%1200>=6;
            if(i%1800==0){shots+=local.Weapon.Starts;local.Weapon=new HBIBDMMOOOK {Owner=local};}
            UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager.GameUpdate(local);Run("MzRender");Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);
            if(i%600==0){Check(!Get<bool>("MzAutoDispatch")&&!Get<bool>("MzAimRunning")&&!Get<bool>("MzSilentRunning")&&!Get<bool>("MzRendering"),"Autofire/ESP guards balanced at soak frame "+i);Check(Mesh.Created==meshes&&Texture2D.Created==textures,"Autofire/ESP resources stable at soak frame "+i);}
        }
        shots+=local.Weapon.Starts;long allocation=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize-allocated;long end=GC.GetTotalMemory(true);Run("MzResetAim");GUI.Record=true;
        Check(shots>1000&&end-retained<8388608,"ten simulated minutes of Autofire plus ESP have native calls and bounded retained VM memory");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"autofire-r8-soak.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {frames,simulatedSeconds=600,shots,meshesCreated=Mesh.Created-meshes,texturesCreated=Texture2D.Created-textures,retainedStart=retained,retainedEnd=end,allocatedBytes=allocation,deviceVerified=false}));
        Console.WriteLine("PASS: Autofire R8 input-consumption soak: "+frames+" frames / 600 simulated seconds, "+shots+" simulated game shots; fixed mesh/texture counts, balanced guards and bounded retained VM memory.");
    }
    static void R7AimFrame(float now){Time.Now=now;Time.Frame++;Run("MzRunAim");}
    static void NativeR7Checks(string[] args)
    {
        int before=Checks;var sw=Get<bool[]>("MzSwitch");Run("MzResetAim");Array.Clear(sw,0,sw.Length);
        Screen.Width=1280;Screen.Height=720;Camera.main=new Camera {gameObject=new GameObject("r7-camera")};
        var local=Enemy();local.Local=true;local.Origin=new Vector3(0,.9f,1);COW.GameFacade.Local=local;
        var target=Enemy();var bot=Enemy(-.2f);bot.IsClientBot=true;var match=new JMAGGLCNGIG {Players=new ArrayList {target,bot}};COW.GameFacade.Match=match;
        Time.Now=100;Set("MzLeaseDeadline",100000f);Set("MzFov",350f);Set("MzRange",150f);Set("MzHeadChance",100f);Set("MzMemoryEspDelay",.04f);
        sw[0]=true;sw[1]=true;sw[7]=true;sw[10]=true;GUI.Calls.Clear();Render();
        Check(Get<int>("MzQuadCount")==30,"two actors each have solid box, ten bone strokes and snapline");
        var vertices=Get<Mesh>("MzMesh").Vertices;
        Check(Math.Abs(Vector3.Distance(vertices[0],vertices[3])-1.5f)<.001f,"ESP box stroke is a clean 1.5 physical pixels in batched mesh");
        // Every joint lies inside both extended strokes; diagonal strokes use a
        // complete quad rather than a row of separate pixel/line fragments.
        Set("MzBuilding",true);Set("MzBatchReady",true);Run("MzClearCommands");Run("MzLine",100f,100f,200f,180f,2.25f);Run("MzLine",200f,180f,280f,130f,2.25f);
        var xy=Get<Vector3[]>("MzVertices");Check(Get<int>("MzQuadCount")==2&&xy.Take(8).All(v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)),"joined diagonal segments consist of two finite solid quads");
        float dx=100,dy=80,len=(float)Math.Sqrt(dx*dx+dy*dy);Check((xy[1].x+xy[2].x)*.5f>200&&Math.Abs((xy[1].y+xy[2].y)*.5f-(180+dy/len*1.125f))<.001f,"diagonal endpoint cap extends beyond shared joint");
        Set("MzBuilding",false);target.Downed=true;bot.Downed=true;Run("MzResetCounts");GUI.Calls.Clear();Render();
        Check(Get<int>("MzQuadCount")==0&&Get<int>("MzHeartCount")==0&&Get<string>("MzCountText")=="Clear","positive HP knocked enemies produce neither ESP nor counts");
        target.Downed=false;bot.Downed=false;Run("MzResetCounts");GUI.Calls.Clear();Render();
        var hud=GUI.Calls.Single(d=>d.Text=="Người 1   ·   Bot 1");Check(hud.FontSize==17&&hud.Rect.height==34&&GUI.Calls.Any(d=>d.Text==null&&d.Rect.width==238),"larger player/bot HUD uses 17px text and 238 by 34 pill");
        foreach(int origin in new[]{0,1,2}){Set("MzLineOrigin",origin);GUI.Calls.Clear();Run("MzCountHud");hud=GUI.Calls.Single(d=>d.Text=="Người 1   ·   Bot 1");Check(hud.Rect.y>=0&&hud.Rect.y+hud.Rect.height<=Screen.height,"enlarged HUD stays in viewport at line origin "+origin);}
        Set("MzOpen",false);Set("MzOpenAnim",0f);GUI.Calls.Clear();Frame(EventType.Repaint);NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r7-esp.png"));
        Array.Clear(sw,0,sw.Length);sw[12]=true;match.Players=new ArrayList {target};Run("MzResetAim");local.Firing=false;R7AimFrame(101);
        Check(System.Object.ReferenceEquals(Get<Player>("MzAimTarget"),target),"silent prewarms a valid target while trigger is idle");
        int projections=Camera.ProjectionCalls;R7AimFrame(101.01f);Check(Camera.ProjectionCalls==projections,"idle silent reuses short-lived snapshot instead of projecting every repaint");
        local.Firing=true;Time.Frame++;Run("get_LastAimingInfoFromWeapon",local);var record=local.FDMIEDDNCEC;
        Check(System.Object.ReferenceEquals(record.JHAINOBIFMA,target.gameObject),"first shot getter before next repaint consumes prewarmed snapshot");
        record.HLDECMJFKJK=new Vector3(9,8,7);record.HPCOJHGFEEG=new Vector3(4,5,6);Run("get_LastAimingInfoFromWeapon",local);
        Check(Vector3.Distance(record.HPCOJHGFEEG,target.Head.position)<.001f&&Vector3.Distance(record.HLDECMJFKJK,Vector3.Normalize(target.Head.position-local.Origin))<.001f,"native rewrite of same aim record in same frame is reapplied");
        record.HPCOJHGFEEG=new Vector3(90,91,92);Run("MzRestoreSilent");
        Check(record.HPCOJHGFEEG.x==90&&record.JHAINOBIFMA==null,"partial native rewrite is preserved while other owned target fields restore");
        R7AimFrame(102);Run("get_LastAimingInfoFromWeapon",local);target.Downed=true;Run("get_LastAimingInfoFromWeapon",local);
        Check(Get<Player>("MzAimTarget")==null&&record.JHAINOBIFMA==null,"knock during getter burst immediately releases silent target");target.Downed=false;
        Run("MzResetAim");sw[15]=false;local.Weapon=null;Physics.First=null;Physics.Blocked=false;COW.GameFacade.Match=null;
        Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzTabAnim",1f);var scroll=Get<float[]>("MzScroll");var scrollTarget=Get<float[]>("MzScrollTarget");scroll[1]=138;scrollTarget[1]=138;Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Text=="Autofire"),"Autofire is reachable in native AIM scroll");
        Click(Get<float>("MzPanelX")+40,Get<float>("MzPanelY")+112+362-94+20);Check(sw[15],"native Autofire row toggles on from matching press and release");Click(Get<float>("MzPanelX")+40,Get<float>("MzPanelY")+112+362-94+20);Check(!sw[15],"native Autofire row toggles off");Frame(EventType.Repaint);NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r7-aim.png"));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r7-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,deviceVerified=false,downedFiltered=true,stroke=2.25,hudFont=17,autofireNativeCalls=false,silentIdleSnapshot=true,sameFrameRewrite=true}));
        Console.WriteLine("PASS: R7 "+(Checks-before)+" checks; knocked filtering, continuous thick ESP, larger HUD and prewarmed/reapplied silent; Autofire now covered by R8 tests.");
    }
}
