using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Run EungyeTaxi.exe -unifiedModeCheck. Uses a fresh temporary campaign only.
public sealed class UnifiedModeProbe : MonoBehaviour {
    public static bool Enabled {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-unifiedModeCheck")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-brightUiCheck")>=0;}}
    static readonly string campaignPath=Path.Combine(Path.GetTempPath(),"taxi-unified-"+Guid.NewGuid().ToString("N"),"campaign.json");
    public static string CampaignPath {get{return campaignPath;}}
    bool ok=true,done;float activeSeconds,lastRealtime;string phase="initial menu";
    readonly Dictionary<string,byte[]> originals=new Dictionary<string,byte[]>();
    string actualDirectory;
    void Check(bool value,string label){ok&=value;Debug.Log("UNIFIED_MODE "+(value?"PASS ":"FAIL ")+label);}
    void Awake(){DontDestroyOnLoad(gameObject);lastRealtime=Time.realtimeSinceStartup;}
    void Update(){
        float now=Time.realtimeSinceStartup,delta=Mathf.Max(0,now-lastRealtime);lastRealtime=now;
        // A sleeping/suspended host can add hours between two frames. Keep a
        // bounded allowance for slow startup frames, and report every excluded gap.
        if(delta>30)Debug.LogWarning("UNIFIED_MODE clock_gap_seconds="+delta+" counted_seconds=30 phase="+phase);
        activeSeconds+=Mathf.Min(delta,30);
        if(!done&&activeSeconds>600){Debug.LogError("UNIFIED_MODE timeout active_seconds="+activeSeconds+" phase="+phase+" mode="+UnifiedModeBootstrap.Selected);Application.Quit(2);done=true;}
    }
    DriveSession Human(){foreach(var s in FindObjectsOfType<DriveSession>())if(!s.IsSimulationBot)return s;return null;}
    IEnumerator Ready(){phase="waiting for gameplay "+UnifiedModeBootstrap.Selected;Debug.Log("UNIFIED_MODE_STAGE "+phase);while(!Human()||!Human().StartupReady||!Human().business||Human().business.IsRestoring)yield return null;yield return null;yield return null;}
    IEnumerator Menu(){phase="waiting for unloaded mode menu";Debug.Log("UNIFIED_MODE_STAGE "+phase);while(!FindObjectOfType<UnifiedModeBootstrap>()||UnifiedModeBootstrap.Selected!=UnifiedModeBootstrap.Mode.Menu||Human())yield return null;yield return null;}
    static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    IEnumerator Start(){
        var run=Run();
        while(!done){object next;try{if(!run.MoveNext())yield break;next=run.Current;}catch(Exception ex){done=true;Debug.LogError("UNIFIED_MODE_EXCEPTION "+ex);Application.Quit(2);yield break;}yield return next;}
    }
    IEnumerator Capture(string name){
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-unifiedModeCaptureOutput");
        if(i<0||i+1>=args.Length||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;
        Directory.CreateDirectory(args[i+1]);phase="capture "+name;
        bool captured=false;string error="";
        var capture=StartCoroutine(CaptureFrame(Path.Combine(args[i+1],name+".png"),(failure)=>{error=failure;captured=true;}));
        float start=activeSeconds;
        while(!captured&&activeSeconds-start<10)yield return null;
        if(!captured){StopCoroutine(capture);Check(false,"capture timeout "+name+"; rendering may be suspended/minimized");}
        else Check(error.Length==0,"capture "+name+(error.Length==0?"":" error="+error));
    }
    IEnumerator CaptureFrame(string path,Action<string> complete){
        // Rendering can stop while the process is still updating. The caller
        // bounds this coroutine so an unavailable screenshot cannot hang the run.
        yield return new WaitForEndOfFrame();
        Texture2D image=null;string error="";
        try{image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(path,image.EncodeToPNG());}
        catch(Exception ex){error=ex.Message;}
        finally{if(image)Destroy(image);}
        complete(error);
    }
    IEnumerator Run(){
        actualDirectory=CampaignSaveLocation.DirectoryFor(Path.GetDirectoryName(Application.persistentDataPath));
        if(Directory.Exists(actualDirectory))foreach(var path in Directory.GetFiles(actualDirectory,"*.json*"))originals[path]=File.ReadAllBytes(path);
        yield return null;
        Check(UnifiedModeBootstrap.Selected==UnifiedModeBootstrap.Mode.Menu&&Human()==null&&!File.Exists(CampaignPath),"initial menu does not initialize campaign");
        yield return Capture("01-mode-selection");
        FindObjectOfType<UnifiedModeBootstrap>().StartPvp();yield return null;
        yield return Capture("02-pvp-city-setup");
        FindObjectOfType<Pvp.PvpBootstrap>().StartSimulation(true,120);
        yield return Ready();
        var human=Human();var match=human.simulationMatch;
        Check(match&&match.Drivers.Count==4&&PvpSimulationMode.Expanded,"native city four-driver PVP");
        yield return Capture("03-pvp-four-drivers");
        PvpSimulationMode.Reset();
        foreach(var driver in match.Drivers)Check(driver.business.SimulationSession&&driver.business.SavePath==""&&driver.business.SaveCheckpoint()&&!driver.business.RequestEndDay(),"sticky PVP save/day guard after global reset driver="+driver.SimulationDriver);
        Check(!File.Exists(CampaignPath),"PVP has not created campaign");
        PvpSimulationMode.Configure(true,120);match.Finish();match.Restart();
        yield return null;yield return Ready();
        human=Human();match=human.simulationMatch;
        Check(match&&match.Drivers.Count==4&&!match.Finished&&match.Elapsed<5,"restart creates fresh PVP match");
        match.ReturnToMenu();yield return null;yield return Menu();
        Check(!PvpSimulationMode.Active&&!File.Exists(CampaignPath),"PVP return unloads sessions without campaign write");
        FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();
        human=Human();
        Check(!human.business.SimulationSession&&human.business.preparing&&!human.business.ResumeBlocked,"single starts normal three-day preparation");
        Check(human.business.MenuPrimaryLabel=="영업 시작"&&!human.business.HasCampaignToContinue,"fresh single shows start without restart");
        yield return Capture("04-single-preparation");
        human.business.ContinueFromMenu();
        Check(!human.business.preparing&&!human.paused&&Time.timeScale==1&&human.business.ledger.Operating,"fresh start enters driving with one action");
        yield return Capture("04b-single-driving");
        Check(human.business.SavePath==CampaignPath&&human.business.SaveCheckpoint(),"single saves to isolated real checkpoint pipeline");
        string id=human.law.ids.campaign;
        Check(FindObjectOfType<UnifiedModeBootstrap>().ReturnToMenu(),"single saves before returning to mode menu");
        yield return null;yield return Menu();
        FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();human=Human();
        Check(!human.business.ResumeBlocked&&human.law.ids.campaign==id&&!human.business.preparing&&human.paused,"single re-entry restores existing active campaign paused");
        Check(human.business.MenuPrimaryLabel=="영업 계속하기"&&human.business.HasCampaignToContinue,"restored driving exposes continue and restart");
        yield return Capture("05-single-restored-menu");
        human.business.ContinueFromMenu();
        Check(!human.business.preparing&&!human.paused&&Time.timeScale==1&&human.business.ledger.Operating,"one menu action directly starts driving");
        human.paused=true;Time.timeScale=0;
        yield return Capture("06-single-paused-menu");
        Check(human.business.RequestRestart()&&!human.business.ConfirmRestart(false)&&human.law.ids.campaign==id,"restart cancellation preserves campaign");
        human.business.ContinueFromMenu();
        Check(!human.paused&&Time.timeScale==1&&human.law.ids.campaign==id,"continue resumes same campaign");
        FindObjectOfType<UnifiedModeBootstrap>().ReturnToMenu();yield return null;yield return Menu();
        // Invalid primary+backup must be preserved even when returning to menu.
        File.WriteAllText(CampaignPath,"invalid-unified-probe");File.WriteAllText(CampaignPath+".bak","invalid-unified-probe-backup");
        FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();human=Human();
        Check(human.business.ResumeBlocked&&!human.business.SaveCheckpoint(),"invalid save refuses restoration and writing");
        human.business.Depart();Check(human.business.restartPrompt,"departure retains explicit recovery confirmation");
        Check(FindObjectOfType<UnifiedModeBootstrap>().ReturnToMenu(),"rejected save permits safe return to menu");
        yield return null;yield return Menu();
        Check(File.ReadAllText(CampaignPath)=="invalid-unified-probe"&&File.ReadAllText(CampaignPath+".bak")=="invalid-unified-probe-backup","rejected original and backup preserved");
        foreach(var item in originals)Check(File.Exists(item.Key)&&Same(item.Value,File.ReadAllBytes(item.Key)),"user campaign unchanged "+Path.GetFileName(item.Key));
        if(Directory.Exists(actualDirectory))foreach(var path in Directory.GetFiles(actualDirectory,"*.json*"))Check(originals.ContainsKey(path),"no new user campaign file "+Path.GetFileName(path));
        done=true;Debug.Log("UNIFIED_MODE_RESULT "+(ok?"PASS":"FAIL")+" active_seconds="+activeSeconds+" isolated_campaign="+CampaignPath);Application.Quit(ok?0:1);
    }
}
