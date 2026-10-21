using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static void R132Tick(Player local,float chance)
    {
        Set("MzHeadChance",chance);Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);
    }
    static void NativeR132Checks(string[] args)
    {
        int before=Checks;Player local,target;R11Setup(out local,out target);var sw=Get<bool[]>("MzSwitch");sw[12]=true;local.Firing=true;local.Origin=new Vector3(0,.5f,1);
        var aim=local.FDMIEDDNCEC;var nativePoint=new Vector3(7,8,9);aim.HPCOJHGFEEG=nativePoint;R132Tick(local,0);
        Check(aim.JHAINOBIFMA==target.gameObject&&aim.JINDNOFABKB==target.BodyCollider&&Vector3.Distance(aim.HPCOJHGFEEG,target.BodyCollider.bounds.center)<.001f,"0 percent chooses actual owned body collider and center instead of falling back to native aim");
        int reads=target.BodyListReads;long seed=Get<long>("MzChanceSeed");for(int i=0;i<50;i++)Run("get_LastAimingInfoFromWeapon",local);
        Check(target.BodyListReads==reads&&Get<long>("MzChanceSeed")==seed&&aim.JINDNOFABKB==target.BodyCollider,"repeated getter calls do not reroll or repeatedly resolve body metadata");
        Set("MzHeadChance",100f);Run("get_LastAimingInfoFromWeapon",local);Check(aim.JINDNOFABKB==target.HeadCollider&&Vector3.Distance(aim.HPCOJHGFEEG,target.Head.position)<.001f,"slider change to 100 percent switches the same-frame native record back to head");
        Set("MzHeadChance",0f);Run("get_LastAimingInfoFromWeapon",local);Check(aim.JINDNOFABKB==target.BodyCollider,"slider change to zero cannot leave an already-applied head record active");
        Run("MzResetAim");Check(aim.JHAINOBIFMA==null&&Vector3.Distance(aim.HPCOJHGFEEG,nativePoint)<.001f,"reset restores previous native aim fields after body and head outcomes");
        Set("MzChanceSeed",1L);Set("MzChanceFrame",-1);int heads=0,bodies=0;
        for(int i=0;i<1000;i++){R132Tick(local,40);if(aim.JINDNOFABKB==target.HeadCollider)heads++;else if(aim.JINDNOFABKB==target.BodyCollider)bodies++;else throw new Exception("head/body selection dropped an eligible native hit record");}
        Check(heads>330&&heads<470&&heads+bodies==1000,"40 percent applies both explicit native routes at configured head/body distribution");
        var parts=target.FireColliders;target.FireColliders=new ArrayList {target.HeadCollider};R132Tick(local,0);reads=target.BodyListReads;for(int i=0;i<30;i++)Run("get_LastAimingInfoFromWeapon",local);
        Check(aim.JHAINOBIFMA==null&&target.BodyListReads==reads,"missing body hitboxes restore native state, never force a head or repeat failed lookup within one frame");
        var foreign=Enemy().BodyCollider;target.FireColliders=new ArrayList {target.HeadCollider,foreign};R132Tick(local,0);Check(aim.JHAINOBIFMA==null,"foreign actor body collider cannot be reused for selected target");
        target.FireColliders=parts;target.BodyCollider.enabled=false;R132Tick(local,0);Check(aim.JHAINOBIFMA==null,"disabled body collider is not published as a valid body hit");target.BodyCollider.enabled=true;
        target.BodyCollider.FailBounds=true;R132Tick(local,0);Check(aim.JHAINOBIFMA==null&&!Get<bool>("MzSilentRunning"),"despawned native bounds fail without stuck guard or stale head redirect");target.BodyCollider.FailBounds=false;
        target.FailBodyList=true;R132Tick(local,100);Check(aim.JINDNOFABKB==target.HeadCollider,"head-only path stays functional when optional body metadata is unavailable");target.FailBodyList=false;
        R132Tick(local,0);target.BodyCollider.enabled=false;Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);Check(aim.JHAINOBIFMA==null,"body disappearance on a later frame releases old body record");target.BodyCollider.enabled=true;

        Run("MzResetAim");Set("MzAimlagEnabled",true);local.Scoped=true;var skin=R13Avatar(target);target.BodyCollider.gameObject.transform.Parent=target.Neck;target.BodyCollider.gameObject.transform.localPosition=new Vector3(0,-.2f,0);Set("MzHeadChance",0f);
        R13Camera();R11After(Camera.main);target.Root.position=target.Root.position+new Vector3(2,0,0);Vector3 live=target.BodyCollider.bounds.center;R13Camera();
        Set("MzAimTarget",target);Set("MzAimLagSlot",0);Set("MzAimFrame",Time.Frame);Set("MzAimStamp",Time.Now);Set("MzAimMatch",COW.GameFacade.Match);Set("MzLagDispatch",true);Run("get_LastAimingInfoFromWeapon",local);Set("MzLagDispatch",false);
        Check(aim.JINDNOFABKB==target.BodyCollider&&Vector3.Distance(aim.HPCOJHGFEEG,live)<.001f,"scoped held model uses cached real body position even when native getter runs during held render");R11After(Camera.main);Set("MzAimlagEnabled",false);Run("MzAimlagTick");Run("MzResetAim");

        Array.Clear(sw,0,sw.Length);sw[0]=true;sw[1]=true;sw[5]=true;sw[10]=true;local.Scoped=false;var near=Enemy(.1f,1.2f,6);var far=Enemy(-.4f,1.2f,30);var roster=new CountedRoster();roster.Add(near);roster.Add(far);COW.GameFacade.Match=new JMAGGLCNGIG {Players=roster};
        Camera.main=new Camera {gameObject=new GameObject("r132-camera")};var camera=Camera.main;Time.Now+=2;Render();int actors=Get<int>("MzEspCacheCount"),rosterReads=roster.Reads,meshes=Mesh.Created,textures=Texture2D.Created;Set("MzNextEsp",Time.Now+1);
        Check(actors==2,"per-target world snapshot stores near and far actors in one bounded cache");
        var world=(Vector3[])Get<Vector3[]>("MzEspWorld").Clone();camera.transform.position=new Vector3(.1f,.1f,0);float angle=.08f;camera.transform.rotation=new Quaternion {y=(float)Math.Sin(angle/2),w=(float)Math.Cos(angle/2)};Time.Now+=.001f;GUI.Calls.Clear();Run("MzRender");
        var points=Get<Vector3[]>("MzVertices");int perActor=5;float nearShift=0,farShift=0;
        for(int actor=0;actor<2;actor++){
            Vector3 feet=camera.WorldToScreenPoint(world[actor*11+1]),head=camera.WorldToScreenPoint(world[actor*11]);float h=head.y-feet.y,w=h*.5f,left=(feet.x+head.x)*.5f-w*.5f,top=Screen.height-head.y;int v=actor*perActor*4;
            Check(Math.Abs(points[v].x-(left-.75f))<.01f&&Math.Abs(points[v].y-(top-.75f))<.01f,"independent camera yaw/translation projection matches actor "+actor+" at its own depth");
            int line=v+16;float dx=points[line+1].x-points[line].x,dy=points[line+1].y-points[line].y;float mx=(points[line].x+points[line+3].x)*.5f,my=(points[line].y+points[line+3].y)*.5f;
            Check(Math.Abs(Vector3.Distance(points[v],points[v+3])-1.5f)<.001f&&Math.Abs(Vector3.Distance(points[line],points[line+3])-1.25f)<.001f,"box/line use clean thinner continuous strokes for actor "+actor);
            Check(Math.Abs(mx-Screen.width*.5f)<1&&Math.Abs(my-(Screen.height-8))<1,"snapline screen origin remains fixed while camera moves, actor "+actor);
            float shift=feet.x-(Screen.width*.5f+world[actor*11+1].x*1000/world[actor*11+1].z);if(actor==0)nearShift=shift;else farShift=shift;
        }
        Check(Math.Abs(nearShift-farShift)>5&&roster.Reads==rosterReads,"different-depth ESP is never translated by one common anchor or re-read from roster during swipe");
        int projections=Camera.ProjectionCalls;Run("MzRender");Check(Camera.ProjectionCalls==projections,"unchanged camera reuses built geometry without additional projection");
        camera.fieldOfView=40;GUI.Calls.Clear();Run("MzRender");Check(Camera.ProjectionCalls==projections+4&&roster.Reads==rosterReads,"zoom reprojects each cached actor without another actor scan");
        var heartPoints=Get<Vector3[]>("MzHeartVertices");Check(Get<int>("MzHeartCount")==2&&heartPoints[0].x!=heartPoints[4].x,"health rows rebuild at each independently projected head");
        camera.transform.rotation=new Quaternion {y=1,w=0};Run("MzRender");Check(Get<int>("MzQuadCount")==0&&Get<int>("MzHeartCount")==0,"turning away clips actors behind camera rather than leaving shifted ghosts");
        camera.transform.rotation=new Quaternion {w=1};Run("MzRender");Check(Get<int>("MzQuadCount")>0&&Mesh.Created==meshes&&Texture2D.Created==textures,"turning back recovers current projection without creating meshes or textures");
        sw[0]=false;sw[13]=false;Run("MzRender");Check(Get<int>("MzEspCacheCount")==0&&Get<string[]>("MzEspNames").All(n=>n==null),"overlay OFF clears world snapshot count and text references");
        COW.GameFacade.Match=null;Set("MzHeadChance",100f);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzTabAnim",1f);Frame(EventType.Repaint);Get<float[]>("MzScroll")[1]=0;Get<float[]>("MzScrollTarget")[1]=0;Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Text=="Silent · Đầu 100% · Thân 0%")&&GUI.Calls.Any(d=>d.Text!=null&&d.Text.Contains("R14")),"menu shows unambiguous head/body probability and new build label while preserving Claude UI");NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r132-menu.png"));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r132-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,nativeHitRecords=1000,heads,bodies,explicitBodyCollider=true,noReroll=true,perActorCameraProjection=true,multipleDepths=true,snaplineOriginFixed=true,thinSolidStrokes=true,deviceVerified=false}));
        Console.WriteLine("PASS: R13.2 "+(Checks-before)+" checks; explicit native head/body routes, optional body failures, scoped body pose, per-actor yaw/pan/zoom projection and thin continuous geometry.");
    }
}
