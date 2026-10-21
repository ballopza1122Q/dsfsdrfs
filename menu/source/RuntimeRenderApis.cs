// Offline renderer doubles. Never included in the game patch build.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class MaterialPropertyBlock {public int Code;}
    public class Renderer:Component {public bool enabled {get;set;}public Material[] sharedMaterials {get;set;}public int PropertyCode;public void GetPropertyBlock(MaterialPropertyBlock p){p.Code=PropertyCode;}}
    public class SkinnedMeshRenderer:Renderer
    {
        public bool updateWhenOffscreen {get;set;}public bool forceMatrixRecalculationPerRender {get;set;}
        public Mesh sharedMesh {get;set;}public int PoseRevision,Bakes;public bool FailBake;
        public void BakeMesh(Mesh mesh){if(FailBake)throw new Exception("skin bake failed");Bakes++;mesh.TestVertexCount=sharedMesh.vertexCount;mesh.TestSubMeshCount=sharedMesh.subMeshCount;mesh.BakedPose=PoseRevision;}
    }
    public class MeshRenderer:Renderer {}
    public class MeshFilter:Component {public Mesh sharedMesh {get;set;}}
    public class WorldDraw {public Mesh Mesh;public Material Material;public Matrix4x4 Matrix;public Camera Camera;public MaterialPropertyBlock Properties;public int Submesh;}
    public static partial class Graphics
    {
        public static List<WorldDraw> WorldDraws=new List<WorldDraw>();public static int WorldSubmissions,FailWorldAt=-1;public static bool RecordWorld=true;public static Action BeforeWorldDraw;
        public static void DrawMesh(Mesh mesh,Matrix4x4 matrix,Material material,int layer,Camera camera,int submesh,MaterialPropertyBlock properties)
        {
            if(BeforeWorldDraw!=null)BeforeWorldDraw();WorldSubmissions++;if(FailWorldAt==WorldSubmissions)throw new Exception("world submission failed");
            if(RecordWorld)WorldDraws.Add(new WorldDraw {Mesh=mesh,Matrix=matrix,Material=material,Camera=camera,Submesh=submesh,Properties=properties});
        }
    }
}
