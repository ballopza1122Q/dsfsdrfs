using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static MethodInfo MzFireCollidersGetter;
        public static float MzBodyBindAfter;
        public static Player MzBodyTarget;
        public static Collider MzBodyCollider;
        public static Vector3 MzBodyPoint;
        public static int MzBodyFrame;
        public static bool MzSilentWasHead;
        // The verified native fire-collider list supplies the body hit collider.
        // Resolve lazily so unsupported body metadata cannot prevent menu load.
        public static bool MzBodyHit(Player target,out Collider collider,out Vector3 point)
        {
            collider=null;point=new Vector3();int frame=Time.frameCount;
            if(MzLagPlayers!=null)for(int i=0;i<16;i++)if(MzLagPlayers[i]==target&&MzLagPoseActive[i]){if(MzLagLiveBodyFrames[i]!=frame||MzLagLiveBodyColliders[i]==null)return false;collider=MzLagLiveBodyColliders[i];point=MzLagLiveBodyPoints[i];return true;}
            if(target==MzBodyTarget&&MzBodyFrame==frame){collider=MzBodyCollider;point=MzBodyPoint;return collider!=null&&collider.enabled;}
            MzBodyTarget=target;MzBodyCollider=null;MzBodyFrame=frame;
            try{
                if(target==null)return false;var getter=MzFireCollidersGetter;
                if(getter==null){float now=Time.realtimeSinceStartup;if(now<MzBodyBindAfter)return false;MzBodyBindAfter=now+2;getter=typeof(Player).GetMethod("get_FireColliders");if(getter==null)return false;MzFireCollidersGetter=getter;}
                var list=getter.Invoke(target,null) as IList;if(list==null||list.Count==0||list.Count>64)return false;
                var root=target.RootTransform;var obj=target.gameObject;var head=target.HeadCollider;if(root==null||obj==null)return false;
                Vector3 chest=target.GetChestPos(false);if(!MzPoint(chest))return false;
                float best=Single.MaxValue;
                for(int i=0;i<list.Count;i++){
                    var hit=list[i] as Collider;if(hit==null||hit==head||!hit.enabled)continue;var transform=hit.transform;
                    if(transform==null||(!transform.IsChildOf(root)&&!transform.IsChildOf(obj.transform)))continue;
                    Vector3 center=hit.bounds.center;if(!MzPoint(center))continue;Vector3 delta=center-chest;float d=delta.x*delta.x+delta.y*delta.y+delta.z*delta.z;
                    if(MzFinite(d)&&d<best){best=d;collider=hit;point=center;}
                }
                if(collider==null)return false;MzBodyTarget=target;MzBodyCollider=collider;MzBodyPoint=point;MzBodyFrame=frame;return true;
            }catch(Exception){collider=null;return false;}
        }
    }
}
