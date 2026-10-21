using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static bool MzAimNeck;
        public static float[] MzAimChoiceAnim;
        public static int MzAimMode(){return MzSwitch==null?0:MzSwitch[12]?3:MzSwitch[11]?(MzAimNeck?2:1):0;}
        public static void MzSelectAim(int mode)
        {
            if(MzSwitch==null||mode<0||mode>3)return;
            MzResetAim();MzAimNeck=mode==2;MzSwitch[11]=mode==1||mode==2;MzSwitch[12]=mode==3;
        }
        public static void MzAimChoice(int mode,float x,float y,string title)
        {
            if(MzAimChoiceAnim==null)MzAimChoiceAnim=new float[3];
            bool selected=MzAimMode()==mode;int index=mode-1;
            if(MzUiPaint)MzAimChoiceAnim[index]=MzTween(MzAimChoiceAnim[index],selected?1:0,Mathf.Clamp(Time.unscaledDeltaTime*16,0,1));
            float a=MzAimChoiceAnim[index],o=MzOpenAnim;
            MzTint(.10f+.03f*a,.13f+.08f*a,.17f+.05f*a,(.30f+.20f*a)*o);MzFill(x,y,354,40,12);
            MzTint(.30f,.95f,.76f,a*o);MzFill(x+5,y+11,3,18,1.5f);
            MzTint(.94f,.96f,.98f,o);MzText(x+16,y+6,276,title);
            MzTint(.48f+.08f*a,.55f+.40f*a,.62f+.14f*a,.65f*o);MzFill(x+314,y+10,20,20,5);
            MzTint(.06f,.09f,.12f,o);MzFill(x+315,y+11,18,18,4);
            if(a>.01f){MzTint(.30f,.95f,.76f,a*o);MzFill(x+316,y+12,16,16,3);MzTint(.03f,.12f,.09f,a*o);for(int i=0;i<3;i++)MzFill(x+318+i,y+18+i,2,2,0);for(int i=0;i<7;i++)MzFill(x+321+i,y+21-i,2,2,0);}
            if(MzTap(x,y,354,40))MzSelectAim(selected?0:mode);
        }
    }
}
