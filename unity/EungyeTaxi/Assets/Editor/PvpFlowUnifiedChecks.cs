using System;
using UnityEngine;
public static class PvpFlowUnifiedChecks {
 public static void Corrections(){PvpNpcMergeTests.Run();PvpCityMapTests.Run();Debug.Log("FLOW_UNIFIED_CORRECTIONS_OK");}
 public static void Run(){int failures=0;Action<string,Action> check=(name,run)=>{try{run();Debug.Log("FLOW_UNIFIED_OK "+name);}catch(Exception e){failures++;Debug.LogError("FLOW_UNIFIED_FAIL "+name+": "+e);}};
 check("mode contracts",UnifiedModeTests.Run);
 check("traffic safety and natural flow",PvpNpcMergeTests.Run);
 check("road and arrow geometry",PvpCityA2Tests.Run);
 check("lane preparation",PvpCityA2Tests.LaneChoices); check("signal waits and back lanes",PvpCityA2Tests.SignalWaits);
 check("bays and facilities",PvpCityMapTests.Run);
 check("four drivers and calls",PvpSimulationFourDriverTests.Run);
 check("live map controls",PvpLiveMapTests.Run);
 check("call pin visibility",PvpCallPinTests.Run);
 if(failures>0)throw new Exception("FLOW_UNIFIED failures="+failures);Debug.Log("FLOW_UNIFIED_EDITOR_OK");
 }
}
