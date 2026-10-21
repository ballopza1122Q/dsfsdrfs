using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static Color MzEspColor,MzFovColor,MzHealthColor;
        public static Color[] MzPalette;
        public static int MzColorTarget;
        public static Font MzFont,MzStrongFont;
        public static bool MzFontTried;
        public static GUIStyle MzSmallStyle,MzTitleStyle;
        public static float MzMotion;
        public static void MzAppearanceInit()
        {
            MzPalette=new Color[18];
            MzPalette[0]=new Color(.4f,.92f,.79f,1);MzPalette[1]=new Color(.3f,.65f,1,1);MzPalette[2]=new Color(.62f,.44f,1,1);MzPalette[3]=new Color(1,.4f,.66f,1);MzPalette[4]=new Color(1,.32f,.32f,1);MzPalette[5]=new Color(1,.72f,.28f,1);
            MzPalette[6]=new Color(.25f,1,.48f,1);MzPalette[7]=new Color(.2f,.9f,1,1);MzPalette[8]=new Color(.88f,.4f,1,1);MzPalette[9]=new Color(1,.64f,.82f,1);MzPalette[10]=new Color(1,.54f,.3f,1);MzPalette[11]=new Color(1,.95f,.35f,1);
            MzPalette[12]=new Color(.7f,1,.84f,1);MzPalette[13]=new Color(.65f,.82f,1,1);MzPalette[14]=new Color(.82f,.7f,1,1);MzPalette[15]=new Color(1,.84f,.9f,1);MzPalette[16]=new Color(1,1,1,1);MzPalette[17]=new Color(.68f,.72f,.8f,1);
            MzEspColor=MzLoadColor("ESP",MzPalette[0]);MzFovColor=MzLoadColor("FOV",MzPalette[7]);MzHealthColor=MzLoadColor("HP",new Color(.96f,.19f,.24f,1));
        }
        public static Color MzLoadColor(string key,Color fallback)
        {
            try{string s=PlayerPrefs.GetString("Mizze.Color."+key,"");if(s.Length!=6)return fallback;int n=Convert.ToInt32(s,16);return new Color(((n>>16)&255)/255f,((n>>8)&255)/255f,(n&255)/255f,1);}catch(Exception){return fallback;}
        }
        public static string MzColorHex(Color c){return ((int)(Mathf.Clamp(c.r,0,1)*255)).ToString("X2")+((int)(Mathf.Clamp(c.g,0,1)*255)).ToString("X2")+((int)(Mathf.Clamp(c.b,0,1)*255)).ToString("X2");}
        public static Color MzSelectedColor(){return MzColorTarget==0?MzEspColor:MzColorTarget==1?MzFovColor:MzHealthColor;}
        public static void MzSetColor(Color color,bool save)
        {
            color=new Color(Mathf.Clamp(color.r,0,1),Mathf.Clamp(color.g,0,1),Mathf.Clamp(color.b,0,1),1);
            MzNextEsp=0;if(MzColorTarget==0)MzEspColor=color;else if(MzColorTarget==1)MzFovColor=color;else MzHealthColor=color;
            if(save)PlayerPrefs.SetString("Mizze.Color."+(MzColorTarget==0?"ESP":MzColorTarget==1?"FOV":"HP"),MzColorHex(color));
        }
        public static void MzFontInit()
        {
            if(MzFontTried)return;MzFontTried=true;
            // Load once, keep the font alive, and restore the game's default after this OnGUI.
            // OS font availability varies. Failure retains the game's working Unicode font.
            try{MzFont=Font.CreateDynamicFontFromOSFont(new string[]{"Avenir Next","SF Pro Text","Helvetica Neue","Arial"},18);}catch(Exception){MzFont=null;}
            try{MzStrongFont=Font.CreateDynamicFontFromOSFont(new string[]{"AvenirNext-DemiBold","Avenir Next Demi Bold","Helvetica Neue","Arial"},18);}catch(Exception){MzStrongFont=MzFont;}
        }
        public static void MzStrongLabel(Rect rect,string text,GUIStyle style)
        {
            if(MzStrongFont!=null)GUIStyle.SetDefaultFont(MzStrongFont);
            try{GUI.Label(rect,text,style);}finally{GUIStyle.SetDefaultFont(MzFont!=null?MzFont:GUI.skin.font);}
        }
        public static void MzTitle(float x,float y,float w,string text)
        {
            if(!MzUiPaint)return;
            if(text=="Mizze X Ceo 𝒅𝒐𝒏𝒒 𝒏𝒉𝒂𝒕"&&MzBrandTitle(x,y,w))return;
            if(MzTitleStyle==null){var style=new GUIStyle();style.fontSize=17;style.wordWrap=false;style.alignment=TextAnchor.MiddleLeft;style.normal.textColor=new Color(1,1,1,1);MzTitleStyle=style;}MzStrongLabel(new Rect(x,y,w,32),text,MzTitleStyle);
        }
        public static void MzSmall(float x,float y,float w,string text)
        {
            if(!MzUiPaint)return;
            if(MzSmallStyle==null){var style=new GUIStyle();style.fontSize=13;style.wordWrap=false;style.alignment=TextAnchor.MiddleLeft;style.normal.textColor=new Color(1,1,1,1);MzSmallStyle=style;}
            GUI.Label(new Rect(x,y,w,22),MzUiText(text),MzSmallStyle);
        }
        public static void MzColorSlider(int channel,float x,float y,string label)
        {
            Color c=MzSelectedColor();float value=channel==0?c.r:channel==1?c.g:c.b;MzTint(.85f,.89f,.95f,MzOpenAnim);MzSmall(x,y,80,label+" "+((int)(value*255)).ToString());
            var e=Event.current;int mode=5+channel;
            if(MzOpen&&e.type==EventType.MouseDown&&new Rect(x+84,y-2,270,28).Contains(MzPointer)){MzDragMode=mode;e.Use();}
            if(MzDragMode==mode&&(e.type==EventType.Used||e.type==EventType.MouseDrag||e.type==EventType.MouseUp)){
                bool save=e.type==EventType.MouseUp;value=Mathf.Clamp((MzPointer.x-x-84)/270,0,1);if(channel==0)c.r=value;else if(channel==1)c.g=value;else c.b=value;MzSetColor(c,save);if(save)MzDragMode=0;e.Use();
            }
            float cr=channel==0?1:.4f,cg=channel==1?1:.4f,cb=channel==2?1:.4f;MzTint(.07f,.085f,.11f,.92f*MzOpenAnim);MzFill(x+84,y+8,270,6,3);MzTint(cr,cg,cb,MzOpenAnim);MzFill(x+84,y+8,270*value,6,3);MzTint(0,0,0,.30f*MzOpenAnim);MzFill(x+84+270*value-8,y+4.5f,16,16,8);MzTint(1,1,1,MzOpenAnim);MzFill(x+84+270*value-8,y+3,16,16,8);MzTint(cr,cg,cb,MzOpenAnim);MzFill(x+84+270*value-3,y+8,6,6,3);
        }
        public static void MzColorPanel(float x,float y)
        {
            for(int i=0;i<3;i++){if(MzColorTarget==i){MzTint(.30f,.95f,.76f,.16f*MzOpenAnim);MzFill(x+i*120-2,y-2,118,38,13);}MzTint(MzColorTarget==i?.15f:.08f,MzColorTarget==i?.55f:.10f,MzColorTarget==i?.45f:.13f,(MzColorTarget==i?.90f:.80f)*MzOpenAnim);MzFill(x+i*120,y,114,34,12);MzTint(.92f,.97f,1,MzOpenAnim);MzCenter(x+i*120,y,114,34,i==0?"Màu ESP":i==1?"Màu FOV":"Máu");if(MzTap(x+i*120,y,114,34))MzColorTarget=i;}
            Color selected=MzSelectedColor();MzTint(.08f,.10f,.13f,.85f*MzOpenAnim);MzFill(x,y+44,354,40,12);GUI.color=new Color(selected.r,selected.g,selected.b,.25f*MzOpenAnim);MzFill(x+6,y+48,32,32,10);GUI.color=new Color(selected.r,selected.g,selected.b,MzOpenAnim);MzFill(x+10,y+52,24,24,8);MzSmall(x+46,y+53,296,"#"+MzColorHex(selected)+"   •   Màu đã chọn");
            for(int i=0;i<18;i++){float px=x+(i%6)*60,py=y+94+(i/6)*39;Color c=MzPalette[i];bool active=Math.Abs(selected.r-c.r)<.006f&&Math.Abs(selected.g-c.g)<.006f&&Math.Abs(selected.b-c.b)<.006f;
                if(active){MzTint(1,1,1,MzOpenAnim);MzFill(px-2-MzMotion,py-2-MzMotion,56+2*MzMotion,35+2*MzMotion,10);}GUI.color=new Color(c.r,c.g,c.b,MzOpenAnim);MzFill(px,py,52,31,8);if(MzTap(px,py,52,31))MzSetColor(c,true);
            }
            MzColorSlider(0,x,y+221,"Đỏ");MzColorSlider(1,x,y+254,"Lục");MzColorSlider(2,x,y+287,"Lam");
        }

        // UI-only GIF frames: quantized from the supplied GIF; no added native APIs.
        public static Texture[] MzGlassFrames;
        public static Color[] MzGlassPixels,MzGlassColors;
        public static string[] MzGlassEncoded;
        public static int MzGlassLoaded;
        public static bool MzGlassFailed;
        public static float MzGlassNext,MzGlassLast,MzGlassClock;
        public static void MzGlassInit()
        {
            if(MzGlassFrames!=null)return;MzGlassFrames=new Texture[15];MzGlassEncoded=MzGlassData();MzGlassHashes=MzGlassImageHashes();
        }
        public static void MzGlassBackground(float x,float y)
        {
            if(!MzUiPaint||MzGlassFailed)return;Texture2D created=null;
            try{
                float now=Time.realtimeSinceStartup;
                if(MzOpen){if(MzGlassLast>0){float dt=now-MzGlassLast;if(dt>0&&dt<.25f)MzGlassClock+=dt;}MzGlassLast=now;
                    if(MzGlassLoaded<15&&now>=MzGlassNext){MzGlassInit();string data=MzGlassEncoded[MzGlassLoaded];
                        if(data==null)throw new InvalidOperationException("Missing menu PNG");
                        created=MzCheckedImage(data,MzGlassHashes[MzGlassLoaded]);
                        MzGlassFrames[MzGlassLoaded]=created;created=null;MzGlassEncoded[MzGlassLoaded]=null;MzGlassHashes[MzGlassLoaded]=null;MzGlassLoaded++;MzGlassNext=now+.12f;
                        if(MzGlassLoaded==15){MzGlassEncoded=null;MzGlassHashes=null;MzGlassPixels=null;MzGlassColors=null;}
                    }
                }else MzGlassLast=0;
                if(MzGlassLoaded==0)return;
                float phase=MzGlassClock%2.5f;int group=(int)(phase/.5f);float part=phase-group*.5f;int frame=group*3+(part<.17f?0:part<.33f?1:2);if(frame>=MzGlassLoaded)frame=0;
                var texture=MzGlassFrames[frame];if(texture==null)return;MzTint(1,1,1,.55f*MzOpenAnim);GUI.DrawTexture(new Rect(x,y,382,440),texture);
            }catch(Exception){if(created!=null)UnityEngine.Object.Destroy(created);MzGlassFailed=true;MzGlassEncoded=null;MzGlassHashes=null;MzGlassPixels=null;MzGlassColors=null;}
        }
        public static void MzGlassBackdrop(float x,float y)
        {
            if(!MzUiPaint)return;
            MzTint(0,0,0,.10f*MzOpenAnim);MzFill(x-6,y+4,394,452,29);
            MzTint(0,0,0,.24f*MzOpenAnim);MzFill(x+3,y+7,382,440,25);
            MzTint(.36f,.90f,.78f,.26f*MzOpenAnim);MzFill(x-1,y-1,384,442,25);MzTint(.24f,.62f,1,.16f*MzOpenAnim);MzFill(x+150,y+220,233.5f,221.5f,25);
            MzTint(.022f,.025f,.035f,.94f*MzOpenAnim);MzFill(x,y,382,440,24);
            MzGlassBackground(x,y);
            MzTint(.012f,.016f,.026f,.48f*MzOpenAnim);MzFill(x,y,382,440,24);
            MzTint(.88f,.94f,1,.055f*MzOpenAnim);MzFill(x+2,y+2,378,65,23);
            MzTint(1,1,1,.16f*MzOpenAnim);MzFill(x+25,y+1,332,1,0);MzTint(.40f,.97f,.80f,.55f*MzOpenAnim);MzFill(x+121,y,140,2,1);
            MzTint(.84f,.93f,1,(.10f+.07f*MzMotion)*MzOpenAnim);MzFill(x+40+MzMotion*72,y+3,146,1,0);
        }
    }
}
