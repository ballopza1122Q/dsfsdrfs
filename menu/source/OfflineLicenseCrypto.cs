// Test host only. Production uses the checked Mono.Math implementation in game.
using System;
using System.Numerics;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
public sealed class OfflineMonoBigInteger
{
    readonly BigInteger value;
    public static int ModPowCalls;
    public OfflineMonoBigInteger(byte[] bigEndian){var little=new byte[bigEndian.Length+1];for(int i=0;i<bigEndian.Length;i++)little[i]=bigEndian[bigEndian.Length-1-i];value=new BigInteger(little);}
    OfflineMonoBigInteger(BigInteger n){value=n;}
    public OfflineMonoBigInteger ModPow(OfflineMonoBigInteger exponent,OfflineMonoBigInteger modulus){ModPowCalls++;return new OfflineMonoBigInteger(BigInteger.ModPow(value,exponent.value,modulus.value));}
    public byte[] GetBytes(){var bytes=value.ToByteArray();int length=bytes.Length;while(length>1&&bytes[length-1]==0)length--;var result=new byte[length];for(int i=0;i<length;i++)result[i]=bytes[length-1-i];return result;}
}
public static class OfflineLicenseTypeBridge
{
    public static bool Unavailable;
    public static Type GetType(string name,bool throws){if(name=="Mono.Math.BigInteger, mscorlib")return Unavailable?null:typeof(OfflineMonoBigInteger);return Type.GetType(name,throws);}
}
unsafe static partial class MizzeRuntimeTest
{
    static string CurrentPatchPath;
    // Test-only licensing service double. Production accepts signed proofs only.
    // Independently exercised Node/RTDB tests cover the real service policy.
    static void FixtureLicense(string record,string date)
    {
        if(Get<UnityEngine.Networking.UnityWebRequest>("MzRequest")!=null){Get<UnityEngine.Networking.UnityWebRequest>("MzRequest").Dispose();Set("MzRequest",null);}
        if(String.IsNullOrEmpty(Get<string>("MzKeyHash")))Set("MzKeyHash",new string('b',64));
        Set("MzCanClaim",false);Run("MzBeginRead");var request=Get<UnityEngine.Networking.UnityWebRequest>("MzRequest");
        const long issued=1791633600000L;long expiry=0;int maximum=0;bool grant=false;
        try{
            var value=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(record);
            expiry=Convert.ToInt64(value["expiresAt"]);maximum=Convert.ToInt32(value["maxDevices"]);
            var devices=(Dictionary<string,object>)value["devices"];
            grant=value["enabled"] is bool&&(bool)value["enabled"]&&Convert.ToInt32(value["protocol"])==9&&Convert.ToInt32(value["credentialMode"])==2&&expiry>issued&&maximum>=1&&maximum<=50&&devices.Count<=maximum&&devices.ContainsKey(Get<string>("MzDeviceHash"));
            if(value.ContainsKey("lock")&&value["lock"]!=null){var block=(Dictionary<string,object>)value["lock"];long until=Convert.ToInt64(block["until"]);grant=grant&&until>0&&until<=issued&&block["message"] is string;}
        }catch{grant=false;}
        var payload=new Dictionary<string,object>{{"v",9},{"aud","mizze-th-license-v9"},{"keyHash",Get<string>("MzKeyHash")},{"deviceHash",Get<string>("MzDeviceHash")},{"nonce",Get<string>("MzLicenseNonce")},{"issuedAt",issued},{"expiresAt",Math.Min(expiry,issued+30000)},{"maxDevices",maximum}};
        request.responseCode=grant?200:403;request.Date=date;request.downloadHandler=new UnityEngine.Networking.DownloadHandler {text=grant?SignedEnvelope(payload,CurrentPatchPath):"{\"error\":\"license_denied\"}"};request.isDone=true;Run("MzLicensePoll");
    }
    static RSACryptoServiceProvider LicenseTestSigner;
    static RSACryptoServiceProvider TestSigner(string patchFile)
    {
        if(LicenseTestSigner!=null)return LicenseTestSigner;
        string file=Environment.GetEnvironmentVariable("MIZZE_TEST_SIGNER_FILE");
        if(String.IsNullOrEmpty(file))file=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(patchFile),"../../../.git/mizze-license-signing-v9/test-signer-private.xml"));
        if(!File.Exists(file))throw new InvalidOperationException("Set MIZZE_TEST_SIGNER_FILE to the matching operator-owned test signer. Never put the private file in source/runtime ZIPs.");
        var signer=new RSACryptoServiceProvider();signer.PersistKeyInCsp=false;signer.FromXmlString(File.ReadAllText(file));LicenseTestSigner=signer;return signer;
    }
    static string SignedEnvelope(Dictionary<string,object> payload,string patchFile)
    {
        byte[] message=System.Text.Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(payload));
        byte[] signed=TestSigner(patchFile).SignData(message,CryptoConfig.MapNameToOID("SHA256"));
        return new JavaScriptSerializer().Serialize(new Dictionary<string,object>{{"alg","RS256"},{"kid",(string)Run("MzLicenseKeyId")},{"payload",Convert.ToBase64String(message)},{"signature",Convert.ToBase64String(signed)}});
    }
}
