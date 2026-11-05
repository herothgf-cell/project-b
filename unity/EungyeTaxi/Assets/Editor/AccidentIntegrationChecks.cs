using System;
using UnityEngine;
public static class AccidentIntegrationChecks {
 static void Check(bool value,string label){if(!value)throw new Exception("ACCIDENT_INTEGRATION: "+label);}
 public static void Run(){
  var type=typeof(DriveSession).Assembly.GetType("AccidentController");
  Check(type!=null,"vehicle accident controller exists");
  var sweep=type.GetMethod("SweptContact");Check(sweep!=null,"continuous pedestrian contact query");
  Func<Vector3,Vector3,Vector3,bool> hit=(a,b,p)=>(bool)sweep.Invoke(null,new object[]{a,b,Quaternion.identity,new Vector3(1,.65f,2),p,.32f});
  Check(hit(new Vector3(0,1,-10),new Vector3(0,1,10),new Vector3(0,1,0)),"fast forward contact does not tunnel");
  Check(hit(new Vector3(0,1,10),new Vector3(0,1,-10),new Vector3(0,1,0)),"reverse contact does not tunnel");
  Check(!hit(new Vector3(0,1,-10),new Vector3(0,1,10),new Vector3(2,1,0)),"outside body clearance is not a contact");
  Check(!hit(new Vector3(0,1,-10),new Vector3(0,1,10),new Vector3(0,5,0)),"different vertical level excluded");
  Check(typeof(LiveCampaignCheckpoint).GetField("accident")!=null,"live save carries accident");
  Debug.Log("ACCIDENT_INTEGRATION_OK checks=7");
 }
}
