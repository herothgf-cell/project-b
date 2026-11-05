using System;
using UnityEngine;
public static class UnifiedModeTests {
    public static void Build(){Run();CampaignStartupTests.Run();CampaignCheckpointWriterTests.Run();LiveCampaignTests.Run();PrototypeBuild.BuildHybrid();}
    static void Check(bool value,string label){if(!value)throw new Exception("UNIFIED_MODE: "+label);}
    public static void Run(){
        MenuFlow();
        Check(!UnifiedModeBootstrap.IsSingleCommand(new string[0]),"default is mode menu");
        Check(UnifiedModeBootstrap.IsSingleCommand(new[]{"-campaignStartupTest"}),"campaign diagnostics still enter single");
        Check(UnifiedModeBootstrap.IsSingleCommand(new[]{"-trafficPerformanceTest"}),"existing traffic diagnostics stay single");
        Check(!UnifiedModeBootstrap.IsSingleCommand(new[]{"-trafficPerformanceTest","-pvpCityPerformanceTest"}),"PVP city performance has priority");
        Check(!UnifiedModeBootstrap.IsSingleCommand(new[]{"-pvpSimulationFourDriverCheck"}),"PVP checks enter PVP");
        Check(!UnifiedModeBootstrap.IsSingleCommand(new[]{"-unifiedModeCheck"}),"mode acceptance begins at menu");
        Check(UnifiedModeBootstrap.IsSingleCommand(new[]{"-singlePlayer"}),"explicit single CLI");
        PvpSimulationMode.Configure(true,120);Check(PvpSimulationMode.Active&&PvpSimulationMode.Expanded,"city mode setup");PvpSimulationMode.Reset();Check(!PvpSimulationMode.Active,"menu clears mode");
        Debug.Log("UNIFIED_MODE_EDITOR_OK checks=9");
    }
    public static void MenuFlow(){
        var go=new GameObject("isolated start menu state");
        try {
            var b=go.AddComponent<BusinessProgress>();b.ledger=new EconomyLedger(PrototypeSettings.ForThreeDays());b.preparing=true;
            var primary=typeof(BusinessProgress).GetProperty("MenuPrimaryLabel");
            var restart=typeof(BusinessProgress).GetProperty("HasCampaignToContinue");
            Check(primary!=null&&restart!=null,"single entry menu exposes campaign-aware actions");
            Check((string)primary.GetValue(b)=="영업 시작"&&!(bool)restart.GetValue(b),"fresh preparation has only start");
            typeof(BusinessProgress).GetField("loaded",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(b,new CampaignCheckpoint());
            Check((string)primary.GetValue(b)=="영업 계속하기"&&(bool)restart.GetValue(b),"restored preparation can continue or restart");
            typeof(BusinessProgress).GetField("loaded",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(b,null);b.preparing=false;
            Check((string)primary.GetValue(b)=="영업 계속하기"&&(bool)restart.GetValue(b),"in-session pause can continue without reload");
            Debug.Log("START_MENU_STATE_OK checks=4");
        } finally {UnityEngine.Object.DestroyImmediate(go);}
    }
}
