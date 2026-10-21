using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static float[] MzScroll,MzScrollTarget;
        public static float MzScrollDown,MzScrollStart;
        public static float MzHeadChance;
        public static string MzChanceLabel;
        public static int MzChanceValue;
        public static void MzScrollBefore(float x,float y,float height,float content)
        {
            if(MzScroll==null){MzScroll=new float[2];MzScrollTarget=new float[2];}
            int tab=MzTab;float maximum=content-height;if(maximum<0)maximum=0;var e=Event.current;
            if(MzDragMode==8&&(e.type==EventType.MouseDrag||e.type==EventType.MouseUp)){
                float delta=MzPointer.y-MzScrollDown;if(Math.Abs(delta)>6)MzDragged=true;
                if(MzDragged){MzScrollTarget[tab]=Mathf.Clamp(MzScrollStart-delta,0,maximum);MzScroll[tab]=MzScrollTarget[tab];}
                if(e.type==EventType.MouseUp){MzDragMode=0;if(MzDragged||!new Rect(x,y,354,height).Contains(MzPointer))e.Use();}else e.Use();
            }
            MzScrollTarget[tab]=Mathf.Clamp(MzScrollTarget[tab],0,maximum);
            if(MzUiPaint&&MzDragMode!=8)MzScroll[tab]=MzTween(MzScroll[tab],MzScrollTarget[tab],Mathf.Clamp(Time.unscaledDeltaTime*18,0,1));
        }
        public static void MzScrollAfter(float x,float y,float height,float content)
        {
            var e=Event.current;int tab=MzTab;
            if(MzOpen&&MzDragMode==0&&e.type==EventType.MouseDown&&new Rect(x,y,354,height).Contains(MzPointer)){MzDragMode=8;MzScrollDown=MzPointer.y;MzScrollStart=MzScrollTarget[tab];MzDragged=false;e.Use();}
            float maximum=content-height;if(maximum>0){float thumb=height*height/content;MzTint(.58f,.72f,.82f,.10f*MzOpenAnim);MzFill(x+358,y,3,height,1.5f);MzTint(.36f,.96f,.80f,.80f*MzOpenAnim);MzFill(x+358,y+(height-thumb)*MzScroll[tab]/maximum,3,thumb,1.5f);}
        }
        public static bool MzRowVisible(float y,float height,float top,float bottom){return y>=top&&y+height<=bottom;}
        public static void MzChanceSlider(float x,float y)
        {
            float value=Mathf.Clamp(MzHeadChance,0,100);var e=Event.current;
            if(MzOpen&&e.type==EventType.MouseDown&&new Rect(x,y+25,354,27).Contains(MzPointer))MzDragMode=9;
            if(MzDragMode==9&&(e.type==EventType.MouseDown||e.type==EventType.MouseDrag||e.type==EventType.MouseUp)){
                MzHeadChance=(int)(Mathf.Clamp((MzPointer.x-x)/354,0,1)*100+.5f);MzChanceFrame=-1;if(e.type==EventType.MouseUp)MzDragMode=0;e.Use();value=MzHeadChance;
            }
            if(MzUiPaint){int rounded=(int)value;if(MzChanceLabel==null||rounded!=MzChanceValue){MzChanceValue=rounded;MzChanceLabel="Silent · Đầu "+rounded.ToString()+"% · Thân "+(100-rounded).ToString()+"%";}MzTint(.94f,.96f,.98f,MzOpenAnim);MzText(x,y,354,MzChanceLabel);}
            float fill=354*value/100;MzTrack(x,y+36,354,fill,.36f,.74f,1);MzKnob(x+fill,y+39,.36f,.74f,1);
        }
        public static void MzEspList(float x,float y)
        {
            const float height=312,content=588;MzScrollBefore(x,y,height,content);float top=y,bottom=y+height,offset=MzScroll[0];
            for(int i=0;i<11;i++){float row=y+i*44-offset;if(!MzRowVisible(row,40,top,bottom))continue;string title=i==0?"Bật ESP":i==1?"Khung người chơi":i==2?"Khung góc":i==3?"Khung 3D":i==4?"Tên người chơi":i==5?"Tim Minecraft":i==6?"Khoảng cách":i==7?"Xương người chơi":i==8?"Đánh dấu đầu":i==9?"Trục XYZ":"Đường nối + bộ đếm";MzToggle(i,x,row,title);}
            float range=y+484-offset;if(MzRowVisible(range,56,top,bottom))MzSlider(x,range,"Tầm ESP",false);
            float origin=y+550-offset;if(MzRowVisible(origin,34,top,bottom)){MzTint(.12f,.19f,.24f,.75f*MzOpenAnim);MzFill(x,origin,354,34,12);MzTint(.78f,.94f,1,MzOpenAnim);MzCenter(x,origin,354,34,"Tia nối: "+(MzLineOrigin==0?"Dưới màn hình":MzLineOrigin==1?"Giữa màn hình":"Trên màn hình"));if(MzTap(x,origin,354,34))MzLineOrigin=(MzLineOrigin+1)%3;}
            MzScrollAfter(x,y,height,content);
        }
        public static void MzAimList(float x,float y)
        {
            const float height=312,content=794;MzScrollBefore(x,y,height,content);float top=y,bottom=y+height,offset=MzScroll[1];
            for(int i=0;i<3;i++){float row=y+i*44-offset;if(MzRowVisible(row,40,top,bottom))MzAimChoice(i+1,x,row,i==0?"Aim đầu":i==1?"Aim cổ":"Aim silent");}
            for(int i=0;i<2;i++){float row=y+132+i*44-offset;if(MzRowVisible(row,40,top,bottom))MzToggle(13+i,x,row,i==0?"Vòng FOV":"Đường ngắm mục tiêu");}
            float chance=y+224-offset;if(MzRowVisible(chance,52,top,bottom))MzChanceSlider(x,chance);
            float fov=y+284-offset;if(MzRowVisible(fov,56,top,bottom))MzSlider(x,fov,"Kích cỡ FOV",true);
            float range=y+346-offset;if(MzRowVisible(range,56,top,bottom))MzSlider(x,range,"Tầm ngắm",false);
            float auto=y+406-offset;if(MzRowVisible(auto,40,top,bottom))MzToggle(15,x,auto,"Autofire");
            float status=y+450-offset;if(MzRowVisible(status,24,top,bottom))MzAutoStatus(x,status);
            float lag=y+482-offset;if(MzRowVisible(lag,40,top,bottom))MzAimlagRow(x,lag);
            float lagStatus=y+526-offset;if(MzRowVisible(lagStatus,24,top,bottom))MzAimlagStatus(x,lagStatus);
            float hide=y+558-offset;if(MzRowVisible(hide,40,top,bottom))MzRenderFeatureRow(x,hide,true);
            float reload=y+602-offset;if(MzRowVisible(reload,40,top,bottom))MzWeaponFeatureRow(x,reload,false);
            float rapid=y+646-offset;if(MzRowVisible(rapid,40,top,bottom))MzWeaponFeatureRow(x,rapid,true);
            float weaponStatus=y+690-offset;if(MzRowVisible(weaponStatus,24,top,bottom))MzWeaponFeatureStatus(x,weaponStatus);
            float kill=y+718-offset;if(MzRowVisible(kill,40,top,bottom))MzAimkillRow(x,kill);
            float killStatus=y+762-offset;if(MzRowVisible(killStatus,24,top,bottom))MzAimkillStatus(x,killStatus);
            MzScrollAfter(x,y,height,content);
        }
    }
}
