using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static bool MzHideAutoAnimation,MzAutoHideInitialized;
        public static float MzHideAnim,MzAutoPoseUntil;
        public static int MzAutoPoseCount;
        public static Player MzAutoPoseOwner;
        public static HBIBDMMOOOK MzAutoPoseWeapon;
        public static Transform MzAutoPoseRoot;
        public static SkinnedMeshRenderer[] MzAutoPoseSkins;
        public static bool[] MzAutoPoseSkinRecalc;
        public static int MzAutoPoseSkinCount;
        public static Transform[] MzAutoPoseNodes,MzAutoPoseParents;
        public static Vector3[] MzAutoPoseHeldPosition,MzAutoPoseRealPosition;
        public static Quaternion[] MzAutoPoseHeldRotation,MzAutoPoseRealRotation;
        public static bool[] MzAutoPoseApplied;
        public static bool MzPoseSame(Quaternion a,Quaternion b){float d=a.x-b.x,e=a.y-b.y,f=a.z-b.z,g=a.w-b.w;return d*d+e*e+f*f+g*g<.000001f;}
        public static void MzAutoPoseRestore()
        {
            if(MzAutoPoseNodes==null)return;for(int i=0;i<MzAutoPoseCount;i++){
                if(!MzAutoPoseApplied[i])continue;MzAutoPoseApplied[i]=false;
                try{var node=MzAutoPoseNodes[i];if(node==null||node.parent!=MzAutoPoseParents[i])continue;if(MzSame(node.localPosition,MzAutoPoseHeldPosition[i]))node.localPosition=MzAutoPoseRealPosition[i];if(MzPoseSame(node.localRotation,MzAutoPoseHeldRotation[i]))node.localRotation=MzAutoPoseRealRotation[i];}catch(Exception){}
            }
        }
        public static void MzAutoPoseClear()
        {
            MzAutoPoseRestore();if(MzAutoPoseNodes!=null)for(int i=0;i<MzAutoPoseCount;i++){MzAutoPoseNodes[i]=null;MzAutoPoseParents[i]=null;}
            if(MzAutoPoseSkins!=null)for(int i=0;i<MzAutoPoseSkinCount;i++){try{var skin=MzAutoPoseSkins[i];if(skin!=null&&!MzAutoPoseSkinRecalc[i]&&skin.forceMatrixRecalculationPerRender)skin.forceMatrixRecalculationPerRender=false;}catch(Exception){}MzAutoPoseSkins[i]=null;}
            MzAutoPoseSkinCount=0;MzAutoPoseCount=0;MzAutoPoseOwner=null;MzAutoPoseWeapon=null;MzAutoPoseRoot=null;MzAutoPoseUntil=0;
        }
        public static Transform MzAutoUpper(Player local,Transform left,Transform right,Transform root)
        {
            var node=left;for(int n=0;n<16&&node!=null&&node!=root;n++,node=node.parent){
                if(right==null||!right.IsChildOf(node))continue;var legLeft=local.LegBoneLeft;var legRight=local.LegBoneRight;
                if((legLeft!=null&&legLeft.IsChildOf(node))||(legRight!=null&&legRight.IsChildOf(node)))return null;
                return node;
            }return null;
        }
        public static void MzAutoPosePrepare(Player local)
        {
            if(!MzHideAutoAnimation||!MzOnMainThread()||local==null)return;
            if(MzAutoPoseOwner==local&&MzAutoPoseWeapon==MzAutoWeapon&&MzAutoPoseRoot==local.RootTransform&&MzAutoPoseCount>0&&Time.realtimeSinceStartup<MzAutoPoseUntil){MzAutoPoseUntil=Time.realtimeSinceStartup+.5f;return;}
            MzAutoPoseClear();
            try{
                if(MzAutoPoseNodes==null){MzAutoPoseNodes=new Transform[64];MzAutoPoseParents=new Transform[64];MzAutoPoseHeldPosition=new Vector3[64];MzAutoPoseRealPosition=new Vector3[64];MzAutoPoseHeldRotation=new Quaternion[64];MzAutoPoseRealRotation=new Quaternion[64];MzAutoPoseApplied=new bool[64];}
                var root=local.RootTransform;if(root==null)return;var left=local.ArmBoneLeft;var right=local.ArmBoneRight;var upper=MzAutoUpper(local,left,right,root);
                if(upper!=null)MzAutoPoseNodes[MzAutoPoseCount++]=upper;
                else {if(left!=null&&left!=root&&left.IsChildOf(root))MzAutoPoseNodes[MzAutoPoseCount++]=left;if(right!=null&&right!=root&&right!=left&&right.IsChildOf(root))MzAutoPoseNodes[MzAutoPoseCount++]=right;}
                for(int i=0;i<MzAutoPoseCount&&i<64;i++){
                    var node=MzAutoPoseNodes[i];MzAutoPoseParents[i]=node.parent;MzAutoPoseHeldPosition[i]=node.localPosition;MzAutoPoseHeldRotation[i]=node.localRotation;
                    int children=node.childCount;if(children>64)throw new Exception("upper rig cap");for(int n=0;n<children;n++){var child=node.GetChild(n);if(child==null||child==root)continue;bool duplicate=false;for(int k=0;k<MzAutoPoseCount;k++)if(MzAutoPoseNodes[k]==child){duplicate=true;break;}if(!duplicate){if(MzAutoPoseCount>=64)throw new Exception("upper rig cap");MzAutoPoseNodes[MzAutoPoseCount++]=child;}}
                }
                if(MzAutoPoseCount==0)return;
                if(MzAutoPoseSkins==null){MzAutoPoseSkins=new SkinnedMeshRenderer[16];MzAutoPoseSkinRecalc=new bool[16];}
                var skins=local.gameObject.GetComponentsInChildren(typeof(SkinnedMeshRenderer),false);if(skins!=null&&skins.Length<=16)for(int i=0;i<skins.Length;i++){var skin=skins[i] as SkinnedMeshRenderer;if(skin==null)continue;int k=MzAutoPoseSkinCount++;MzAutoPoseSkins[k]=skin;MzAutoPoseSkinRecalc[k]=skin.forceMatrixRecalculationPerRender;skin.forceMatrixRecalculationPerRender=true;}
                MzAutoPoseOwner=local;MzAutoPoseWeapon=MzAutoWeapon;MzAutoPoseRoot=root;MzAutoPoseUntil=Time.realtimeSinceStartup+.5f;MzAimlagTick();
            }catch(Exception){MzAutoPoseClear();}
        }
        public static void MzAutoPoseApply(Player local)
        {
            if(!MzHideAutoAnimation||!MzSwitch[15]||local!=MzAutoPoseOwner||MzAutoWeapon!=MzAutoPoseWeapon||Time.realtimeSinceStartup>=MzAutoPoseUntil)return;
            for(int i=0;i<MzAutoPoseCount;i++){
                try{var node=MzAutoPoseNodes[i];if(node==null||node.parent!=MzAutoPoseParents[i])continue;MzAutoPoseRealPosition[i]=node.localPosition;MzAutoPoseRealRotation[i]=node.localRotation;MzAutoPoseApplied[i]=true;node.localPosition=MzAutoPoseHeldPosition[i];node.localRotation=MzAutoPoseHeldRotation[i];}catch(Exception){}
            }
        }
    }
}
