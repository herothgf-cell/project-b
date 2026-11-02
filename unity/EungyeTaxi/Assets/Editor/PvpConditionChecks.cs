using System;
using UnityEngine;
public static class PvpConditionChecks {
 public static void Run(){int failures=0;Action<string,Action> run=(name,test)=>{try{test();Debug.Log("CONDITION_FINAL_OK "+name);}catch(Exception e){failures++;Debug.LogError("CONDITION_FINAL_FAIL "+name+" "+e);}};
 run("model and campaign isolation",PvpConditionModelTests.Run);
 run("pending service and end clock",PvpConditionServiceTests.Run);
 run("bot prediction and counters",PvpConditionBotTests.Run);
 run("native city access",PvpCityMapTests.Run);
 run("mode and save contracts",UnifiedModeTests.Run);
 if(failures>0)throw new Exception("CONDITION_FINAL failures="+failures);Debug.Log("PVP_CONDITION_EDITOR_OK");
 }
}
