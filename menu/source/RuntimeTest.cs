// Execute the final patch in the pinned Tencent VM with offline game/Unity doubles.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Text;
using IFix.Core;
using UnityEngine;
using UnityEngine.Networking;
using COW.GamePlay;
using Object=System.Object;
unsafe static partial class MizzeRuntimeTest
{
    static VirtualMachine Vm;static Dictionary<string,int> Methods=new Dictionary<string,int>();static Dictionary<string,NewFieldInfo> Fields=new Dictionary<string,NewFieldInfo>();static Component Driver;static int Checks,SoakFrames=36000;
    static void Check(bool ok,string message){Checks++;if(!ok)throw new Exception("FAIL: "+message);}
    static bool BlockOptionalInputTypes;static Dictionary<string,string> Symbols=new Dictionary<string,string>();
    static Type Resolve(string full)
    {
        if(BlockOptionalInputTypes&&(full.StartsWith("VirtualButton,")||full.StartsWith("UnityStandardAssets.CrossPlatformInput.CrossPlatformInputManager,")))throw new TypeLoadException("Optional input unavailable: "+full);
        return Type.GetType(full,a=>a.Name=="mscorlib"?typeof(object).Assembly:a.Name=="System"?typeof(Uri).Assembly:typeof(MizzeRuntimeTest).Assembly,(a,n,c)=>a.GetType(n,true,c),true);
    }
    static MethodBase ReadMethod(BinaryReader r,Type[] types)
    {
        bool generic=r.ReadBoolean();var owner=types[r.ReadInt32()];string name=r.ReadString();var args=generic?new Type[r.ReadInt32()]:new Type[0];for(int i=0;i<args.Length;i++)args[i]=types[r.ReadInt32()];
        var ps=new Type[r.ReadInt32()];for(int i=0;i<ps.Length;i++){if(generic&&r.ReadBoolean())throw new Exception("Unexpected generic parameter descriptor");ps[i]=types[r.ReadInt32()];}
        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        MethodBase method=generic?(MethodBase)owner.GetMethods(flags).Single(m=>m.Name==name&&m.IsGenericMethodDefinition&&m.GetGenericArguments().Length==args.Length&&m.GetParameters().Select(p=>p.ParameterType).SequenceEqual(ps)).MakeGenericMethod(args):name==".ctor"?(MethodBase)owner.GetConstructor(flags,null,ps,null):owner.GetMethod(name,flags,null,ps,null);
        if(owner==typeof(Type)&&name=="GetType"&&ps.SequenceEqual(new[]{typeof(string),typeof(bool)}))method=typeof(OfflineLicenseTypeBridge).GetMethod("GetType",new[]{typeof(string),typeof(bool)});
        Check(method!=null,"resolve "+owner+"."+name);return method;
    }
    static VirtualMachine Load(string file)
    {
        var allocated=new List<IntPtr>();using(var r=new BinaryReader(File.OpenRead(file)))
        {
            Check(r.ReadUInt64()==0x66a319b20db4a2cb,"game container magic");r.ReadString();var types=new Type[r.ReadInt32()];for(int i=0;i<types.Length;i++)types[i]=Resolve(r.ReadString());
            int count=r.ReadInt32();var root=Marshal.AllocHGlobal(IntPtr.Size*count);allocated.Add(root);Instruction** codes=(Instruction**)root;var handlers=new ExceptionHandler[count][];
            for(int i=0;i<count;i++){int n=r.ReadInt32();var ptr=Marshal.AllocHGlobal(sizeof(Instruction)*n);allocated.Add(ptr);codes[i]=(Instruction*)ptr;for(int j=0;j<n;j++){codes[i][j].Code=(IFix.Core.Code)r.ReadInt32();codes[i][j].Operand=r.ReadInt32();}handlers[i]=new ExceptionHandler[r.ReadInt32()];for(int j=0;j<handlers[i].Length;j++){int kind=r.ReadInt32(),catchId=r.ReadInt32();handlers[i][j]=new ExceptionHandler {HandlerType=(ExceptionHandlerType)kind,CatchType=catchId<0?null:types[catchId],TryStart=r.ReadInt32(),TryEnd=r.ReadInt32(),HandlerStart=r.ReadInt32(),HandlerEnd=r.ReadInt32()};}}
            var externs=new MethodBase[r.ReadInt32()];for(int i=0;i<externs.Length;i++)externs[i]=ReadMethod(r,types);var strings=new string[r.ReadInt32()];for(int i=0;i<strings.Length;i++)strings[i]=r.ReadString();
            var fields=new FieldInfo[r.ReadInt32()];var newFields=new Dictionary<int,NewFieldInfo>();
            for(int i=0;i<fields.Length;i++){bool added=r.ReadBoolean();var owner=types[r.ReadInt32()];string name=r.ReadString();if(added){var f=new NewFieldInfo {Name=name,DeclaringType=owner,FieldType=types[r.ReadInt32()],MethodId=r.ReadInt32()};newFields.Add(i,f);Fields.Add(Symbols.ContainsKey(name)?Symbols[name]:name,f);Check(owner.GetField(name)==null,"VM owns new field "+name);}else{fields[i]=owner.GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);Check(fields[i]!=null,"resolve field "+name);}}
            Check(r.ReadInt32()==0,"no static-slot metadata");Check(r.ReadInt32()==0,"no closures");r.ReadString();r.ReadString();Check(r.ReadBoolean(),"native position-index lookup flag");int fixes=r.ReadInt32();Check(fixes==4,"four lifecycle/GUI/aim fixes");for(int i=0;i<fixes;i++){var m=ReadMethod(r,types);int id=r.ReadInt32();Check(Methods[m.Name]==id,"fix table maps "+m.Name);}Check(r.ReadInt32()==0&&r.BaseStream.Position==r.BaseStream.Length,"complete container");
            return new VirtualMachine(codes,delegate{foreach(var ptr in allocated)Marshal.FreeHGlobal(ptr);}) {WrappersManager=new OfflineWrappers(),ExternTypes=types,ExternMethods=externs,InternStrings=strings,FieldInfos=fields,NewFieldInfos=newFields,ExceptionHandlers=handlers,StaticFieldTypes=new Type[0],Cctors=new int[0]};
        }
    }
    static void Set(string name,object value){Fields[name].SetValue(null,value);}static T Get<T>(string name){return (T)Fields[name].GetValue(null);}
    static object Run(string name,params object[] args)
    {
        var c=Call.Begin();foreach(var a in args){if(a is long)c.PushInt64((long)a);else if(a is float)c.PushSingle((float)a);else if(a is int)c.PushInt32((int)a);else if(a is bool)c.PushBoolean((bool)a);else if(a!=null&&a.GetType().IsValueType)c.PushValueType(a);else c.PushObject(a);}Vm.Execute(Methods[name],ref c,args.Length);object result=null;
        if(name=="MzLicenseApi"||name=="MzLicenseKeyId"||name=="MzLicenseModulus"||name=="MzCredentialHash"||name=="MzVaultOpen"||name=="MzVaultMac"||name=="MzGlassData"||name=="MzGlassImageHashes"||name=="MzCheckedImage"||name=="MzLicenseOrigin"||name=="MzRightsNotice"||name=="MzRightsContextNotice"||name=="MzUiText"||name=="MzVaultGet"||name=="MzKeyUrl"||name=="get_UIModelRole"||name=="get_LastAimingInfoFromWeapon"||name=="MzPickTarget"||name=="MzHash"||name=="MzDistanceLabel"||name=="MzWebState"||name=="MzWebDocument")result=c.GetObject();else if(name=="MzAimkillBind"||name=="MzVerifyLeaseSignature"||name=="IsReallyInStealth"||name=="MzValidLicenseUrl"||name=="MzAuthorized"||name=="MzLagScope"||name=="MzLagAimBridge"||name=="MzAutoGun"||name=="MzAutoClear"||name=="MzSilentApplied"||name=="MzEnemy"||name=="MzSilentHeadAllowed"||name=="MzFinite"||name=="MzWebTick"||name=="MzWebSamePage")result=c.GetBoolean();else if(name=="MzHeartUnits"||name=="MzLagFind"||name=="MzAimMode")result=c.GetInt32();Call.End(ref c);return result;
    }
    static void Frame(EventType type,float x=0,float y=0)
    {
        Time.Frame++;Event.current=new Event {type=type,mousePosition=new Vector2(x*(Get<float>("MzScale")>0?Get<float>("MzScale"):1.35f),y*(Get<float>("MzScale")>0?Get<float>("MzScale"):1.35f))};GUI.Calls.Clear();GUI.color=new Color(.3f,.4f,.5f,1);GUI.contentColor=new Color(.4f,.5f,.6f,.7f);GUI.matrix=new Matrix4x4 {Tag=99};Run("OnGUI",Driver);Check(GUI.color.r==.3f&&GUI.contentColor.r==.4f&&GUI.contentColor.a==.7f&&GUI.matrix.Tag==99,"GUI color/content/matrix restored");
    }
    static void Click(float x,float y){Frame(EventType.MouseDown,x,y);Frame(EventType.MouseUp,x,y);}
    static Transform Bone(float x,float y,float z){return new Transform {position=new Vector3(x,y,z)};}
    static Player Enemy(float x=.2f,float headY=1.2f,float z=10)
    {
        var p=new Player {gameObject=new GameObject("enemy"),Root=Bone(x,-.5f,z),Head=Bone(x,headY,z),Neck=Bone(x,headY-.3f,z),ArmL=Bone(x-.2f,.5f,z),HandL=Bone(x-.4f,.2f,z),ArmR=Bone(x+.2f,.5f,z),HandR=Bone(x+.4f,.2f,z),LegL=Bone(x-.15f,-.8f,z),FootL=Bone(x-.15f,-1.2f,z),LegR=Bone(x+.15f,-.8f,z),FootR=Bone(x+.15f,-1.2f,z)};p.Collider=new Collider {gameObject=p.gameObject};var body=new GameObject("body-hit");body.transform.Parent=p.Root;body.transform.localPosition=new Vector3(0,(headY+.5f)*.6f,0);p.BodyCollider=new Collider {gameObject=body};p.FireColliders=new System.Collections.ArrayList {p.Collider,p.BodyCollider};return p;
    }
    static string Record(string device,bool enabled=true,long expiry=1791633660000,string extra="")
    {
        return "{\"protocol\":9,\"credentialMode\":2,\"maxDevices\":1,\"enabled\":"+(enabled?"true":"false")+",\"expiresAt\":"+expiry+(device==null?"":",\"devices\":{\""+device+"\":{\"version\":1,\"claimedAt\":1791633600000}}")+extra+"}";
    }
    const string Date="Sat, 10 Oct 2026 12:00:00 GMT";
    static void License(string record,string date=Date){FixtureLicense(record,date);}
    static int Lines(){return GUI.Calls.Count(d=>d.Text==null&&d.Texture==Texture2D.whiteTexture&&d.Radius==0);}
    static void Render(){Set("MzNextEsp",0f);Run("MzRender");}static int Labels(){return GUI.Calls.Count(d=>d.Text!=null);}
    static string Number(float f){return f.ToString("0.###",CultureInfo.InvariantCulture);}
    static void Snapshot(string file)
    {
        var b=new StringBuilder("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\""+Screen.width+"\" height=\""+Screen.height+"\" viewBox=\"0 0 "+Screen.width+" "+Screen.height+"\"><rect width=\"100%\" height=\"100%\" fill=\"#26333d\"/>");
        foreach(var d in GUI.Calls){var r=d.Rect;string c="rgb("+(int)(d.Color.r*255)+","+(int)(d.Color.g*255)+","+(int)(d.Color.b*255)+")";float sx=d.Matrix.Scale.x==0?1:d.Matrix.Scale.x,sy=d.Matrix.Scale.y==0?1:d.Matrix.Scale.y;
            b.Append("<g transform=\"translate("+Number(d.Matrix.Position.x)+" "+Number(d.Matrix.Position.y)+") rotate("+Number(d.Matrix.Angle)+") scale("+Number(sx)+" "+Number(sy)+")\" opacity=\""+Number(d.Color.a)+"\">");
            if(d.Text!=null){int font=d.FontSize>0?d.FontSize:12;float x=d.Align==TextAnchor.MiddleCenter?r.x+r.width*.5f:r.x,y=d.Align==TextAnchor.UpperLeft?r.y+font:r.y+r.height*.5f+font*.33f;string anchor=d.Align==TextAnchor.MiddleCenter?"middle":"start";var lines=new List<string>();if(d.Wrap){string line="";foreach(string word in d.Text.Split(' ')){if(line.Length>0&&(line.Length+word.Length+1)*font*.54f>r.width){lines.Add(line);line=word;}else line+=(line.Length>0?" ":"")+word;}lines.Add(line);}else lines.Add(d.Text);
                for(int i=0;i<lines.Count&&i*font*1.2f<r.height;i++)b.Append("<text x=\""+Number(x)+"\" y=\""+Number(y+i*font*1.2f)+"\" text-anchor=\""+anchor+"\" font-family=\"Arial\" font-size=\""+font+"\" fill=\""+c+"\">"+System.Security.SecurityElement.Escape(lines[i])+"</text>");
            }else if(d.Texture is Texture2D&&(((Texture2D)d.Texture).Width==96||((Texture2D)d.Texture).Width==128)){var tex=(Texture2D)d.Texture;bool atlas=tex.Width==128;int left=atlas?(int)Math.Round(d.Uv.x*tex.Width):0,top=atlas?(int)Math.Round((1-d.Uv.y-d.Uv.height)*tex.Height):0,width=atlas?(int)Math.Round(d.Uv.width*tex.Width):tex.Width,height=atlas?(int)Math.Round(d.Uv.height*tex.Height):tex.Height;using(var bitmap=new System.Drawing.Bitmap(width,height)){for(int py=0;py<height;py++)for(int px=0;px<width;px++){var pc=tex.Pixels[(tex.Height-1-top-py)*tex.Width+left+px];bitmap.SetPixel(px,py,System.Drawing.Color.FromArgb((int)Math.Round(pc.a*255),(int)Math.Round(pc.r*(atlas?d.Color.r:1)*255),(int)Math.Round(pc.g*(atlas?d.Color.g:1)*255),(int)Math.Round(pc.b*(atlas?d.Color.b:1)*255)));}using(var stream=new MemoryStream()){bitmap.Save(stream,System.Drawing.Imaging.ImageFormat.Png);b.Append("<image x=\""+Number(r.x)+"\" y=\""+Number(r.y)+"\" width=\""+Number(r.width)+"\" height=\""+Number(r.height)+"\" image-rendering=\"pixelated\" preserveAspectRatio=\"none\" href=\"data:image/png;base64,"+Convert.ToBase64String(stream.ToArray())+"\"/>");}}}else if(d.Texture is Texture2D&&((Texture2D)d.Texture).Width>0){var tex=(Texture2D)d.Texture;float radius=(tex.Width*.5f-2)*(r.width/tex.Width),stroke=(tex.Width==32?3:1.5f)*(r.width/tex.Width);b.Append("<circle cx=\""+Number(r.x+r.width*.5f)+"\" cy=\""+Number(r.y+r.height*.5f)+"\" r=\""+Number(radius-stroke*.5f)+"\" fill=\"none\" stroke=\""+c+"\" stroke-width=\""+Number(stroke)+"\"/>");}else b.Append("<rect x=\""+Number(r.x)+"\" y=\""+Number(r.y)+"\" width=\""+Number(r.width)+"\" height=\""+Number(r.height)+"\" rx=\""+Number(d.Radius)+"\" fill=\""+c+"\"/>");b.Append("</g>");
        }b.Append("</svg>");File.WriteAllText(file,b.ToString());
    }
    static void UiOnlyGlassChecks(string[] args)
    {
        Driver=GameObject.Find("__mizze_driver").Added;COW.GameFacade.Match=null;Set("MzOpen",false);Set("MzOpenAnim",0f);Frame(EventType.Repaint);
        Check(BadgeVisible(),"unchanged old launcher visible before opening glass panel");
        Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",2);Time.Now=500;int created=Texture2D.Created;
        for(int i=0;i<16;i++){Time.Now+=.13f;Frame(EventType.Repaint);}
        var textures=Get<Texture[]>("MzGlassFrames");Check(Get<int>("MzGlassLoaded")==15&&textures.Length==15,"exactly fifteen GIF frames loaded progressively");
        Check(Get<Color[]>("MzGlassPixels")==null&&Get<Color[]>("MzGlassColors")==null&&Get<string[]>("MzGlassEncoded")==null,"GIF releases decode buffer palette and encoded slots after loading");
        Check(Texture2D.Created-created<=15,"GIF has fixed texture allocation cap");
        byte[] rgba=new byte[96*112*4*15];
        for(int f=0;f<15;f++){var texture=(Texture2D)textures[f];Check(texture.Width==96&&texture.Height==112&&texture.Released,"frame uses bounded nonreadable native texture");
            for(int y=0;y<112;y++)for(int x=0;x<96;x++){var c=texture.Pixels[(111-y)*96+x];int k=((f*112+y)*96+x)*4;rgba[k]=(byte)Math.Round(c.r*255);rgba[k+1]=(byte)Math.Round(c.g*255);rgba[k+2]=(byte)Math.Round(c.b*255);rgba[k+3]=(byte)Math.Round(c.a*255);}
        }File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(args[0]),"gif-decoded.rgba"),rgba);
        Time.Now+=.18f;Frame(EventType.Repaint);var first=GUI.Calls.First(d=>d.Texture is Texture2D&&((Texture2D)d.Texture).Width==96).Texture;Time.Now+=.18f;Frame(EventType.Repaint);Check(GUI.Calls.Any(d=>d.Texture is Texture2D&&((Texture2D)d.Texture).Width==96&&!Object.ReferenceEquals(d.Texture,first)),"real GIF frame changes during panel animation");
        int uploads=Texture2D.Uploaded;created=Texture2D.Created;for(int i=0;i<240;i++){Time.Now+=1f/60;Frame(EventType.Repaint);}Check(Texture2D.Uploaded==uploads&&Texture2D.Created==created,"loaded background reuses textures without per-frame uploads");
        Check(BadgeVisible()&&GUI.Calls.Any(d=>d.Text=="KHÓA"),"original key/menu controls remain visible over GIF");
        Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-glass.svg"));
        Set("MzOpen",false);Set("MzOpenAnim",0f);Frame(EventType.Repaint);float paused=Get<float>("MzGlassClock");for(int i=0;i<60;i++){Time.Now+=.016f;Frame(EventType.Repaint);}Check(Get<float>("MzGlassClock")==paused,"closed menu stops GIF animation");
        Set("MzGlassLoaded",0);Set("MzGlassFailed",false);Set("MzGlassFrames",null);Set("MzGlassNext",0f);Texture2D.FailUpload=true;Set("MzOpen",true);Set("MzOpenAnim",1f);Frame(EventType.Repaint);Texture2D.FailUpload=false;
        Check(Get<bool>("MzGlassFailed")&&Texture2D.LastCreated.Destroyed,"failed GIF upload destroys incomplete texture and disables retry");
        Check(BadgeVisible()&&GUI.Calls.Any(d=>d.Text=="KHÓA"),"GIF failure preserves original launcher and menu controls");
        created=Texture2D.Created;Time.Now+=2;Frame(EventType.Repaint);Check(Texture2D.Created==created,"failed GIF does not repeatedly allocate");
        Check(Font.Loads==2&&GUIStyle.DefaultFont==GUI.skin.font,"new font family still loads once and restores game font");
    }
    static void ExtendedChecks(string[] args)
    {
        var switches=Get<bool[]>("MzSwitch");Array.Clear(switches,0,switches.Length);switches[0]=true;switches[1]=true;switches[13]=true;
        Screen.Width=1280;Screen.Height=720;Set("MzFov",150f);Set("MzRange",150f);Set("MzPanelX",120f);Set("MzPanelY",16f);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",3);Set("MzColorTarget",0);
        COW.GameFacade.Match=new JMAGGLCNGIG();var bad=Enemy();bad.FailHP=true;var enemy=Enemy();COW.GameFacade.Match.Players.Add(bad);COW.GameFacade.Match.Players.Add(enemy);
        GUI.Calls.Clear();Render();Check(GUI.Calls.Count==5,"despawn getter failure leaves next enemy and FOV rendering intact");Check(!Get<bool>("MzDrawingEsp"),"drawing flag restored after exception");
        bad.FailHP=false;bad.Destroyed=true;GUI.Calls.Clear();Render();Check(GUI.Calls.Count==5,"Unity destroyed player wrapper skipped");
        Click(210,222);Color esp=Get<Color>("MzEspColor");Check(esp.r==.3f&&esp.g==.65f,"palette changes actual ESP color");Check(PlayerPrefs.Saved.ContainsKey("Mizze.Color.ESP"),"palette persisted after tap");
        Click(270,143);Click(210,222);Color fov=Get<Color>("MzFovColor");Check(fov.r==.3f&&fov.b==1,"FOV palette independent");
        Frame(EventType.MouseDown,230,349);Frame(EventType.MouseDrag,390,349);Frame(EventType.MouseUp,390,349);fov=Get<Color>("MzFovColor");Check(Math.Abs(fov.r-(390-134-84)/270f)<.001f,"RGB drag controls actual red channel");Check(Get<int>("MzDragMode")==0&&PlayerPrefs.Saved["Mizze.Color.FOV"].Length==6,"RGB releases drag and persists color");
        GUI.Calls.Clear();Render();Check(GUI.Calls.Any(d=>d.Text==null&&Math.Abs(d.Color.r-fov.r)<.001f&&d.Color.b==1),"FOV renderer consumes selected color");Check(GUI.Calls.Any(d=>d.Text==null&&d.Color.r==esp.r&&d.Color.g==esp.g),"box renderer consumes selected color");
        Click(390,143);Click(210,222);Check(Get<Color>("MzHealthColor").b==1,"health palette independent");
        Set("MzColorTarget",0);Run("MzAppearanceInit");Check(Get<Color>("MzEspColor").b==1&&Math.Abs(Get<Color>("MzFovColor").r-fov.r)<.005f,"colors survive appearance reinitialization");
        Frame(EventType.Repaint);Check(GUI.Calls.Any(d=>d.Text=="Màu ESP")&&GUI.Calls.Any(d=>d.Text=="Đỏ "+((int)(Get<Color>("MzEspColor").r*255)).ToString()),"UTF-8 Vietnamese survives translated bytecode");Check(Font.Loads==2&&GUIStyle.DefaultFont==GUI.skin.font,"font loaded once and game default restored");Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-colors.svg"));
        COW.GameFacade.Match.Players.Clear();for(int i=0;i<150;i++)COW.GameFacade.Match.Players.Add(Enemy((i%10-5)*.1f));for(int i=0;i<11;i++)switches[i]=true;switches[13]=true;
        int maximum=0;for(int frame=0;frame<1200;frame++){if(frame%20==0)((Player)COW.GameFacade.Match.Players[frame%150]).Destroyed=true;GUI.Calls.Clear();Render();maximum=Math.Max(maximum,GUI.Calls.Count);if(frame%100==0){Check(GUI.Calls.Count<=513&&Get<int>("MzCommandCount")<=512&&!Get<bool>("MzDrawingEsp"),"bounded all-ESP stress frame "+frame);GC.Collect();}}Check(maximum>250,"stress exercised dense all-feature drawing");
        COW.GameFacade.Match=null;GUI.Calls.Clear();Render();Check(GUI.Calls.Count==0&&!Get<bool>("MzDrawingEsp"),"match teardown resets render state");
        Set("MzOpen",true);Set("MzTab",3);for(int i=0;i<18;i++)Frame(EventType.Repaint);Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-colors.svg"));
    }
    static void StabilityChecks(string[] args)
    {
        var switches=Get<bool[]>("MzSwitch");Array.Clear(switches,0,switches.Length);switches[0]=true;switches[1]=true;switches[13]=true;
        var enemy=Enemy();COW.GameFacade.Match=new JMAGGLCNGIG();COW.GameFacade.Match.Players.Add(enemy);Time.Now=0;Set("MzNextEsp",0f);GUI.Calls.Clear();Run("MzRender");int reads=Camera.ProjectionCalls,uploads=Texture2D.Uploaded,created=Texture2D.Created;
        for(int frame=0;frame<60;frame++){Time.Now=.0003f*frame;GUI.Calls.Clear();Run("MzRender");}
        Check(Camera.ProjectionCalls==reads,"stationary camera reuses cached ESP geometry without projection or roster reads");Check(Texture2D.Uploaded==uploads&&Texture2D.Created==created,"rings never allocate or upload per repaint");
        Time.Now=.08f;reads=Camera.ProjectionCalls;Run("MzRender");Check(Camera.ProjectionCalls==reads+4,"sample refresh reads current world positions and rebuilds two projected anchors");
        {var cam=Camera.main;var origin=cam.transform.position;var anchor=enemy.RootTransform.position;float old=Get<Vector3[]>("MzVertices")[0].x;cam.transform.position=origin+new Vector3(.05f,0,0);float expected=cam.WorldToScreenPoint(anchor).x-cam.WorldToScreenPoint(anchor+new Vector3(.05f,0,0)).x;
         Time.Now=.085f;reads=Camera.ProjectionCalls;GUI.Calls.Clear();Run("MzRender");Check(Camera.ProjectionCalls==reads+2&&Math.Abs(Get<Vector3[]>("MzVertices")[0].x-old-expected)<.01f&&Math.Abs(expected)>1,"cached world head/root follow camera swipe with independent projection");
         cam.transform.position=origin;Set("MzNextEsp",0f);Run("MzRender");}
        var ring=Get<Texture2D>("MzRing");Check(ring.Width==256&&ring.Pixels[128*256+128].a==0&&ring.Released,"ring centre transparent, upload releases CPU storage on device");
        for(int i=0;i<360;i++){double angle=i*Math.PI/180;bool found=false;for(int radius=124;radius<=127;radius++){int x=(int)(128+Math.Cos(angle)*radius),y=(int)(128+Math.Sin(angle)*radius);if(x>=0&&x<256&&y>=0&&y<256&&ring.Pixels[y*256+x].a>.15f)found=true;}Check(found,"continuous raster ring angle "+i);}
        GUI.Calls.Clear();Render();var circle=GUI.Calls.Single(d=>d.Texture==ring);Check(circle.Rect.width==circle.Rect.height&&circle.Matrix.Tag!=2,"FOV remains square and unrotated");
        Set("MzFov",480f);GUI.Calls.Clear();Render();circle=GUI.Calls.Single(d=>d.Texture==ring);Check(circle.Rect.y>=0&&circle.Rect.y+circle.Rect.height<=Screen.height&&circle.Rect.x>=0&&circle.Rect.x+circle.Rect.width<=Screen.width,"maximum FOV fits viewport without clipping its outline");
        Array.Clear(switches,0,switches.Length);switches[0]=true;switches[1]=true;GUI.Calls.Clear();Render();Check(GUI.Calls.Count==4&&GUI.Calls.All(d=>d.Matrix.Tag!=2),"2D edges use direct rectangles without rotation matrices");var edges=GUI.Calls.ToArray();Check(Math.Abs(edges[0].Rect.x-edges[2].Rect.x)<.001f&&Math.Abs(edges[0].Rect.y-edges[2].Rect.y)<.001f,"box endpoint caps overlap at top left");
        // Distinct weapon and camera origins expose the old lateral parallax bug.
        var local=Enemy(0);local.Local=true;local.Firing=true;local.Origin=new Vector3(-.4f,.8f,1);COW.GameFacade.Local=local;Camera.main.transform.position=new Vector3(.4f,1,1);Set("MzFov",480f);switches[11]=true;
        Run("MzRunAim");Vector3 desired=Vector3.Normalize(enemy.Head.position-Camera.main.transform.position);Check(local.AimCalls==1&&Vector3.Distance(local.LastRotation.Direction,desired)<.0001f,"head aim uses camera ray with shoulder offset");Check(Vector3.Distance(local.LastRotation.Direction,Vector3.Normalize(enemy.Head.position-local.Origin))>.05f,"weapon origin no longer introduces lateral bias in camera aim");
        enemy.Head.position=new Vector3(-.1f,1.3f,10);Time.Frame++;Run("MzRunAim");desired=Vector3.Normalize(enemy.Head.position-Camera.main.transform.position);Check(local.AimCalls==2&&Vector3.Distance(local.LastRotation.Direction,desired)<.0001f,"head aim follows current head position while firing");local.Firing=false;Run("MzRunAim");Check(local.AimCalls==2,"camera aim stops without firing");switches[11]=false;Camera.main.transform.position=new Vector3();
        COW.GameFacade.Match.Players.Clear();for(int i=0;i<150;i++)COW.GameFacade.Match.Players.Add(Enemy((i%10-5)*.1f));for(int i=0;i<11;i++)switches[i]=true;switches[13]=true;
        Time.Now=0;int startReads=Camera.ProjectionCalls;for(int frame=0;frame<1200;frame++){Time.Now=frame/60f;GUI.Calls.Clear();Run("MzRender");if(frame%120==0){Check(GUI.Calls.Count<=513&&!Get<bool>("MzBuilding"),"scheduled dense repaint frame "+frame);GC.Collect();}}Check(Camera.ProjectionCalls-startReads<1200*16*20,"scheduled dense scene does not resample every repaint");Check(Texture2D.Created==created&&Texture2D.Uploaded==uploads,"dense scene keeps two ring textures and fixed command buffers");
        COW.GameFacade.Match=null;GUI.Calls.Clear();Run("MzRender");Check(GUI.Calls.Count==0&&Get<int>("MzCommandCount")==0,"match teardown clears all cached overlay commands");
        COW.GameFacade.Match=new JMAGGLCNGIG();COW.GameFacade.Match.Players.Add(enemy);Array.Clear(switches,0,switches.Length);switches[0]=true;switches[1]=true;switches[4]=true;switches[5]=true;switches[13]=true;Set("MzNextEsp",0f);Set("MzOpen",false);Set("MzOpenAnim",0f);Frame(EventType.Repaint);Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"esp-continuous.svg"));
        var bad=Enemy();bad.FailNick=true;COW.GameFacade.Match.Players.Clear();COW.GameFacade.Match.Players.Add(bad);COW.GameFacade.Match.Players.Add(enemy);GUI.Calls.Clear();Render();Check(GUI.Calls.Count==7,"late getter failure rolls back whole enemy including the heart mesh");
        ring.Destroyed=true;GUI.Calls.Clear();Render();Check(Get<Texture2D>("MzRing")!=ring&&Get<Texture2D>("MzRing")!=null,"destroyed native ring is rebuilt before overlay draw");Check(GUI.Calls.All(d=>!System.Object.ReferenceEquals(d.Texture,ring)),"no destroyed ring reaches GUI draw");
        Get<Texture2D>("MzRing").Destroyed=true;Texture2D.FailUpload=true;GUI.Calls.Clear();Render();Check(Texture2D.LastCreated.Destroyed&&GUI.Calls.Count==0,"failed native texture upload destroys partial allocation and skips unsafe draw");Texture2D.FailUpload=false;int noRetryAlloc=Texture2D.Created;Render();Check(Texture2D.Created==noRetryAlloc,"failed ring upload does not allocate again within cooldown");Time.Now+=5.01f;Render();Check(Get<Texture2D>("MzRing")!=null,"renderer recovers after bounded texture upload retry");
        Time.Now=0;
    }
    static void BatchChecks()
    {
        var switches=Get<bool[]>("MzSwitch");Array.Clear(switches,0,switches.Length);for(int i=0;i<11;i++)switches[i]=true;switches[13]=true;COW.GameFacade.Match=new JMAGGLCNGIG();for(int i=0;i<150;i++)COW.GameFacade.Match.Players.Add(Enemy((i%10-5)*.1f));Set("MzRange",150f);Time.Now=0;
        int draws=Graphics.Draws;GUI.Calls.Clear();Render();Check(Graphics.Draws==draws+2,"dense all-feature ESP uses one solid and one atlas draw");Check(GUI.Calls.Count(d=>d.Text==null&&!d.MeshQuad&&d.Radius==0)<=18&&GUI.Calls.Count(d=>d.Text==null&&d.Radius>0)==3,"head/FOV texture draws stay bounded plus three rounded count-HUD shapes");Check(Get<int>("MzQuadCount")<=512&&GL.Depth==0,"batch capacity and GL matrix stack bounded");
        var material=Get<Material>("MzMaterial");Check(material.Values["_ZWrite"]==0&&material.Values["_ZTest"]==8&&material.Values["_Cull"]==0,"overlay material disables depth writes and culling");
        var mesh=Get<Mesh>("MzMesh");Check(mesh.Indices.All(i=>i>=0&&i<mesh.Vertices.Length)&&mesh.Vertices.All(v=>!float.IsNaN(v.x)&&!float.IsInfinity(v.y)),"uploaded mesh has valid finite vertices and indices");
        int whiteReads=Texture2D.WhiteReads,matrixWrites=GUI.MatrixWrites,nativeTextures=GUI.NativeTextures;GUI.Calls.Clear();Run("MzRender");Check(Texture2D.WhiteReads==whiteReads&&GUI.MatrixWrites-matrixWrites<=2,"cached solid batch avoids per-line texture lookup and GUI matrix calls");Check(GUI.NativeTextures-nativeTextures<=21,"dense frame keeps head/FOV and three count-HUD texture draws bounded");
        int uploads=Mesh.Uploads,created=Mesh.Created;for(int frame=0;frame<600;frame++){GUI.Calls.Clear();Run("MzRender");}Check(Mesh.Uploads==uploads&&Mesh.Created==created,"six hundred cached repaints do not rebuild or upload mesh");
        Graphics.FailDraw=true;GUI.Calls.Clear();Run("MzRender");Check(GL.Depth==0&&!Get<bool>("MzBatchReady")&&mesh.Destroyed&&material.Destroyed,"GPU failure restores stack and destroys failed native resources");Graphics.FailDraw=false;Shader.Available=false;GUI.Calls.Clear();Render();Check(GUI.Calls.Count(d=>d.Text==null&&d.Texture==Texture2D.whiteTexture)<=96,"shader-unavailable fallback remains bounded");Check(!Get<bool>("MzBatchReady"),"missing shader does not masquerade as active batch");
        Shader.Available=true;Time.Now=6;GUI.Calls.Clear();Render();Check(Get<bool>("MzBatchReady")&&Get<Mesh>("MzMesh")!=mesh,"batch recovers after bounded retry interval");
        Material.FailPass=true;GUI.Calls.Clear();Run("MzRender");Check(!Get<bool>("MzBatchReady")&&GL.Depth==0,"unsupported material pass disables batch safely");Material.FailPass=false;Time.Now=12;Render();Check(Get<bool>("MzBatchReady"),"valid material pass recovers");
        var first=Get<Vector3[]>("MzVertices");var colors=Get<Color[]>("MzVertexColors");for(int frame=0;frame<1200;frame++){Time.Now=12+frame/60f;GUI.Calls.Clear();Run("MzRender");}Check(System.Object.ReferenceEquals(first,Get<Vector3[]>("MzVertices"))&&System.Object.ReferenceEquals(colors,Get<Color[]>("MzVertexColors")),"long dense scene reuses CPU vertex buffers");Check(GL.Depth==0,"dense batch leaves GL stack balanced");Time.Now=0;
    }
    static void UiPerformanceChecks()
    {
        Get<float[]>("MzScroll")[1]=44;Get<float[]>("MzScrollTarget")[1]=44;
        COW.GameFacade.Match=null;Time.Now=0;Set("MzKeyHash","");Set("MzRequest",null);Set("MzKeyboard",null);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzPanelX",120f);Set("MzPanelY",16f);Set("MzFov",150f);
        Frame(EventType.Repaint);int fonts=GUIStyle.FontWrites,white=Texture2D.WhiteReads,textures=GUI.NativeTextures,matrices=GUI.MatrixWrites;
        Frame((EventType)8);Check(GUI.Calls.Count==0&&GUI.NativeTextures==textures&&GUIStyle.FontWrites==fonts&&GUI.MatrixWrites==matrices+1,"layout event skips all menu drawing, font and matrix work");
        Frame(EventType.MouseDown,2,2);Check(GUI.Calls.Count==0&&GUI.NativeTextures==textures&&GUIStyle.FontWrites==fonts,"touch-down handles input without native UI drawing or font switching");
        Frame(EventType.MouseUp,2,2);Check(GUI.Calls.Count==0&&GUI.NativeTextures==textures&&GUIStyle.FontWrites==fonts,"touch-up handles input without drawing or font switching");
        var label=Get<string[]>("MzSliderText")[0];int transforms=Matrix4x4.TrsCalls;
        for(int i=0;i<120;i++)Frame(EventType.Repaint);
        Check(Texture2D.WhiteReads==white,"open menu reuses white texture without per-shape native getter");Check(Matrix4x4.TrsCalls==transforms,"stable scale reuses menu transform for 120 repaints");Check(Object.ReferenceEquals(label,Get<string[]>("MzSliderText")[0]),"unchanged slider reuses formatted text");
        Set("MzFov",151f);Frame(EventType.Repaint);Check(Get<string[]>("MzSliderText")[0].Contains("151 px")&&!Object.ReferenceEquals(label,Get<string[]>("MzSliderText")[0]),"FOV text updates when rounded value changes");
        Set("MzTab",0);Frame(EventType.Repaint);Check(Get<string[]>("MzSliderText")[1].StartsWith("Tầm ESP"),"shared range label refreshes its title when switching tab");
        Set("MzOpen",false);for(int i=0;i<120;i++)Frame(EventType.Repaint);Check(Get<float>("MzOpenAnim")==0,"close animation settles exactly to zero");var animation=Get<float[]>("MzSwitchAnim");float before=animation[0];Get<bool[]>("MzSwitch")[0]=!Get<bool[]>("MzSwitch")[0];Frame(EventType.Repaint);Check(animation[0]==before,"fully closed panel skips hidden toggle animation updates");
        Check(Get<bool>("MzUiPaint"),"draw gating restored after pointer events");
    }
    static void EspCoreChecks(string[] args)
    {
        var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[0]=true;sw[1]=true;sw[10]=true;sw[4]=true;sw[5]=true;sw[6]=true;
        COW.GameFacade.Match=new JMAGGLCNGIG();var good=Enemy();COW.GameFacade.Match.Players.Add(good);Time.Now=1000;Set("MzNextEsp",0f);Run("MzRender");
        Check(Get<int>("MzCommandCount")==2&&Get<int>("MzQuadCount")==5,"box and snapline solids are stored once; hearts use their own atlas mesh");
        int trs=Matrix4x4.TrsCalls;Run("MzClearCommands");Set("MzBuilding",true);Set("MzEspSampleStarted",Time.Now);Run("MzSampleEsp");Set("MzBuilding",false);
        Check(Matrix4x4.TrsCalls==trs,"mesh sample creates no unused rotation matrices for snaplines");
        Array.Clear(sw,0,sw.Length);sw[0]=true;sw[1]=true;sw[3]=true;int reads=Camera.ProjectionCalls;Render();Check(Camera.ProjectionCalls-reads==12,"3D sample clips two anchors; view geometry projects head/root and eight corners");
        sw[1]=false;sw[3]=false;sw[7]=true;reads=Camera.ProjectionCalls;Render();Check(Camera.ProjectionCalls-reads==13,"skeleton sample clips two anchors; view geometry reuses head/root for nine cached bones");
        string a=(string)Run("MzDistanceLabel",25.2f),b=(string)Run("MzDistanceLabel",25.8f);Check(Object.ReferenceEquals(a,b)&&a=="25 m","unchanged rounded distance reuses label string");
        sw[7]=false;sw[1]=true;sw[4]=true;var bad=Enemy();bad.FailNick=true;COW.GameFacade.Match.Players.Insert(0,bad);Render();Check(Get<int>("MzQuadCount")==4&&Get<int>("MzCommandCount")==1,"failed enemy rolls back mesh and label buffers together");
        var vertices=Get<Vector3[]>("MzVertices");var colors=Get<Color[]>("MzVertexColors");var labels=Get<string[]>("MzDistanceLabels");
        // Ten minutes at 60Hz, including repeated scene/roster churn, without a
        // test-injected collection in the frame loop. Native draws are counted.
        GUI.Record=false;COW.GameFacade.Match.Players.Clear();for(int i=0;i<16;i++)COW.GameFacade.Match.Players.Add(Enemy((i%8-4)*.1f));
        for(int i=0;i<11;i++)sw[i]=true;sw[13]=true;long before=GC.GetTotalMemory(true);int textures=Texture2D.Created,meshes=Mesh.Created;
        for(int frame=0;frame<SoakFrames;frame++){
            Time.Now=1001+frame/60f;if(frame%1800==0){var match=new JMAGGLCNGIG();for(int i=0;i<16;i++)match.Players.Add(Enemy((i%8-4)*.1f));COW.GameFacade.Match=match;}
            if(frame%997==0)((Player)COW.GameFacade.Match.Players[frame%16]).FailNick=true;
            Run("MzRender");if(frame%3600==0)Check(Get<int>("MzCommandCount")<=48&&Get<int>("MzQuadCount")<=512&&!Get<bool>("MzRendering")&&GL.Depth==0,"ten-minute ESP soak state "+frame);
        }
        long retained=GC.GetTotalMemory(true)-before;GUI.Record=true;Check(retained<8388608,"ten-minute soak retains less than 8 MiB after collection");
        Check(Object.ReferenceEquals(vertices,Get<Vector3[]>("MzVertices"))&&Object.ReferenceEquals(colors,Get<Color[]>("MzVertexColors"))&&Object.ReferenceEquals(labels,Get<string[]>("MzDistanceLabels")),"soak keeps fixed vertex color and label buffers");
        Check(Texture2D.Created==textures&&Mesh.Created==meshes,"soak does not create native textures or meshes over time");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"esp-soak.json"),"{\"simulatedSeconds\":"+(SoakFrames/60)+",\"frames\":"+SoakFrames+",\"retainedDeltaBytes\":"+retained+",\"nativeTexturesAdded\":0,\"nativeMeshesAdded\":0,\"deviceVerified\":false}");
        COW.GameFacade.Match=null;Run("MzRender");Check(Get<int>("MzCommandCount")==0&&Get<int>("MzQuadCount")==0&&Get<Camera>("MzEspCamera")==null&&Get<JMAGGLCNGIG>("MzEspMatch")==null,"scene teardown clears geometry and native scene references");
        Set("MzRendering",true);int projectionReads=Camera.ProjectionCalls;Run("MzRender");Check(Camera.ProjectionCalls==projectionReads,"nested rendering returns without touching game objects");Set("MzRendering",false);
        Set("MzOpen",false);Set("MzOpenAnim",0f);Time.Now=0;Frame(EventType.Repaint);Check(BadgeVisible(),"unchanged launcher remains visible after long ESP run and scene teardown");

    }
    static void HeartCounterScrollChanceChecks(string[] args)
    {
        Time.Now=2000;Set("MzHealthColor",new Color(.96f,.19f,.24f,1));Set("MzDragMode",0);Run("MzHeartsInit");
        Check((int)Run("MzHeartUnits",100,200)==10&&(int)Run("MzHeartUnits",1,200)==1&&(int)Run("MzHeartUnits",199,200)==20,"health ratio selects full and half heart atlas states");
        Check((int)Run("MzHeartUnits",300,200)==20&&(int)Run("MzHeartUnits",-1,200)==0&&(int)Run("MzHeartUnits",10,0)==0,"heart values clamp overhealth and invalid HP/maxHP");
        var atlas=Get<Texture2D>("MzHeartAtlas");Check(atlas.Width==128&&atlas.Height==256&&atlas.Released&&atlas.filterMode==FilterMode.Point,"one pixel atlas releases native CPU pixels and preserves stepped hearts");
        int previous=-1;for(int state=0;state<=20;state++){
            int lit=0;for(int y=0;y<9;y++)for(int x=0;x<99;x++){var pixel=atlas.Pixels[(255-state*11-y)*128+x];if(pixel.a>.5f&&pixel.r>.5f&&pixel.g>.5f)lit++;}
            Check(lit>=previous,"atlas fills monotonically at half-heart state "+state);previous=lit;
            Run("MzClearCommands");Run("MzHeartRow",640f,200f,140f,state*10,200);var uv=Get<Vector2[]>("MzHeartUvs");var points=Get<Vector3[]>("MzHeartVertices");
            Check(Get<int>("MzHeartCount")==1&&uv.Take(4).All(v=>v.x>=0&&v.x<=1&&v.y>=0&&v.y<=1)&&points.Take(4).All(v=>v.y<200),"state "+state+" uses a bounded atlas region above the head");
        }
        var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[0]=true;sw[5]=true;Camera.main=new Camera {gameObject=new GameObject("hearts-camera")};COW.GameFacade.Match=new JMAGGLCNGIG();for(int i=0;i<28;i++)COW.GameFacade.Match.Players.Add(Enemy((i%8-4)*.1f));
        int draws=Graphics.Draws;GUI.Calls.Clear();Render();Check(Graphics.Draws==draws+1&&Get<int>("MzHeartCount")==16,"sixteen health rows share one draw with no extra per-heart calls");
        var vertices=Get<Vector3[]>("MzHeartVertices");int textures=Texture2D.Created,meshes=Mesh.Created;for(int i=0;i<120;i++){GUI.Calls.Clear();Render();}
        Check(Texture2D.Created==textures&&Mesh.Created==meshes&&Object.ReferenceEquals(vertices,Get<Vector3[]>("MzHeartVertices")),"health refresh reuses the atlas mesh and CPU buffers");
        var good=Enemy();var bad=Enemy();bad.FailNeck=true;COW.GameFacade.Match.Players.Clear();COW.GameFacade.Match.Players.Add(bad);COW.GameFacade.Match.Players.Add(good);sw[7]=true;GUI.Calls.Clear();Render();Check(Get<int>("MzHeartCount")==1&&GUI.Calls.Count(d=>Object.ReferenceEquals(d.Texture,atlas))==1,"late native getter failure rolls back already emitted hearts before next enemy");sw[7]=false;
        COW.GameFacade.Match.Players.Clear();for(int i=0;i<28;i++){var p=Enemy((i%8-4)*.1f);p.IsClientBot=i>=20;COW.GameFacade.Match.Players.Add(p);}
        var local=Enemy();local.Local=true;COW.GameFacade.Match.Players.Add(local);var team=Enemy();team.Team=true;COW.GameFacade.Match.Players.Add(team);var dead=Enemy();dead.HP=0;COW.GameFacade.Match.Players.Add(dead);var hidden=Enemy();hidden.gameObject.activeInHierarchy=false;COW.GameFacade.Match.Players.Add(hidden);var stale=Enemy();stale.FailHP=true;COW.GameFacade.Match.Players.Add(stale);COW.GameFacade.Match.Players.Add(null);
        sw[10]=true;Run("MzResetCounts");GUI.Calls.Clear();Render();Check(Get<int>("MzPeople")==16&&Get<int>("MzBots")==0&&Get<int>("MzHeartCount")==16,"count reflects sixteen rendered ESP rows, not the full roster");
        string people=Get<string>("MzPeopleText");Set("MzNextEsp",Time.Now+1);Run("MzRender");Check(Object.ReferenceEquals(people,Get<string>("MzPeopleText")),"cached geometry retains the same count text");((Player)COW.GameFacade.Match.Players[0]).IsClientBot=true;Render();Check(Get<int>("MzPeople")==15&&Get<int>("MzBots")==1,"rendered count follows captured native bot classification on next ESP sample");
        COW.GameFacade.Match=null;Run("MzRender");Check(Get<int>("MzHeartCount")==0&&Get<string>("MzPeopleText")==null&&Get<string>("MzBotsText")==null,"scene teardown removes heart geometry and stale counts");
        COW.GameFacade.Match=new JMAGGLCNGIG();COW.GameFacade.Match.Players.Add(good);Render();Check(Get<JMAGGLCNGIG>("MzEspMatch")!=null,"active ESP holds only current scene snapshot");sw[0]=false;sw[13]=false;GUI.Calls.Clear();Run("MzRender");Check(Get<JMAGGLCNGIG>("MzEspMatch")==null&&Get<Camera>("MzEspCamera")==null&&Get<int>("MzHeartCount")==0&&GUI.Calls.Count==0,"disabling overlay releases previous scene references without waiting for another match");COW.GameFacade.Match=null;
        Set("MzPanelX",120f);Set("MzPanelY",16f);Set("MzBubbleX",24f);Set("MzBubbleY",96f);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",0);Set("MzDragMode",0);if(Get<float[]>("MzScroll")==null){Set("MzScroll",new float[2]);Set("MzScrollTarget",new float[2]);}Get<float[]>("MzScroll")[0]=0;Get<float[]>("MzScrollTarget")[0]=0;Set("MzRequestStarted",Time.Now);Set("MzRequest",null);License(Record(Get<string>("MzDeviceHash")));Set("MzTabAnim",0f);Frame(EventType.Repaint);Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-esp-top.svg"));bool[] before=(bool[])sw.Clone();
        Frame(EventType.MouseDown,220,180);Frame(EventType.MouseDrag,220,40);Frame(EventType.MouseUp,220,40);Check(Math.Abs(Get<float[]>("MzScroll")[0]-140)<.01f&&sw.SequenceEqual(before),"vertical ESP drag scrolls without activating a toggle underneath");
        Frame(EventType.MouseDown,220,400);Frame(EventType.MouseDrag,220,150);Frame(EventType.MouseUp,220,150);Frame(EventType.Repaint);Check(Get<float[]>("MzScroll")[0]==276&&GUI.Calls.Any(d=>d.Text!=null&&d.Text.StartsWith("Tia nối:")),"single ESP column reaches last controls and clamps to content");Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-esp-bottom.svg"));
        Set("MzTab",1);Set("MzTabAnim",1f);Get<float[]>("MzScroll")[1]=0;Get<float[]>("MzScrollTarget")[1]=0;Frame(EventType.Repaint);Check(Get<float[]>("MzScroll")[0]==276,"AIM tab preserves independent ESP scroll position");
        Frame(EventType.MouseDown,134,388);Check(Get<float>("MzHeadChance")==0,"head chance slider reaches zero");Frame(EventType.MouseDrag,488,388);Check(Get<float>("MzHeadChance")==100,"head chance slider reaches one hundred");Frame(EventType.MouseUp,311,388);Frame(EventType.Repaint);Check(Get<float>("MzHeadChance")==50&&GUI.Calls.Any(d=>d.Text=="Silent · Đầu 50% · Thân 50%"),"head chance slider updates VM setting and visible Vietnamese label");Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-aim-chance.svg"));
        Set("MzHeadChance",0f);Check(!(bool)Run("MzSilentHeadAllowed"),"zero chance never redirects head aim");Set("MzHeadChance",100f);Check((bool)Run("MzSilentHeadAllowed"),"one hundred chance always permits eligible head redirection");Set("MzHeadChance",40f);Set("MzChanceSeed",1L);Set("MzChanceFrame",-1);int accepted=0;
        for(int i=0;i<1000;i++){Time.Frame=10000+i;bool allowed=(bool)Run("MzSilentHeadAllowed");if(allowed)accepted++;long seed=Get<long>("MzChanceSeed");Check((bool)Run("MzSilentHeadAllowed")==allowed&&Get<long>("MzChanceSeed")==seed,"multiple native getters cannot reroll the same frame "+i);}
        Check(accepted>330&&accepted<470,"intermediate head chance produces both outcomes around configured probability");
        Array.Clear(sw,0,sw.Length);sw[12]=true;Set("MzHeadChance",100f);Set("MzFov",150f);Set("MzRange",150f);COW.GameFacade.Match=new JMAGGLCNGIG();COW.GameFacade.Match.Players.Add(good);local.Firing=true;local.Origin=new Vector3(0,0,1);COW.GameFacade.Local=local;var aim=local.FDMIEDDNCEC;aim.HPCOJHGFEEG=new Vector3(7,8,9);aim.EFMBNPNFGHM=true;Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);Check(aim.JHAINOBIFMA==good.gameObject,"one hundred chance reaches real native target record in VM");Set("MzHeadChance",0f);Run("get_LastAimingInfoFromWeapon",local);Check(aim.JHAINOBIFMA==good.gameObject&&aim.JINDNOFABKB==good.BodyCollider&&Vector3.Distance(aim.HPCOJHGFEEG,good.BodyCollider.bounds.center)<.001f,"zero chance explicitly redirects native hit record to body collider and body point");
        local.Firing=false;sw[12]=false;Set("MzHeadChance",100f);Time.Now=0;Get<float[]>("MzScroll")[0]=0;Get<float[]>("MzScrollTarget")[0]=0;COW.GameFacade.Match=null;
    }
    sealed class CountedRoster:System.Collections.ArrayList
    {
        public int Reads;public float ReadCost;
        public override object this[int index]{get{Reads++;Time.Now+=ReadCost;return base[index];}set{base[index]=value;}}
    }
    static void StabilityR2Checks()
    {
        float[] values={0,-0f,1,-1,float.Epsilon,-float.Epsilon,float.MinValue,float.MaxValue,float.NaN,float.PositiveInfinity,float.NegativeInfinity};
        foreach(float value in values)Check((bool)Run("MzFinite",value)==(!float.IsNaN(value)&&!float.IsInfinity(value)),"finite guard preserves IEEE boundary "+value);
        Time.Now=4000;
        var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[0]=true;sw[1]=true;sw[5]=true;sw[10]=true;Set("MzRange",150f);Camera.main=new Camera {gameObject=new GameObject("r2-camera")};var pool=new List<Player>();var match=new JMAGGLCNGIG();for(int i=0;i<16;i++){var p=Enemy((i%8-4)*.1f);pool.Add(p);match.Players.Add(p);}COW.GameFacade.Match=match;Time.Now+=6;GUI.Calls.Clear();Render();
        int clears=Mesh.ClearCalls,indices=Mesh.IndexUploads,textures=Texture2D.Created,meshes=Mesh.Created;var atlas=Get<Texture2D>("MzHeartAtlas");var mesh=Get<Mesh>("MzMesh");var hearts=Get<Mesh>("MzHeartMesh");int capacity=mesh.Vertices.Length,heartCapacity=hearts.Vertices.Length;
        for(int frame=0;frame<96;frame++){int visible=frame%16+1;for(int i=0;i<16;i++)pool[i].Team=i>=visible;Time.Now+=1f/60;GUI.Calls.Clear();Render();Check(Lines()==visible*5&&Get<int>("MzHeartCount")==visible,"despawn/respawn churn draws only active rows "+frame);Check(mesh.Vertices.Length==capacity&&hearts.Vertices.Length==heartCapacity,"GPU capacity remains stable across actor-count changes "+frame);}
        Check(Mesh.ClearCalls==clears&&Mesh.IndexUploads==indices,"actor churn does not clear meshes or rebuild native index buffers");Check(Texture2D.Created==textures&&Mesh.Created==meshes,"actor churn does not create textures or meshes");
        Set("MzColorTarget",2);for(int i=0;i<32;i++){Run("MzSetColor",new Color(i/31f,.3f,.8f,1),false);GUI.Calls.Clear();Render();}
        Check(Object.ReferenceEquals(atlas,Get<Texture2D>("MzHeartAtlas"))&&Texture2D.Created==textures&&Mesh.Created==meshes,"health color changes reuse grayscale atlas and native mesh instead of rebuilding images");Check(Math.Abs(hearts.Colors[0].r-1)<.001f&&Math.Abs(hearts.Colors[0].b-.8f)<.001f,"GPU health tint follows the selected color");
        var owner=Enemy();var collider=owner.Collider;var obj=owner.gameObject;Set("MzAimOwner",owner);Set("MzChangedAim",owner.FDMIEDDNCEC);Set("MzAppliedObject",obj);Set("MzAppliedCollider",collider);Set("MzOldAimObject",obj);Set("MzOldAimCollider",collider);owner.Destroyed=true;Run("MzRestoreSilent");Check(Get<Player>("MzAimOwner")==null&&Get<GameObject>("MzAppliedObject")==null&&Get<GameObject>("MzOldAimObject")==null&&Get<Collider>("MzAppliedCollider")==null&&Get<Collider>("MzOldAimCollider")==null,"destroyed aim owner cannot retain previous scene objects after restoration");
        COW.GameFacade.Match=null;Run("MzRender");Time.Now=0;
    }
    static void StabilityR3Checks()
    {
        var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[12]=true;Set("MzHeadChance",100f);Set("MzFov",150f);Set("MzRange",150f);Set("MzLeaseDeadline",100000f);
        Time.Now=5000;Time.Frame=500000;Camera.main=new Camera {gameObject=new GameObject("r3-camera")};var local=Enemy();local.Local=true;local.Firing=true;COW.GameFacade.Local=local;
        var bad=Enemy();bad.FailHP=true;var good=Enemy();var roster=new CountedRoster();roster.Add(bad);roster.Add(good);COW.GameFacade.Match=new JMAGGLCNGIG {Players=roster};Run("MzResetAim");
        Run("MzRunAim");Check(Object.ReferenceEquals(Get<Player>("MzAimTarget"),good),"one failing actor cannot abort aim selection for later valid actors");
        int reads=roster.Reads,projections=Camera.ProjectionCalls;var record=local.FDMIEDDNCEC;Run("get_LastAimingInfoFromWeapon",local);Check(record.JHAINOBIFMA==good.gameObject,"silent consumes main-thread target snapshot");
        for(int i=0;i<1000;i++)Run("get_LastAimingInfoFromWeapon",local);
        Check(roster.Reads==reads&&Camera.ProjectionCalls==projections,"one thousand weapon getters do not rescan actors or call camera projection");
        good.Collider.Destroyed=true;Run("get_LastAimingInfoFromWeapon",local);Check(Object.ReferenceEquals(record.JINDNOFABKB,null)&&Get<CGKJLKPMGDJ>("MzChangedAim")==null,"collider destroyed during same-frame getter burst is removed immediately");good.Collider.Destroyed=false;
        Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);Time.Now+=.11f;Run("get_LastAimingInfoFromWeapon",local);Check(Get<Player>("MzAimTarget")==null&&record.JHAINOBIFMA==null,"expired target snapshot cannot write into the native aim record");
        Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);COW.GameFacade.Match=new JMAGGLCNGIG();Run("get_LastAimingInfoFromWeapon",local);Check(Get<Player>("MzAimTarget")==null&&record.JHAINOBIFMA==null,"match replacement invalidates silent snapshot before native writes");COW.GameFacade.Match=new JMAGGLCNGIG {Players=roster};
        var oldObject=new GameObject("old-aim-object");var oldCollider=new Collider {gameObject=oldObject};record.JHAINOBIFMA=oldObject;record.JINDNOFABKB=oldCollider;Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);oldObject.Destroyed=true;oldCollider.Destroyed=true;Run("MzRestoreSilent");
        Check(Object.ReferenceEquals(record.JHAINOBIFMA,null)&&Object.ReferenceEquals(record.JINDNOFABKB,null),"restoration never attaches destroyed old Unity object or collider references");
        Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);var replacement=new GameObject("native-replacement-destroyed");replacement.Destroyed=true;good.gameObject.Destroyed=true;record.JHAINOBIFMA=replacement;Run("MzRestoreSilent");
        Check(Object.ReferenceEquals(record.JHAINOBIFMA,replacement),"distinct destroyed objects cannot fool reference-identity restoration guard");good.gameObject.Destroyed=false;record.JHAINOBIFMA=null;record.JINDNOFABKB=null;
        Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);var applied=record.JHAINOBIFMA;reads=roster.Reads;Exception workerError=null;
        var worker=new System.Threading.Thread(delegate(){try{for(int i=0;i<100;i++){Run("get_LastAimingInfoFromWeapon",local);Run("MzRunAim");}}catch(Exception e){workerError=e;}});worker.Start();Check(worker.Join(5000)&&workerError==null,"worker-thread getter burst returns without VM exception");
        Check(roster.Reads==reads&&Object.ReferenceEquals(record.JHAINOBIFMA,applied),"worker-thread hooks do not scan or mutate main-thread aiming");
        Run("MzResetAim");local.FiringRead=delegate(){Run("get_LastAimingInfoFromWeapon",local);Run("MzRunAim");};Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);local.FiringRead=null;
        Check(!Get<bool>("MzAimRunning")&&!Get<bool>("MzSilentRunning")&&record.JHAINOBIFMA==good.gameObject,"nested firing/getter callbacks cannot re-enter either aim transaction");
        Run("MzResetAim");sw[12]=false;sw[11]=true;Time.Frame++;int writes=local.AimCalls;local.RotationWrite=delegate(){Run("MzRunAim");};Run("MzRunAim");local.RotationWrite=null;reads=roster.Reads;Run("MzRunAim");Run("MzRunAim");
        Check(local.AimCalls==writes+1&&roster.Reads==reads,"reentrant and duplicate repaints apply camera rotation and target scan only once per frame");
        local.gameObject.activeInHierarchy=false;Time.Frame++;writes=local.AimCalls;reads=roster.Reads;Run("MzRunAim");Check(local.AimCalls==writes&&roster.Reads==reads&&Get<Player>("MzAimTarget")==null,"inactive local avatar stops aim before actor scan or native rotation");local.gameObject.activeInHierarchy=true;
        Run("MzResetAim");Time.Now=10;var slow=new CountedRoster {ReadCost=.0012f};for(int i=0;i<15;i++){var p=Enemy();p.Team=true;slow.Add(p);}slow.Add(good);COW.GameFacade.Match=new JMAGGLCNGIG {Players=slow};bool found=false;
        for(int pass=0;pass<8;pass++){Time.Frame++;int before=slow.Reads;Run("MzRunAim");Check(slow.Reads-before<=3,"slow aim roster obeys 3 ms work budget "+pass);if(Object.ReferenceEquals(Get<Player>("MzAimTarget"),good))found=true;}
        Check(found,"bounded aim cursor eventually visits targets after a slow friendly prefix");slow.ReadCost=0;COW.GameFacade.Match=new JMAGGLCNGIG {Players=roster};
        Array.Clear(sw,0,sw.Length);sw[0]=true;reads=roster.Reads;Render();Check(roster.Reads==reads,"master ESP with no selected visual performs no actor traversal");
        Set("MzHeapFloor",0L);Run("MzHeapObserve",134217728L);Run("MzHeapObserve",218103808L);Check(Get<int>("MzMemoryPressure")==2,"heap pressure adapts work without a feature pause flag");
        Run("MzResetAim");Array.Clear(sw,0,sw.Length);COW.GameFacade.Match=null;local.Firing=false;Run("MzRender");Time.Now=0;
        Console.WriteLine("PASS: R3 thread/reentry, bounded weapon getters, despawn/identity restoration and memory circuit breaker.");
    }
    static void GameplayR3Soak(string[] args,bool quick)
    {
        var results=new List<object>();GUI.Record=false;var sw=Get<bool[]>("MzSwitch");int frames=quick?600:36000;
        for(int mode=0;mode<3;mode++){
            Array.Clear(sw,0,sw.Length);if(mode>0){sw[0]=true;sw[1]=true;sw[4]=true;sw[5]=true;sw[6]=true;sw[10]=true;}if(mode!=1)sw[12]=true;
            Set("MzHeadChance",100f);Set("MzLeaseDeadline",100000f);Set("MzHeapFloor",0L);Set("MzHeapNext",0f);Run("MzResetAim");Camera.main=new Camera {gameObject=new GameObject("r3-soak-camera")};var local=Enemy();local.Local=true;local.Firing=true;COW.GameFacade.Local=local;
            var match=new JMAGGLCNGIG();for(int i=0;i<16;i++)match.Players.Add(Enemy((i%8-4)*.1f));COW.GameFacade.Match=match;Time.Now=10000;Time.Frame=1000000+mode*100000;Render();int textures=Texture2D.Created,meshes=Mesh.Created;long before=GC.GetTotalMemory(true),maximum=before;
            for(int f=0;f<frames;f++){
                Time.Frame++;Time.Now=10000+f/60f;local.Firing=f%31!=0;
                if(f%997==0){var p=(Player)match.Players[(f/997)%16];p.HP=p.HP>0?0:100;}
                if(f%1800==0){Run("MzResetAim");match=new JMAGGLCNGIG();for(int i=0;i<16;i++)match.Players.Add(Enemy((i%8-4)*.1f));COW.GameFacade.Match=match;}
                Run("MzMemoryTick");Run("MzRender");Run("MzRunAim");for(int read=0;read<10;read++)Run("get_LastAimingInfoFromWeapon",local);
                if(f%600==0){maximum=Math.Max(maximum,GC.GetTotalMemory(false));Check(!Get<bool>("MzAimRunning")&&!Get<bool>("MzSilentRunning")&&!Get<bool>("MzRendering")&&GL.Depth==0,"aim/ESP transaction state balanced mode "+mode+" frame "+f);}
            }
            Run("MzResetAim");long retained=GC.GetTotalMemory(true)-before;Check(retained<8388608&&Texture2D.Created==textures&&Mesh.Created==meshes,"mode "+mode+" soak retains bounded managed memory and creates no native render resources");
            results.Add(new {mode=mode==0?"aim-only":mode==1?"esp-only":"aim-plus-esp",frames=frames,simulatedSeconds=frames/60,gettersPerFrame=10,retainedDeltaBytes=retained,maxObservedManagedBytes=maximum,nativeTexturesAdded=Texture2D.Created-textures,nativeMeshesAdded=Mesh.Created-meshes,deviceVerified=false});
        }
        GUI.Record=true;COW.GameFacade.Match=null;Run("MzRender");Time.Now=0;File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"gameplay-r3-soak.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(results));Console.WriteLine("PASS: R3 aim-only, ESP-only and combined gameplay soak.");
    }
    static void AimProfile(string[] args)
    {
        Run("MzEnsure");Set("MzLeaseDeadline",100000f);Set("MzHeadChance",100f);Set("MzFov",150f);Set("MzRange",150f);var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[12]=true;
        Camera.main=new Camera {gameObject=new GameObject("aim-profile-camera")};var local=Enemy();local.Local=true;local.Firing=true;COW.GameFacade.Local=local;COW.GameFacade.Match=new JMAGGLCNGIG();for(int i=0;i<16;i++)COW.GameFacade.Match.Players.Add(Enemy((i%10-5)*.1f));GUI.Record=false;Time.Now=0;Time.Frame=1;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);AppDomain.MonitoringIsEnabled=true;GC.Collect();long before=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;int projections=Camera.ProjectionCalls;var timer=System.Diagnostics.Stopwatch.StartNew();
        const int frames=600,getters=10;for(int i=0;i<frames;i++){Time.Frame++;Time.Now=(i+1)/60f;Run("MzRunAim");for(int g=0;g<getters;g++)Run("get_LastAimingInfoFromWeapon",local);}timer.Stop();long allocated=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize-before;
        string json=new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {frames=frames,simulatedSeconds=10,gettersPerFrame=getters,allocatedBytes=allocated,bytesPerSecond=allocated/10,projections=Camera.ProjectionCalls-projections,wallMs=timer.ElapsedMilliseconds,deviceVerified=false});File.WriteAllText(args[3],json);Console.WriteLine(json);
    }
    static void EspProfile(string[] args)
    {
        Run("MzEnsure");Camera.main=new Camera {gameObject=new GameObject("profile-camera")};GUI.Record=false;
        AppDomain.MonitoringIsEnabled=true;var results=new List<object>();
        for(int scenario=0;scenario<3;scenario++){
            var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);sw[0]=true;sw[1]=true;sw[4]=true;sw[5]=true;sw[6]=true;sw[13]=true;
            int enemies=scenario==0?2:16;if(scenario==2)for(int i=0;i<11;i++)sw[i]=true;
            COW.GameFacade.Match=new JMAGGLCNGIG();for(int i=0;i<enemies;i++)COW.GameFacade.Match.Players.Add(Enemy((i%10-5)*.1f));
            Time.Now=0;Set("MzNextEsp",0f);Run("MzRender");GC.Collect();long retained=GC.GetTotalMemory(true),allocated=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;int reads=Camera.ProjectionCalls;
            var timer=System.Diagnostics.Stopwatch.StartNew();const int frames=1200;for(int f=0;f<frames;f++){Time.Now=(f+1)/60f;Run("MzRender");}timer.Stop();GC.Collect();
            long delta=AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize-allocated,live=GC.GetTotalMemory(true)-retained;
            results.Add(new {scenario=scenario==0?"two-enemies":scenario==1?"sixteen-enemies":"all-esp",frames=frames,simulatedSeconds=frames/60,allocatedBytes=delta,bytesPerSecond=delta/(frames/60),retainedDelta=live,wallMs=timer.ElapsedMilliseconds,projections=Camera.ProjectionCalls-reads});
        }
        string json=new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(results);File.WriteAllText(args[2],json);Console.WriteLine(json);
    }
    static void LocalConfigChecks()
    {
        string folder=Path.Combine(Path.GetTempPath(),"MizzeConfig-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);string file=Path.Combine(folder,"localConfig.json"),backup=file+".mizze-backup";
        try
        {
            Application.PersistentPath=folder;
            Set("MzConfigCleanupAttempted",false);Set("MzConfigRemoved",false);Run("MzRemoveLocalConfig");
            Check(!File.Exists(file)&&!File.Exists(backup),"loaded patch does not create localConfig or a backup");
            Check(Get<bool>("MzConfigRemoved"),"missing localConfig is already clean");
            File.WriteAllText(file,"{\"testCodePatch\":true}");File.WriteAllText(backup,"keep adjacent file");
            Set("MzConfigCleanupAttempted",false);Set("MzConfigRemoved",false);Run("MzEnsure");
            Check(!File.Exists(file)&&Get<bool>("MzConfigRemoved"),"first loaded menu hook removes localConfig");
            Check(File.ReadAllText(backup)=="keep adjacent file","cleanup leaves every adjacent file alone");
            File.WriteAllText(file,"external change");Run("MzRemoveLocalConfig");
            Check(File.ReadAllText(file)=="external change","cleanup does not repeat I/O each player or frame");
            File.WriteAllText(file,"invalid json");Set("MzConfigCleanupAttempted",false);Set("MzConfigRemoved",false);Run("MzRemoveLocalConfig");
            Check(Get<bool>("MzConfigRemoved")&&!File.Exists(file),"cleanup does not require parsing localConfig");
            File.WriteAllText(file,"locked config");
            using(var locked=File.Open(file,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                Set("MzConfigCleanupAttempted",false);Set("MzConfigRemoved",false);Run("MzRemoveLocalConfig");
                Check(Get<bool>("MzConfigCleanupAttempted")&&!Get<bool>("MzConfigRemoved")&&File.Exists(file),"delete failure is isolated from menu startup");
            }
            Application.PersistentPath=null;Set("MzConfigCleanupAttempted",false);Set("MzConfigRemoved",false);Run("MzRemoveLocalConfig");
            Check(Get<bool>("MzConfigCleanupAttempted")&&!Get<bool>("MzConfigRemoved"),"missing persistent path cannot delete another file");
        }
        finally
        {
            Application.PersistentPath=null;Set("MzConfigCleanupAttempted",false);Set("MzConfigRemoved",false);
            if(File.Exists(file))File.Delete(file);if(File.Exists(backup))File.Delete(backup);Directory.Delete(folder);
        }
    }
    static void NativeR147FootprintChecks(string[] args)
    {
        int before=Checks;
        string folder=Path.Combine(Path.GetTempPath(),"MizzeFootprint-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string legacy=Path.Combine(folder,"MizzeWeb"),config=Path.Combine(folder,"localConfig.json"),patch=Path.Combine(folder,"Assembly-CSharp-patch.bytes");
        string[] logs={"Mizze-webview.log","Mizze-session.log","Mizze-startup.log","Mizze-error.log"};
        try
        {
            Application.PersistentPath=folder;Set("MzConfigCleanupAttempted",false);Run("MzRemoveLocalConfig");
            Check(Directory.GetFileSystemEntries(folder).Length==0,"native cleanup creates no directories, files, logs or backups");
            Check(Get<bool>("MzConfigRemoved")&&Get<bool>("MzPatchRemoved")&&Get<bool>("MzLegacyCleanupFinished"),"absent owned artifacts already count as clean");
            Directory.CreateDirectory(legacy);File.WriteAllText(Path.Combine(legacy,"menu.html"),"legacy html");
            File.WriteAllText(config,"config");File.WriteAllText(patch,"loaded patch placeholder");
            foreach(string name in logs)File.WriteAllText(Path.Combine(folder,name),"old log");
            string keep=Path.Combine(folder,"game-save.dat");File.WriteAllText(keep,"preserve game save");
            Set("MzConfigCleanupAttempted",false);Run("MzEnsure");
            Check(!File.Exists(config)&&!File.Exists(patch),"loaded hook removes exactly the two bootstrap files");
            Check(!Directory.Exists(legacy)&&logs.All(name=>!File.Exists(Path.Combine(folder,name))),"old HTML and known diagnostic artifacts removed with empty legacy folder");
            Check(File.ReadAllText(keep)=="preserve game save"&&Directory.GetFileSystemEntries(folder).Length==1,"unrelated game data remains untouched");
            File.WriteAllText(config,"created later");File.WriteAllText(patch,"created later");Run("MzRemoveLocalConfig");
            Check(File.Exists(config)&&File.Exists(patch),"file cleanup is bounded to one attempt per loaded session");
            Directory.CreateDirectory(legacy);string unknown=Path.Combine(legacy,"user-file.txt");File.WriteAllText(unknown,"preserve");
            File.WriteAllText(Path.Combine(legacy,"menu.html"),"old html");
            Set("MzConfigCleanupAttempted",false);Run("MzRemoveLocalConfig");
            Check(File.ReadAllText(unknown)=="preserve"&&Directory.Exists(legacy),"nonrecursive cleanup preserves unknown files inside legacy folder");
            Check(!Get<bool>("MzLegacyCleanupFinished")&&!File.Exists(Path.Combine(legacy,"menu.html")),"incomplete legacy directory cleanup is reported truthfully");
            File.Delete(unknown);Directory.Delete(legacy);File.WriteAllText(config,"locked config");File.WriteAllText(patch,"remove independently");
            using(var locked=File.Open(config,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                Set("MzConfigCleanupAttempted",false);Run("MzEnsure");
                Check(!Get<bool>("MzConfigRemoved")&&File.Exists(config),"locked config failure isolated");
                Check(Get<bool>("MzPatchRemoved")&&!File.Exists(patch),"locked config does not block independent artifact cleanup");
                Check(Get<bool>("MzInitialized"),"cleanup failure does not prevent native menu initialization");
            }
            Application.PersistentPath=null;Set("MzConfigCleanupAttempted",false);Run("MzRemoveLocalConfig");
            Check(File.Exists(config),"missing persistent path cannot delete unrelated current-directory files");
            Check(!Methods.Keys.Any(name=>name.StartsWith("MzWeb")),"compiled method table has no WebView code");
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r147-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,nativeMenuOnly=true,noCreatedArtifacts=true,knownFilesOnly=true,recursiveDeletion=false,independentFailureIsolation=true,cleanupOnce=true,deviceVerified=false}));
            Console.WriteLine("PASS: R14.7 "+(Checks-before)+" footprint checks; no creation, exact two-file cleanup, old WebView/log cleanup, unknown-file preservation, locked-file isolation and native bootstrap.");
        }
        finally
        {
            Application.PersistentPath=null;Set("MzConfigCleanupAttempted",false);
            // Only test-created known paths; no recursive deletion.
            foreach(string name in logs)File.Delete(Path.Combine(folder,name));
            File.Delete(config);File.Delete(patch);File.Delete(Path.Combine(folder,"game-save.dat"));
            if(Directory.Exists(legacy)){File.Delete(Path.Combine(legacy,"menu.html"));File.Delete(Path.Combine(legacy,"user-file.txt"));Directory.Delete(legacy);}
            Directory.Delete(folder);
        }
    }
    static int Main(string[] args)
    {
        CurrentPatchPath=args[0];PlayerPrefs.Saved["Mizze.Language"]="vi";var symbolPath=Path.Combine(Path.GetDirectoryName(args[0]),"symbol-map.json");if(File.Exists(symbolPath))Symbols=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,string>>(File.ReadAllText(symbolPath));
        foreach(var line in File.ReadAllLines(args[1])){var a=line.Split('\t');Methods.Add(a[2],int.Parse(a[0]));}
        BlockOptionalInputTypes=args.Contains("--input-unavailable")||args.Contains("--expect-input-load-failure");
        try{Vm=Load(args[0]);}catch(TypeLoadException e){if(!args.Contains("--expect-input-load-failure")||!e.Message.StartsWith("Optional input unavailable:"))throw;Console.WriteLine("PASS: negative control: R8 eager optional type failure blocks patch load before bootstrap.");return 0;}
        if(args.Contains("--expect-input-load-failure"))throw new Exception("Negative control did not fail loading optional type");
        if(args.Contains("--incident-only")){NativeIncidentChecks(args);return 0;}
        if(args.Contains("--aimkill-only")){NativeAimkillChecks(args);return 0;}
        if(args.Contains("--visible-count-only")){NativeVisibleCountChecks(args);return 0;}
        if(args.Contains("--r149-signature-only")){NativeR149SignatureChecks(args);return 0;}
        if(args.Contains("--r148-only")){NativeR148CredentialsChecks(args);return 0;}
        if(args.Contains("--r147-only")){NativeR147FootprintChecks(args);return 0;}
        if(args.Contains("--input-unavailable")){NativeR9RecoveryChecks(args);return 0;}
        if(args.Contains("--weapon-gate-only")){NativeR10Checks(args);return 0;}
        if(args.Contains("--r11-only")){NativeR11Checks(args);return 0;}
        if(args.Contains("--r12-only")){throw new NotSupportedException("Run R12 snapshot checks against the archived R12 bytes; use --r13-only for current original-rig behavior.");}
        if(args.Contains("--r143-only")){NativeR143Checks(args);return 0;}
        if(args.Contains("--r144-only")){NativeR144Checks(args);return 0;}
        if(args.Contains("--r145-only")){NativeR145Checks(args);return 0;}
        if(args.Contains("--r142-only")){NativeR142Checks(args);return 0;}
        if(args.Contains("--r141-only")){NativeR141Checks(args);return 0;}
        if(args.Contains("--r14-only")){NativeR14Checks(args);return 0;}
        if(args.Contains("--r132-only")){NativeR132Checks(args);return 0;}
        if(args.Contains("--r13-only")){NativeR13Checks(args);return 0;}
        if(args.Contains("--expect-owner-gate-failure")){NativeR11NegativeControl();return 0;}
        if(args.Contains("--expect-weapon-gate-failure")){NativeR10NegativeControl();return 0;}
        if(Fields.ContainsKey("MzWebDisabled"))Set("MzWebDisabled",true);bool configOnly=args.Length>2&&args[2]=="--config-only",quick=args.Length>2&&args[2]=="--quick",featuresOnly=args.Length>2&&args[2]=="--features-only";if(args.Length>2&&args[2]=="--autofire-soak"){AutofireR7Soak(args);return 0;}if(args.Length>2&&args[2]=="--native-only"){NativeR6Checks(args);NativeR7Checks(args);NativeR8Checks(args);return 0;}if(quick)SoakFrames=1200;if(args.Length>2&&args[2]=="--aim-profile"){AimProfile(args);return 0;}if(args.Length>2&&!configOnly&&!quick&&!featuresOnly){EspProfile(args);return 0;}LocalConfigChecks();
        if(configOnly){Console.WriteLine("PASS: "+Checks+" checks; final IFix bytecode config cleanup and menu bootstrap, exact file scope, missing file, one attempt per session, locked-file isolation and missing persistent path.");GC.KeepAlive(Vm);return 0;}
        if(featuresOnly){Driver=GameObject.Find("__mizze_driver").Added;HeartCounterScrollChanceChecks(args);Console.WriteLine("PASS: "+Checks+" checks; final bytecode hearts, counts, scroll and head chance.");GC.KeepAlive(Vm);return 0;}
        // Lobby getter preserves the lazy model and bootstraps with no match or player.
        var model=Run("get_UIModelRole");Check(model!=null&&System.Object.ReferenceEquals(model,Run("get_UIModelRole")),"lobby cached model preserved");Check(GCommon.UIModelManager.instance.Calls==1,"model initialized once");
        var g=GameObject.Find("__mizze_driver");Check(g!=null&&g.Added!=null&&g.Persistent,"persistent driver created in lobby");Driver=g.Added;Check(Get<float>("MzPanelX")==120&&Get<float>("MzPanelY")==16,"new initial panel position");Set("MzPanelX",98f);Set("MzPanelY",30f);
        Frame(EventType.Repaint);Check(Labels()==0&&BadgeVisible(),"only compact Mizze badge initially");Check(Math.Abs(Get<float>("MzScale")-1.35f)<.0001f,"larger display scale at 720p");Check(GUI.Calls.Any(d=>d.Text==null&&d.Rect.width==76&&d.Rect.height==76&&d.Color.r<.06f&&d.Color.g<.1f),"launcher uses explicit black shader color");Check(GUI.Calls.Any(d=>d.Texture==Get<Texture2D>("MzBadgeTexture")&&d.Matrix.Scale.x==1.35f),"image launcher scale and size");Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"launcher.svg"));
        var local=Enemy(0);local.Local=true;local.Stealth=true;local.Origin=new Vector3(0,0,1);COW.GameFacade.Local=local;
        Check((bool)Run("IsReallyInStealth",local)&&local.StealthCalls==1&&local.Movable,"native stealth and movement preserved");local.Stealth=false;Check(!(bool)Run("IsReallyInStealth",local),"false stealth preserved");
        Click(56,128);Check(Get<bool>("MzOpen")&&Get<int>("MzTab")==2,"launcher opens key panel");Frame(EventType.MouseDown,56,128);Frame(EventType.MouseDrag,206,228);Frame(EventType.MouseUp,206,228);Check(Get<float>("MzBubbleX")==174&&Get<bool>("MzOpen"),"bubble drag does not toggle");
        Frame(EventType.MouseDown,140,54);Frame(EventType.MouseDrag,180,94);Frame(EventType.MouseUp,180,94);Check(Get<float>("MzPanelX")==138&&Get<float>("MzPanelY")==70,"panel drags independently");Set("MzPanelX",98f);Set("MzPanelY",30f);
        string hash=Get<string>("MzDeviceHash");Check(hash.Length==64,"device hash initialized");License(Record(hash));Check((bool)Run("MzAuthorized"),"missing lock handled as absent with game-like lazy JSON node");License(Record(hash,extra:",\"lock\":null"));Check((bool)Run("MzAuthorized"),"explicit null lock accepted");License(Record(hash,extra:",\"lock\":{\"until\":1791633500000,\"message\":\"finished\"}"));Check((bool)Run("MzAuthorized"),"expired temporary lock accepted");License(Record(hash,extra:",\"lock\":{}"));Check(!(bool)Run("MzAuthorized"),"malformed lock is denied without disclosing backend schema");License(Record(hash,extra:",\"lock\":false"));Check(!(bool)Run("MzAuthorized"),"non-object lock still rejected");License(Record(hash));
        License(Record(hash,false));Check(!(bool)Run("MzAuthorized"),"disabled key rejected");License(Record(hash,expiry:1791633599000));Check(!(bool)Run("MzAuthorized"),"expired key rejected");License(Record(new string('a',64)));Check(!(bool)Run("MzAuthorized"),"other device rejected");License(Record(hash,extra:",\"lock\":{\"until\":0,\"message\":\"locked\"}"));Check(!(bool)Run("MzAuthorized"),"permanent lock rejected");License(Record(hash).Replace("\"enabled\":true","\"enabled\":\"true\""));Check(!(bool)Run("MzAuthorized"),"typed boolean required");License(Record(hash),"bad-date");Check((bool)Run("MzAuthorized"),"signed proof is independent of untrusted HTTP Date");
        Set("MzKeyHash",new string('b',64));Set("MzCanClaim",true);Run("MzBeginRead");var request=Get<UnityWebRequest>("MzRequest");Check(request.Method=="POST"&&request.Body.Contains("\"claim\":true"),"first device registration is one signed server transaction");request.responseCode=403;request.isDone=true;Run("MzLicensePoll");Check(!(bool)Run("MzAuthorized")&&Get<UnityWebRequest>("MzRequest")==null,"losing quota claim stays denied without unsigned readback");
        Run("MzBeginRead");request=Get<UnityWebRequest>("MzRequest");Run("MzBeginCredentials");Run("MzAcceptCredential","  test-key-new  ");Run("MzCredentialButton");Run("MzAcceptCredential","Strong-Password-1234");Check(request.Disposed&&Get<UnityWebRequest>("MzRequest").Body.Contains((string)Run("MzCredentialHash","TEST-KEY-NEW","Strong-Password-1234")),"complete credential change cancels stale request and uses all three inputs");Get<UnityWebRequest>("MzRequest").Dispose();Set("MzRequest",null);Set("MzKeyHash","");
        License(Record(hash));float deadline=Get<float>("MzLeaseDeadline");Time.Now=deadline+1;Check(!(bool)Run("MzAuthorized"),"lease expires without network result");Time.Now=0;License(Record(hash));
        COW.GameFacade.Match=new JMAGGLCNGIG();Camera.main=new Camera {gameObject=new GameObject("camera")};var enemy=Enemy();COW.GameFacade.Match.Players.Add(enemy);var switches=Get<bool[]>("MzSwitch");Array.Clear(switches,0,switches.Length);switches[0]=true;switches[1]=true;
        GUI.Calls.Clear();Render();Check(Lines()==4,"2D box four edges");switches[2]=true;GUI.Calls.Clear();Render();Check(Lines()==8,"corner box eight edges");switches[2]=false;switches[3]=true;GUI.Calls.Clear();Render();Check(Lines()==12,"3D box twelve edges");switches[1]=false;switches[3]=false;
        switches[4]=true;GUI.Calls.Clear();Render();Check(GUI.Calls.Any(d=>d.Text==enemy.Nick),"real nickname label");switches[4]=false;switches[5]=true;GUI.Calls.Clear();Render();var bars=GUI.Calls.Where(d=>d.Text==null).ToArray();Check(bars.Length==1&&bars[0].Texture==Get<Texture2D>("MzHeartAtlas")&&bars[0].Rect.y<360-enemy.Head.position.y*100,"health hearts use one atlas quad above the head");switches[5]=false;
        switches[6]=true;GUI.Calls.Clear();Render();Check(GUI.Calls.Any(d=>d.Text!=null&&d.Text.EndsWith(" m")),"distance label");switches[6]=false;switches[7]=true;GUI.Calls.Clear();Render();Check(Lines()==10,"skeleton projects ten bone segments");switches[7]=false;switches[8]=true;GUI.Calls.Clear();Render();Check(GUI.Calls.Count==1&&GUI.Calls[0].Texture is Texture2D,"head circle is one cached texture");switches[8]=false;switches[9]=true;GUI.Calls.Clear();Render();Check(Lines()==3,"XYZ axes");switches[9]=false;switches[10]=true;GUI.Calls.Clear();Render();Check(Lines()==1&&GUI.Calls.Any(d=>d.Text!=null&&d.Text.StartsWith("Người "))&&GUI.Calls.Any(d=>d.Text==null&&d.Rect.width==238),"ESP snapline and cached count pill");switches[10]=false;
        switches[1]=true;enemy.Team=true;GUI.Calls.Clear();Render();Check(Lines()==0&&Run("MzPickTarget")==null,"team filtered from ESP and aim");enemy.Team=false;enemy.HP=0;GUI.Calls.Clear();Render();Check(Lines()==0,"dead filtered");enemy.HP=100;enemy.Head.position=new Vector3(float.NaN,1,10);Check(Run("MzPickTarget")==null,"invalid projection filtered");enemy.Head.position=new Vector3(.2f,1.2f,10);
        Set("MzFov",20f);Check(Run("MzPickTarget")==null,"small FOV excludes target");Set("MzFov",150f);Check(System.Object.ReferenceEquals(Run("MzPickTarget"),enemy),"head inside FOV selected");Set("MzRange",5f);Check(Run("MzPickTarget")==null,"range cap applies to aim");Set("MzRange",150f);
        switches[11]=true;Run("MzRunAim");Check(local.AimCalls==0,"normal aim idle without firing");local.Firing=true;Run("MzRunAim");var expected=Vector3.Normalize(enemy.Head.position-Camera.main.transform.position);Check(local.AimCalls==1&&Vector3.Distance(local.LastRotation.Direction,expected)<.0001,"normal aim calls game rotation toward head");switches[11]=false;switches[12]=true;Time.Frame++;Run("MzRunAim");
        var aim=local.FDMIEDDNCEC;aim.HPCOJHGFEEG=new Vector3(7,8,9);aim.EFMBNPNFGHM=true;var result=Run("get_LastAimingInfoFromWeapon",local);Check(Object.ReferenceEquals(result,aim)&&aim.JHAINOBIFMA==enemy.gameObject&&aim.JINDNOFABKB==enemy.Collider&&Vector3.Distance(aim.HLDECMJFKJK,Vector3.Normalize(enemy.Head.position-local.Origin))<.0001&&aim.DKPDBLLJEAA==LKOOALMKJND.Head&&!aim.EFMBNPNFGHM,"silent updates native aiming record and preserves returned object");Check(local.AimCalls==1,"silent avoids camera rotation call");Run("get_LastAimingInfoFromWeapon",enemy);Check(aim.JHAINOBIFMA==enemy.gameObject,"remote aiming getter leaves local silent record intact");local.Firing=false;Run("get_LastAimingInfoFromWeapon",local);Check(aim.HPCOJHGFEEG.x==7&&aim.EFMBNPNFGHM&&aim.JHAINOBIFMA==null,"silent restores when firing stops");
        local.Firing=true;Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);aim.JHAINOBIFMA=new GameObject("native-new-target");Run("MzRestoreSilent");Check(aim.JHAINOBIFMA.name=="native-new-target","native updates not overwritten by restoration");switches[12]=false;local.Firing=false;
        COW.GameFacade.Match.Players.Clear();for(int i=0;i<150;i++)COW.GameFacade.Match.Players.Add(enemy);GUI.Calls.Clear();Render();Check(Lines()==16*4,"bounded ESP traversal and sixteen visible-player cap");COW.GameFacade.Match.Players.Clear();COW.GameFacade.Match.Players.Add(enemy);
        Set("MzTab",1);Set("MzOpen",true);Set("MzPanelX",120f);Set("MzPanelY",16f);Set("MzFov",150f);Frame(EventType.Repaint);Get<float[]>("MzScroll")[1]=44;Get<float[]>("MzScrollTarget")[1]=44;Frame(EventType.MouseDown,289,405);Frame(EventType.MouseDrag,510,405);Frame(EventType.MouseUp,510,405);Check(Get<float>("MzFov")==352,"FOV slider stops at full visible circle");
        Get<float[]>("MzScroll")[1]=0;Get<float[]>("MzScrollTarget")[1]=0;Click(140,148);Check(switches[11]&&!switches[12]&&!Get<bool>("MzAimNeck"),"head checkbox connected");Click(140,192);Check(switches[11]&&!switches[12]&&Get<bool>("MzAimNeck"),"neck checkbox mutually exclusive");Click(140,236);Check(switches[12]&&!switches[11],"silent checkbox mutually exclusive");Click(140,236);Check(!switches[12],"silent checkbox turns off");
        Set("MzTab",0);Get<float[]>("MzScroll")[0]=276;Get<float[]>("MzScrollTarget")[0]=276;Click(337,418);Check(Get<int>("MzLineOrigin")==1,"line origin cycles to center");Click(337,418);Check(Get<int>("MzLineOrigin")==2,"line origin cycles to top");switches[10]=true;GUI.Calls.Clear();Render();Check(GUI.Calls.Any(d=>d.Texture==Texture2D.whiteTexture&&d.Matrix.Tag==2&&Math.Abs(d.Matrix.Position.y-8)<2),"top line projects from selected origin");Click(337,418);Check(Get<int>("MzLineOrigin")==0,"line origin cycles to bottom");
        COW.GameFacade.Match=null;GUI.Calls.Clear();switches[13]=true;Render();Check(GUI.Calls.Count==0,"lobby has no ESP or FOV overlay");COW.GameFacade.Match=new JMAGGLCNGIG();COW.GameFacade.Match.Players.Add(enemy);
        Set("MzTab",1);Set("MzPanelX",120f);Set("MzPanelY",16f);Set("MzFov",150f);Set("MzBubbleX",24f);Set("MzBubbleY",96f);switches[11]=true;switches[2]=true;switches[4]=true;switches[5]=true;switches[6]=true;
        for(int i=0;i<18;i++)Frame(EventType.Repaint);Check(Get<float>("MzOpenAnim")>.95f&&Get<float[]>("MzSwitchAnim")[1]>.95f,"panel and toggle animation advances");Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-aim.svg"));Set("MzTab",0);for(int i=0;i<18;i++)Frame(EventType.Repaint);Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-esp.svg"));
        Set("MzOpen",false);bool was=switches[0];Frame(EventType.MouseUp,140,140);Check(switches[0]==was,"closing animation accepts no hidden toggle taps");
        Set("MzOpen",true);Set("MzTab",2);for(int i=0;i<18;i++)Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Rect.width==382&&d.Rect.height==440&&d.Color.r<.03f&&d.Color.g<.03f),"compact panel supplies explicit black color");Check(GUI.Calls.Any(d=>d.Wrap&&d.FontSize==16),"key message wraps with independent readable style");Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-key.svg"));
        COW.GameFacade.Match=null;Screen.Width=1883;Screen.Height=870;Frame(EventType.Repaint);Check(Math.Abs(Get<float>("MzScale")-1.63125f)<.0001f,"scale matches supplied screenshot resolution");Check(80*Get<float>("MzScale")>125&&382*Get<float>("MzScale")>600,"button and panel grow on user's screen");float savedLease=Get<float>("MzLeaseDeadline");string savedMessage=Get<string>("MzLicenseMessage");Set("MzLeaseDeadline",0f);Set("MzLicenseMessage","Nhập khóa Mizze để kích hoạt");Frame(EventType.Repaint);Snapshot(Path.Combine(Path.GetDirectoryName(args[0]),"menu-key-870p.svg"));Set("MzLeaseDeadline",savedLease);Set("MzLicenseMessage",savedMessage);
        Screen.Width=2340;Screen.Height=1080;Frame(EventType.Repaint);Check(Math.Abs(Get<float>("MzScale")-2.025f)<.0001f,"high resolution scaling");Set("MzTab",1);Click(Get<float>("MzPanelX")+30,Get<float>("MzPanelY")+126);Check(!switches[11],"scaled touch toggles correct aim row");
        Screen.Width=390;Screen.Height=844;Frame(EventType.Repaint);float scale=Get<float>("MzScale");Check((Get<float>("MzPanelX")+382)*scale<=Screen.width&&(Get<float>("MzPanelY")+440)*scale<=Screen.height,"portrait layout clamps within viewport");
        Screen.Width=1280;Screen.Height=720;Frame(EventType.Repaint);Check(Get<float>("MzScale")==1.35f,"landscape scale restored after resize");
        Set("MzDriver",null);GameObject.World.Clear();GameObject.FailAdd=true;Check(!(bool)Run("IsReallyInStealth",local)&&local.Movable,"bootstrap failure preserves game behavior");GameObject.FailAdd=false;Run("MzEnsure");Check(GameObject.Find("__mizze_driver").Added!=null,"bootstrap retries component creation");
        ExtendedChecks(args);
        Console.WriteLine("PASS: bootstrap, menu, licensing and aim regression.");
        StabilityChecks(args);
        BatchChecks();
        Console.WriteLine("PASS: ESP batching, resource failures and scene teardown.");
        UiPerformanceChecks();
        UiOnlyGlassChecks(args);
        HeartCounterScrollChanceChecks(args);
        StabilityR2Checks();
        StabilityR3Checks();
        GameplayR3Soak(args,quick);
        Console.WriteLine("PASS: GIF/font, hearts, player/bot counts, scrolling and head chance controls.");
        EspCoreChecks(args);
        Console.WriteLine("PASS: "+Checks+" checks; final IFix bytecode executed by pinned Tencent VM. Lobby, drag, animations, ESP geometry/filtering, head aim/fire/FOV, silent record/restore, key binding/expiry/races. Unity/game/network are offline doubles; iPhone loading and in-game aim remain unverified.");GC.KeepAlive(Vm);return 0;
    }
}
