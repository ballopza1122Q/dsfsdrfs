using System;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static int MzPeople,MzBots;
        public static string MzPeopleText,MzBotsText;
        public static bool MzCountValid;
        public static string MzCountText;
        public static GUIStyle MzCountStyle;
        public static GUIStyle MzEspStyle;
        public static string[] MzFullNames,MzShortNames;
        public static string MzCachedName(string name,int slot)
        {
            if(name==null)return null;if(MzFullNames==null){MzFullNames=new string[16];MzShortNames=new string[16];}
            if(!String.Equals(MzFullNames[slot],name)){MzFullNames[slot]=name;MzShortNames[slot]=name.Length>24?name.Substring(0,24):name;}return MzShortNames[slot];
        }
        // Publish only actors whose current projected ESP emitted geometry/text.
        // The renderer already bounds actor work; do not scan the roster again.
        public static void MzCommitCounts(int people,int bots)
        {
            bool changed=MzCountText==null||people!=MzPeople||bots!=MzBots;
            if(MzPeopleText==null||people!=MzPeople){MzPeople=people;MzPeopleText="Người chơi: "+people.ToString();}
            if(MzBotsText==null||bots!=MzBots){MzBots=bots;MzBotsText="Bot: "+bots.ToString();}
            if(changed)MzCountText=people==0&&bots==0?"Clear":"Người "+people.ToString()+"   ·   Bot "+bots.ToString();
            MzCountValid=true;
        }
        public static void MzResetCounts()
        {
            MzCountValid=false;MzCountText=null;MzPeopleText=null;MzBotsText=null;MzPeople=0;MzBots=0;
        }
        public static void MzEspStyles()
        {
            if(MzEspStyle!=null)return;var style=new GUIStyle();style.fontSize=12;style.wordWrap=false;style.alignment=TextAnchor.MiddleCenter;style.normal.textColor=new Color(1,1,1,1);MzEspStyle=style;
        }
        public static void MzCountHud()
        {
            if(!MzSwitch[10]||!MzCountValid||MzCountText==null)return;
            bool clear=MzPeople==0&&MzBots==0;float width=clear?108:238,x=(Screen.width-width)*.5f,y=MzLineOrigin==0?Screen.height-46:MzLineOrigin==1?Screen.height*.5f-42:12;
            MzTint(0,0,0,.18f);MzFill(x,y+2,width,34,17);MzTint(.045f,.075f,.085f,.9f);MzFill(x,y,width,34,17);
            MzTint(.55f,.92f,.74f,1);MzFill(x+13,y+13,8,8,4);
            if(MzCountStyle==null){var style=new GUIStyle();style.fontSize=17;style.alignment=TextAnchor.MiddleCenter;style.normal.textColor=new Color(1,1,1,1);MzCountStyle=style;}
            MzTint(clear?.63f:.9f,clear?.94f:.95f,clear?.78f:.96f,1);GUI.Label(new Rect(x+22,y,width-30,34),MzUiText(MzCountText),MzCountStyle);

        }
    }
}
