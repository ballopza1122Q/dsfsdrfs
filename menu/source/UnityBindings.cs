using System;
namespace UnityEngine.Profiling {public sealed class Profiler {public static long GetMonoUsedSizeLong(){return 0;}}}
namespace UnityEngine
{
    public class Object {public string name {get;set;}public static bool operator ==(Object a,Object b){return ReferenceEquals(a,b);}public static bool operator !=(Object a,Object b){return !ReferenceEquals(a,b);}public override bool Equals(object o){return ReferenceEquals(this,o);}public override int GetHashCode(){return base.GetHashCode();}public static void DontDestroyOnLoad(Object o){}public static void Destroy(Object o){} }
    public class Component:Object {public GameObject gameObject {get;set;}public Transform transform {get{return gameObject.transform;}}}
    public class GameObject:Object {public GameObject(string n){name=n;}public Transform transform {get{return null;}}public int layer {get{return 0;}}public Component[] GetComponentsInChildren(Type type,bool includeInactive){return null;}public bool activeInHierarchy {get{return true;}}public static GameObject Find(string n){return null;}public Component GetComponent(Type t){return null;}public Component AddComponent(Type t){return null;}}
    public class Transform:Component {public Quaternion rotation {get;set;}public Vector3 position {get;set;}public Vector3 localPosition {get;set;}public Quaternion localRotation {get;set;}public Transform parent {get{return null;}}public int childCount {get{return 0;}}public Transform GetChild(int index){return null;}public Matrix4x4 localToWorldMatrix {get{return new Matrix4x4();}}public Vector3 localScale {get;set;}public bool IsChildOf(Transform parent){return false;}}
    public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3();}public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3();}public static Vector3 operator *(Vector3 a,float b){return new Vector3();}public static Vector3 Normalize(Vector3 v){return v;}public static float Distance(Vector3 a,Vector3 b){return 0;}}
    public struct Vector2 {public float x,y;public Vector2(float a,float b){x=a;y=b;}}
    public struct Bounds {public Vector3 center {get{return new Vector3();}}}
    public struct Color {public float r,g,b,a;public Color(float x,float y,float z,float w){r=x;g=y;b=z;a=w;}}
    public struct Quaternion {public float x,y,z,w;public static Quaternion Euler(float a,float b,float c){return new Quaternion();}public static Quaternion LookRotation(Vector3 d){return new Quaternion();}}
    public struct Matrix4x4 {public static Matrix4x4 identity {get{return new Matrix4x4();}}public static Matrix4x4 TRS(Vector3 p,Quaternion q,Vector3 s){return new Matrix4x4();}}
    public struct Rect {public float x {get;set;}public float y {get;set;}public float width {get;set;}public float height {get;set;}public Rect(float a,float b,float c,float d){this=new Rect();x=a;y=b;width=c;height=d;}public bool Contains(Vector2 p){return p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height;}}
    public class Camera:Component {public delegate void CameraCallback(Camera camera);public static CameraCallback onPreCull,onPostRender;public static Camera main {get;set;}public float fieldOfView {get;set;}public Vector3 WorldToScreenPoint(Vector3 p){return p;}}
    public static class Screen {public static int height {get{return 720;}}public static int width {get{return 1280;}}}
    public static class Time {public static int frameCount {get{return 0;}}public static float unscaledDeltaTime {get{return .016f;}}public static float realtimeSinceStartup {get{return 0;}}}
    public static class Mathf {public static float Sqrt(float f){return (float)Math.Sqrt(f);}public static float Sin(float f){return (float)Math.Sin(f);}public static float Cos(float f){return (float)Math.Cos(f);}public static float Atan2(float y,float x){return (float)Math.Atan2(y,x);}public static float Clamp(float v,float a,float b){return Math.Max(a,Math.Min(b,v));}public static float Lerp(float a,float b,float t){return a+(b-a)*Math.Min(1,Math.Max(0,t));}}
    public static class PlayerPrefs {public static string GetString(string k,string d){return d;}public static void SetString(string k,string v){}public static void DeleteKey(string k){} }
    public static class SystemInfo {public static string deviceUniqueIdentifier {get{return "test-device";}}}
    public static class Application {public static string persistentDataPath {get{return null;}}}
    public enum TouchScreenKeyboardType {Default=0,ASCIICapable=1}
    public class TouchScreenKeyboard {public enum Status {Visible=0,Done=1,Canceled=2,LostFocus=3}public static TouchScreenKeyboard Open(string text,TouchScreenKeyboardType t,bool autocorrect,bool multiline,bool secure,bool alert,string placeholder,int limit){return null;}public string text {get;set;}public Status status {get;set;}}
    public class Shader:Object {public static Shader Find(string name){return null;}}
    public class Material:Object {public Material(Shader shader){}public void SetTexture(string name,Texture value){}public void SetInt(string name,int value){}public bool SetPass(int pass){return true;}}
    public class Mesh:Object {public int vertexCount {get{return 0;}}public int subMeshCount {get{return 0;}}public Mesh(){}public void MarkDynamic(){}public void Clear(){}public void SetVertices(Vector3[] a,int start,int length){}public void SetUVs(int channel,Vector2[] a,int start,int length){}public void SetColors(Color[] a,int start,int length){}public void SetTriangles(int[] a,int start,int length,int submesh,bool bounds,int baseVertex){}}
    public static class GL {public static void PushMatrix(){}public static void PopMatrix(){}public static void LoadPixelMatrix(float left,float right,float bottom,float top){}}
    public class Renderer:Component {public bool enabled {get;set;}public Material[] sharedMaterials {get;set;}public void GetPropertyBlock(MaterialPropertyBlock properties){}}
    public class SkinnedMeshRenderer:Renderer {public bool updateWhenOffscreen {get;set;}public bool forceMatrixRecalculationPerRender {get;set;}public Mesh sharedMesh {get;set;}public void BakeMesh(Mesh mesh){}}
    public class MeshRenderer:Renderer {}
    public class MeshFilter:Component {public Mesh sharedMesh {get;set;}}
    public class MaterialPropertyBlock {public MaterialPropertyBlock(){}}
    public static class Graphics {public static void DrawMesh(Mesh mesh,Matrix4x4 matrix,Material material,int layer,Camera camera,int submesh,MaterialPropertyBlock properties){}public static void DrawMeshNow(Mesh mesh,Matrix4x4 matrix){}}
    public class Texture:Object {public FilterMode filterMode {set{}}public TextureWrapMode wrapMode {set{}}}
    public enum TextureFormat {RGBA32=4}
    public enum FilterMode {Point=0,Bilinear=1}
    public enum TextureWrapMode {Clamp=1}
    public class Texture2D:Texture {public Texture2D(int w,int h,TextureFormat f,bool mip){}public void SetPixels(Color[] p){}public void Apply(bool mip,bool release){}public static Texture2D whiteTexture {get{return null;}}}
}
