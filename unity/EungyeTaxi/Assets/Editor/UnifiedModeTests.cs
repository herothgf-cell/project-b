using System;
using UnityEngine;
public static class UnifiedModeTests {
    static void Check(bool value,string label){if(!value)throw new Exception("UNIFIED_MODE: "+label);}
    public static void Run(){
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
}
