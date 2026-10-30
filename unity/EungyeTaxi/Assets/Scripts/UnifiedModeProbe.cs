using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Run EungyeTaxi.exe -unifiedModeCheck. Uses a fresh temporary campaign only.
public sealed class UnifiedModeProbe : MonoBehaviour {
    public static bool Enabled {get{return Array.IndexOf(Environment.GetCommandLineArgs(),"-unifiedModeCheck")>=0;}}
    static readonly string campaignPath=Path.Combine(Path.GetTempPath(),"taxi-unified-"+Guid.NewGuid().ToString("N"),"campaign.json");
    public static string CampaignPath {get{return campaignPath;}}
    bool ok=true,done;float deadline;
    readonly Dictionary<string,byte[]> originals=new Dictionary<string,byte[]>();
    string actualDirectory;
    void Check(bool value,string label){ok&=value;Debug.Log("UNIFIED_MODE "+(value?"PASS ":"FAIL ")+label);}
    void Awake(){DontDestroyOnLoad(gameObject);deadline=Time.realtimeSinceStartup+600;}
    void Update(){if(!done&&Time.realtimeSinceStartup>deadline){Debug.LogError("UNIFIED_MODE timeout");Application.Quit(2);done=true;}}
    DriveSession Human(){foreach(var s in FindObjectsOfType<DriveSession>())if(!s.IsSimulationBot)return s;return null;}
    IEnumerator Ready(){while(!Human()||!Human().StartupReady||!Human().business||Human().business.IsRestoring)yield return null;yield return null;yield return null;}
    IEnumerator Menu(){while(!FindObjectOfType<UnifiedModeBootstrap>()||UnifiedModeBootstrap.Selected!=UnifiedModeBootstrap.Mode.Menu||Human())yield return null;yield return null;}
    static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
    IEnumerator Start(){
        var run=Run();
        while(!done){object next;try{if(!run.MoveNext())yield break;next=run.Current;}catch(Exception ex){done=true;Debug.LogError("UNIFIED_MODE_EXCEPTION "+ex);Application.Quit(2);yield break;}yield return next;}
    }
    IEnumerator Capture(string name){
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-unifiedModeCaptureOutput");
        if(i<0||i+1>=args.Length||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)yield break;
        Directory.CreateDirectory(args[i+1]);yield return new WaitForEndOfFrame();
        var image=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(args[i+1],name+".png"),image.EncodeToPNG());}finally{Destroy(image);}
    }
    IEnumerator Run(){
        actualDirectory=Path.Combine(Path.GetDirectoryName(Application.persistentDataPath),"은계 택시 시뮬레이터");
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
        yield return Capture("04-single-preparation");
        Check(human.business.SavePath==CampaignPath&&human.business.SaveCheckpoint(),"single saves to isolated real checkpoint pipeline");
        string id=human.law.ids.campaign;
        Check(FindObjectOfType<UnifiedModeBootstrap>().ReturnToMenu(),"single saves before returning to mode menu");
        yield return null;yield return Menu();
        FindObjectOfType<UnifiedModeBootstrap>().StartSingle();yield return Ready();human=Human();
        Check(!human.business.ResumeBlocked&&human.law.ids.campaign==id&&human.business.preparing,"single re-entry restores existing campaign and preparation");
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
        done=true;Debug.Log("UNIFIED_MODE_RESULT "+(ok?"PASS":"FAIL")+" isolated_campaign="+CampaignPath);Application.Quit(ok?0:1);
    }
}
