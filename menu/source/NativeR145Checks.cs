using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static string R145Hex(byte[] data){return BitConverter.ToString(data).Replace("-","").ToLowerInvariant();}
    static void NativeR145Checks(string[] args)
    {
        int before=Checks;Player local,target;R11Setup(out local,out target);
        byte[] key=new byte[32],nonce=new byte[12],block=new byte[64];
        for(int i=0;i<32;i++)key[i]=(byte)i;nonce[3]=9;nonce[7]=0x4a;
        Run("MzVaultCrypt",block,0,64,key,0,nonce,1);
        Check(R145Hex(block)=="10f1e7e4d13b5915500fdd1fa32071c4c7d1f4c733c068030422aa9ac3d46c4ed2826446079faa0914c2d705d98b02a2b5129cd1de164eb9cbd083e8a2503c4e","ChaCha20 matches RFC 8439 block vector inside actual IFix VM");
        Run("MzVaultCrypt",block,0,64,key,0,nonce,1);Check(block.All(b=>b==0),"same ChaCha20 stream restores all original bytes");
        byte[] macKey=new byte[32];for(int i=0;i<20;i++)macKey[i]=0x0b;byte[] message=Encoding.ASCII.GetBytes("Hi There");
        var tag=(byte[])Run("MzVaultMac",macKey,0,message,message.Length);
        Check(R145Hex(tag)=="b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7","HMAC-SHA256 matches RFC 4231 vector");
        foreach(int length in new[]{0,1,63,64,65,255,4097}){
            byte[] payload=new byte[length];for(int i=0;i<length;i++)payload[i]=(byte)(i*73);
            byte[] actual=(byte[])Run("MzVaultMac",key,0,payload,length),expected;
            using(var h=new HMACSHA256(key))expected=h.ComputeHash(payload);
            Check(actual.SequenceEqual(expected),"managed MAC agrees with platform HMAC at length "+length);
        }
        string[] values=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<string[]>(
            File.ReadAllText(Path.Combine(Path.GetDirectoryName(args[0]),"../build/vault-test-vectors.json")));
        var encoded=Get<string[]>("MzVaultEncoded");var cache=Get<string[]>("MzVaultCache");
        Check(!Fields.ContainsKey("MzVaultSeeds")&&Get<byte[]>("MzVaultKeys").Length==64,"old adjacent XOR seed table removed and separated cipher/MAC keys reconstructed");
        int slot=Array.FindIndex(encoded,s=>s!=null&&Convert.FromBase64String(s).Length>50);
        Check(slot>=0,"uncached encrypted entry available for tamper tests");string original=encoded[slot];
        foreach(int position in new[]{0,1,5,17,Convert.FromBase64String(original).Length-1}){
            byte[] bad=Convert.FromBase64String(original);bad[position]^=0x40;bool rejected=false;
            try{Run("MzVaultOpen",slot,Convert.ToBase64String(bad));}catch(Exception){rejected=true;}
            Check(rejected&&cache[slot]==null,"modified version/id/nonce/ciphertext/tag rejected before cache at byte "+position);
        }
        bool wrongId=false;try{Run("MzVaultOpen",(slot+1)%encoded.Length,original);}catch(Exception){wrongId=true;}
        Check(wrongId,"entry substitution between call sites rejected by authenticated identifier");
        byte[] changed=Convert.FromBase64String(original);changed[changed.Length-1]^=1;encoded[slot]=Convert.ToBase64String(changed);
        int decoded=Get<int>("MzVaultDecodeCount");bool failed=false;try{Run("MzVaultGet",slot);}catch(Exception){failed=true;}
        Check(failed&&cache[slot]==null&&Get<int>("MzVaultDecodeCount")==decoded,"failed authentication never counts or caches a decoded string");encoded[slot]=original;
        Check((string)Run("MzVaultGet",slot)==values[slot],"restoring the original authenticated record recovers correctly");
        var timer=Stopwatch.StartNew();
        for(int i=0;i<values.Length;i++)Check((string)Run("MzVaultGet",i)==values[i],"exact original UTF-8 value recovered "+i);
        timer.Stop();long warmup=timer.ElapsedMilliseconds;
        decoded=Get<int>("MzVaultDecodeCount");for(int repeat=0;repeat<10;repeat++)for(int i=0;i<values.Length;i++)Run("MzVaultGet",i);
        Check(Get<int>("MzVaultDecodeCount")==decoded&&encoded.All(v=>v==null),"all cached calls skip decrypt/MAC and release consumed ciphertext slots");
        Check(values.Length==cache.Length&&values.Length==decoded,"one successful decode per unique protected string");
        Check(new Uri((string)Run("MzLicenseApi")).GetLeftPart(UriPartial.Authority)=="https://27.130.147.114","VPS license origin behavior preserved alongside independent server proof");
        Frame(EventType.Repaint);Check(BadgeVisible(),"native image launcher remains visible after warming protected strings");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r145-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,strings=values.Length,chacha20Rfc8439=true,hmacSha256Rfc4231=true,tamperingRejected=true,cacheOnce=true,warmupMilliseconds=warmup,embeddedKeysRecoverable=true,deviceVerified=false}));
        Console.WriteLine("PASS: R14.5 "+(Checks-before)+" checks; RFC cipher/MAC vectors, all "+values.Length+" original strings, tamper rejection and decode-once cache; cold bulk warmup "+warmup+" ms on test PC.");
    }
}
