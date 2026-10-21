using System;
using UnityEngine;
using UnityEngine.Networking;
namespace COW.GamePlay
{
    public partial class SceneEditBoxSelectTool
    {
        public static string MzLoginKey,MzLoginPassword;
        public static int MzLoginStep;
        // High-entropy Key/password pair. License responses still require server signatures.
        public static bool MzCredentialText(string value,int minimum,int maximum)
        {
            if(value==null||value.Length<minimum||value.Length>maximum)return false;
            for(int i=0;i<value.Length;i++)if(value[i]<33||value[i]>126)return false;
            return true;
        }
        public static string MzCredentialHash(string key,string password)
        {
            if(key==null)return null;key=key.Trim().ToUpperInvariant();
            if(!MzCredentialText(key,8,128)||!MzCredentialText(password,16,128)||key==password)return null;
            return MzHash("mizze-pair-v1\n"+key.Length+":"+key+"\n"+password.Length+":"+password);
        }
        public static void MzLoginClear()
        {
            MzLoginKey=null;MzLoginPassword=null;MzLoginStep=0;MzKeyboard=null;
        }
        public static void MzBeginCredentials()
        {
            if(MzRequest!=null){MzRequest.Dispose();MzRequest=null;}
            MzKeyHash="";MzLeaseDeadline=0;MzRestoreSilent();MzLoginClear();
            PlayerPrefs.DeleteKey("Mizze.IFix.CredentialHashPairV10");
            MzLoginStep=1;MzLicenseMessage="Bước 1/2 · Nhập Key do admin cấp";
            MzKeyboard=TouchScreenKeyboard.Open("",TouchScreenKeyboardType.ASCIICapable,false,false,true,false,MzUiText("Key do admin cấp"),128);
        }
        public static void MzCredentialButton()
        {
            if(MzLoginStep==2){MzKeyboard=TouchScreenKeyboard.Open("",TouchScreenKeyboardType.ASCIICapable,false,false,true,false,MzUiText("Mật khẩu do admin cấp"),128);return;}
            MzBeginCredentials();
        }
        public static void MzAcceptCredential(string value)
        {
            if(MzLoginStep==1)
            {
                string key=value==null?null:value.Trim().ToUpperInvariant();
                if(!MzCredentialText(key,8,128)){MzLoginClear();MzReject("Key phải có 8–128 ký tự ASCII, không có khoảng trắng");return;}
                MzLoginKey=key;MzLoginStep=2;MzLicenseMessage="Bước 2/2 · Nhập mật khẩu do admin cấp";
                MzKeyboard=null;return;
            }
            if(MzLoginStep!=2){MzLoginClear();MzReject("Cần nhập đủ Key và mật khẩu");return;}
            if(!MzCredentialText(value,16,128)){MzLoginClear();MzReject("Mật khẩu phải có 16–128 ký tự ASCII, phân biệt hoa thường");return;}
            string hash=MzCredentialHash(MzLoginKey,value);MzLoginClear();
            if(hash==null){MzReject("Key hoặc mật khẩu không hợp lệ");return;}
            MzKeyHash=hash;MzClaimAttempted=false;MzCanClaim=true;MzBeginRead();
        }
        public static void MzCredentialPoll()
        {
            var keyboard=MzKeyboard;if(keyboard==null)return;
            var status=keyboard.status;
            if(status==TouchScreenKeyboard.Status.Done){string value=keyboard.text;MzKeyboard=null;MzAcceptCredential(value);}
            else if(status==TouchScreenKeyboard.Status.Canceled||status==TouchScreenKeyboard.Status.LostFocus)
            {MzLoginClear();MzReject("Đã hủy nhập thông tin. Nhập lại đủ 2 mục");}
        }
    }
}
