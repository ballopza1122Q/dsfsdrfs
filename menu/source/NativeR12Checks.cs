using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static SkinnedMeshRenderer R12Avatar(Player p,int vertices=1000)
    {
        p.Visual=Bone(p.Root.position.x,p.Root.position.y,p.Root.position.z);p.gameObject.ChildComponents.Clear();
        var body=new GameObject("snapshot-body");body.transform.position=p.Root.position;
        var skin=new SkinnedMeshRenderer {gameObject=body,enabled=true,PoseRevision=7,PropertyCode=77,sharedMesh=new Mesh {TestVertexCount=vertices,TestSubMeshCount=2},sharedMaterials=new[]{new Material(new Shader()),new Material(new Shader())}};
        var hair=new GameObject("snapshot-hair");hair.transform.position=p.Root.position;
        var hairSkin=new SkinnedMeshRenderer {gameObject=hair,enabled=true,PoseRevision=9,sharedMesh=new Mesh {TestVertexCount=vertices/2},sharedMaterials=new[]{skin.sharedMaterials[0]}};
        var gun=new GameObject("snapshot-gun");gun.transform.position=p.Root.position;gun.Components.Add(new MeshFilter {gameObject=gun,sharedMesh=new Mesh {TestVertexCount=50}});
        var rigid=new MeshRenderer {gameObject=gun,enabled=true,sharedMaterials=new[]{skin.sharedMaterials[0]}};
        p.gameObject.ChildComponents.Add(skin);p.gameObject.ChildComponents.Add(hairSkin);p.gameObject.ChildComponents.Add(rigid);return skin;
    }
    static void R12Camera()
    {
        Time.Frame++;Run("MzAimlagTick");Graphics.WorldDraws.Clear();R11Before(Camera.main);
    }
    static void NativeR12Checks(string[] args)
    {
        int before=Checks;Player local,target;R11Setup(out local,out target);var sw=Get<bool[]>("MzSwitch");Set("MzAimlagEnabled",true);local.Scoped=true;
        var skin=R12Avatar(target);var original=skin.sharedMesh;var far=Enemy(20);var farSkin=R12Avatar(far);COW.GameFacade.Match.Players.Add(far);
        int foreignPre=0,foreignPost=0;Camera.CameraCallback pre=delegate(Camera c){foreignPre++;},post=delegate(Camera c){foreignPost++;};Camera.onPreCull=pre;Camera.onPostRender=post;
        R12Camera();Check(Get<int>("MzLagCount")==1&&Get<int[]>("MzLagParts")[0]==3,"one full avatar includes body, hair and rigid weapon; outside-FOV enemy stays live");
        Check(Graphics.WorldDraws.Count==4&&Graphics.WorldDraws[0].Mesh.BakedPose==7&&Graphics.WorldDraws[0].Properties.Code==77,"all submeshes keep captured pose, shared materials and property block");
        Check(!skin.enabled&&farSkin.enabled,"only successfully submitted in-FOV source renderers are suppressed");
        var baked=Graphics.WorldDraws[0].Mesh;var matrix=Graphics.WorldDraws[0].Matrix;Vector3 root=target.Root.position,head=target.Head.position;
        R11After(Camera.main);Check(skin.enabled&&foreignPre==1&&foreignPost==1,"PostRender restores source visibility and retains game callbacks");
        int bakes=skin.Bakes,created=Mesh.Created;skin.PoseRevision=50;target.Root.position=root+new Vector3(10,0,0);target.Head.position=head+new Vector3(10,0,0);target.Visual.position=target.Root.position;skin.transform.position=target.Root.position;
        R12Camera();Check(Graphics.WorldDraws[0].Mesh==baked&&Graphics.WorldDraws[0].Mesh.BakedPose==7&&Graphics.WorldDraws[0].Matrix.Position.x==matrix.Position.x&&skin.Bakes==bakes&&Mesh.Created==created,"whole-body pose and world anchor reuse the first bake while live animation and position advance");
        Check(target.Root.position.x==root.x+10&&target.Head.position.x==head.x+10,"enemy gameplay root, head and collider transforms are never repositioned");R11After(Camera.main);
        Time.Frame++;Run("MzRunAim");Check(Get<Player>("MzAimTarget")==target&&Get<Vector3>("MzAimPoint").x==target.Head.position.x,"FOV chooses the visible held head but aims at current live coordinates");
        Check(target.Head.position.x>Get<Vector3[]>("MzLagHeads")[0].x+9&&local.FDMIEDDNCEC.JHAINOBIFMA==target.gameObject&&local.FDMIEDDNCEC.JINDNOFABKB==target.HeadCollider&&local.FDMIEDDNCEC.DKPDBLLJEAA==LKOOALMKJND.Head,"scope publishes the real target object/collider to the native aiming record, not a painted red reticle");
        Check(local.FDMIEDDNCEC.HPCOJHGFEEG.x==target.Head.position.x&&!local.Firing&&local.AimCalls==0,"idle scoped hit information follows real target without rotating the camera or inventing a fired shot");
        sw[0]=true;sw[1]=true;sw[7]=true;Render();Check(Get<Mesh>("MzMesh").Vertices[0].x<700,"ESP geometry uses the frozen pose rather than the displaced live root");
        skin.enabled=false;R12Camera();Check(Graphics.WorldDraws.Count==4&&!skin.enabled,"cached full pose survives source renderer culling without forcing source visibility");R11After(Camera.main);Check(!skin.enabled,"a source already disabled by the game remains disabled");skin.enabled=true;
        Graphics.FailWorldAt=Graphics.WorldSubmissions+2;R12Camera();Check(skin.enabled&&(int)Run("MzLagFind",target)<0,"partial submission failure leaves original model visible and disables stale frozen-target mapping");Check(!(bool)Run("MzLagAimBridge"),"native aiming bridge also rejects a previously selected snapshot after render submission fails");R11After(Camera.main);Graphics.FailWorldAt=-1;
        R12Camera();Check(!skin.enabled&&skin.Bakes==bakes,"next successful render recovers cached geometry without rebaking");R11After(Camera.main);
        local.Scoped=false;Time.Now+=.3f;Run("MzAimlagTick");Time.Frame++;Run("MzRunAim");
        Check(skin.enabled&&baked.Destroyed&&!original.Destroyed&&Get<int>("MzLagVertices")==0&&Get<Player[]>("MzLagPlayers").All(p=>p==null),"scope release frees only owned snapshots and returns every original renderer");
        Check(Get<CGKJLKPMGDJ>("MzChangedAim")==null&&local.FDMIEDDNCEC.JHAINOBIFMA==null,"scope release restores the native target record");

        target.Root.position=root;target.Head.position=head;target.Visual.position=root;skin.transform.position=root;local.Scoped=true;skin.FailBake=true;Time.Now+=1;R12Camera();
        Check(skin.enabled&&Get<int>("MzLagCount")==0&&Get<int>("MzLagVertices")==0&&Graphics.WorldDraws.Count==0,"bake failure preserves live full model and rolls back vertex ownership");R11After(Camera.main);skin.FailBake=false;
        skin.sharedMesh.TestVertexCount=120001;R12Camera();Check(skin.enabled&&Get<int>("MzLagCount")==0&&Get<int>("MzLagVertices")==0,"oversized avatar is passed through live within fixed memory budget");R11After(Camera.main);skin.sharedMesh.TestVertexCount=1000;
        R12Camera();R11After(Camera.main);var snapshot=Get<Mesh[]>("MzLagMeshes")[0];Time.Now+=1;R11Before(Camera.main);
        Check(!Get<bool>("MzLagBound")&&skin.enabled&&snapshot.Destroyed,"expired camera lease self-releases snapshots without GUI refresh");

        Set("MzAimlagEnabled",false);Run("MzAimlagTick");Array.Clear(sw,0,16);sw[15]=true;local.Scoped=false;local.Weapon=new OverrideGun {Owner=local};
        local.ArmL.Parent=local.Root;local.ArmR.Parent=local.Root;local.HandL.Parent=local.ArmL;local.HandR.Parent=local.ArmR;local.ArmL.Children.Add(local.HandL);local.ArmR.Children.Add(local.HandR);
        local.ArmL.localRotation=new Quaternion {w=1};local.ArmR.localRotation=new Quaternion {w=1};Vector3 leg=local.LegL.position;Vector3 movingRoot=local.Root.position+new Vector3(1,0,0);
        R8Reset(local);float at=Time.Now+2;R8Frame(at,false);Check(Get<int>("MzAutoPoseCount")==4&&Get<bool>("MzLagBound"),"owned Autofire request captures only local upper-body branches and primes render callbacks");
        local.ArmL.localRotation=new Quaternion {x=.6f,w=.8f};local.ArmR.localPosition=new Vector3(2,1,0);R8Frame(at+.02f);
        local.Root.position=movingRoot;R11Before(Camera.main);Check(local.Weapon.Starts==1&&local.Weapon.Ammo==4999&&local.Firing&&local.ArmL.localRotation.w==1&&local.ArmR.localPosition.x==0,"render-only upper-body hold hides fire movement while native shot/ammo continue");
        Check(Vector3.Distance(local.LegL.position,leg)<.001f&&Vector3.Distance(local.Root.position,movingRoot)<.001f,"legs and gameplay root retain native movement during local fire-pose suppression");R11After(Camera.main);
        Check(local.ArmL.localRotation.x==.6f&&local.ArmR.localPosition.x==2,"after rendering the current firing animation pose is restored for the game");
        sw[15]=false;Run("MzResetAim");Run("MzAimlagTick");Check(Get<int>("MzAutoPoseCount")==0&&!Get<bool>("MzLagBound"),"Autofire OFF releases arm references and callback ownership");

        Set("MzAimlagEnabled",true);local.Scoped=true;var roster=COW.GameFacade.Match.Players;roster.Clear();for(int i=0;i<24;i++){var p=Enemy((i%4)*.05f);R12Avatar(p);roster.Add(p);}Graphics.RecordWorld=false;GUI.Record=false;
        for(int i=0;i<20;i++){Time.Now+=.02f;R12Camera();R11After(Camera.main);}Check(Get<int>("MzLagCount")==16&&Get<int>("MzLagVertices")<=120000,"dense scene respects sixteen slots and shared vertex budget");
        int totalBakes=Get<int>("MzLagBakes"),meshes=Mesh.Created,textures=Texture2D.Created;GC.Collect();long start=GC.GetTotalMemory(true);var timer=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<1800;i++){Time.Now+=1f/60;Time.Frame++;Run("MzAimlagTick");R11Before(Camera.main);R11After(Camera.main);if(i%300==0)Check(Get<Camera>("MzLagRenderCamera")==null&&!Get<bool>("MzLagBusy"),"snapshot render ownership balanced at frame "+i);}
        timer.Stop();long retained=GC.GetTotalMemory(true)-start;GUI.Record=true;Graphics.RecordWorld=true;
        Check(Get<int>("MzLagBakes")==totalBakes&&Mesh.Created==meshes&&Texture2D.Created==textures&&retained<1048576,"steady dense frames reuse meshes without skin baking, graphics allocation or growing retained memory");
        Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzTabAnim",1f);Frame(EventType.Repaint);Get<float[]>("MzScroll")[1]=290;Get<float[]>("MzScrollTarget")[1]=290;for(int i=0;i<12;i++)Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Text=="Ẩn hoạt ảnh Autofire")&&GUI.Calls.Any(d=>d.Text!=null&&d.Text.Contains("R12")),"native menu exposes fire-animation option and R12 identity");NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r12-aimlag.png"));
        Set("MzAimlagEnabled",false);Run("MzAimlagTick");Check(Get<int>("MzLagVertices")==0&&Get<Mesh[]>("MzLagMeshes").All(m=>m==null)&&Camera.onPreCull.GetInvocationList().Length==1,"OFF clears GPU mesh ownership and preserves unrelated camera handlers");
        Camera.onPreCull=null;Camera.onPostRender=null;COW.GameFacade.Match=null;
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r12-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,fullAvatarParts=true,fovOnly=true,liveHitRecordBridge=true,bakeAndDrawFailurePassThrough=true,upperBodyRenderHold=true,soakFrames=1800,soakSeconds=30,wallMs=timer.ElapsedMilliseconds,retainedDeltaBytes=retained,steadyBakes=Get<int>("MzLagBakes")-totalBakes,deviceVerified=false}));
        Console.WriteLine("PASS: R12 "+(Checks-before)+" checks; full-avatar snapshots, FOV/live-target bridge, native hit record, renderer failure/culling/release, local fire pose and steady no-bake rendering.");
    }
}
