using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static int MzControlId;
        public static bool MzPointerCaptured,MzPressValid;
        public static float MzPressX,MzPressY;
        public static bool MzBeginPointer(EventType type)
        {
            if(type==EventType.Repaint)return true;
            if(type==EventType.MouseDown){
                if(MzPointerCaptured)MzReleasePointer();
                bool inside=new Rect(MzBubbleX-4,MzBubbleY-4,96,96).Contains(MzPointer)||(MzOpen&&new Rect(MzPanelX,MzPanelY,382,440).Contains(MzPointer));
                int owner=GUIUtility.hotControl;if(!inside||(owner!=0&&owner!=MzControlId))return false;
                GUIUtility.hotControl=MzControlId;MzPointerCaptured=true;MzPressValid=true;MzPressX=MzPointer.x;MzPressY=MzPointer.y;MzDragged=false;return true;
            }
            if(!MzPointerCaptured)return false;
            if(GUIUtility.hotControl!=MzControlId){MzPointerCaptured=false;MzPressValid=false;MzDragMode=0;return false;}
            return true;
        }
        public static void MzReleasePointer()
        {
            try{if(MzPointerCaptured&&GUIUtility.hotControl==MzControlId)GUIUtility.hotControl=0;}catch(Exception){}
            MzPointerCaptured=false;MzPressValid=false;MzDragMode=0;MzDragged=false;
        }
        public static void MzFinishPointer(bool release,bool pointer)
        {
            if(pointer&&MzPointerCaptured&&Event.current!=null)Event.current.Use();
            if(release)MzReleasePointer();
        }
        public static GUIStyle MzBadgeStyle;
        public static void MzBadge()
        {
            if(!MzUiPaint)return;
            float x=MzBubbleX,y=MzBubbleY;MzBadgeImageInit();
            float pressed=MzPointerCaptured&&MzDragMode==1?2:0;
            MzTint(0,0,0,.28f);MzFill(x+6,y+8,76,76,38);
            if(MzBadgeTexture!=null){
                MzTint(1,1,1,1);GUI.DrawTexture(new Rect(x+6+pressed,y+6+pressed,76-pressed*2,76-pressed*2),MzBadgeTexture,ScaleMode.StretchToFill,true,0,new Color(1,1,1,1),0,38);
            }else{
                // Resource failure must not remove the usable touch target.
                MzTint(.03f,.04f,.06f,.96f);MzFill(x+6,y+6,76,76,38);
                if(MzBadgeStyle==null){var style=new GUIStyle();style.fontSize=17;style.alignment=TextAnchor.MiddleCenter;style.normal.textColor=new Color(1,1,1,1);MzBadgeStyle=style;}
                MzTint(.94f,1,.98f,1);MzStrongLabel(new Rect(x+6,y+6,76,76),"Mizze",MzBadgeStyle);
            }
            bool live=MzAuthorized();MzTint(live?.66f:.72f,live?.12f:.72f,live?.18f:.74f,.95f);
            MzFill(x+69,y+69,7,7,3.5f);
        }
    }
}
