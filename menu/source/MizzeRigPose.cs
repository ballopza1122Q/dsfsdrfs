using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static Transform[] MzLagNodes,MzLagParents;
        public static Vector3[] MzLagHeldPos,MzLagRealPos,MzLagAppliedPos,MzLagLiveHeads;
        public static Quaternion[] MzLagHeldRot,MzLagRealRot,MzLagAppliedRot,MzLagWorldRot;
        public static Vector3[] MzLagWorldPos;
        public static bool[] MzLagApplied,MzLagPositionOwned,MzLagRotationOwned,MzLagPoseActive;
        public static SkinnedMeshRenderer[] MzLagSkins;
        public static bool[] MzLagOffscreen,MzLagRecalc;
        public static int[] MzLagSkinCount;
        public static float MzLagCaptureAfter;
        public static Collider[] MzLagLiveBodyColliders;
        public static Vector3[] MzLagLiveBodyPoints;
        public static int[] MzLagLiveBodyFrames;
        public static bool MzPoseValid(Quaternion q){float n=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;return MzFinite(n)&&n>.5f&&n<1.5f;}
        public static void MzRigBuffers()
        {
            MzLagNodes=new Transform[2048];MzLagParents=new Transform[2048];MzLagHeldPos=new Vector3[2048];MzLagRealPos=new Vector3[2048];MzLagAppliedPos=new Vector3[2048];
            MzLagHeldRot=new Quaternion[2048];MzLagRealRot=new Quaternion[2048];MzLagAppliedRot=new Quaternion[2048];MzLagApplied=new bool[2048];MzLagPositionOwned=new bool[2048];MzLagRotationOwned=new bool[2048];MzLagPoseActive=new bool[16];
            MzLagWorldPos=new Vector3[16];MzLagWorldRot=new Quaternion[16];MzLagLiveHeads=new Vector3[16];MzLagSkins=new SkinnedMeshRenderer[256];MzLagOffscreen=new bool[256];MzLagRecalc=new bool[256];MzLagSkinCount=new int[16];
            MzLagLiveBodyColliders=new Collider[16];MzLagLiveBodyPoints=new Vector3[16];MzLagLiveBodyFrames=new int[16];
        }
        public static void MzRigRestore(int slot)
        {
            int start=slot*128,count=MzLagParts[slot];for(int n=0;n<count;n++){
                int k=start+n;if(!MzLagApplied[k])continue;MzLagApplied[k]=false;
                try{var node=MzLagNodes[k];if(node!=null&&node.parent==MzLagParents[k]){if(MzLagPositionOwned[k]&&MzSame(node.localPosition,MzLagAppliedPos[k]))node.localPosition=MzLagRealPos[k];if(MzLagRotationOwned[k]&&MzPoseSame(node.localRotation,MzLagAppliedRot[k]))node.localRotation=MzLagRealRot[k];}}catch(Exception){}
                MzLagPositionOwned[k]=false;MzLagRotationOwned[k]=false;
            }
            MzLagPoseActive[slot]=false;
        }
        public static void MzRigDrop(int slot)
        {
            MzRigRestore(slot);for(int n=0;n<128;n++){int k=slot*128+n;MzLagNodes[k]=null;MzLagParents[k]=null;}
            for(int n=0;n<MzLagSkinCount[slot];n++){int k=slot*16+n;try{var skin=MzLagSkins[k];if(skin!=null){if(!MzLagOffscreen[k]&&skin.updateWhenOffscreen)skin.updateWhenOffscreen=false;if(!MzLagRecalc[k]&&skin.forceMatrixRecalculationPerRender)skin.forceMatrixRecalculationPerRender=false;}}catch(Exception){}MzLagSkins[k]=null;}
            MzLagSkinCount[slot]=0;MzLagLiveBodyColliders[slot]=null;MzLagLiveBodyFrames[slot]=-1;
        }
        public static bool MzRigCapture(Player p,int slot)
        {
            try{
                var physical=p.RootTransform;var root=p.get_BoneRootTransform();if(root==null||root==physical||!root.IsChildOf(physical))return false;
                MzLagPlayers[slot]=p;MzLagObjects[slot]=p.gameObject;MzLagVisuals[slot]=root;int start=slot*128,count=1;MzLagNodes[start]=root;
                for(int n=0;n<count;n++){
                    int k=start+n;var node=MzLagNodes[k];if(node==null)throw new Exception("rig node missing");var pos=node.localPosition;var rot=node.localRotation;
                    if(!MzPoint(pos)||!MzPoseValid(rot))throw new Exception("invalid rig pose");MzLagParents[k]=node.parent;MzLagHeldPos[k]=pos;MzLagHeldRot[k]=rot;
                    int children=node.childCount;if(children>128)throw new Exception("rig too large");for(int c=0;c<children;c++){
                        var child=node.GetChild(c);if(child==null||child.parent!=node||count>=128)throw new Exception("rig changed or too large");
                        for(int j=0;j<count;j++)if(MzLagNodes[start+j]==child)throw new Exception("rig cycle");MzLagNodes[start+count++]=child;
                    }
                }
                var world=root.position;var rotation=root.rotation;if(!MzPoint(world)||!MzPoseValid(rotation))throw new Exception("invalid rig anchor");
                MzLagParts[slot]=count;MzLagWorldPos[slot]=world;MzLagWorldRot[slot]=rotation;MzLagAnchors[slot]=physical.position;MzLagHeads[slot]=p.GetHeadTF().position;
                for(int b=0;b<11;b++){var bone=MzLagBone(p,b);int k=slot*11+b;MzLagBoneValid[k]=bone!=null;if(bone!=null)MzLagBones[k]=bone.position;}
                var skins=p.gameObject.GetComponentsInChildren(typeof(SkinnedMeshRenderer),false);if(skins==null||skins.Length>16)throw new Exception("skin cap");
                for(int n=0;n<skins.Length;n++){var skin=skins[n] as SkinnedMeshRenderer;if(skin==null)continue;int k=slot*16+MzLagSkinCount[slot]++;MzLagSkins[k]=skin;MzLagOffscreen[k]=skin.updateWhenOffscreen;MzLagRecalc[k]=skin.forceMatrixRecalculationPerRender;skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;}
                return true;
            }catch(Exception){MzLagDrop(slot);MzLagCaptureAfter=Time.realtimeSinceStartup+.25f;return false;}
        }
        public static bool MzRigApply(int slot)
        {
            try{
                MzLagDrawnFrame[slot]=-1;int count=MzLagParts[slot],start=slot*128;
                // Validate the complete hierarchy before writing any joint. Keep
                // original renderers/materials/scales; never draw a baked duplicate.
                for(int n=0;n<count;n++){int k=start+n;var node=MzLagNodes[k];if(node==null||node.parent!=MzLagParents[k])throw new Exception("rig replaced");}
                var player=MzLagPlayers[slot];MzLagLiveHeads[slot]=player.GetHeadTF().position;
                if(MzHeadChance<100&&(MzSwitch[12]||MzSwitch[15])){Collider body;Vector3 bodyPoint;if(MzBodyHit(player,out body,out bodyPoint)){MzLagLiveBodyColliders[slot]=body;MzLagLiveBodyPoints[slot]=bodyPoint;MzLagLiveBodyFrames[slot]=Time.frameCount;}else {MzLagLiveBodyColliders[slot]=null;MzLagLiveBodyFrames[slot]=-1;}}
                MzLagPoseActive[slot]=true;
                for(int n=0;n<count;n++){
                    int k=start+n;var node=MzLagNodes[k];var pos=node.localPosition;var rot=node.localRotation;if(!MzPoint(pos)||!MzPoseValid(rot))throw new Exception("rig pose unavailable");
                    MzLagRealPos[k]=pos;MzLagRealRot[k]=rot;
                    if(n==0){
                        if(!MzSame(node.position,MzLagWorldPos[slot])){MzLagApplied[k]=true;MzLagPositionOwned[k]=true;node.position=MzLagWorldPos[slot];MzLagAppliedPos[k]=node.localPosition;}
                        if(!MzPoseSame(node.rotation,MzLagWorldRot[slot])){MzLagApplied[k]=true;MzLagRotationOwned[k]=true;node.rotation=MzLagWorldRot[slot];MzLagAppliedRot[k]=node.localRotation;}
                    }else {
                        if(!MzSame(pos,MzLagHeldPos[k])){MzLagApplied[k]=true;MzLagPositionOwned[k]=true;MzLagAppliedPos[k]=MzLagHeldPos[k];node.localPosition=MzLagHeldPos[k];}
                        if(!MzPoseSame(rot,MzLagHeldRot[k])){MzLagApplied[k]=true;MzLagRotationOwned[k]=true;MzLagAppliedRot[k]=MzLagHeldRot[k];node.localRotation=MzLagHeldRot[k];}
                    }
                }
                MzLagDrawnFrame[slot]=Time.frameCount;return true;
            }catch(Exception){MzRigRestore(slot);return false;}
        }
        public static Vector3 MzLagLivePoint(Player p,Vector3 live)
        {
            if(MzLagPlayers==null)return live;for(int i=0;i<16;i++)if(MzLagPlayers[i]==p&&MzLagPoseActive[i])return MzLagLiveHeads[i];return live;
        }
    }
}
