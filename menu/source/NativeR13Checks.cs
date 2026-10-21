using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using COW.GamePlay;
using VI=UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager;
unsafe static partial class MizzeRuntimeTest
{
    static Transform R13Child(Transform parent,string name,Vector3 local)
    {
        var node=new GameObject(name).transform;node.Parent=parent;parent.Children.Add(node);node.localPosition=local;return node;
    }
    static SkinnedMeshRenderer R13Avatar(Player p,int extras=0)
    {
        p.Visual=R13Child(p.Root,"r13-visual",new Vector3());p.Visual.localScale=new Vector3(1.3f,1.3f,1.3f);
        var spine=R13Child(p.Visual,"r13-spine",new Vector3(0,.8f,0));p.Neck=R13Child(spine,"r13-neck",new Vector3(0,.6f,0));p.Head=R13Child(p.Neck,"r13-head",new Vector3(0,.2f,0));
        p.ArmL=R13Child(spine,"r13-left",new Vector3(-.3f,.3f,0));p.HandL=R13Child(p.ArmL,"r13-left-hand",new Vector3(-.3f,0,0));p.ArmR=R13Child(spine,"r13-right",new Vector3(.3f,.3f,0));p.HandR=R13Child(p.ArmR,"r13-right-hand",new Vector3(.3f,0,0));
        p.LegL=R13Child(p.Visual,"r13-leg-left",new Vector3(-.1f,0,0));p.FootL=R13Child(p.LegL,"r13-foot-left",new Vector3(0,-.5f,0));p.LegR=R13Child(p.Visual,"r13-leg-right",new Vector3(.1f,0,0));p.FootR=R13Child(p.LegR,"r13-foot-right",new Vector3(0,-.5f,0));
        for(int i=0;i<extras;i++)R13Child(spine,"r13-extra-"+i,new Vector3(.01f*i,0,0));
        var body=new GameObject("r13-body");body.transform.Parent=p.Visual;p.Visual.Children.Add(body.transform);
        var skin=new SkinnedMeshRenderer {gameObject=body,enabled=true,sharedMesh=new Mesh {TestVertexCount=5000},sharedMaterials=new[]{new Material(new Shader())}};
        p.gameObject.ChildComponents.Clear();p.gameObject.ChildComponents.Add(skin);return skin;
    }
    static void R13Camera(){Time.Frame++;Run("MzAimlagTick");R11Before(Camera.main);}
    static void NativeR13Checks(string[] args)
    {
        int before=Checks;Player local,target;R11Setup(out local,out target);var sw=Get<bool[]>("MzSwitch");Set("MzAimlagEnabled",true);local.Scoped=true;
        target.Root.localScale=new Vector3(.75f,1.1f,.9f);var skin=R13Avatar(target);var mesh=skin.sharedMesh;var material=skin.sharedMaterials[0];Vector3 size=target.Visual.WorldScale,shown=target.Head.position,root=target.Root.position;
        int meshes=Mesh.Created,textures=Texture2D.Created,worldDraws=Graphics.WorldSubmissions;R13Camera();
        Check(Get<int>("MzLagCount")==1&&Get<int[]>("MzLagParts")[0]>=12,"complete original avatar hierarchy is retained");
        Check(skin.enabled&&skin.sharedMesh==mesh&&skin.sharedMaterials[0]==material&&skin.Bakes==0&&Graphics.WorldSubmissions==worldDraws,"original mesh, materials and renderer stay active without BakeMesh or duplicate DrawMesh");
        Check(Vector3.Distance(target.Visual.WorldScale,size)<.0001f&&Mesh.Created==meshes&&Texture2D.Created==textures,"scaled parent and avatar keep their exact size without replacement GPU assets");
        Check(skin.forceMatrixRecalculationPerRender&&skin.updateWhenOffscreen,"original skinned renderer refreshes bone matrices for each render");R11After(Camera.main);
        target.Root.position=root+new Vector3(2,0,0);target.Neck.localRotation=new Quaternion {x=.6f,w=.8f};target.HandR.localPosition=new Vector3(.6f,.2f,0);Vector3 live=target.Head.position;var nativeNeck=target.Neck.localRotation;
        R13Camera();Check(Vector3.Distance(target.Head.position,shown)<.001f&&target.Neck.localRotation.w==1&&target.HandR.localPosition.x==.3f,"original rendered body keeps its first full pose while native root and animation advance");
        Check(Vector3.Distance(target.Root.position,root+new Vector3(2,0,0))<.001f&&Vector3.Distance(target.Visual.WorldScale,size)<.001f,"visual hold never writes gameplay root or any scale");
        Set("MzAimTarget",target);Set("MzAimLagSlot",0);Set("MzAimFrame",Time.Frame);Set("MzAimStamp",Time.Now);Set("MzAimMatch",COW.GameFacade.Match);Set("MzAimCamera",Camera.main);Set("MzLagDispatch",true);Run("get_LastAimingInfoFromWeapon",local);Set("MzLagDispatch",false);
        Check(Vector3.Distance(local.FDMIEDDNCEC.HPCOJHGFEEG,live)<.001f,"native aiming record consumes saved live head when weapon getter runs during held render");
        R11After(Camera.main);Check(Vector3.Distance(target.Head.position,live)<.001f&&target.Neck.localRotation.x==nativeNeck.x,"PostRender restores current native pose in scaled moving parent");
        Time.Frame++;Run("MzRunAim");Check(Get<Player>("MzAimTarget")==target&&Vector3.Distance(Get<Vector3>("MzAimPoint"),live)<.001f,"FOV remains based on shown head while ordinary aim uses live head");
        R13Camera();target.HandL.localPosition=new Vector3(8,0,0);R11After(Camera.main);Check(target.HandL.localPosition.x==8,"native joint correction during rendering wins over stale restoration");
        var hand=target.HandR;hand.Parent=target.Root;R13Camera();Check(Get<int>("MzLagCount")==0&&!Get<bool[]>("MzLagApplied").Any(v=>v),"reparented hierarchy is rejected before partial pose writes");R11After(Camera.main);
        Check(!skin.forceMatrixRecalculationPerRender&&!skin.updateWhenOffscreen&&skin.enabled,"rejected avatar restores only owned skin flags and leaves renderer visible");
        Run("MzLagClear");Time.Now+=1;skin=R13Avatar(target);skin.forceMatrixRecalculationPerRender=true;skin.updateWhenOffscreen=true;R13Camera();R11After(Camera.main);local.Scoped=false;Time.Now+=.3f;Run("MzAimlagTick");
        Check(skin.forceMatrixRecalculationPerRender&&skin.updateWhenOffscreen&&Get<Transform[]>("MzLagNodes").All(n=>n==null),"release preserves pre-existing native skin flags and drops joint references");
        local.Scoped=true;skin=R13Avatar(target,130);Time.Now+=1;R13Camera();Check(Get<int>("MzLagCount")==0&&skin.enabled&&skin.Bakes==0,"oversized skeleton continues live without freezing a partial body");R11After(Camera.main);
        Run("MzLagClear");skin=R13Avatar(target);Time.Now+=1;R13Camera();R11After(Camera.main);Time.Now+=1;R11Before(Camera.main);Check(!Get<bool>("MzLagBound")&&Get<Transform[]>("MzLagNodes").All(n=>n==null),"expired render lease restores and detaches without another GUI tick");

        Set("MzAimlagEnabled",false);Run("MzAimlagTick");sw[15]=true;local.Scoped=false;var localSkin=R13Avatar(local);var spine=local.ArmL.parent;Vector3 leg=local.LegL.localPosition,localRoot=local.Root.position;var gun=new OverrideGun {Owner=local,Kind=OfflineWeaponKind.SMG,Interval=.05f};local.Weapon=gun;
        COW.GameFacade.Match.Players.Clear();COW.GameFacade.Match.Players.Add(target);Physics.TestHits=null;Physics.Blocked=false;R8Reset(local);float at=Time.Now+2;R8Frame(at,false);
        Check(VI.Downs==1&&Get<bool>("MzAutoContinuous")&&localSkin.forceMatrixRecalculationPerRender,"automatic gun acquires one owned continuous HUD hold and original skin refresh");
        int poseCount=Get<int>("MzAutoPoseCount");Check(poseCount>4&&Get<Transform[]>("MzAutoPoseNodes")[0]==spine,"fire-motion hold includes common upper-body spine and both arms without legs");
        spine.localRotation=new Quaternion{x=.6f,w=.8f};local.HandR.localPosition=new Vector3(.8f,.3f,0);R8Frame(at+.02f);R11Before(Camera.main);
        Check(spine.localRotation.w==1&&local.HandR.localPosition.x==.3f&&localSkin.enabled,"original upper-body pose hides native firing recoil without suppressing renderer");
        Check(Vector3.Distance(local.Root.position,localRoot)<.001f&&Vector3.Distance(local.LegL.localPosition,leg)<.001f&&gun.Starts==1&&gun.Ammo==4999,"root, legs and native shot/ammo continue independently");R11After(Camera.main);
        Check(spine.localRotation.x==.6f&&local.HandR.localPosition.x==.8f,"normal current recoil pose is restored after camera finishes");
        for(int i=2;i<=120;i++)R8Frame(at+i/60f);Check(VI.Downs==1&&VI.Ups==0&&gun.Starts>=30&&gun.Starts<=41,"two-second automatic burst follows native interval with one press, no artificial 350ms pulse gap or catch-up spam");
        Check(Get<int>("MzAutoPoseCount")==poseCount&&Get<Quaternion[]>("MzAutoPoseHeldRotation")[0].w==1&&Time.Now<Get<float>("MzAutoPoseUntil"),"upper-body pre-fire pose remains stable throughout a continuous burst rather than recapturing recoil");

        var wall=new Collider {gameObject=new GameObject("r13-wall")};var own=new Collider {gameObject=new GameObject("r13-own")};own.transform.Parent=local.Root;
        Physics.TestHits=new[]{new RaycastHit {collider=target.HeadCollider,distance=9},new RaycastHit {collider=own,distance=.05f}};Check((bool)Run("MzAutoClear",local,target)&&Physics.LastMask==-5&&Physics.LastTriggers==QueryTriggerInteraction.Ignore,"unsorted ray hits ignore local body and IgnoreRaycast layer while recognizing exact target collider");
        Physics.TestHits=new[]{new RaycastHit {collider=wall,distance=8},new RaycastHit {collider=own,distance=.05f},new RaycastHit {collider=target.HeadCollider,distance=9}};
        Check(!(bool)Run("MzAutoClear",local,target),"real wall before target is never bypassed as self geometry");R8Frame(Time.Now+.02f,false);Check(!VI.Button.Held&&VI.Ups==1&&Get<int>("MzAutoState")==3&&!localSkin.forceMatrixRecalculationPerRender,"solid cover releases continuous input and restores local presentation flags");
        Physics.TestHits=new[]{new RaycastHit {collider=wall,distance=12},new RaycastHit {collider=target.HeadCollider,distance=9}};Check((bool)Run("MzAutoClear",local,target),"a wall behind the first target does not falsely report occlusion");
        Physics.TestHits=Enumerable.Range(0,16).Select(i=>new RaycastHit{collider=own,distance=.01f*i}).ToArray();Check(!(bool)Run("MzAutoClear",local,target)&&Get<int>("MzAutoCoverState")==15,"saturated nonalloc buffer reports uncertainty instead of treating unknown geometry as clear");
        Physics.TestHits=null;R8Frame(Time.Now+.3f);R8Frame(Time.Now+.02f);Check(VI.Button.Held,"clear line of sight resumes a new owned hold after release update");
        Time.Now+=.2f;Time.Frame++;Run("MzAutoInputTick");Check(!VI.Button.Held,"stale input lease releases even without target/GUI refresh");
        sw[15]=false;Run("MzResetAim");Run("MzAimlagTick");Check(Get<Transform[]>("MzAutoPoseNodes").All(n=>n==null)&&!localSkin.forceMatrixRecalculationPerRender,"Autofire OFF clears upper-body and renderer ownership");

        Set("MzAimlagEnabled",true);local.Scoped=true;var list=COW.GameFacade.Match.Players;list.Clear();for(int i=0;i<24;i++){var p=Enemy((i%4)*.05f);R13Avatar(p,12);list.Add(p);}Time.Now+=1;
        for(int i=0;i<20;i++){Time.Now+=.02f;R13Camera();R11After(Camera.main);}Check(Get<int>("MzLagCount")==16,"dense original-rig mode respects sixteen actor slots");
        meshes=Mesh.Created;textures=Texture2D.Created;GC.Collect();long start=GC.GetTotalMemory(true);var timer=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<600;i++){Time.Now+=1f/60;Time.Frame++;Run("MzAimlagTick");R11Before(Camera.main);R11After(Camera.main);}
        timer.Stop();long retained=GC.GetTotalMemory(true)-start;Check(Mesh.Created==meshes&&Texture2D.Created==textures&&retained<1048576&&!Get<bool[]>("MzLagApplied").Any(v=>v),"dense steady frames have balanced original-rig restoration and no mesh/texture growth");
        Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzTabAnim",1f);Frame(EventType.Repaint);Get<float[]>("MzScroll")[1]=290;Get<float[]>("MzScrollTarget")[1]=290;for(int i=0;i<12;i++)Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Text=="Ẩn hoạt ảnh Autofire")&&GUI.Calls.Any(d=>d.Texture==Get<Texture2D>("MzBrandTexture")&&d.Rect.width==292&&d.Rect.height==32)&&GUI.Calls.Any(d=>d.Text!=null&&d.Text.Contains("R14")),"menu identifies R13 and retains hide-fire control");
        Check(Get<Texture2D>("MzBrandTexture").Pixels.Count(p=>p.a>.1f)>1000,"exact script title contains rasterized visible glyphs independent of OS font fallback");NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r13-aimlag.png"));
        Set("MzAimlagEnabled",false);Run("MzAimlagTick");Camera.onPreCull=null;Camera.onPostRender=null;COW.GameFacade.Match=null;Physics.TestHits=null;
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r13-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,originalRigNoBake=true,scaledParents=true,liveHeadDuringRender=true,upperBodyOriginalSkin=true,continuousNativeCadence=true,selfOcclusionIgnored=true,solidCoverPreserved=true,soakFrames=600,simulatedSeconds=10,heldActors=16,wallMs=timer.ElapsedMilliseconds,retainedDeltaBytes=retained,deviceVerified=false}));
        Console.WriteLine("PASS: R13 "+(Checks-before)+" checks; scaled original rigs, complete pose/restore, native skin refresh, live head, full upper-body fire hold, continuous input and bounded sorted-cover logic.");
    }
}
