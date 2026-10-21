using UnityEngine;
namespace UnityEngine
{
    public class Collider:Component {public bool enabled {get;set;}public Bounds bounds {get{return new Bounds();}}}
    public enum QueryTriggerInteraction {UseGlobal=0,Ignore=1,Collide=2}
    public struct RaycastHit {public Collider collider {get {return null;}}public float distance {get {return 0;}}}
    public static class Physics
    {
        public static bool Raycast(Vector3 origin,Vector3 direction,out RaycastHit hit,float distance,int mask,QueryTriggerInteraction triggers){hit=new RaycastHit();return false;}
        public static int RaycastNonAlloc(Vector3 origin,Vector3 direction,RaycastHit[] results,float distance,int mask,QueryTriggerInteraction triggers){return 0;}
    }
}
