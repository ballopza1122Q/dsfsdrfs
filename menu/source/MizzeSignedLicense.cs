using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using SimpleJSON;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static Type MzBigType;
        public static ConstructorInfo MzBigConstructor;
        public static MethodInfo MzBigPower,MzBigBytes;
        public static object MzRsaModulus,MzRsaExponent;
        public static byte[] MzRsaModulusBytes;
        public static string MzLicenseNonce;
        public static bool MzVerifierReady;
        // Mono.Math.BigInteger(byte[]), ModPow and GetBytes have checked native
        // implementations in the supplied TH mscorlib dump. Resolve lazily so a
        // missing optional crypto primitive fails licensing, not menu startup.
        public static void MzVerifierInit()
        {
            if(MzVerifierReady)return;
            var type=Type.GetType("Mono.Math.BigInteger, mscorlib",false);
            if(type==null)throw new InvalidOperationException("License verifier unavailable");
            var ctor=type.GetConstructor(new Type[]{typeof(byte[])});
            var power=type.GetMethod("ModPow",new Type[]{type,type});var bytes=type.GetMethod("GetBytes",new Type[0]);
            if(ctor==null||power==null||bytes==null)throw new InvalidOperationException("License verifier unavailable");
            byte[] modulus=Convert.FromBase64String(MzLicenseModulus());
            if(modulus.Length!=384||(modulus[0]&128)==0)throw new InvalidOperationException("Invalid pinned key");
            MzRsaModulus=ctor.Invoke(new object[]{modulus});MzRsaExponent=ctor.Invoke(new object[]{new byte[]{1,0,1}});
            MzBigType=type;MzBigConstructor=ctor;MzBigPower=power;MzBigBytes=bytes;MzRsaModulusBytes=modulus;MzVerifierReady=true;
        }
        public static bool MzVerifyLeaseSignature(byte[] message,byte[] signature)
        {
            if(message==null||message.Length==0||message.Length>2048||signature==null||signature.Length!=384)return false;
            MzVerifierInit();
            // RFC 8017: reject s >= n; do not silently reduce an out-of-range signature.
            bool less=false;for(int i=0;i<384;i++){if(signature[i]<MzRsaModulusBytes[i]){less=true;break;}if(signature[i]>MzRsaModulusBytes[i])return false;}if(!less)return false;
            object value=MzBigConstructor.Invoke(new object[]{signature});
            object powered=MzBigPower.Invoke(value,new object[]{MzRsaExponent,MzRsaModulus});
            byte[] raw=(byte[])MzBigBytes.Invoke(powered,new object[0]);if(raw==null||raw.Length>384)return false;
            byte[] block=new byte[384];Array.Copy(raw,0,block,384-raw.Length,raw.Length);
            // Strict EMSA-PKCS1-v1_5 SHA-256 encoding: full FF padding, exact
            // DigestInfo, exactly 32 digest bytes and no trailing data.
            if(block[0]!=0||block[1]!=1)return false;
            int separator=384-52;for(int i=2;i<separator;i++)if(block[i]!=255)return false;if(block[separator]!=0)return false;
            byte[] prefix=Convert.FromBase64String("MDEwDQYJYIZIAWUDBAIBBQAEIA==");
            for(int i=0;i<prefix.Length;i++)if(block[separator+1+i]!=prefix[i])return false;
            byte[] digest=SHA256.Create().ComputeHash(message);int difference=0;
            for(int i=0;i<32;i++)difference|=block[352+i]^digest[i];return difference==0;
        }
        public static void MzCheckSignedLease(string json)
        {
            MzLeaseDeadline=0;
            if(String.IsNullOrEmpty(MzLicenseNonce)||json==null||json.Length>4096){MzReject("Không có yêu cầu cấp phép đang chờ");return;}
            var envelope=JSONNode.Parse(json);if(MzMissing(envelope)||!envelope.IsObject){MzReject("Phản hồi cấp phép không hợp lệ");return;}
            var algorithm=envelope["alg"];var kid=envelope["kid"];var body=envelope["payload"];var signed=envelope["signature"];
            if(MzMissing(algorithm)||!algorithm.IsString||algorithm.Value!="RS256"||MzMissing(kid)||!kid.IsString||kid.Value!=MzLicenseKeyId()||
              MzMissing(body)||!body.IsString||MzMissing(signed)||!signed.IsString){MzReject("Chữ ký máy chủ không hợp lệ");return;}
            byte[] payload=Convert.FromBase64String(body.Value),signature=Convert.FromBase64String(signed.Value);
            if(!MzVerifyLeaseSignature(payload,signature)){MzReject("Chữ ký máy chủ không hợp lệ");return;}
            var record=JSONNode.Parse(Encoding.UTF8.GetString(payload));if(MzMissing(record)||!record.IsObject){MzReject("Giấy phép đã ký sai định dạng");return;}
            var v=record["v"];var audience=record["aud"];var hash=record["keyHash"];var device=record["deviceHash"];var nonce=record["nonce"];
            var issued=record["issuedAt"];var expires=record["expiresAt"];var maximum=record["maxDevices"];
            if(MzMissing(v)||!v.IsNumber||v.AsDouble!=9||MzMissing(audience)||!audience.IsString||audience.Value!="mizze-th-license-v9"||
              MzMissing(hash)||!hash.IsString||hash.Value!=MzKeyHash||MzMissing(device)||!device.IsString||device.Value!=MzDeviceHash||
              MzMissing(nonce)||!nonce.IsString||nonce.Value!=MzLicenseNonce){MzReject("Giấy phép không khớp yêu cầu hoặc thiết bị");return;}
            if(MzMissing(issued)||!issued.IsNumber||MzMissing(expires)||!expires.IsNumber||issued.AsDouble<1||issued.AsDouble>9007199254740991d||
              expires.AsDouble<1||expires.AsDouble>9007199254740991d||Double.IsNaN(issued.AsDouble)||Double.IsNaN(expires.AsDouble)||
              expires.AsDouble<=issued.AsDouble||expires.AsDouble-issued.AsDouble>30000||
              MzMissing(maximum)||!maximum.IsNumber||maximum.AsDouble<1||maximum.AsDouble>50||maximum.AsDouble!=(int)maximum.AsDouble){MzReject("Giới hạn giấy phép đã ký không hợp lệ");return;}
            float elapsed=Time.realtimeSinceStartup-MzRequestStarted;
            float remaining=(float)((expires.AsDouble-issued.AsDouble)/1000)-elapsed;
            if(elapsed<0||remaining<=0||remaining>30){MzReject("Phản hồi cấp phép đã hết thời gian chờ");return;}
            MzLicenseNonce="";MzCanClaim=false;MzDeviceLimit=(int)maximum.AsDouble;
            MzLeaseDeadline=Time.realtimeSinceStartup+remaining;MzNextRefresh=Time.realtimeSinceStartup+10;
            MzLicenseMessage="Đã xác minh chữ ký máy chủ";PlayerPrefs.SetString("Mizze.IFix.CredentialHashPairV10",MzKeyHash);
        }
    }
}
