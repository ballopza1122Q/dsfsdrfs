using System;
using System.IO;
using System.Linq;
using UnityEngine;
unsafe static partial class MizzeRuntimeTest
{
    static void NativeR14Checks(string[] args)
    {
        int before=Checks;PlayerPrefs.Saved.Remove("Mizze.Language");Run("get_UIModelRole");Driver=GameObject.Find("__mizze_driver").Added;
        Check(Get<bool>("MzChooseLanguage")&&Get<bool>("MzOpen"),"first launch opens language chooser without key or match");
        for(int i=0;i<16;i++)Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Text=="Tiếng Việt")&&GUI.Calls.Any(d=>d.Text=="English"),"both language choices render in native C#");
        NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r14-language.png"));
        Click(Get<float>("MzPanelX")+190,Get<float>("MzPanelY")+316);
        Check(Get<int>("MzLanguage")==1&&!Get<bool>("MzChooseLanguage")&&PlayerPrefs.GetString("Mizze.Language","")=="en","English selection receives actual touch and persists");
        Frame(EventType.Repaint);Check(GUI.Calls.Any(d=>d.Text=="MIZZE KEY")&&GUI.Calls.Any(d=>d.Text=="Enter Key"),"English key UI uses native translated labels");
        Check((string)Run("MzUiText","Silent · Đầu 40% · Thân 60% ")=="Silent · Head 40% · Body 60% ","dynamic head/body percentages translate correctly");
        Check((string)Run("MzUiText","Người 2   ·   Bot 4")=="Players 2   ·   Bot 4","counter translates counts without changing values");
        Check((string)Run("MzUiText","Người chơi: 3")=="Players: 3","full player label translates before shorter prefix");
        Check((string)Run("MzUiText","user-generated-name")=="user-generated-name","unknown user text is preserved");
        int seedCount=Get<string[]>("MzVaultCache").Length;var cache=Get<string[]>("MzVaultCache");
        for(int i=0;i<seedCount;i++)Check(Run("MzVaultGet",i) is string,"protected UTF-8 string decodes in actual IFix VM "+i);
        int warmed=Get<string[]>("MzVaultEncoded").Count(s=>s!=null);for(int i=0;i<seedCount;i++)Run("MzVaultGet",i);
        Check(warmed==0&&System.Object.ReferenceEquals(cache,Get<string[]>("MzVaultCache")),"all decoded strings retain a fixed cache and release encoded slots");
        Check(((string)Run("MzLicenseApi")).StartsWith("https://")&&((string)Run("MzLicenseApi"))=="https://144.79.172.172/v9/lease","pinned signed license endpoint still decodes correctly");
        Set("MzLanguage",0);Run("MzLanguageInit");Check(Get<int>("MzLanguage")==1&&!Get<bool>("MzChooseLanguage"),"saved English reloads without another prompt");
        Click(Get<float>("MzPanelX")+184,Get<float>("MzPanelY")+49);Check(Get<bool>("MzChooseLanguage"),"header language control reopens chooser");
        Click(Get<float>("MzPanelX")+190,Get<float>("MzPanelY")+247);
        Check(Get<int>("MzLanguage")==0&&PlayerPrefs.GetString("Mizze.Language","")=="vi"&&!Get<bool>("MzChooseLanguage"),"Vietnamese can be selected again using actual touch");
        Frame(EventType.Repaint);Check(GUI.Calls.Any(d=>d.Text=="KHÓA MIZZE")&&GUI.Calls.Any(d=>d.Text=="Nhập Key"),"Vietnamese accents and original key UI restored");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r14-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,strings=seedCount,languages=2,firstRunChooser=true,persistedChoice=true,keyProtocolUnchanged=true,deviceVerified=false}));
        Console.WriteLine("PASS: R14 "+(Checks-before)+" checks; protected strings execute in VM, native language chooser/touch/persistence and English/Vietnamese key UI.");
    }
}
