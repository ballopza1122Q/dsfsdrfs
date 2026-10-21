using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        // World-space values only: no actor/Transform is retained by the ESP cache.
        public static Vector3[] MzEspWorld;
        public static bool[] MzEspWorldValid;
        public static int[] MzEspHp,MzEspMaxHp;
        public static bool[] MzEspBots;
        public static string[] MzEspNames,MzEspDistances;
        public static int MzEspCacheCount;
        public static bool MzEspGeometryValid;
        public static Vector3 MzEspViewPosition;
        public static Quaternion MzEspViewRotation;
        public static float MzEspViewFov;
        public static void MzEspCacheInit()
        {
            if(MzEspWorld!=null)return;MzEspWorld=new Vector3[176];MzEspWorldValid=new bool[176];MzEspHp=new int[16];MzEspMaxHp=new int[16];MzEspNames=new string[16];MzEspDistances=new string[16];MzEspBots=new bool[16];
        }
        public static void MzEspCacheClear()
        {
            MzEspCacheCount=0;MzEspGeometryValid=false;MzCountValid=false;if(MzEspNames!=null)for(int i=0;i<16;i++){MzEspNames[i]=null;MzEspDistances[i]=null;}
        }
        public static void MzEspCaptureBone(Player player,int actor,int bone)
        {
            int index=actor*11+bone;MzEspWorldValid[index]=false;
            if(MzLagEspSlot>=0){int held=MzLagEspSlot*11+bone;if(!MzLagBoneValid[held])return;MzEspWorld[index]=MzLagBones[held];}
            else {var transform=MzLagBone(player,bone);if(transform==null)return;MzEspWorld[index]=transform.position;}
            MzEspWorldValid[index]=MzPoint(MzEspWorld[index]);
        }
        public static void MzEspCacheActor(Player player,int actor,Vector3 root,Vector3 head,int hp,float distance)
        {
            int start=actor*11;for(int b=0;b<11;b++)MzEspWorldValid[start+b]=false;
            MzEspWorld[start]=head;MzEspWorld[start+1]=root;MzEspWorldValid[start]=true;MzEspWorldValid[start+1]=true;
            if(MzSwitch[7])for(int b=2;b<11;b++)MzEspCaptureBone(player,actor,b);
            MzEspHp[actor]=hp;MzEspMaxHp[actor]=MzSwitch[5]?player.MaxHP:0;MzEspBots[actor]=player.IsClientBot;
            MzEspNames[actor]=MzSwitch[4]?MzCachedName(player.NickName,actor):null;MzEspDistances[actor]=MzSwitch[6]?MzDistanceLabel(distance):null;
        }
        public static void MzEspCachedSkeleton(Camera camera,int actor,Vector2 head,Vector2 root)
        {
            MzProjectionScratch();MzProjectedX[0]=head.x;MzProjectedY[0]=head.y;MzProjectedX[1]=root.x;MzProjectedY[1]=root.y;MzProjectedValid[0]=true;MzProjectedValid[1]=true;
            for(int b=2;b<11;b++){int k=actor*11+b;MzProjectedValid[b]=false;if(MzEspWorldValid[k])MzProjectSlot(camera,b,MzEspWorld[k]);}
            MzProjectedEdge(0,2);MzProjectedEdge(2,1);MzProjectedEdge(2,3);MzProjectedEdge(3,4);MzProjectedEdge(2,5);MzProjectedEdge(5,6);MzProjectedEdge(1,7);MzProjectedEdge(7,8);MzProjectedEdge(1,9);MzProjectedEdge(9,10);
        }
        public static void MzEspDrawCache(Camera camera)
        {
            var transform=camera.transform;Vector3 position=transform.position;Quaternion rotation=transform.rotation;float fov=camera.fieldOfView;
            if(MzEspGeometryValid&&MzSame(position,MzEspViewPosition)&&MzPoseSame(rotation,MzEspViewRotation)&&fov==MzEspViewFov)return;
            MzClearCommands();MzBuilding=true;MzDrawingEsp=true;MzDrawBudget=512;int people=0,bots=0;
            try{
                for(int actor=0;actor<MzEspCacheCount&&MzCommandCount+40<=512;actor++){
                    int before=MzCommandCount,quads=MzQuadCount,hearts=MzHeartCount;
                    try{
                        int start=actor*11;Vector3 head=MzEspWorld[start],root=MzEspWorld[start+1];Vector2 top,feet;
                        if(!MzProject(camera,root,out feet)||!MzProject(camera,head,out top))continue;
                        float h=feet.y-top.y,w=h*.5f,x=(feet.x+top.x)*.5f-w*.5f,y=top.y;
                        if(!MzFinite(h)||h<=4||h>Screen.height*2||x+w<0||x>Screen.width||y+h<0||y>Screen.height)continue;
                        MzBatchColor=MzEspColor;if(MzSwitch[1]){if(MzSwitch[3])MzBox3D(camera,root,head);else MzBox(x,y,w,h);}
                        if(MzEspNames[actor]!=null&&MzSwitch[4])MzText(top.x-80,y-(MzSwitch[5]?41:23),160,MzEspNames[actor]);
                        if(MzEspDistances[actor]!=null&&MzSwitch[6])MzText(feet.x-40,y+h+1,80,MzEspDistances[actor]);
                        if(MzSwitch[5])MzHeartRow(top.x,top.y,h,MzEspHp[actor],MzEspMaxHp[actor]);
                        MzBatchColor=MzEspColor;if(MzSwitch[8])MzCircle(top.x,top.y,Mathf.Clamp(h*.025f,3,8),1.5f);
                        if(MzSwitch[10])MzLine(Screen.width*.5f,MzLineOrigin==0?Screen.height-8:MzLineOrigin==1?Screen.height*.5f:8,feet.x,feet.y,1.25f);
                        if(MzSwitch[9]){MzTint(1,.3f,.3f,1);MzWorldLine(camera,root,root+new Vector3(.65f,0,0),1.5f);MzTint(.3f,1,.3f,1);MzWorldLine(camera,root,root+new Vector3(0,.65f,0),1.5f);MzTint(.3f,.5f,1,1);MzWorldLine(camera,root,root+new Vector3(0,0,.65f),1.5f);}
                        if(MzSwitch[7]){MzBatchColor=MzEspColor;MzEspCachedSkeleton(camera,actor,top,feet);}
                        if(MzCommandCount>before||MzQuadCount>quads||MzHeartCount>hearts){if(MzEspBots[actor])bots++;else people++;}
                    }catch(Exception){MzTrimCommands(before);MzQuadCount=quads;MzHeartCount=hearts;MzHeartDirty=true;MzBatchDirty=true;}
                }
                MzCommitCounts(people,bots);MzEspViewPosition=position;MzEspViewRotation=rotation;MzEspViewFov=fov;MzEspGeometryValid=true;
            }finally{MzBuilding=false;MzDrawingEsp=false;}
        }
    }
}
