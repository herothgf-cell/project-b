using System;
using System.IO;
using UnityEngine;
// Explicit isolated fixture execution before world startup, never user save IO.
public static class P1P2IntegrationProbe {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Run(){
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-p1p2IntegrationTest")<0)return;
  string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../..","artifacts/p1p2-native-integration.txt"));
  try{OperationPlanningRuntimeChecks.Run();TrafficDemandIndicatorIntegrationTests.Run();P1P2CheckpointLawTests.Run();StopOccupancyAlternativeIntegrationTests.Run();File.WriteAllText(path,"PASS position-controlled native adapters; not physical driving or manual play\n");Debug.Log("P1P2_NATIVE_INTEGRATION_OK");Application.Quit(0);}
  catch(Exception e){File.WriteAllText(path,"FAIL\n"+e);Debug.LogException(e);Application.Quit(1);}
 }
}
