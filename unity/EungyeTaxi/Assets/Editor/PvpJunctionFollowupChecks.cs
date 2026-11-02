using System;
using System.Reflection;
using UnityEngine;
public static class PvpJunctionFollowupChecks {
 public static void Run(){int failures=0;Action<string,Action> run=(name,check)=>{try{check();Debug.Log("JUNCTION_FOLLOWUP_OK "+name);}catch(Exception e){failures++;Debug.LogError("JUNCTION_FOLLOWUP_FAIL "+name+" "+e);}};
 run("all authored turn trajectories",PvpJunctionTrajectoryTests.Run);
 run("turn sensing and yield",PvpJunctionFlowTests.Run);
 run("reservation lifecycle",TrafficReservationLifecycleTests.Run);
 run("campaign right-turn convoy",RightTurnWaitingTests.RunConvoy);
 run("campaign curve occupancy",CurveFootprintTests.Run);
 run("PVP lane and bay merges",PvpNpcMergeTests.Run);
 if(failures>0)throw new Exception("JUNCTION_FOLLOWUP failures="+failures);Debug.Log("JUNCTION_FOLLOWUP_EDITOR_OK");
 }
 public static void BuildStaged(){ProjectMapBuild.Prepare();var method=typeof(PrototypeBuild).GetMethod("BuildAt",BindingFlags.Static|BindingFlags.NonPublic);method.Invoke(null,new object[]{System.IO.Path.GetFullPath("../../artifacts/pvp/junction-runtime1/EungyeTaxi.exe"),"eungye"});}
}
