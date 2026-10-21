using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static void NativeR142Checks(string[] args)
    {
        int before=Checks;Player local,target;R11Setup(out local,out target);
        var previous=Get<UnityWebRequest>("MzRequest");if(previous!=null)previous.Dispose();Set("MzRequest",null);Set("MzKeyHash",new string('a',64));
        string origin="https://27.130.147.114",url=origin+"/v9/lease";
        Check(new Uri((string)Run("MzLicenseApi")).GetLeftPart(UriPartial.Authority)==origin&&(string)Run("MzLicenseApi")==url,"exact configured VPS HTTPS endpoint");
        Check((bool)Run("MzValidLicenseUrl",url),"only configured signed endpoint is allowed");
        foreach(string hostile in new[]{url.Replace(origin,"https://other-key.invalid"),url.Replace("https:","http:"),url+"?redirect=1",url+"#fragment",url+"/extra",origin+"/licensesV9.json",url.Replace("27.130.147.114","27.130.147.114.attacker.invalid")})
            Check(!(bool)Run("MzValidLicenseUrl",hostile),"strict origin/path guard rejects "+hostile);
        int sent=UnityWebRequest.Requests.Count;var bad=UnityWebRequest.Get("https://other-key.invalid/key");Run("MzStartRequest",bad,1);
        Check(bad.Disposed&&UnityWebRequest.Requests.Count==sent&&Get<UnityWebRequest>("MzRequest")==null&&Get<float>("MzLeaseDeadline")==0,"off-origin request is disposed before transmission and invalidates lease");
        License(Record(Get<string>("MzDeviceHash")));Check((bool)Run("MzAuthorized"),"signed backend response authorizes configured device");
        Run("MzBeginRead");var redirected=Get<UnityWebRequest>("MzRequest");redirected.Url="https://other-key.invalid/success.json";redirected.responseCode=200;redirected.downloadHandler=new DownloadHandler {text=SignedEnvelope(SignaturePayload(),args[0])};redirected.isDone=true;Run("MzLicensePoll");
        Check(redirected.Disposed&&Get<float>("MzLeaseDeadline")==0,"off-origin final response cannot activate even with signed-looking data");
        Run("MzBeginRead");var forged=Get<UnityWebRequest>("MzRequest");forged.responseCode=200;forged.downloadHandler=new DownloadHandler {text=Record(Get<string>("MzDeviceHash"))};forged.isDone=true;Run("MzLicensePoll");Check(!(bool)Run("MzAuthorized"),"unsigned Firebase record no longer activates client");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r142-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,newBackend=true,strictOrigin=true,rejectReportedRedirect=true,signatureVerification=true,deviceVerified=false}));
        Console.WriteLine("PASS: endpoint regression "+(Checks-before)+" checks; pinned HTTPS route, outgoing/final-origin guards and unsigned response rejection.");
    }
}
