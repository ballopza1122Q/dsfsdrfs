// Offline test doubles only. This file is excluded from the patch build.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
namespace UnityEngine.Profiling {public sealed class Profiler {public static long ReportedHeap;public static long GetMonoUsedSizeLong(){return ReportedHeap;}}}
namespace IFix.Core {static class PatchManager {public static void Load(System.IO.Stream s){throw new NotSupportedException();}public static void Unload(System.Reflection.Assembly a){throw new NotSupportedException();}}}
namespace IFix {public class ILFixInterfaceBridge {}}
namespace UnityEngine
{
    public class Object
    {
        public string name {get;set;}public bool Persistent,Destroyed;
        public static bool operator ==(Object a,Object b){return (ReferenceEquals(a,null)||a.Destroyed)?(ReferenceEquals(b,null)||b.Destroyed):ReferenceEquals(a,b);}public static bool operator !=(Object a,Object b){return !(a==b);}
        public override bool Equals(object o){return ReferenceEquals(this,o);}public override int GetHashCode(){return base.GetHashCode();}public static void DontDestroyOnLoad(Object o){o.Persistent=true;}public static void Destroy(Object o){o.Destroyed=true;}
    }
    public class Component:Object {public GameObject gameObject {get;set;}public Transform transform {get{return gameObject.transform;}}}
    public class MonoBehaviour:Component {}
    public class GameObject:Object
    {
        public static Dictionary<string,GameObject> World=new Dictionary<string,GameObject>();public static bool FailAdd;
        public Transform transform {get;private set;}public bool activeInHierarchy {get;set;}public Component Added;public int layer {get;set;}public List<Component> ChildComponents=new List<Component>();public List<Component> Components=new List<Component>();public Component[] GetComponentsInChildren(Type type,bool inactive){return ChildComponents.Concat(Components).Where(c=>type.IsInstanceOfType(c)).ToArray();}
        public GameObject(string n){name=n;transform=new Transform();activeInHierarchy=true;World[n]=this;}
        public static GameObject Find(string n){GameObject g;return World.TryGetValue(n,out g)?g:null;}
        public Component GetComponent(Type t){if(Added!=null&&t.IsInstanceOfType(Added))return Added;return Components.FirstOrDefault(c=>t.IsInstanceOfType(c));}
        public Component AddComponent(Type t){if(FailAdd)throw new Exception("creation failure");Added=(Component)Activator.CreateInstance(t);Added.gameObject=this;return Added;}
    }
    public class Transform:Component
    {
        public Transform Parent;public List<Transform> Children=new List<Transform>();public Transform parent {get{return Parent;}}public int childCount {get{return Children.Count;}}public Transform GetChild(int i){return Children[i];}
        public Action PositionWrite,RotationWrite;Vector3 pos,scale=new Vector3(1,1,1);Quaternion rot=new Quaternion {w=1};
        public Vector3 localPosition {get{return pos;}set{if(PositionWrite!=null)PositionWrite();pos=value;}}
        public Quaternion localRotation {get{return rot;}set{if(RotationWrite!=null)RotationWrite();rot=value;}}
        static Quaternion Mul(Quaternion a,Quaternion b){return new Quaternion {x=a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,y=a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,z=a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,w=a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z};}
        static Quaternion Inv(Quaternion q){float n=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;return n>0?new Quaternion{x=-q.x/n,y=-q.y/n,z=-q.z/n,w=q.w/n}:new Quaternion {w=1};}
        static Vector3 Rotate(Quaternion q,Vector3 p){var v=Mul(Mul(q,new Quaternion{x=p.x,y=p.y,z=p.z}),Inv(q));return new Vector3(v.x,v.y,v.z);}
        public Vector3 WorldScale {get{if(Parent==null)return scale;var p=Parent.WorldScale;return new Vector3(p.x*scale.x,p.y*scale.y,p.z*scale.z);}}
        public Quaternion rotation {get{return Parent==null?rot:Mul(Parent.rotation,rot);}set{localRotation=Parent==null?value:Mul(Inv(Parent.rotation),value);}}
        public Vector3 position {get{if(Parent==null)return pos;var p=Parent.WorldScale;return Parent.position+Rotate(Parent.rotation,new Vector3(pos.x*p.x,pos.y*p.y,pos.z*p.z));}set{if(Parent==null){localPosition=value;return;}var p=Parent.WorldScale;var v=Rotate(Inv(Parent.rotation),value-Parent.position);localPosition=new Vector3(v.x/p.x,v.y/p.y,v.z/p.z);}}
        public Matrix4x4 localToWorldMatrix {get{return new Matrix4x4 {Position=position,Scale=WorldScale,Tag=2};}}public Vector3 localScale {get{return scale;}set{scale=value;}}
        public bool IsChildOf(Transform p){for(var t=this;t!=null;t=t.Parent)if(Object.ReferenceEquals(t,p))return true;return false;}
    }
    public class Collider:Component {public bool enabled {get;set;}public Collider(){enabled=true;}public bool FailBounds;public Vector3 Offset;public Bounds bounds {get{if(FailBounds)throw new Exception("collider unavailable");return new Bounds {center=transform.position+Offset};}}}
    public struct Bounds {public Vector3 center {get;set;}}
    public enum QueryTriggerInteraction {UseGlobal=0,Ignore=1,Collide=2}
    public struct RaycastHit {public Collider collider {get;set;}public float distance {get;set;}}
    public static class Physics
    {
        public static RaycastHit[] TestHits;public static int LastMask;public static QueryTriggerInteraction LastTriggers;public static bool Blocked;public static Collider First;public static int Queries;public static Vector3 LastOrigin;public static Action Query;
        public static int RaycastNonAlloc(Vector3 origin,Vector3 direction,RaycastHit[] results,float distance,int mask,QueryTriggerInteraction triggers){Queries++;LastOrigin=origin;LastMask=mask;LastTriggers=triggers;if(Query!=null)Query();if(TestHits!=null){int count=Math.Min(TestHits.Length,results.Length);Array.Copy(TestHits,results,count);return count;}if(!Blocked)return 0;results[0]=new RaycastHit {collider=First,distance=0};return 1;}
        public static bool Raycast(Vector3 origin,Vector3 direction,out RaycastHit hit,float distance,int mask,QueryTriggerInteraction triggers){Queries++;LastOrigin=origin;if(Query!=null)Query();hit=new RaycastHit {collider=First};return Blocked;}
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
        public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}
        public static Vector3 Normalize(Vector3 v){float l=Distance(v,new Vector3());return l>0?v*(1/l):new Vector3();}public static float Distance(Vector3 a,Vector3 b){Vector3 d=a-b;return (float)Math.Sqrt(d.x*d.x+d.y*d.y+d.z*d.z);}
    }
    public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}}
    public struct Color {public float r,g,b,a;public Color(float x,float y,float z,float w){r=x;g=y;b=z;a=w;}}
    public struct Quaternion {public float x,y,z,w;public Vector3 Direction;public float Angle;public static Quaternion Euler(float a,float b,float c){return new Quaternion {Angle=c};}public static Quaternion LookRotation(Vector3 d){return new Quaternion {Direction=Vector3.Normalize(d)};}}
    public struct Matrix4x4 {public static int TrsCalls;public Vector3 Position,Scale;public float Angle;public int Tag;public static Matrix4x4 identity {get{return new Matrix4x4 {Tag=1,Scale=new Vector3(1,1,1)};}}public static Matrix4x4 TRS(Vector3 p,Quaternion q,Vector3 s){TrsCalls++;return new Matrix4x4 {Position=p,Scale=s,Angle=q.Angle,Tag=2};}}
    public struct Rect {public float x {get;set;}public float y {get;set;}public float width {get;set;}public float height {get;set;}public Rect(float a,float b,float c,float d){this=new Rect();x=a;y=b;width=c;height=d;}public bool Contains(Vector2 p){return p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height;}}
    public class Camera:Component {public delegate void CameraCallback(Camera camera);public static CameraCallback onPreCull,onPostRender;public static Camera main {get;set;}public static int ProjectionCalls;public float fieldOfView {get;set;}public float ProjectionScale=1;public bool FailProjection;public Camera(){fieldOfView=60;}public Vector3 WorldToScreenPoint(Vector3 p){ProjectionCalls++;p=p-transform.position;var q=transform.rotation;float x=-q.x,y=-q.y,z=-q.z,w=q.w;float tx=2*(y*p.z-z*p.y),ty=2*(z*p.x-x*p.z),tz=2*(x*p.y-y*p.x);p=new Vector3(p.x+w*tx+y*tz-z*ty,p.y+w*ty+z*tx-x*tz,p.z+w*tz+x*ty-y*tx);if(FailProjection)throw new Exception("projection failure");float focal=1000*ProjectionScale*(float)(Math.Tan(Math.PI/6)/Math.Tan(fieldOfView*Math.PI/360));return new Vector3(Screen.width*.5f+p.x*focal/p.z,Screen.height*.5f+p.y*focal/p.z,p.z);}}
    public static class Screen {public static int Width=1280,Height=720;public static int height {get{return Height;}}public static int width {get{return Width;}}}
    public static class Time {public static float Now;public static float unscaledDeltaTime {get{return .016f;}}public static int Frame;public static int frameCount {get{return Frame;}}public static float realtimeSinceStartup {get{return Now;}}}
    public static class Mathf {public static float Sqrt(float f){return (float)Math.Sqrt(f);}public static float Sin(float f){return (float)Math.Sin(f);}public static float Cos(float f){return (float)Math.Cos(f);}public static float Atan2(float y,float x){return (float)Math.Atan2(y,x);}public static float Clamp(float v,float a,float b){return Math.Max(a,Math.Min(b,v));}public static float Lerp(float a,float b,float t){return a+(b-a)*Math.Min(1,Math.Max(0,t));}}
    public static class PlayerPrefs {public static Dictionary<string,string> Saved=new Dictionary<string,string>();public static string GetString(string k,string d){string v;return Saved.TryGetValue(k,out v)?v:d;}public static void SetString(string k,string v){Saved[k]=v;}public static void DeleteKey(string k){Saved.Remove(k);}}
    public static class Application {public static string PersistentPath;public static string persistentDataPath {get{return PersistentPath;}}} public static class SystemInfo {public static string deviceUniqueIdentifier {get{return "offline-device";}}}
    public enum TouchScreenKeyboardType {Default=0,ASCIICapable=1}
    public class TouchScreenKeyboard {public enum Status {Visible=0,Done=1,Canceled=2,LostFocus=3}public static TouchScreenKeyboard Open(string text,TouchScreenKeyboardType t,bool a,bool m,bool secure,bool alert,string placeholder,int limit){return new TouchScreenKeyboard {text=text};}public string text {get;set;}public Status status {get;set;}}
    public enum TextureFormat {RGBA32=4}public enum FilterMode {Point=0,Bilinear=1}public enum TextureWrapMode {Clamp=1}
    public class Shader:Object {public static bool Available=true;public static Shader Find(string name){return Available?new Shader():null;}}
    public class Material:Object {public static bool FailPass;public static int Created;public Texture BoundTexture;public Dictionary<string,int> Values=new Dictionary<string,int>();public void SetTexture(string key,Texture texture){BoundTexture=texture;}public Material(Shader s){Created++;}public void SetInt(string n,int v){Values[n]=v;}public bool SetPass(int p){if(FailPass)return false;Graphics.BoundMaterial=this;return true;}}
    public class Mesh:Object {public int TestVertexCount,TestSubMeshCount=1,BakedPose;public int vertexCount {get{return Vertices!=null?Vertices.Length:TestVertexCount;}}public int subMeshCount {get{return TestSubMeshCount;}}public static int Created,Uploads,ClearCalls,IndexUploads;public Vector3[] Vertices;public Vector2[] Uvs;public Color[] Colors;public int[] Indices;public Mesh(){Created++;}public void MarkDynamic(){}public void Clear(){ClearCalls++;Vertices=null;Uvs=null;Colors=null;Indices=null;}public void SetUVs(int channel,Vector2[] a,int start,int length){Uvs=new Vector2[length];Array.Copy(a,start,Uvs,0,length);}public void SetVertices(Vector3[] a,int start,int length){Uploads++;Vertices=new Vector3[length];Array.Copy(a,start,Vertices,0,length);}public void SetColors(Color[] a,int start,int length){Colors=new Color[length];Array.Copy(a,start,Colors,0,length);}public void SetTriangles(int[] a,int start,int length,int submesh,bool bounds,int baseVertex){IndexUploads++;Indices=new int[length];Array.Copy(a,start,Indices,0,length);}}
    public static class GL {public static int Depth;public static void PushMatrix(){Depth++;}public static void PopMatrix(){Depth--;}public static void LoadPixelMatrix(float l,float r,float b,float t){}}
    public static partial class Graphics {public static int Draws;public static Material BoundMaterial;public static bool FailDraw;public static void DrawMeshNow(Mesh mesh,Matrix4x4 matrix){if(FailDraw)throw new Exception("GPU draw failure");Draws++;if(!GUI.Record)return;var a=mesh.Vertices;for(int i=0;i<a.Length;i+=4){var first=a[i];var next=a[i+1];var last=a[i+3];float dx=next.x-first.x,dy=next.y-first.y,width=(float)Math.Sqrt(dx*dx+dy*dy),hx=last.x-first.x,hy=last.y-first.y,height=(float)Math.Sqrt(hx*hx+hy*hy);bool axis=dy==0&&hx==0;if(width<.00001f||height<.00001f)continue;GUI.Calls.Add(new Draw {MeshQuad=true,Rect=axis?new Rect(first.x,first.y,width,height):new Rect(0,0,width,height),Color=mesh.Colors[i],Texture=BoundMaterial!=null&&BoundMaterial.BoundTexture!=null?BoundMaterial.BoundTexture:Texture2D.TestWhite,Uv=mesh.Uvs==null?new Rect(0,0,1,1):new Rect(mesh.Uvs[i].x,mesh.Uvs[i+3].y,mesh.Uvs[i+1].x-mesh.Uvs[i].x,mesh.Uvs[i].y-mesh.Uvs[i+3].y),Matrix=axis?Matrix4x4.identity:Matrix4x4.TRS(first,Quaternion.Euler(0,0,(float)(Math.Atan2(dy,dx)*180/Math.PI)),new Vector3(1,1,1))});}}}
    public class Texture:Object {public FilterMode filterMode {get;set;}public TextureWrapMode wrapMode {get;set;}}public class Texture2D:Texture {public static int Created,Uploaded,WhiteReads;public static bool FailUpload;public static Texture2D LastCreated;static Texture2D White=new Texture2D();public int Width,Height;public Color[] Pixels;public bool Released;public Texture2D(){}public Texture2D(int w,int h,TextureFormat f,bool mip){Width=w;Height=h;Created++;LastCreated=this;}public void SetPixels(Color[] p){if(FailUpload)throw new Exception("upload failure");Pixels=(Color[])p.Clone();Uploaded++;}public void Apply(bool mip,bool release){Released=release;}public static Texture2D TestWhite {get{return White;}}public static Texture2D whiteTexture {get{WhiteReads++;return White;}}}
    public static class ImageConversion
    {
        public static bool LoadImage(Texture2D texture,byte[] bytes,bool release)
        {
            if(Texture2D.FailUpload)throw new Exception("image upload failure");
            try{using(var stream=new System.IO.MemoryStream(bytes))using(var bitmap=new System.Drawing.Bitmap(stream)){
                texture.Width=bitmap.Width;texture.Height=bitmap.Height;texture.Pixels=new Color[bitmap.Width*bitmap.Height];
                for(int y=0;y<bitmap.Height;y++)for(int x=0;x<bitmap.Width;x++){var c=bitmap.GetPixel(x,y);texture.Pixels[(bitmap.Height-1-y)*bitmap.Width+x]=new Color(c.R/255f,c.G/255f,c.B/255f,c.A/255f);}
                texture.Released=release;Texture2D.Uploaded++;return true;
            }}catch(ArgumentException){return false;}
        }
    }
    public enum FocusType {Passive=2}
    public class GUIUtility {public static int hotControl {get;set;}public static int GetControlID(int hint,FocusType focus){return hint;}}
    public enum FontStyle {Normal=0,Bold=1}
    public enum EventType {MouseDown=0,MouseUp=1,MouseMove=2,MouseDrag=3,Repaint=7,Layout=8,Ignore=11,Used=12,MouseLeaveWindow=21,TouchDown=30,TouchUp=31,TouchMove=32}public enum ScaleMode {StretchToFill=0}
    public class Event {public static Event current {get;set;}public EventType type {get;set;}public Vector2 mousePosition {get;set;}public void Use(){type=EventType.Used;}}
    public enum TextAnchor {UpperLeft=0,MiddleLeft=3,MiddleCenter=4}
    public class GUIStyleState {public Color textColor {get;set;}}
    public sealed class Font {public static int Loads;public string Name;public static Font CreateDynamicFontFromOSFont(string[] names,int size){Loads++;return new Font {Name=names[0]};}}
    public class GUISkin {public Font font {get;set;}}
    public class GUIStyle {public static Font DefaultFont;public static int FontWrites;public static void SetDefaultFont(Font f){FontWrites++;DefaultFont=f;}public Font font {get;set;}public FontStyle fontStyle {get;set;}public GUIStyle(){normal=new GUIStyleState();}public int fontSize {get;set;}public bool wordWrap {get;set;}public TextAnchor alignment {get;set;}public GUIStyleState normal {get;private set;}}
    public class Draw {public bool MeshQuad;public Texture Texture;public Rect Rect,Uv;public Matrix4x4 Matrix;public Color Color;public float Radius;public string Text;public int FontSize;public string FontName;public FontStyle FontWeight;public TextAnchor Align;public bool Wrap;}
    public static class GUI
    {
        public static bool Record=true;
        static GUISkin Skin=new GUISkin {font=new Font {Name="game-original"}};public static GUISkin skin {get{return Skin;}set{Skin=value;}}
        static Matrix4x4 Matrix;public static int MatrixWrites,NativeTextures;public static Matrix4x4 matrix {get{return Matrix;}set{Matrix=value;MatrixWrites++;}}public static Color color {get;set;}public static Color contentColor {get;set;}public static List<Draw> Calls=new List<Draw>();
        static string DefaultFontName(){return GUIStyle.DefaultFont!=null?GUIStyle.DefaultFont.Name:null;}
        public static void Label(Rect r,string s){if(!Record)return;Calls.Add(new Draw {Rect=r,Matrix=matrix,Color=color,Text=s});}
        public static void Label(Rect r,string s,GUIStyle style){if(!Record)return;Color c=style.normal.textColor;Calls.Add(new Draw {Rect=r,Matrix=matrix,Color=new Color(color.r*contentColor.r*c.r,color.g*contentColor.g*c.g,color.b*contentColor.b*c.b,color.a*contentColor.a*c.a),Text=s,FontSize=style.fontSize,FontName=style.font!=null?style.font.Name:DefaultFontName(),FontWeight=style.fontStyle,Align=style.alignment,Wrap=style.wordWrap});}
        public static void DrawTexture(Rect r,Texture t){if(!Record)return;NativeTextures++;Calls.Add(new Draw {Rect=r,Matrix=matrix,Color=color,Texture=t});}
        public static void DrawTexture(Rect r,Texture t,ScaleMode m,bool a,float aspect,Color c,float b,float radius){if(!Record)return;NativeTextures++;Calls.Add(new Draw {Rect=r,Matrix=matrix,Color=c,Radius=radius,Texture=t});}
    }
}
namespace UnityEngine.Networking
{
    public class DownloadHandler {public string text {get;set;}}
    public class UnityWebRequest:IDisposable
    {
        public static List<UnityWebRequest> Requests=new List<UnityWebRequest>();public string Url,Method,Body;public string method {get{return Method;}set{Method=value;}}public string url {get{return Url;}}public bool Disposed;public Dictionary<string,string> Headers=new Dictionary<string,string>();public string Date;
        public static UnityWebRequest Get(string u){return new UnityWebRequest {Url=u,Method="GET"};}public static UnityWebRequest Put(string u,byte[] b){return new UnityWebRequest {Url=u,Method="PUT",Body=System.Text.Encoding.UTF8.GetString(b)};}
        public UnityWebRequestAsyncOperation SendWebRequest(){Requests.Add(this);return new UnityWebRequestAsyncOperation();}public void SetRequestHeader(string k,string v){Headers[k]=v;}public string GetResponseHeader(string k){return k=="Date"?Date:null;}
        public int timeout {get;set;}public bool isDone {get;set;}public long responseCode {get;set;}public string error {get;set;}public DownloadHandler downloadHandler {get;set;}public void Dispose(){Disposed=true;}
    }
    public class UnityWebRequestAsyncOperation {}
}
namespace SimpleJSON
{
    public enum JSONNodeType {Array=1,Object=2,String=3,Number=4,NullValue=5,Boolean=6,None=7,Custom=255}
    public class JSONNode
    {
        public object ValueObject;public bool Lazy;public Dictionary<string,object> Parent;public string ParentKey;public JSONNode(object v){ValueObject=v;}public JSONNode this[string key] {get{var d=ValueObject as Dictionary<string,object>;return d!=null&&d.ContainsKey(key)?new JSONNode(d[key]){Parent=d,ParentKey=key}:new JSONNode(null){Lazy=true,Parent=d,ParentKey=key};}}
        public bool IsObject {get{return ValueObject is Dictionary<string,object>;}}public JSONNodeType Tag {get{return Lazy?JSONNodeType.None:IsNull?JSONNodeType.NullValue:IsObject?JSONNodeType.Object:IsBoolean?JSONNodeType.Boolean:IsNumber?JSONNodeType.Number:IsString?JSONNodeType.String:JSONNodeType.Array;}}
        public bool IsNull {get{return !Lazy&&ValueObject==null;}}public bool IsBoolean {get{return ValueObject is bool;}}public bool IsNumber {get{return ValueObject is int||ValueObject is long||ValueObject is double||ValueObject is decimal;}}public bool IsString {get{return ValueObject is string;}}
        public bool AsBool {get{return Convert.ToBoolean(ValueObject);}set{ValueObject=value;Lazy=false;if(Parent!=null)Parent[ParentKey]=value;}}public double AsDouble {get{return Convert.ToDouble(ValueObject);}}public int AsInt {get{return Convert.ToInt32(ValueObject);}}public string Value {get{return Convert.ToString(ValueObject,System.Globalization.CultureInfo.InvariantCulture);}}
        public override string ToString(){return new JavaScriptSerializer().Serialize(ValueObject);} public static JSONNode Parse(string json){return new JSONNode(new JavaScriptSerializer().DeserializeObject(json));}
    }
}
namespace GCommon
{
    public class SingletonModule<T> where T:class,new() {static T Value=new T();public static T instance {get{return Value;}}}
    public class UIModelManager:SingletonModule<UIModelManager> {public int Calls;public T GetModel<T>() where T:class,new(){Calls++;return new T();}}
}
namespace COW
{
    public class UIModelRole {}
    public static class GameFacade {public static UIModelRole m_modelRole;public static UIModelRole UIModelRole {get{return m_modelRole;}}public static GamePlay.JMAGGLCNGIG Match;public static GamePlay.Player Local;public static GamePlay.JMAGGLCNGIG CurrentMatch(){return Match;}public static GamePlay.Player GetLocalPlayerOrObServer(){return Local;}}
}
namespace COW.GamePlay
{
    public class SceneEditBoxSelectTool:UnityEngine.Component {public void OnGUI(){}}
    public class JMAGGLCNGIG {public IList Players=new ArrayList();public IList ILPNGCMEFFI(){return Players;}}
    public enum LKOOALMKJND {Default=0,Head=1,Body=2}
    public class CGKJLKPMGDJ {public UnityEngine.GameObject JHAINOBIFMA;public UnityEngine.Collider JINDNOFABKB;public UnityEngine.Vector3 HPCOJHGFEEG,DOJDNHCGFGK,HLDECMJFKJK,EKPLMDDKOGB,ONMKMEGMHOL;public LKOOALMKJND DKPDBLLJEAA;public short CLOLKHNMNEK;public int OLGEMBBDBID;public float CFIPDNFBKHK;public bool EFMBNPNFGHM,CEOKOILBODN;}
    public enum OfflineWeaponKind { Rifle=0,Sniper=1,HandGun=2,Knife=3,ShotGun=4,Grenade=5,SMG=6,CrossBow=8,ExternalMachineGun=10,HeavyWeapon=13,ChargeGun=12,NotReady=23,UGCSprayRifle=28 }
    public class CDMMCIIFHGM {public Player FDEPGLKBBKP {get;set;}}
    public class HBIBDMMOOOK:CDMMCIIFHGM
    {
        public OfflineWeaponKind Kind;public bool FailKind;public int KindReads;
        public virtual OfflineWeaponKind JBHDCDGJDPK(){KindReads++;if(FailKind)throw new Exception("weapon metadata not ready");return Kind;}
        public Player ReportedOwner;public bool OverrideReportedOwner,FailReportedOwner;public int OwnerReads;public Player Owner {get{return FDEPGLKBBKP;}set{FDEPGLKBBKP=value;}}public float Interval=.1f;public int Starts,Stops;public Action Start,Stop;public bool FailStart;public bool Ready=true;public int Ammo=5000;public float EngineNext;
        public int NativeDamage=24,NativeDamageCalls,NativeHitCalls,NativeReturn=1,LastNativeDamage;public float LastNativeDistance,LastNativeFactor;public LKOOALMKJND LastNativeKind;public short LastNativeSpecial;public bool FailNativeDamage,FailNativeHit,NativeBehaviourNull;public Action NativeCalculated,NativeCalled;public UnityEngine.GameObject LastNativeTarget;
        public virtual int JEMMELJLKCN(float distance){NativeDamageCalls++;if(NativeCalculated!=null)NativeCalculated();if(FailNativeDamage)throw new Exception("native damage unavailable");return NativeDamage;}
        public virtual int CIHHEOKFADJ(CGKJLKPMGDJ info,UnityEngine.MonoBehaviour behaviour,float factor){NativeHitCalls++;LastNativeDamage=info.OLGEMBBDBID;LastNativeDistance=info.CFIPDNFBKHK;LastNativeKind=info.DKPDBLLJEAA;LastNativeSpecial=info.CLOLKHNMNEK;LastNativeTarget=info.JHAINOBIFMA;LastNativeFactor=factor;NativeBehaviourNull=behaviour==null;if(NativeCalled!=null)NativeCalled();if(FailNativeHit)throw new Exception("native hit failed");return NativeReturn;}
        public Player DHNEEGMCLNO(){OwnerReads++;if(FailReportedOwner)throw new Exception("owner proxy unavailable");return OverrideReportedOwner?ReportedOwner:Owner;}public float LDKCJEMDAMC(){return Interval;}
        public virtual void ALJMFCJMDCF(){Starts++;if(Start!=null)Start();if(FailStart)throw new Exception("fire rejected");}
        public virtual void NFFJEBKJCAK(){Stops++;if(Stop!=null)Stop();}
    }
    public class ForeignFireWeapon:HBIBDMMOOOK {public ForeignFireWeapon(){Kind=OfflineWeaponKind.Grenade;}public override void ALJMFCJMDCF(){throw new Exception("must not dispatch foreign override");}}
    public class OverrideGun:HBIBDMMOOOK {public override void ALJMFCJMDCF(){base.ALJMFCJMDCF();}public override void NFFJEBKJCAK(){base.NFFJEBKJCAK();}}
    public class OverloadedGun:OverrideGun {public void ALJMFCJMDCF(int mode){}public void NFFJEBKJCAK(bool manual){}}
    public class PlayerAttributes:UnityEngine.Component
    {
        private Player BDOEGBMMEOK;public bool ShootNoReload;public bool FailRateRead,FailRateWrite,FailAmmoWrite;public int RateWrites,AmmoWrites;private bool ammo;private float rate=1;
        public PlayerAttributes(Player owner){BDOEGBMMEOK=owner;gameObject=new UnityEngine.GameObject("attributes");}
        public bool IsAmmoFree {get{return ammo;}set{if(FailAmmoWrite)throw new Exception("ammo setter transient");ammo=value;AmmoWrites++;}}
        public float FireIntervalScale {get{if(FailRateRead)throw new Exception("rate getter transient");return rate;}set{if(FailRateWrite)throw new Exception("rate setter transient");rate=value;RateWrites++;}}
    }
    public class Player:UnityEngine.Component
    {
        public PlayerAttributes Attributes;public bool FailAttributes;public PlayerAttributes get_Attributes(){if(FailAttributes)throw new Exception("attributes unavailable");return Attributes;}
        public bool IsReallyInStealth(){return IsInStealth();}public CGKJLKPMGDJ LastAimingInfoFromWeapon {get{return FDMIEDDNCEC;}}
        public CGKJLKPMGDJ FDMIEDDNCEC=new CGKJLKPMGDJ();public bool IsClientBot;public bool Local,Team,Stealth,Firing,Downed,Movable=true;public bool IsDieing {get{return Downed;}}public HBIBDMMOOOK Weapon;public Action WeaponRead;public int WeaponReads;public HBIBDMMOOOK GetWeaponOnHand(){WeaponReads++;if(WeaponRead!=null)WeaponRead();return Weapon;}public bool Scoped,FailScope;public UnityEngine.Transform Visual;public bool get_IsSighting(){if(FailScope)throw new Exception("scope unavailable");return Scoped;}public UnityEngine.Transform get_BoneRootTransform(){return Visual;}public int HP=100,MaximumHP=200,StealthCalls,AimCalls;public string Nick="Enemy test";public UnityEngine.Vector3 Origin;public UnityEngine.Quaternion LastRotation;
        public bool IsInStealth(){StealthCalls++;return Stealth;}public bool IsLocalPlayer(){return Local;}public bool FailHP;public int CurHP {get{if(FailHP)throw new Exception("despawn getter");return HP;}}public int MaxHP {get{return MaximumHP;}}public bool FailNick;public string NickName {get{if(FailNick)throw new Exception("despawn after box");return Nick;}}public bool IsLocalTeammate(bool b){return Team;}
        public bool FailNeck;public UnityEngine.Transform Root,Head,Neck,ArmL,ArmR,HandL,HandR,LegL,LegR,FootL,FootR;public UnityEngine.Collider Collider,BodyCollider;public IList FireColliders;public int BodyListReads;public bool FailBodyList,FailChest;public IList get_FireColliders(){BodyListReads++;if(FailBodyList)throw new Exception("body list unavailable");return FireColliders;}public UnityEngine.Vector3 GetChestPos(bool useCamera){if(FailChest)throw new Exception("chest unavailable");return BodyCollider!=null?BodyCollider.bounds.center:Root.position+(Head.position-Root.position)*.6f;}
        public UnityEngine.Transform RootTransform {get{return Root;}}public UnityEngine.Transform GetHeadTF(){return Head;}public UnityEngine.Transform NeckBone {get{if(FailNeck)throw new Exception("despawn after hearts");return Neck;}}public UnityEngine.Transform ArmBoneLeft {get{return ArmL;}}public UnityEngine.Transform ArmBoneRight {get{return ArmR;}}public UnityEngine.Transform HandBoneLeft {get{return HandL;}}public UnityEngine.Transform HandBoneRight {get{return HandR;}}public UnityEngine.Transform LegBoneLeft {get{return LegL;}}public UnityEngine.Transform LegBoneRight {get{return LegR;}}public UnityEngine.Transform FootBoneLeft {get{return FootL;}}public UnityEngine.Transform FootBoneRight {get{return FootR;}}
        public Action FiringRead,RotationWrite;public int LocalReads;
        public UnityEngine.Collider HeadCollider {get{return Collider;}}public UnityEngine.Vector3 AimStartPostion {get{return Origin;}}public bool IsFiring(){if(FiringRead!=null)FiringRead();return Firing;}public void SetAimRotation(UnityEngine.Quaternion q,bool b){if(RotationWrite!=null)RotationWrite();LastRotation=q;AimCalls++;}
    }
}

public class VirtualButton {public bool Held;public int PressFrame,ReleaseFrame;}
namespace UnityStandardAssets.CrossPlatformInput
{
    public static class CrossPlatformInputManager
    {
        public static VirtualButton Button=new VirtualButton();public static bool Registered=true,FailDown;public static int Downs,Ups,Reads;public static Action OnDown;
        public static void Reset(){Button=new VirtualButton();Registered=true;FailDown=false;Downs=0;Ups=0;Reads=0;OnDown=null;}
        public static bool ButtonExists(string name){return Registered&&name=="Action1";}
        public static bool GetButton(string name){Reads++;return ButtonExists(name)&&Button.Held;}
        public static VirtualButton VirtualButtonReference(string name){return ButtonExists(name)?Button:null;}
        public static void SetButtonDown(string name){if(!ButtonExists(name))throw new Exception("missing input registration");if(FailDown)throw new Exception("input rejected");Downs++;Button.Held=true;Button.PressFrame=UnityEngine.Time.frameCount;if(OnDown!=null)OnDown();}
        public static void SetButtonUp(string name){if(!ButtonExists(name))return;Ups++;Button.Held=false;Button.ReleaseFrame=UnityEngine.Time.frameCount;}
        // Only the simulated native game update fires. The IFix callback itself
        // cannot increment shots or ammunition, matching the HUD -> input -> Update order.
        public static void GameUpdate(COW.GamePlay.Player local)
        {
            if(local==null)return;var weapon=local.Weapon;
            if(!GetButton("Action1")||local.Downed||local.HP<=0||weapon==null||!System.Object.ReferenceEquals(weapon.Owner,local)||!weapon.Ready||weapon.Ammo<=0){local.Firing=false;return;}
            local.Firing=true;if(UnityEngine.Time.realtimeSinceStartup>=weapon.EngineNext){weapon.EngineNext=UnityEngine.Time.realtimeSinceStartup+weapon.Interval;weapon.Ammo--;weapon.ALJMFCJMDCF();}
        }
    }
}
