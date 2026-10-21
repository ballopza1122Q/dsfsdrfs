// Managed runtime for the generated vault. Not a secret-storage boundary:
// the key shares and verifier ship in the client and can be recovered or patched.
using System;
using System.Text;
using System.Security.Cryptography;
namespace COW.GamePlay { public partial class SceneEditBoxSelectTool {
    public static string[] MzVaultEncoded,MzVaultCache;
    public static byte[] MzVaultKeys;
    public static int MzVaultDecodeCount;
    public static uint MzVaultWord(byte[] b,int p){return (uint)b[p]|((uint)b[p+1]<<8)|((uint)b[p+2]<<16)|((uint)b[p+3]<<24);}
    public static uint MzVaultRotate(uint v,int bits){return (v<<bits)|(v>>(32-bits));}
    public static void MzVaultQuarter(uint[] x,int a,int b,int c,int d)
    {
        unchecked {
            x[a]+=x[b];x[d]=MzVaultRotate(x[d]^x[a],16);
            x[c]+=x[d];x[b]=MzVaultRotate(x[b]^x[c],12);
            x[a]+=x[b];x[d]=MzVaultRotate(x[d]^x[a],8);
            x[c]+=x[d];x[b]=MzVaultRotate(x[b]^x[c],7);
        }
    }
    // ChaCha20 block/counter layout from RFC 8439. Called only on a cache miss.
    public static void MzVaultCrypt(byte[] data,int offset,int length,byte[] key,int keyOffset,byte[] nonce,int firstCounter)
    {
        if(data==null||key==null||nonce==null||offset<0||length<0||offset>data.Length-length||keyOffset<0||keyOffset>key.Length-32||nonce.Length!=12||firstCounter<1)throw new InvalidOperationException("Vault input invalid");
        uint[] state=new uint[16],work=new uint[16];
        state[0]=0x61707865;state[1]=0x3320646e;state[2]=0x79622d32;state[3]=0x6b206574;
        for(int i=0;i<8;i++)state[4+i]=MzVaultWord(key,keyOffset+i*4);
        state[12]=(uint)firstCounter;state[13]=MzVaultWord(nonce,0);state[14]=MzVaultWord(nonce,4);state[15]=MzVaultWord(nonce,8);
        for(int start=0;start<length;start+=64){
            for(int i=0;i<16;i++)work[i]=state[i];
            for(int round=0;round<10;round++){
                MzVaultQuarter(work,0,4,8,12);MzVaultQuarter(work,1,5,9,13);MzVaultQuarter(work,2,6,10,14);MzVaultQuarter(work,3,7,11,15);
                MzVaultQuarter(work,0,5,10,15);MzVaultQuarter(work,1,6,11,12);MzVaultQuarter(work,2,7,8,13);MzVaultQuarter(work,3,4,9,14);
            }
            int count=length-start;if(count>64)count=64;
            for(int i=0;i<count;i++){uint word=unchecked(work[i/4]+state[i/4]);data[offset+start+i]=(byte)(data[offset+start+i]^(byte)(word>>((i%4)*8)));}
            state[12]++;
        }
        for(int i=0;i<16;i++){state[i]=0;work[i]=0;}
    }
    // Encrypt-then-MAC: HMAC-SHA256, with a separate 32-byte MAC key.
    // Reuses existing SHA256.Create/ComputeHash bindings; no new native crypto API.
    public static byte[] MzVaultMac(byte[] key,int keyOffset,byte[] data,int length)
    {
        if(key==null||data==null||keyOffset<0||keyOffset>key.Length-32||length<0||length>data.Length)throw new InvalidOperationException("Vault input invalid");
        byte[] inner=new byte[64+length],outer=new byte[96];
        for(int i=0;i<64;i++){byte k=i<32?key[keyOffset+i]:(byte)0;inner[i]=(byte)(k^0x36);outer[i]=(byte)(k^0x5c);}
        for(int i=0;i<length;i++)inner[64+i]=data[i];
        var hash=SHA256.Create();byte[] digest=hash.ComputeHash(inner);
        for(int i=0;i<32;i++)outer[64+i]=digest[i];
        byte[] result=hash.ComputeHash(outer);
        for(int i=0;i<inner.Length;i++)inner[i]=0;
        for(int i=0;i<96;i++)outer[i]=0;
        for(int i=0;i<32;i++)digest[i]=0;
        return result;
    }
    public static string MzVaultOpen(int id,string encoded)
    {
        if(encoded==null||encoded.Length>65536)throw new InvalidOperationException("Vault integrity check failed");
        byte[] data=Convert.FromBase64String(encoded);
        if(data.Length<49||data.Length>32768||data[0]!=1||MzVaultWord(data,1)!=(uint)id)throw new InvalidOperationException("Vault integrity check failed");
        int signedLength=data.Length-32;byte[] expected=MzVaultMac(MzVaultKeys,32,data,signedLength);int mismatch=0;
        for(int i=0;i<32;i++)mismatch|=expected[i]^data[signedLength+i];
        if(mismatch!=0)throw new InvalidOperationException("Vault integrity check failed");
        byte[] nonce=new byte[12];for(int i=0;i<12;i++)nonce[i]=data[5+i];
        int count=data.Length-49;MzVaultCrypt(data,17,count,MzVaultKeys,0,nonce,1);
        byte[] plain=new byte[count];for(int i=0;i<count;i++)plain[i]=data[17+i];
        string value=Encoding.UTF8.GetString(plain);
        for(int i=0;i<plain.Length;i++)plain[i]=0;
        for(int i=0;i<data.Length;i++)data[i]=0;
        return value;
    }
    public static string MzVaultGet(int id)
    {
        if(MzVaultEncoded==null)MzVaultInit();
        if(id<0||id>=MzVaultCache.Length)throw new InvalidOperationException("Vault index invalid");
        string cached=MzVaultCache[id];if(cached!=null)return cached;
        cached=MzVaultOpen(id,MzVaultEncoded[id]);MzVaultCache[id]=cached;MzVaultEncoded[id]=null;MzVaultDecodeCount++;return cached;
    }
}}
