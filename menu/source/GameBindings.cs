// Build-time declarations only. Their bodies are never put into the patch.
using System;
using System.Collections;
using UnityEngine;
[assembly: System.Reflection.AssemblyVersion("0.86.0.518")]
namespace GCommon
{
    public class SingletonModule<T> where T:class {public static T instance {get {return null;}}}
    public class UIModelManager : SingletonModule<UIModelManager> {public T GetModel<T>() where T:class {return null;}}
}
namespace COW
{
    public class UIModelRole {}
    public static partial class GameFacade
    {
        public static UIModelRole m_modelRole;
        public static GamePlay.JMAGGLCNGIG CurrentMatch(){return null;}
        public static GamePlay.Player GetLocalPlayerOrObServer(){return null;}
        public static UIModelRole UIModelRole
        {
            get
            {
                GamePlay.SceneEditBoxSelectTool.MzEnsure();
                if(m_modelRole==null)m_modelRole=GCommon.UIModelManager.instance.GetModel<UIModelRole>();
                return m_modelRole;
            }
        }
    }
}
namespace COW.GamePlay
{
    public class HBIBDMMOOOK
    {
        public Player DHNEEGMCLNO(){return null;}
        public float LDKCJEMDAMC(){return 0;}
        public virtual void ALJMFCJMDCF(){}
        public virtual void NFFJEBKJCAK(){}
    }
    public class JMAGGLCNGIG {public IList ILPNGCMEFFI(){return null;}}
    public enum LKOOALMKJND {Default=0,Head=1,Body=2}
    public class CGKJLKPMGDJ
    {
        public GameObject JHAINOBIFMA;
        public Collider JINDNOFABKB;
        public Vector3 HPCOJHGFEEG,DOJDNHCGFGK,HLDECMJFKJK,EKPLMDDKOGB,ONMKMEGMHOL;
        public int OLGEMBBDBID;public float CFIPDNFBKHK;
        public LKOOALMKJND DKPDBLLJEAA;public short CLOLKHNMNEK;public bool EFMBNPNFGHM,CEOKOILBODN;
    }
    public class Player : Component
    {
        public CGKJLKPMGDJ FDMIEDDNCEC;
        public bool IsInStealth(){return false;}
        public bool IsReallyInStealth(){bool result=IsInStealth();SceneEditBoxSelectTool.MzEnsure();return result;}
        public CGKJLKPMGDJ LastAimingInfoFromWeapon
        {
            get {var result=FDMIEDDNCEC;SceneEditBoxSelectTool.MzSilent(this,result);return result;}
        }
        public bool IsLocalPlayer(){return false;}
        public bool IsClientBot;
        public bool IsDieing {get {return false;}}
        public int CurHP {get {return 0;}}
        public int MaxHP {get {return 0;}}
        public string NickName {get {return "";}}
        public bool IsLocalTeammate(bool b){return false;}
        public Transform RootTransform {get {return null;}}
        public Transform GetHeadTF(){return null;}
        public Transform NeckBone {get {return null;}}
        public Transform ArmBoneLeft {get {return null;}}
        public Transform ArmBoneRight {get {return null;}}
        public Transform HandBoneLeft {get {return null;}}
        public Transform HandBoneRight {get {return null;}}
        public Transform LegBoneLeft {get {return null;}}
        public Transform LegBoneRight {get {return null;}}
        public Transform FootBoneLeft {get {return null;}}
        public Transform FootBoneRight {get {return null;}}
        public Collider HeadCollider {get {return null;}}
        public Vector3 GetChestPos(bool useCamera){return new Vector3();}
        public Vector3 AimStartPostion {get {return new Vector3();}}
        public bool IsFiring(){return false;}
        public HBIBDMMOOOK GetWeaponOnHand(){return null;}
        public bool get_IsSighting(){return false;}
        public Transform get_BoneRootTransform(){return null;}
        public void SetAimRotation(Quaternion q,bool b){}
    }
}

