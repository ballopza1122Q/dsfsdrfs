using System;
using System.IO;
using System.Linq;
using UnityEngine;
unsafe static partial class MizzeRuntimeTest
{
    static bool BadgeVisible()
    {
        var texture=Get<Texture2D>("MzBadgeTexture");
        return GUI.Calls.Any(d=>(texture!=null&&System.Object.ReferenceEquals(d.Texture,texture)&&d.Rect.width>=72&&d.Rect.height>=72)||d.Text=="Mizze");
    }
    static void NativeR143Checks(string[] args)
    {
        int before=Checks;Run("get_UIModelRole");Driver=GameObject.Find("__mizze_driver").Added;
        Frame(EventType.Repaint);var texture=Get<Texture2D>("MzBadgeTexture");
        Check(texture!=null&&texture.Width==1254&&texture.Height==1254,"supplied original JPEG decoded once");
        Check(BadgeVisible(),"image launcher appears without match or active key");
        int created=Texture2D.Created;for(int i=0;i<100;i++)Frame(EventType.Repaint);
        Check(System.Object.ReferenceEquals(texture,Get<Texture2D>("MzBadgeTexture"))&&Texture2D.Created==created,"logo texture cached across frames");
        Set("MzOpen",false);float x=Get<float>("MzBubbleX"),y=Get<float>("MzBubbleY");
        Click(x+44,y+70);Check(Get<bool>("MzOpen"),"lower half of image receives taps");
        Set("MzOpen",false);Frame(EventType.MouseDown,x+44,y+70);Frame(EventType.MouseDrag,x+104,y+110);Frame(EventType.MouseUp,x+104,y+110);
        Check(!Get<bool>("MzOpen")&&Math.Abs(Get<float>("MzBubbleX")-x-60)<.01,"image drag preserves tap distinction");
        var data=(string[])Run("MzGlassData");var hashes=(string[])Run("MzGlassImageHashes");
        Check(data.Length==15&&hashes.Length==15,"all fifteen background frames have expected digests");
        for(int i=0;i<data.Length;i++)Check((string)Run("MzHash",data[i])==hashes[i],"background digest matches "+i);
        int uploads=Texture2D.Uploaded;bool rejected=false;
        try{Run("MzCheckedImage",data[0]+"A",hashes[0]);}catch(Exception){rejected=true;}
        Check(rejected&&Texture2D.Uploaded==uploads,"modified background rejected before native decoding");
        rejected=false;try{Run("MzCheckedImage",data[0],hashes[1]);}catch(Exception){rejected=true;}
        Check(rejected,"reordering signed-frame hashes is detected by client checksum");
        Set("MzBadgeTexture",null);Set("MzBadgeAttempts",0);Set("MzBadgeRetry",0f);Texture2D.FailUpload=true;
        Time.Now=100;Frame(EventType.Repaint);
        Check(Get<Texture2D>("MzBadgeTexture")==null&&BadgeVisible(),"failed image upload retains usable fallback");
        Texture2D.FailUpload=false;Time.Now=106;Frame(EventType.Repaint);
        Check(Get<Texture2D>("MzBadgeTexture")!=null&&BadgeVisible(),"bounded retry recovers image without restarting menu");
        Run("MzDestroyOwned",texture);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",2);
        for(int i=0;i<20;i++)Frame(EventType.Repaint);
        NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r143-logo.png"));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r143-checks.json"),
          new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,originalJpeg=true,cachedTexture=true,backgroundChecks=true,touchAndDrag=true,checksPatchable=true,deviceVerified=false}));
        Console.WriteLine("PASS: R14.3 "+(Checks-before)+" checks; original logo, image touch/drag, cached decode, frame integrity and resource-failure recovery.");
    }
}
