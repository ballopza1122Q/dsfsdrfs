using System;
using System.Collections;
using UnityEngine;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static int MzLanguage;
        public static bool MzChooseLanguage;
        public static Hashtable MzTranslations;
        public static void MzLanguageInit()
        {
            MzEmbeddedRightsNotice=MzRightsNotice();MzEmbeddedRightsContextNotice=MzRightsContextNotice();
            string saved=PlayerPrefs.GetString("Mizze.Language","");MzLanguage=saved=="en"?1:0;
            MzChooseLanguage=saved!="en"&&saved!="vi";if(MzChooseLanguage)MzOpen=true;
        }
        public static void MzSetLanguage(int language)
        {
            MzLanguage=language==1?1:0;MzChooseLanguage=false;PlayerPrefs.SetString("Mizze.Language",MzLanguage==1?"en":"vi");
            MzChanceLabel=null;MzResetCounts();MzNextEsp=0;MzTab=MzAuthorized()?0:2;
        }
        public static string MzUiText(string text)
        {
            if(MzLanguage!=1||text==null)return text;if(MzTranslations==null)MzTranslationInit();
            var translated=MzTranslations[text] as string;if(translated!=null)return translated;
            string value=text;
            if(text.StartsWith("Silent · Đầu "))value=text.Replace("Đầu ","Head ").Replace("Thân ","Body ");
            else if(text.StartsWith("Kích cỡ FOV  "))value=text.Replace("Kích cỡ FOV", "FOV size");
            else if(text.StartsWith("Tầm ESP  "))value=text.Replace("Tầm ESP", "ESP range");
            else if(text.StartsWith("Tầm ngắm  "))value=text.Replace("Tầm ngắm", "Aim range");
            else if(text.StartsWith("Người chơi: "))value=text.Replace("Người chơi: ","Players: ");
            else if(text.StartsWith("Người "))value=text.Replace("Người ","Players ");
            else if(text.EndsWith("Màu đã chọn"))value=text.Replace("Màu đã chọn","Selected color");
            if(value!=text&&MzTranslations.Count<256)MzTranslations[text]=value;return value;
        }
        public static void MzLanguagePanel(float x,float y)
        {
            MzTint(.94f,.98f,1,MzOpenAnim);MzCenter(x,y+18,354,38,"Chọn ngôn ngữ / Language");
            MzTint(.64f,.76f,.80f,MzOpenAnim);MzCenter(x,y+60,354,32,"Mizze X Ceo");
            MzTint(.12f,.39f,.33f,.95f*MzOpenAnim);MzFill(x,y+120,354,54,17);MzTint(.82f,1,.94f,MzOpenAnim);MzCenter(x,y+120,354,54,"Tiếng Việt");
            if(MzTap(x,y+120,354,54))MzSetLanguage(0);
            MzTint(.10f,.25f,.40f,.95f*MzOpenAnim);MzFill(x,y+190,354,54,17);MzTint(.86f,.95f,1,MzOpenAnim);MzCenter(x,y+190,354,54,"English");
            if(MzTap(x,y+190,354,54))MzSetLanguage(1);
        }
        public static void MzTranslationInit()
        {
            MzTranslations=new Hashtable(128);
            // Values are populated by the source-owned translation table.
            MzTranslationRows();
        }
    }
}
