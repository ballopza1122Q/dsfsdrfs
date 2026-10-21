using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool:Component
    {
        // VM-owned state; player movement and the game's shared GUI skin stay separate.
        public static bool MzInitialized,MzOpen,MzDragged,MzDrawingEsp;
        public static GameObject MzDriver;
        public static int MzDrawBudget;
        public static bool MzUiPaint;
        public static Texture2D MzUiWhite;
        public static Matrix4x4 MzUiMatrix;
        public static float MzUiMatrixScale;
        public static string[] MzSliderTitles,MzSliderText;
        public static int[] MzSliderValues;
        public static float MzTabAnim;
        public static float MzBubbleX,MzBubbleY,MzPanelX,MzPanelY,MzOpenAnim,MzDragDX,MzDragDY,MzDownX,MzDownY,MzFov,MzRange,MzScale;
        public static int MzDragMode,MzTab,MzEspPage,MzLineOrigin;
        public static Vector2 MzPointer;
        public static GUIStyle MzLabelStyle,MzCenterStyle,MzWrapStyle;
        public static bool[] MzSwitch;
        public static float[] MzSwitchAnim;
        // ESP, box, corners, 3D, name, health, distance, skeleton, head, XYZ, line,
        // aim head on fire, silent on fire, FOV circle, aim target line.
        public static void MzEnsure()
        {
            MzRemoveLocalConfig();
            try {
                if(!MzInitialized){MzUiPaint=true;MzSliderTitles=new string[2];MzSliderText=new string[2];MzSliderValues=new int[2];MzSwitch=new bool[16];MzSwitchAnim=new float[16];MzSwitch[0]=true;MzSwitch[1]=true;MzSwitch[2]=false;MzSwitch[4]=true;MzSwitch[5]=true;MzSwitch[6]=true;MzSwitch[13]=true;MzFov=150;MzRange=150;MzHeadChance=100;MzBubbleX=24;MzBubbleY=96;MzPanelX=120;MzPanelY=16;MzInitialized=true;MzAppearanceInit();try{MzLicenseInit();}catch(Exception){MzLicenseMessage="Lỗi khởi tạo khóa. Thử nhập lại";}}
                if(MzDriver!=null)return;var g=GameObject.Find("__mizze_driver");if(g==null)g=new GameObject("__mizze_driver");if(g.GetComponent(typeof(SceneEditBoxSelectTool))==null)g.AddComponent(typeof(SceneEditBoxSelectTool));UnityEngine.Object.DontDestroyOnLoad(g);MzDriver=g;MzMainThread=System.Threading.Thread.CurrentThread.ManagedThreadId;MzAimFrame=-1;MzSilentFrame=-1;
            }catch(Exception){}
        }
        // IEEE comparisons reject NaN and both infinities without reflected
        // Single.IsNaN/IsInfinity calls for every coordinate and line segment.
        public static bool MzFinite(float f){return f>=Single.MinValue&&f<=Single.MaxValue;}
        public static bool MzPoint(Vector3 p){return MzFinite(p.x)&&MzFinite(p.y)&&MzFinite(p.z);}
        public static void MzTint(float r,float g,float b,float a){if(MzBuilding)MzBatchColor=new Color(r,g,b,a);else if(MzUiPaint)GUI.color=new Color(r,g,b,a);}
        public static float MzTween(float value,float target,float t){float difference=value-target;return difference>-.001f&&difference<.001f?target:Mathf.Lerp(value,target,t); }
        public static void MzFill(float x,float y,float w,float h,float radius)
        {
            if(!MzFinite(x)||!MzFinite(y)||!MzFinite(w)||!MzFinite(h)||w<=0||h<=0)return;
            if(MzBuilding){if(MzBatchReady){MzQuadXY(x,y,x+w,y,x+w,y+h,x,y+h);return;}MzCommand(new Rect(x,y,w,h),Matrix4x4.identity,false,null,MzWhite);return;}
            if(!MzUiPaint)return;
            if(MzUiWhite==null)MzUiWhite=Texture2D.whiteTexture;
            // Rounded DrawTexture uses its explicit color.
            var tint=GUI.color;try{GUI.color=new Color(1,1,1,1);GUI.DrawTexture(new Rect(x,y,w,h),MzUiWhite,ScaleMode.StretchToFill,true,0,tint,0,radius);}finally{GUI.color=tint;}
        }
        public static void MzStyles()
        {
            if(MzLabelStyle!=null)return;
            var label=new GUIStyle();label.fontSize=17;label.wordWrap=false;label.alignment=TextAnchor.MiddleLeft;label.normal.textColor=new Color(1,1,1,1);
            var center=new GUIStyle();center.fontSize=15;center.wordWrap=false;center.alignment=TextAnchor.MiddleCenter;center.normal.textColor=new Color(1,1,1,1);
            var wrap=new GUIStyle();wrap.fontSize=16;wrap.wordWrap=true;wrap.alignment=TextAnchor.UpperLeft;wrap.normal.textColor=new Color(1,1,1,1);
            MzCenterStyle=center;MzWrapStyle=wrap;MzLabelStyle=label;
        }
        public static void MzText(float x,float y,float w,string s){if(MzBuilding){MzCommand(new Rect(x,y,w,28),Matrix4x4.identity,false,s,null);return;}if(!MzUiPaint)return;MzStyles();GUI.Label(new Rect(x,y,w,28),MzUiText(s),MzLabelStyle);}
        public static void MzCenter(float x,float y,float w,float h,string s){if(!MzUiPaint)return;MzStyles();GUI.Label(new Rect(x,y,w,h),MzUiText(s),MzCenterStyle);}
        public static void MzMessage(float x,float y,float w,float h,string s){if(!MzUiPaint)return;MzStyles();GUI.Label(new Rect(x,y,w,h),MzUiText(s),MzWrapStyle);}
        public static void MzLine(float ax,float ay,float bx,float by,float width)
        {
            if(!MzFinite(ax)||!MzFinite(ay)||!MzFinite(bx)||!MzFinite(by)||!MzFinite(width)||width<=0)return;
            int screenWidth=MzBuilding?MzEspWidth:Screen.width,screenHeight=MzBuilding?MzEspHeight:Screen.height;if((ax<0&&bx<0)||(ax>screenWidth&&bx>screenWidth)||(ay<0&&by<0)||(ay>screenHeight&&by>screenHeight))return;
            float dx=bx-ax,dy=by-ay,length=dy==0?(dx<0?-dx:dx):dx==0?(dy<0?-dy:dy):Mathf.Sqrt(dx*dx+dy*dy);if(!MzFinite(length)||length<.5f||length>screenWidth*3)return;
            if(MzBuilding&&MzBatchReady){
                if(dy==0||dx==0){float x=dy==0?(ax<bx?ax:bx)-width*.5f:ax-width*.5f,y=dy==0?ay-width*.5f:(ay<by?ay:by)-width*.5f,w=dy==0?length+width:width,h=dy==0?width:length+width;MzQuadXY(x,y,x+w,y,x+w,y+h,x,y+h);return;}
                float ux=dx/length,uy=dy/length,half=width*.5f,sx=ax-ux*half,sy=ay-uy*half,ex=bx+ux*half,ey=by+uy*half,nx=-uy*half,ny=ux*half;MzQuadXY(sx-nx,sy-ny,ex-nx,ey-ny,ex+nx,ey+ny,sx+nx,sy+ny);return;}
            Rect rect;Matrix4x4 lineMatrix=Matrix4x4.identity;bool rotated=false;
            // Axis-aligned edges need no matrix calls. Extend both ends by half the
            // stroke so every shared endpoint overlaps instead of leaving a seam.
            if(dy==0)rect=new Rect((ax<bx?ax:bx)-width*.5f,ay-width*.5f,length+width,width);
            else if(dx==0)rect=new Rect(ax-width*.5f,(ay<by?ay:by)-width*.5f,width,length+width);
            else {rotated=true;lineMatrix=Matrix4x4.TRS(new Vector3(ax,ay,0),Quaternion.Euler(0,0,Mathf.Atan2(dy,dx)*57.29578f),new Vector3(1,1,1));rect=new Rect(-width*.5f,-width*.5f,length+width,width);}
            if(MzBuilding){MzCommand(rect,lineMatrix,rotated,null,MzWhite);return;}
            var old=GUI.matrix;try{if(rotated)GUI.matrix=lineMatrix;GUI.DrawTexture(rect,Texture2D.whiteTexture);}finally{if(rotated)GUI.matrix=old;}
        }
        public static void MzCircle(float cx,float cy,float radius,float width)
        {
            if(!MzFinite(cx)||!MzFinite(cy)||!MzFinite(radius)||radius<=0||radius>Screen.width)return;
            if(MzRing==null||MzHeadRing==null)MzRendererInit();var texture=radius<=12?MzHeadRing:MzRing;float correction=radius<=12?16f/14f:128f/126f;float extent=radius*correction;var rect=new Rect(cx-extent,cy-extent,extent*2,extent*2);
            if(MzBuilding){MzCommand(rect,Matrix4x4.identity,false,null,texture);return;}GUI.DrawTexture(rect,texture);
        }
        public static void MzLayout()
        {
            MzScale=Mathf.Clamp(Screen.height/720f*1.35f,.75f,3);float fitH=Screen.height/456f,fitW=Screen.width/398f;if(MzScale>fitH)MzScale=fitH;if(MzScale>fitW)MzScale=fitW;
            // Capture raw input before the draw matrix; convert exactly once.
            var p=Event.current.mousePosition;MzPointer=new Vector2(p.x/MzScale,p.y/MzScale);
            float width=Screen.width/MzScale,height=Screen.height/MzScale;MzPanelX=Mathf.Clamp(MzPanelX,4,width-386);MzPanelY=Mathf.Clamp(MzPanelY,4,height-444);MzBubbleX=Mathf.Clamp(MzBubbleX,4,width-92);MzBubbleY=Mathf.Clamp(MzBubbleY,4,height-92);
        }
        public static bool MzTap(float x,float y,float w,float h){var e=Event.current;if(!MzOpen||!MzPressValid||MzDragged||e==null||e.type!=EventType.MouseUp||MzDragMode!=0)return false;if(!new Rect(x,y,w,h).Contains(new Vector2(MzPressX,MzPressY)))return false;if(!new Rect(x,y,w,h).Contains(MzPointer))return false;e.Use();return true;}
        public static void MzToggle(int id,float x,float y,string title)
        {
            float a=MzSwitchAnim[id];MzRowVisual(x,y,a,title,Mathf.Sin(a*3.14159265f)*1.5f);
            if(MzTap(x,y,354,40)){MzSwitch[id]=!MzSwitch[id];MzNextEsp=0;if(id==11&&MzSwitch[id])MzSwitch[12]=false;if(id==12&&MzSwitch[id])MzSwitch[11]=false;if(id==2&&MzSwitch[id])MzSwitch[3]=false;if(id==3&&MzSwitch[id])MzSwitch[2]=false;if(id==15&&MzSwitch[id])MzAimkillEnabled=false;if(id==11||id==12||id==15)MzResetAim();}
        }
        // Shared row/switch/slider visuals. Geometry and tap rectangles stay unchanged.
        public static void MzRowVisual(float x,float y,float a,string title,float bounce)
        {
            float o=MzOpenAnim;
            MzTint(.10f+.06f*a,.105f,.125f,(.38f+.10f*a)*o);MzFill(x,y,354,40,12);
            MzTint(1,1,1,.035f*o);MzFill(x+14,y,326,1,0);
            MzTint(.62f,.12f,.18f,a*o);MzFill(x+5,y+11,3,18,1.5f);
            MzTint(.94f,.94f,.96f,o);MzText(x+16,y+6,272,title);
            // Matte burgundy track. No halo or coloured dot in the thumb.
            MzTint(.16f+.32f*a,.14f-.065f*a,.16f-.06f*a,o);MzFill(x+294,y+8,46,24,12);
            MzTint(1,1,1,.045f*o);MzFill(x+297,y+9,40,1,0);
            float k=x+297+22*a,t=y+11;
            MzTint(0,0,0,.28f*o);MzFill(k,t+1,18,18,9);
            MzTint(.68f,.69f,.72f,o);MzFill(k,t,18,18,9);
            MzTint(.96f,.96f,.97f,o);MzFill(k+.8f,t+.7f,16.4f,16.4f,8.2f);
        }
        public static void MzKnob(float cx,float cy,float r,float g,float b)
        {
            float o=MzOpenAnim;MzTint(r,g,b,.22f*o);MzFill(cx-13,cy-13,26,26,13);MzTint(0,0,0,.30f*o);MzFill(cx-9,cy-7.5f,18,18,9);MzTint(1,1,1,o);MzFill(cx-9,cy-9,18,18,9);MzTint(r,g,b,o);MzFill(cx-3.5f,cy-3.5f,7,7,3.5f);
        }
        public static void MzTrack(float x,float y,float w,float fill,float r,float g,float b)
        {
            float o=MzOpenAnim;MzTint(.07f,.085f,.11f,.92f*o);MzFill(x,y,w,6,3);MzTint(1,1,1,.05f*o);MzFill(x+3,y,w-6,1,0);if(fill>0){MzTint(r,g,b,o);MzFill(x,y,fill,6,3);if(fill>6){MzTint(1,1,1,.22f*o);MzFill(x+2,y+1,fill-4,2,1);}}
        }
        public static void MzSlider(float x,float y,string title,bool fov)
        {
            float min=fov?20:50,max=fov?MzFovMaximum():500,value=fov?MzEffectiveFov():MzRange;MzTint(.94f,.96f,.98f,MzOpenAnim);if(MzUiPaint){int slot=fov?0:1,rounded=(int)value;if(MzSliderText[slot]==null||MzSliderTitles[slot]!=title||MzSliderValues[slot]!=rounded){MzSliderTitles[slot]=title;MzSliderValues[slot]=rounded;MzSliderText[slot]=title+"  "+rounded.ToString()+(fov?" px":" m");}MzText(x,y,354,MzSliderText[slot]);}
            var e=Event.current;if(MzOpen&&e.type==EventType.MouseDown&&new Rect(x,y+26,354,32).Contains(MzPointer))MzDragMode=fov?3:4;
            if(MzDragMode==(fov?3:4)&&(e.type==EventType.MouseDown||e.type==EventType.MouseDrag||e.type==EventType.MouseUp)){value=min+Mathf.Clamp((MzPointer.x-x)/354,0,1)*(max-min);if(fov)MzFov=value;else {MzRange=value;MzNextEsp=0;}if(e.type==EventType.MouseUp)MzDragMode=0;e.Use();}
            float fill=354*(value-min)/(max-min);MzTrack(x,y+37,354,fill,.30f,.95f,.76f);MzKnob(x+fill,y+40,.30f,.95f,.76f);
        }
        public static void MzDrag()
        {
            var e=Event.current;Vector2 p=MzPointer;
            if(e.type==EventType.MouseDown&&MzDragMode==0){bool panel=MzOpen&&new Rect(MzPanelX,MzPanelY,382,440).Contains(p);if(!panel&&new Rect(MzBubbleX-4,MzBubbleY-4,96,96).Contains(p)){MzDragMode=1;MzDragDX=p.x-MzBubbleX;MzDragDY=p.y-MzBubbleY;}else if(MzOpen&&new Rect(MzPanelX,MzPanelY,320,60).Contains(p)&&!new Rect(MzPanelX+168,MzPanelY+38,31,23).Contains(p)){MzDragMode=2;MzDragDX=p.x-MzPanelX;MzDragDY=p.y-MzPanelY;}if(MzDragMode>0){MzDownX=p.x;MzDownY=p.y;MzDragged=false;e.Use();}}
            if((MzDragMode==1||MzDragMode==2)&&(e.type==EventType.MouseDrag||e.type==EventType.MouseUp)){float dx=p.x-MzDownX,dy=p.y-MzDownY;if(dx*dx+dy*dy>36)MzDragged=true;if(MzDragged){if(MzDragMode==1){MzBubbleX=Mathf.Clamp(p.x-MzDragDX,4,Screen.width/MzScale-92);MzBubbleY=Mathf.Clamp(p.y-MzDragDY,4,Screen.height/MzScale-92);}else{MzPanelX=Mathf.Clamp(p.x-MzDragDX,4,Screen.width/MzScale-386);MzPanelY=Mathf.Clamp(p.y-MzDragDY,4,Screen.height/MzScale-444);}}if(e.type==EventType.MouseUp){if(MzDragMode==1&&!MzDragged){MzOpen=!MzOpen;if(!MzAuthorized())MzTab=2;}MzDragMode=0;}e.Use();}
        }
        public static void MzPanel()
        {
            float x=MzPanelX,y=MzPanelY,o=MzOpenAnim;bool live=MzAuthorized();MzGlassBackdrop(x,y);
            MzTint(.82f,.89f,1,.055f*o);MzFill(x+10,y+10,362,48,16);MzTint(1,1,1,.05f*o);MzFill(x+26,y+10,330,1,0);
            MzTint(.30f,.95f,.76f,o);MzFill(x+13,y+19,3,30,1.5f);MzTint(.24f,.62f,1,.60f*o);MzFill(x+13,y+36,3,13,1.5f);
            MzTint(.45f,.98f,.77f,o);MzTitle(x+23,y+12,292,"Mizze X Ceo 𝒅𝒐𝒏𝒒 𝒏𝒉𝒂𝒕");MzTint(.62f,.70f,.84f,o);MzSmall(x+23,y+38,145,"Free Fire · TH · R14.12");
            MzTint(.30f,.95f,.76f,.15f*o);MzFill(x+168,y+40,31,19,9);MzTint(.82f,1,.94f,o);MzSmall(x+174,y+38.5f,25,MzLanguage==1?"EN":"VI");if(MzTap(x+168,y+38,31,23))MzChooseLanguage=true;
            MzTint(live?.18f:.30f,live?.55f:.22f,live?.42f:.10f,.42f*o);MzFill(x+204,y+40,112,19,9.5f);MzTint(live?.35f:1,live?1:.74f,live?.78f:.30f,(.75f+.25f*MzMotion)*o);MzFill(x+213,y+46.5f,6,6,3);MzTint(live?.80f:1,live?1:.88f,live?.92f:.66f,o);MzSmall(x+224,y+38.5f,92,live?"Đã kích hoạt":"Chưa có khóa");
            MzTint(.35f,.92f,.79f,.30f*o);MzFill(x+23+MzMotion*42,y+61,80,2,1);
            MzTint(1,1,1,.07f*o);MzFill(x+333,y+19,31,31,15.5f);MzTint(1,1,1,.06f*o);MzFill(x+333,y+19,31,15,7.5f);MzTint(.9f,.94f,.96f,o);MzCenter(x+326,y+10,46,48,"x");if(MzTap(x+326,y+10,46,48)){MzOpen=false;MzDragMode=0;}
            if(MzChooseLanguage){MzLanguagePanel(x+14,y+100);return;}
            MzTint(.02f,.03f,.045f,.80f*o);MzFill(x+12,y+67,358,38,19);MzTint(1,1,1,.045f*o);MzFill(x+30,y+67,322,1,0);
            float tab=x+14+MzTabAnim*89;MzTint(.30f,.95f,.76f,.13f*o);MzFill(tab-2,y+68,91,36,18);MzTint(.15f,.60f,.49f,.88f*o);MzFill(tab,y+70,87,32,16);MzTint(.24f,.62f,1,.14f*o);MzFill(tab+40,y+70,47,32,16);MzTint(.70f,1,.90f,.12f*o);MzFill(tab+8,y+71,71,12,6);
            for(int i=0;i<4;i++){MzTint(MzTab==i?.98f:.55f,MzTab==i?1:.63f,MzTab==i?.99f:.73f,o);MzCenter(x+14+i*89,y+70,87,32,i==0?"ESP":i==1?"AIM":i==2?"KHÓA":"MÀU");if(MzTap(x+14+i*89,y+70,87,32))MzTab=MzAuthorized()?i:2;}
            if(!MzAuthorized())MzTab=2;float rowY=y+112;
            if(MzTab==0)MzEspList(x+14,rowY);
            else if(MzTab==1)MzAimList(x+14,rowY);
            else if(MzTab==2)MzKeyPanel(x+14,rowY);else MzColorPanel(x+14,rowY);
            MzTint(.40f,.97f,.80f,MzOpenAnim);MzFill(x+40+MzTabAnim*89,y+103,35,3,1.5f);
        }
        public void OnGUI()
        {
            if(MzGuiRunning)return;
            if(this==null||Event.current==null||gameObject==null||gameObject.name!="__mizze_driver")return;MzControlId=GUIUtility.GetControlID(0x4D5A36,FocusType.Passive);var type=Event.current.type;
            if(type==EventType.Ignore||type==EventType.MouseLeaveWindow){MzReleasePointer();return;}
            if(type==EventType.TouchDown)type=EventType.MouseDown;else if(type==EventType.TouchUp)type=EventType.MouseUp;else if(type==EventType.TouchMove)type=EventType.MouseDrag;
            if(type!=EventType.Repaint&&type!=EventType.MouseDown&&type!=EventType.MouseDrag&&type!=EventType.MouseUp)return;bool release=type==EventType.MouseUp,pointer=type!=EventType.Repaint;
            MzMainThread=System.Threading.Thread.CurrentThread.ManagedThreadId;bool previousPaint=MzUiPaint;MzUiPaint=type==EventType.Repaint;var color=GUI.color;var contentColor=GUI.contentColor;var matrix=GUI.matrix;Font oldFont=null;bool fontChanged=false;MzGuiRunning=true;
            try {
                GUI.matrix=Matrix4x4.identity;MzLayout();if(!MzBeginPointer(type))return;if(Event.current.type!=type)Event.current.type=type;MzDrag();
                if(MzUiPaint){GUI.contentColor=new Color(1,1,1,1);MzLicensePoll();MzFontInit();if(MzFont!=null){oldFont=GUI.skin.font;GUIStyle.SetDefaultFont(MzFont);fontChanged=true;}}
                if(Event.current.type==EventType.Repaint){MzWeaponFeaturesTick();MzAimlagTick();MzMemoryTick();MzMotion=(Mathf.Sin(Time.realtimeSinceStartup*2.4f)+1)*.5f;float t=Mathf.Clamp(Time.unscaledDeltaTime*14,0,1);MzOpenAnim=MzTween(MzOpenAnim,MzOpen?1:0,t);if(MzOpen||MzOpenAnim>.02f){MzTabAnim=MzTween(MzTabAnim,MzTab,t);for(int i=0;i<MzSwitch.Length;i++)MzSwitchAnim[i]=MzTween(MzSwitchAnim[i],MzSwitch[i]?1:0,t);}if(MzAuthorized()){MzRender();MzRunAim();}else MzResetAim();}
                if(MzUiPaint){if(MzUiMatrixScale!=MzScale){MzUiMatrix=Matrix4x4.TRS(new Vector3(),Quaternion.Euler(0,0,0),new Vector3(MzScale,MzScale,1));MzUiMatrixScale=MzScale;}GUI.matrix=MzUiMatrix;}
                MzBadge();if(MzOpen||MzOpenAnim>.02f)MzPanel();
            }catch(Exception){}finally{MzGuiRunning=false;MzUiPaint=previousPaint;MzFinishPointer(release,pointer);if(fontChanged)GUIStyle.SetDefaultFont(oldFont);GUI.color=color;GUI.contentColor=contentColor;GUI.matrix=matrix;}
        }
    }
}
