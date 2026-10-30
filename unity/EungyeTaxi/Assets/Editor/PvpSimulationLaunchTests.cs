using System;
using UnityEngine;
public static class PvpSimulationLaunchTests {
 public static void Run(){
  var assembly=typeof(DriveSession).Assembly;
  var mode=assembly.GetType("PvpSimulationMode");
  if(mode==null)throw new Exception("PVP simulation mode missing: existing DriveSession cannot be launched as a match");
  if(typeof(Pvp.PvpBootstrap).GetMethod("StartSimulation",new[]{typeof(bool),typeof(int)})==null)throw new Exception("PVP startup has no existing-simulation entry");
  if(mode.GetMethod("Configure",new[]{typeof(bool),typeof(int)})==null)throw new Exception("PVP simulation configuration contract missing");
  Debug.Log("PVP_SIMULATION_LAUNCH_TESTS_OK");
 }
}
