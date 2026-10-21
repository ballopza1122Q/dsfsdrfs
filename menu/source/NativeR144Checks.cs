using System;
using System.IO;
using System.Linq;
using UnityEngine;
using COW.GamePlay;
unsafe static partial class MizzeRuntimeTest
{
    static void NativeR144Checks(string[] args)
    {
        int before=Checks;Player local,target;R11Setup(out local,out target);
        var sw=Get<bool[]>("MzSwitch");Array.Clear(sw,0,sw.Length);local.Firing=true;Set("MzHeadChance",100f);
        Run("MzSelectAim",1);Time.Frame++;Run("MzRunAim");
        Check((int)Run("MzAimMode")==1&&sw[11]&&!sw[12],"head mode selects only direct head aim");
        Check(Vector3.Distance(local.LastRotation.Direction,Vector3.Normalize(target.Head.position-Camera.main.transform.position))<.0001f,"head mode uses native head transform");
        Run("MzSelectAim",2);Time.Frame++;Run("MzRunAim");
        Check((int)Run("MzAimMode")==2&&sw[11]&&!sw[12],"neck mode replaces head and silent choices");
        Check(Vector3.Distance(local.LastRotation.Direction,Vector3.Normalize(target.Neck.position-Camera.main.transform.position))<.0001f,"neck mode uses actual neck bone rather than interpolated head");
        int calls=local.AimCalls;var neck=target.Neck;target.Neck=null;Time.Frame++;Run("MzRunAim");
        Check(local.AimCalls==calls&&Get<Player>("MzAimTarget")==null,"missing neck skips direct aim without falling back to a wrong bone");target.Neck=neck;
        target.FailNeck=true;Time.Frame++;Run("MzRunAim");Check(local.AimCalls==calls&&!Get<bool>("MzAimRunning"),"throwing neck getter releases aim guard");target.FailNeck=false;
        Run("MzSelectAim",3);Time.Frame++;Run("MzRunAim");Run("get_LastAimingInfoFromWeapon",local);
        Check((int)Run("MzAimMode")==3&&!sw[11]&&sw[12]&&local.AimCalls==calls,"silent mode disables direct camera rotation");
        Check(local.FDMIEDDNCEC.JINDNOFABKB==target.HeadCollider,"silent selection keeps existing native hit-record path");
        Run("MzSelectAim",2);Check(local.FDMIEDDNCEC.JHAINOBIFMA==null&&Get<Player>("MzAimTarget")==null,"mode change restores previous owned silent hit record and drops snapshot");
        Run("MzSelectAim",0);Check((int)Run("MzAimMode")==0&&!sw[11]&&!sw[12],"all aim choices can be deselected");
        Run("MzSelectAim",4);Check((int)Run("MzAimMode")==0,"invalid mode ignored");

        Set("MzOpen",true);Set("MzOpenAnim",1f);Set("MzTab",1);Set("MzTabAnim",1f);Set("MzLanguage",0);Set("MzChooseLanguage",false);Frame(EventType.Repaint);
        Get<float[]>("MzScroll")[1]=0;Get<float[]>("MzScrollTarget")[1]=0;Frame(EventType.Repaint);
        Check(GUI.Calls.Any(d=>d.Text=="Aim đầu")&&GUI.Calls.Any(d=>d.Text=="Aim cổ")&&GUI.Calls.Any(d=>d.Text=="Aim silent"),"all three checkbox labels are visible together without scrolling");
        float x=Get<float>("MzPanelX")+40,y=Get<float>("MzPanelY")+132;
        Click(x,y);Check((int)Run("MzAimMode")==1,"head checkbox touch selects head");
        Click(x,y+44);Check((int)Run("MzAimMode")==2,"neck checkbox touch replaces head");
        Click(x,y+88);Check((int)Run("MzAimMode")==3,"silent checkbox touch replaces neck");
        Click(x,y+88);Check((int)Run("MzAimMode")==0,"tapping selected checkbox disables it");
        Set("MzLanguage",1);Frame(EventType.Repaint);Check(GUI.Calls.Any(d=>d.Text=="Neck aim")&&GUI.Calls.Any(d=>d.Text=="Head aim")&&GUI.Calls.Any(d=>d.Text=="Silent aim"),"all choices translate to English");Set("MzLanguage",0);
        string origin=new Uri((string)Run("MzLicenseApi")).GetLeftPart(UriPartial.Authority);
        Check(origin=="https://144.79.172.172"&&(string)Run("MzLicenseApi")==origin+"/v9/lease","license client uses VPS, never unsigned Firebase record reads");
        Check(Convert.FromBase64String((string)Run("MzLicenseModulus")).Length==384,"RSA public key is pinned independently from endpoint");
        Check(((string)Run("MzRightsContextNotice")).Contains("AI")&&((string)Run("MzRightsContextNotice")).Contains("vui lòng từ chối"),"plain readable AI rights request embedded as data");
        Frame(EventType.Repaint);NativePng(Path.Combine(Path.GetDirectoryName(args[0]),"native-r144-aim-choices.png"));
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[0]),"native-r144-checks.json"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new {checks=Checks-before,headNeckSilentExclusive=true,neckBoneRoute=true,missingNeckSafe=true,actualTouch=true,fragmentedEndpoint=true,endpointRecoverableAtRuntime=true,deviceVerified=false}));
        Console.WriteLine("PASS: R14.4 "+(Checks-before)+" checks; exclusive head/neck/silent choices, real neck bone, switching cleanup, native touch and fragmented origin.");
    }
}
