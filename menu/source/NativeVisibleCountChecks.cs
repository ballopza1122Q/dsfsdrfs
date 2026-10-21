using System;
using System.IO;
using System.Collections;
using System.Linq;
using UnityEngine;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static void NativeVisibleCountChecks(string[] args)
    {
        int before=Checks;Run("get_UIModelRole");Driver=GameObject.Find("__mizze_driver").Added;
        License(Record(Get<string>("MzDeviceHash")));var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);
        sw[0]=true;sw[1]=true;sw[10]=true;Set("MzRange",150f);
        Camera.main=new Camera {gameObject=new GameObject("visible-count-camera")};var camera=Camera.main;
        var human=Enemy(.1f);var bot=Enemy(-.1f);bot.IsClientBot=true;
        var behind=Enemy(0,1.2f,-10);var offscreen=Enemy(100);var far=Enemy(0,1.2f,200);
        var down=Enemy();down.Downed=true;var dead=Enemy();dead.HP=0;var friend=Enemy();friend.Team=true;
        var self=Enemy();self.Local=true;var hidden=Enemy();hidden.gameObject.activeInHierarchy=false;
        var invalid=Enemy();invalid.FailHP=true;var roster=new CountedRoster();
        foreach(var p in new[]{human,bot,behind,offscreen,far,down,dead,friend,self,hidden,invalid})roster.Add(p);
        COW.GameFacade.Match=new JMAGGLCNGIG {Players=roster};Render();
        Check(Get<int>("MzEspCacheCount")==2&&Get<int>("MzPeople")==1&&Get<int>("MzBots")==1,"only visible recognized ESP actors count; exclude offscreen, range, behind, downed, dead, team, self, inactive and invalid actors");
        int reads=roster.Reads;string label=Get<string>("MzCountText");Set("MzNextEsp",Time.Now+10);Run("MzRender");
        Check(roster.Reads==reads&&System.Object.ReferenceEquals(label,Get<string>("MzCountText")),"replaying unchanged geometry does not rescan roster or allocate new count text");
        camera.transform.rotation=new Quaternion {y=1,w=0};Run("MzRender");
        Check(Get<int>("MzPeople")==0&&Get<int>("MzBots")==0&&Get<string>("MzCountText")=="Clear","turning camera away clears count immediately without waiting for another actor sample");
        Check(roster.Reads==reads,"camera-only count update uses cached classifications without actor reads");
        camera.transform.rotation=new Quaternion {w=1};Run("MzRender");
        Check(Get<int>("MzPeople")==1&&Get<int>("MzBots")==1,"turning back restores count together with visible geometry");
        human.IsClientBot=true;Render();Check(Get<int>("MzPeople")==0&&Get<int>("MzBots")==2,"classification is captured from each actually rendered actor");
        roster.Clear();for(int i=0;i<28;i++){var p=Enemy((i%8-4)*.1f);p.IsClientBot=i>=10;roster.Add(p);}Render();
        Check(Get<int>("MzEspCacheCount")==16&&Get<int>("MzPeople")==10&&Get<int>("MzBots")==6,"bounded sixteen-actor ESP counts rendered rows rather than all twenty-eight enemies");
        Check(roster.Reads==reads+11+16,"counting adds no separate full-roster pass");
        var flags=Get<bool[]>("MzEspBots");Check(flags.Length==16,"classification cache is bounded and stores no actor references");
        roster.Clear();roster.Add(Enemy());sw[1]=false;sw[10]=true;Render();
        Check(Get<int>("MzPeople")==1&&Get<int>("MzBots")==0&&Get<int>("MzQuadCount")==1,"snapline-only ESP counts the one visible actor");
        sw[10]=false;Render();Check(Get<int>("MzPeople")==0&&Get<int>("MzBots")==0,"actors without any enabled ESP primitive are not counted");
        sw[1]=true;sw[10]=true;sw[5]=true;sw[7]=true;var bad=Enemy();bad.FailNeck=true;roster.Clear();roster.Add(bad);roster.Add(Enemy());Render();
        Check(Get<int>("MzEspCacheCount")==1&&Get<int>("MzPeople")==1,"failed native snapshot is excluded together with its rolled-back geometry");
        sw[4]=true;sw[7]=false;Render();
        var world=Get<Vector3[]>("MzEspWorld");for(int actor=0;actor<Get<int>("MzEspCacheCount");actor++){world[actor*11+1]=new Vector3(100,0,10);world[actor*11]=new Vector3(100,1.2f,10);}Set("MzEspGeometryValid",false);Run("MzRender");
        Check(Get<int>("MzPeople")==0&&Get<string>("MzCountText")=="Clear","a cached actor clipped during final projection is not counted");
        Render();sw[0]=false;sw[13]=true;Run("MzRender");Check(!Get<bool>("MzCountValid")&&Get<string>("MzCountText")==null,"FOV-only mode clears previous ESP counts");
        sw[13]=false;Run("MzRender");Run("MzReleaseEspResources");Check(Get<bool[]>("MzEspBots")==null,"idle resource cleanup releases classification buffer");
        sw[0]=true;Render();Check(Get<bool[]>("MzEspBots")!=null,"re-enabling ESP recreates its bounded classification buffer");
        COW.GameFacade.Match=null;Run("MzRender");Check(!Get<bool>("MzCountValid")&&Get<int>("MzPeople")==0&&Get<int>("MzBots")==0,"match teardown clears all counts");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-visible-count-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,countsRenderedActorsOnly=true,noExtraRosterScan=true,cameraClipping=true,cacheLimit=16,deviceVerified=false}));
        Console.WriteLine("PASS: visible ESP count "+(Checks-before)+" checks; actual VM, rendered actors only, no second roster scan, camera clipping and cleanup.");
    }
}
