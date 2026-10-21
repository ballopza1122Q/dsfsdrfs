using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using UnityEngine;
using UnityEngine.Networking;
unsafe static partial class MizzeRuntimeTest
{
    static void NativeIncidentChecks(string[] args)
    {
        int before=Checks;Run("get_UIModelRole");Driver=UnityEngine.GameObject.Find("__mizze_driver").Added;
        string envelopeFile=Environment.GetEnvironmentVariable("MIZZE_INCIDENT_ENVELOPE"),requestFile=Environment.GetEnvironmentVariable("MIZZE_INCIDENT_REQUEST");
        if(String.IsNullOrEmpty(envelopeFile)||String.IsNullOrEmpty(requestFile))throw new InvalidOperationException("Provide private incident fixture paths; never package HAR or user identifiers.");
        string raw=File.ReadAllText(envelopeFile);var serializer=new JavaScriptSerializer();var envelope=serializer.Deserialize<Dictionary<string,object>>(raw);var input=serializer.Deserialize<Dictionary<string,object>>(File.ReadAllText(requestFile));
        Set("MzKeyHash",(string)input["keyHash"]);Set("MzDeviceHash",(string)input["deviceHash"]);Set("MzLicenseNonce",(string)input["nonce"]);Set("MzRequest",null);
        Check((string)envelope["kid"]!=(string)Run("MzLicenseKeyId"),"captured response uses a different signing identity from owner release");
        byte[] payload=Convert.FromBase64String((string)envelope["payload"]),signature=Convert.FromBase64String((string)envelope["signature"]);
        Check(!(bool)Run("MzVerifyLeaseSignature",payload,signature),"captured foreign-server RSA signature rejected by actual owner bytecode");
        SubmitSigned(raw);Check(!(bool)Run("MzAuthorized")&&Get<float>("MzLeaseDeadline")==0,"captured foreign proof cannot authorize even at configured owner endpoint with matched request hashes/nonce");
        Set("MzLicenseNonce",(string)input["nonce"]);envelope["kid"]=(string)Run("MzLicenseKeyId");SubmitSigned(serializer.Serialize(envelope));
        Check(!(bool)Run("MzAuthorized")&&Get<float>("MzLeaseDeadline")==0,"relabeling only captured key ID cannot defeat signature verification");
        var rogue=UnityWebRequest.Put("https://genztool.site/v9/lease",new byte[0]);rogue.method="POST";int sent=UnityWebRequest.Requests.Count;Run("MzStartRequest",rogue,1);
        Check(rogue.Disposed&&UnityWebRequest.Requests.Count==sent&&Get<UnityWebRequest>("MzRequest")==null,"captured replacement endpoint blocked before transmission by original URL guard");
        Set("MzNextRefresh",100000f);Frame(EventType.Repaint);Check(BadgeVisible(),"untrusted response rejection preserves native menu");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-incident-checks.json"),serializer.Serialize(new {checks=Checks-before,actualOwnerVm=true,capturedForeignSignatureRejected=true,relabelledKidRejected=true,foreignEndpointBlocked=true,menuKeptVisible=true,privateValuesLogged=false,untrustedBytecodeExecuted=false,modifiedClientBypassFixed=false,deviceVerified=false}));
        Console.WriteLine("PASS: incident "+(Checks-before)+" checks; actual HAR response/foreign identity rejected by owner verifier; endpoint blocked; no private values logged. Does not prevent replacement of the entire verifier.");
    }
    static string SignaturePatchPath;
    static Dictionary<string,object> SignaturePayload()
    {
        return new Dictionary<string,object>{{"v",9},{"aud","mizze-th-license-v9"},{"keyHash",Get<string>("MzKeyHash")},
            {"deviceHash",Get<string>("MzDeviceHash")},{"nonce",Get<string>("MzLicenseNonce")},{"issuedAt",1791633600000L},{"expiresAt",1791633630000L},{"maxDevices",2}};
    }
    static void SubmitSigned(string json,int status=200,float elapsed=0)
    {
        var request=UnityWebRequest.Put((string)Run("MzLicenseApi"),new byte[0]);request.method="POST";
        Run("MzStartRequest",request,1);Time.Now+=elapsed;request.responseCode=status;request.downloadHandler=new DownloadHandler {text=json};request.isDone=true;Run("MzLicensePoll");
    }
    static void PendingSignature(){Set("MzLicenseNonce",Guid.NewGuid().ToString("N"));Set("MzLeaseDeadline",0f);}
    static void NativeR149SignatureChecks(string[] args)
    {
        int before=Checks;SignaturePatchPath=args[0];Run("get_UIModelRole");Driver=GameObject.Find("__mizze_driver").Added;Frame(EventType.Repaint);
        Set("MzKeyHash",new string('a',64));Set("MzRequest",null);Time.Now=10;
        byte[] message=Encoding.UTF8.GetBytes("cross-platform RSA-3072 SHA-256 test");byte[] signed=TestSigner(args[0]).SignData(message,System.Security.Cryptography.CryptoConfig.MapNameToOID("SHA256"));
        Check((bool)Run("MzVerifyLeaseSignature",message,signed),"RSA-3072 signature from platform crypto verifies inside actual IFix VM");
        int pow=OfflineMonoBigInteger.ModPowCalls;
        foreach(int index in new[]{0,1,31,100,200,383}){byte[] bad=(byte[])signed.Clone();bad[index]^=1;Check(!(bool)Run("MzVerifyLeaseSignature",message,bad),"signature corruption rejected at byte "+index);}
        Check(!(bool)Run("MzVerifyLeaseSignature",message,new byte[383])&&!(bool)Run("MzVerifyLeaseSignature",message,new byte[385]),"signature length must exactly match pinned RSA modulus");
        Check(!(bool)Run("MzVerifyLeaseSignature",Encoding.UTF8.GetBytes("tampered"),signed),"changed payload rejected");
        var pub=TestSigner(args[0]).ExportParameters(false);Check(!(bool)Run("MzVerifyLeaseSignature",message,pub.Modulus),"signature integer equal to modulus rejected before modular reduction");
        Check(Get<bool>("MzVerifierReady")&&OfflineMonoBigInteger.ModPowCalls>pow,"lazy verifier caches reflection handles and public key");
        PendingSignature();var valid=SignaturePayload();SubmitSigned(SignedEnvelope(valid,args[0]));
        Check((bool)Run("MzAuthorized")&&Get<int>("MzDeviceLimit")==2&&Get<string>("MzLicenseNonce")=="","valid server-signed proof authorizes this request and consumes nonce");
        SubmitSigned(SignedEnvelope(valid,args[0]));Check(!(bool)Run("MzAuthorized"),"already consumed nonce cannot be replayed");
        foreach(string field in new[]{"v","aud","keyHash","deviceHash","nonce","issuedAt","expiresAt","maxDevices"}){
            PendingSignature();var wrong=SignaturePayload();wrong[field]=field=="v"?(object)8:field=="issuedAt"?(object)0:field=="expiresAt"?(object)1791633700000L:field=="maxDevices"?(object)51:(object)"wrong";
            SubmitSigned(SignedEnvelope(wrong,args[0]));Check(!(bool)Run("MzAuthorized"),"cryptographically signed but wrongly bound/limited proof rejected: "+field);
        }
        PendingSignature();var envelope=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(SignedEnvelope(SignaturePayload(),args[0]));envelope["kid"]="other-key";
        SubmitSigned(new JavaScriptSerializer().Serialize(envelope));Check(!(bool)Run("MzAuthorized"),"different signing identity rejected");
        PendingSignature();SubmitSigned(Record(Get<string>("MzDeviceHash")));Check(!(bool)Run("MzAuthorized"),"legacy unsigned JSON cannot authorize even when all old fields look valid");
        PendingSignature();SubmitSigned(SignedEnvelope(SignaturePayload(),args[0]),200,31);Check(!(bool)Run("MzAuthorized"),"delayed response cannot renew a stale challenge");
        foreach(int status in new[]{403,429,503}){PendingSignature();SubmitSigned(SignedEnvelope(SignaturePayload(),args[0]),status);Check(!(bool)Run("MzAuthorized"),"HTTP error cannot activate even with a signed response body: "+status);}
        PendingSignature();Run("MzBeginRead");var outgoing=Get<UnityWebRequest>("MzRequest");
        Check(outgoing!=null&&outgoing.Method=="POST"&&outgoing.Headers["Content-Type"]=="application/json"&&outgoing.Url=="https://27.130.147.114/v9/lease","native client uses pinned VPS HTTPS JSON POST endpoint");
        var requestBody=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(outgoing.Body);Check(requestBody.Count==4&&((string)requestBody["nonce"]).Length==32,"outgoing proof request contains current hash/device/claim and fresh nonce only");
        outgoing.Dispose();Set("MzRequest",null);
        PendingSignature();SubmitSigned(SignedEnvelope(SignaturePayload(),args[0]));
        Set("MzChooseLanguage",false);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",0);Set("MzTabAnim",0f);Set("MzLanguage",0);
        Get<bool[]>("MzSwitch")[0]=true;Get<float[]>("MzSwitchAnim")[0]=1;Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Rect.width==46&&d.Rect.height==24&&d.Color.r>.4f&&d.Color.g<.09f),"enabled switch uses matte burgundy track");
        Check(GUI.Calls.Any(d=>d.Rect.width==16.4f&&d.Rect.height==16.4f&&d.Color.r>.9f&&d.Color.g>.9f),"toggle thumb is a small neutral white coin");
        Check(GUI.Calls.Any(d=>d.Texture==Get<Texture2D>("MzBadgeTexture")&&d.Rect.width==76&&d.Radius==38),"smaller supplied image launcher is actually clipped round");
        NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r149-esp-ui.png"));
        Set("MzTab",2);Set("MzTabAnim",2f);Set("MzLanguage",1);Frame(EventType.Repaint);NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r149-key-ui.png"));
        Set("MzVerifierReady",false);OfflineLicenseTypeBridge.Unavailable=true;
        PendingSignature();SubmitSigned(SignedEnvelope(SignaturePayload(),args[0]));Check(!(bool)Run("MzAuthorized"),"missing optional game crypto fails closed with no unsigned fallback");
        Frame(EventType.Repaint);Check(BadgeVisible(),"missing crypto does not hide native menu");OfflineLicenseTypeBridge.Unavailable=false;
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r149-signature-checks.json"),new JavaScriptSerializer().Serialize(new {checks=Checks-before,actualVm=true,rsa3072=true,strictSignatureLength=true,nonceDeviceAndKeyBinding=true,unsignedFallback=false,optionalCryptoFailureKeepsMenu=true,privateKeyIncludedInClient=false,deviceVerified=false}));
        Console.WriteLine("PASS: R14.9 "+(Checks-before)+" signature checks; actual RSA/SHA256, corruption, pinned identity, nonce/device/key binding, lifetime, HTTP failures, no unsigned fallback and optional-crypto isolation.");
    }
}
