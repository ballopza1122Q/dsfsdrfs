using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
 static void R148Input(string value){var keyboard=Get<TouchScreenKeyboard>("MzKeyboard");Check(keyboard!=null,"credential keyboard is open");keyboard.text=value;keyboard.status=TouchScreenKeyboard.Status.Done;Run("MzCredentialPoll");}
 static void NativeR148CredentialsChecks(string[] args){
  int before=Checks;Player local,target;R11Setup(out local,out target);
  const string key="MZ10-12345678",password="Password-0123456789";
  string expected=BitConverter.ToString(System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes("mizze-pair-v1\n13:MZ10-12345678\n19:Password-0123456789"))).Replace("-","").ToLowerInvariant();
  string hash=(string)Run("MzCredentialHash",key,password);Check(hash==expected,"pair hash matches independent host SHA256 framing");
  Check((string)Run("MzCredentialHash"," "+key.ToLowerInvariant()+" ",password)==hash,"Key normalizes case and surrounding spaces");
  Check((string)Run("MzCredentialHash",key,password.ToLowerInvariant())!=hash,"password case changes the pair hash");
  foreach(var parts in new[]{new[]{key,""},new[]{key,"short"},new[]{"short",password},new[]{key,"has a space 123456"},new[]{key,"mật-khẩu-123456789"}})Check(Run("MzCredentialHash",parts[0],parts[1])==null,"invalid input pair rejected");
  Set("MzRequest",null);Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzChooseLanguage",false);Set("MzTab",2);Set("MzLanguage",0);Set("MzPanelX",120f);Set("MzPanelY",16f);Set("MzLoginStep",0);Frame(EventType.Repaint);
  Check(GUI.Calls.Any(d=>d.Text=="Nhập Key"),"initial button says Nhập Key");int sent=UnityWebRequest.Requests.Count;
  Click(200,316);Check(Get<int>("MzLoginStep")==1&&Get<TouchScreenKeyboard>("MzKeyboard")!=null,"tap opens Key keyboard");
  R148Input(key);Check(Get<int>("MzLoginStep")==2&&Get<TouchScreenKeyboard>("MzKeyboard")==null&&UnityWebRequest.Requests.Count==sent,"Key completion returns to menu without request or automatic second keyboard");
  Frame(EventType.Repaint);Check(GUI.Calls.Any(d=>d.Text=="Nhập mật khẩu"),"same menu button changes to Nhập mật khẩu");
  NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r1410-password-step.png"));
  Click(200,316);Check(Get<int>("MzLoginStep")==2&&Get<TouchScreenKeyboard>("MzKeyboard")!=null,"second menu tap opens password keyboard");R148Input(password);
  var request=Get<UnityWebRequest>("MzRequest");Check(request!=null&&request.Method=="POST"&&request.Body.Contains(hash)&&UnityWebRequest.Requests.Count==sent+1,"password completion immediately sends signed-license request, no ID step");
  Check(Get<int>("MzLoginStep")==0&&Get<string>("MzLoginKey")==null&&Get<string>("MzLoginPassword")==null&&Get<TouchScreenKeyboard>("MzKeyboard")==null,"input references cleared after request");
  Check(!request.Body.Contains(password)&&!request.Body.Contains(key),"request includes hash rather than raw credentials");request.responseCode=403;request.isDone=true;Run("MzLicensePoll");Check(!(bool)Run("MzAuthorized"),"denied pair cannot authorize");
  foreach(int step in new[]{1,2}){Run("MzBeginCredentials");if(step==2){R148Input(key);Run("MzCredentialButton");}var keyboard=Get<TouchScreenKeyboard>("MzKeyboard");keyboard.status=TouchScreenKeyboard.Status.Canceled;Run("MzCredentialPoll");Check(Get<int>("MzLoginStep")==0&&Get<string>("MzLoginKey")==null&&!((bool)Run("MzAuthorized")),"cancel at either step clears partial login");}
  Set("MzKeyHash",hash);License(Record(Get<string>("MzDeviceHash")));Check((bool)Run("MzAuthorized")&&PlayerPrefs.GetString("Mizze.IFix.CredentialHashPairV10","")==hash,"signed pair proof authorizes and caches only compound hash");
  Check(!PlayerPrefs.Saved.Values.Any(value=>value==key||value==password),"raw Key/password not stored by game");
  PlayerPrefs.DeleteKey("Mizze.IFix.CredentialHashPairV10");PlayerPrefs.SetString("Mizze.IFix.CredentialHashV9",new string('f',64));Set("MzRequest",null);Set("MzKeyHash","");Run("MzLicenseInit");Check(Get<string>("MzKeyHash")==""&&Get<UnityWebRequest>("MzRequest")==null,"old triple cache not reused by pair client");
  PlayerPrefs.SetString("Mizze.IFix.CredentialHashPairV10",hash);Run("MzLicenseInit");request=Get<UnityWebRequest>("MzRequest");Check(request!=null&&request.Body.Contains(hash)&&!Get<bool>("MzCanClaim"),"saved pair refresh cannot reclaim removed device");request.Dispose();Set("MzRequest",null);
  Set("MzLanguage",1);Set("MzNextRefresh",100000f);Frame(EventType.Repaint);Check(GUI.Calls.Any(d=>d.Text=="Enter Key"),"English menu starts with Enter Key");Run("MzBeginCredentials");R148Input(key);Frame(EventType.Repaint);Check(GUI.Calls.Any(d=>d.Text=="Enter password"),"English password button translates");
  File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r1410-credentials-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,requiredCredentials=2,menuButtonChanges=true,passwordOpensOnSecondTap=true,noIdStep=true,signedLicenseVerification=true,noLegacyCacheFallback=true,rawCredentialsPersisted=false,deviceVerified=false}));
  Console.WriteLine("PASS: R14.10 "+(Checks-before)+" pair checks; actual Key tap, password button transition/second tap, no ID, hash vector, cancel, cache and signed license.");
 }
}
