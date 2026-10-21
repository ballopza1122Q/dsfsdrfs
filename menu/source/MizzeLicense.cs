using System;
using System.Text;
using System.Security.Cryptography;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static string MzKeyHash,MzDeviceHash,MzLicenseMessage;
        public static UnityWebRequest MzRequest;
        public static TouchScreenKeyboard MzKeyboard;
        public static int MzRequestStage,MzDeviceLimit;
        public static float MzRequestStarted,MzLeaseDeadline,MzNextRefresh;
        public static bool MzClaimAttempted,MzCanClaim;
        public static string MzHash(string value){byte[] digest=SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(value));return BitConverter.ToString(digest).Replace("-","").ToLowerInvariant();}
        public static void MzLicenseInit()
        {
            MzLanguageInit();
            string device=SystemInfo.deviceUniqueIdentifier;if(String.IsNullOrEmpty(device)||device=="n/a"){MzLicenseMessage="Không đọc được mã thiết bị";return;}
            MzDeviceHash=MzHash("mizze-ifix-device-v1:"+device);MzKeyHash=PlayerPrefs.GetString("Mizze.IFix.CredentialHashPairV10","");MzLicenseMessage="Nhập Key và mật khẩu do admin cấp";if(MzHex(MzKeyHash)){MzCanClaim=false;MzBeginRead();}
        }
        public static bool MzHex(string s){if(s==null||s.Length!=64)return false;for(int i=0;i<s.Length;i++){char c=s[i];if(!(c>='0'&&c<='9')&&!(c>='a'&&c<='f'))return false;}return true;}
        public static bool MzAuthorized(){return MzLeaseDeadline>0&&Time.realtimeSinceStartup<MzLeaseDeadline;}
        public static string MzLicenseOrigin(){return "https://144.79.172.172";}
        public static string MzKeyUrl(){return MzLicenseApi();}
        public static bool MzValidLicenseUrl(string url){return url==MzLicenseApi();}
        public static void MzStartRequest(UnityWebRequest request,int stage)
        {
            if(request==null||stage!=1||!MzValidLicenseUrl(request.url)){
                if(request!=null)request.Dispose();MzReject("Nguồn xác thực không hợp lệ");return;}
            MzRequest=request;MzRequestStage=1;MzRequestStarted=Time.realtimeSinceStartup;
            request.timeout=6;request.SetRequestHeader("Cache-Control","no-store");request.SendWebRequest();
        }
        public static void MzBeginRead()
        {
            if(MzRequest!=null||!MzHex(MzKeyHash)||!MzHex(MzDeviceHash))return;
            MzLicenseNonce=Guid.NewGuid().ToString("N");
            string body="{\"keyHash\":\""+MzKeyHash+"\",\"deviceHash\":\""+MzDeviceHash+"\",\"nonce\":\""+MzLicenseNonce+"\",\"claim\":"+(MzCanClaim?"true":"false")+"}";
            var request=UnityWebRequest.Put(MzLicenseApi(),Encoding.UTF8.GetBytes(body));request.method="POST";
            request.SetRequestHeader("Content-Type","application/json");MzLicenseMessage="Đang kiểm tra chữ ký máy chủ…";MzStartRequest(request,1);
        }
        public static bool MzMissing(JSONNode n){return System.Object.ReferenceEquals(n,null)||n.IsNull||n.Tag==JSONNodeType.None;}
        public static void MzReject(string message){MzLeaseDeadline=0;MzLicenseNonce="";MzLicenseMessage=message;MzRestoreSilent();MzNextRefresh=Time.realtimeSinceStartup+5;}
        // No unsigned Firebase record compatibility path remains.
        public static void MzCheckRecord(string json,string ignoredDate){MzCheckSignedLease(json);}
        public static void MzLicensePoll()
        {
            try {
                MzCredentialPoll();
                var request=MzRequest;
                if(request!=null&&request.isDone){
                    bool validOrigin=MzValidLicenseUrl(request.url);long code=request.responseCode;
                    string error=request.error;string data=request.downloadHandler==null?null:request.downloadHandler.text;
                    MzRequest=null;request.Dispose();
                    if(!validOrigin){MzReject("Nguồn xác thực không hợp lệ");return;}
                    if(code!=200||error!=null||data==null||data.Length>4096){MzReject(code==429?"Đã vượt giới hạn yêu cầu. Thử lại sau":code==403?"Sai thông tin, hết hạn hoặc đủ số thiết bị":"Không kết nối được API cấp phép HTTPS");return;}
                    MzCheckSignedLease(data);
                }
                if(MzKeyboard==null&&MzRequest==null&&MzHex(MzKeyHash)&&Time.realtimeSinceStartup>=MzNextRefresh)MzBeginRead();
            }catch(Exception){MzLoginClear();if(MzRequest!=null){MzRequest.Dispose();MzRequest=null;}MzReject("Lỗi xác thực khóa. Thử lại");}
        }
        public static void MzKeyPanel(float x,float y)
        {
            float o=MzOpenAnim;bool live=MzAuthorized();float sr=live?.30f:1,sg=live?.95f:.74f,sb=live?.76f:.30f;
            MzTint(.09f,.11f,.14f,.82f*o);MzFill(x,y,354,70,14);MzTint(1,1,1,.05f*o);MzFill(x+14,y,326,1,0);MzTint(sr,sg,sb,o);MzFill(x+5,y+14,3,42,1.5f);
            MzTint(sr,sg,sb,.14f*o);MzFill(x+302,y+17,36,36,18);MzTint(sr,sg,sb,o);MzFill(x+310,y+25,20,20,10);MzTint(.05f,.07f,.09f,o);MzFill(x+314,y+29,12,12,6);
            MzTint(.54f,1,.83f,o);MzText(x+16,y+7,280,"KHÓA MIZZE");MzTint(live?.80f:1,live?1:.86f,live?.92f:.62f,o);MzText(x+16,y+35,280,live?"Đã kích hoạt":"Chưa kích hoạt");
            MzTint(.04f,.05f,.07f,.55f*o);MzFill(x,y+80,354,78,12);MzTint(.79f,.84f,.88f,o);MzMessage(x+12,y+86,330,70,MzLicenseMessage??"Nhập khóa để bật ESP và ngắm");
            MzTint(.30f,.95f,.76f,.16f*o);MzFill(x-2,y+164,358,50,15);MzTint(.13f,.55f,.44f,o);MzFill(x,y+166,354,46,13);MzTint(.70f,1,.90f,.16f*o);MzFill(x+6,y+167,342,17,8.5f);MzTint(.95f,1,.98f,o);MzCenter(x,y+166,354,46,MzRequest!=null?"Đang kiểm tra…":MzLoginStep==2?"Nhập mật khẩu":"Nhập Key");if(MzRequest==null&&MzKeyboard==null&&MzTap(x,y+166,354,46))MzCredentialButton();
            MzTint(1,1,1,.09f*MzOpenAnim);MzFill(x,y+224,354,42,12);MzTint(.08f,.10f,.13f,.92f*MzOpenAnim);MzFill(x+1,y+225,352,40,11);MzTint(.92f,.95f,.98f,MzOpenAnim);MzCenter(x,y+224,354,42,"Kiểm tra lại khóa");if(MzTap(x,y+224,354,42))MzBeginRead();
            MzTint(1,.36f,.45f,.22f*MzOpenAnim);MzFill(x,y+278,354,38,12);MzTint(.15f,.06f,.08f,.94f*MzOpenAnim);MzFill(x+1,y+279,352,36,11);MzTint(1,.73f,.79f,MzOpenAnim);MzCenter(x,y+278,354,38,"Đăng xuất");if(MzTap(x,y+278,354,38)){if(MzRequest!=null){MzRequest.Dispose();MzRequest=null;}MzLoginClear();MzKeyHash="";MzLeaseDeadline=0;PlayerPrefs.DeleteKey("Mizze.IFix.CredentialHashPairV10");MzLicenseMessage="Đã đăng xuất";MzRestoreSilent();}
        }
    }
}
