using System;
using UnityEngine;

public static class CampaignStartupTests {
 static int checks;
 static void Check(bool value,string reason){checks++;if(!value)throw new Exception("CAMPAIGN_STARTUP "+reason);}
 static void Reject(AmbientPedestrianSystem system,string payload,string reason){bool rejected=false;try{system.Validate(BusinessProgress.ParseCheckpoint("{\"hasLive\":true,\"live\":{\"ambientPedestrians\":"+payload+"}}").live.ambientPedestrians);}catch(ArgumentException){rejected=true;}Check(rejected,reason);}
 public static void Run(){
  checks=0;var go=new GameObject("isolated campaign startup serializer test");
  try{
   var system=go.AddComponent<AmbientPedestrianSystem>();var actor=go.AddComponent<AmbientPedestrian>();actor.route=new AmbientPedestrianRoute{id="crossing",crossing=true};system.actors.Add(actor);
   var missing=BusinessProgress.ParseCheckpoint("{\"version\":3,\"hasLive\":true,\"live\":{\"realSeconds\":137,\"tripId\":\"kept-trip\"}}");
   Check(missing.live.ambientPedestrians==null,"pre-pedestrian save has no synthetic actor snapshot");system.Validate(missing.live.ambientPedestrians);
   Check(missing.live.realSeconds==137&&missing.live.tripId=="kept-trip","migration preserves live trip/time");
   var explicitNull=BusinessProgress.ParseCheckpoint("{\"hasLive\":true,\"live\":{\"ambientPedestrians\":null}}");Check(explicitNull.live.ambientPedestrians==null,"explicit legacy null remains absent");system.Validate(explicitNull.live.ambientPedestrians);
   var current=BusinessProgress.ParseCheckpoint("{\"hasLive\":true,\"live\":{\"ambientPedestrians\":{\"actors\":[{\"id\":\"crossing\",\"progress\":0.4,\"crossing\":true}]}}}");system.Validate(current.live.ambientPedestrians);Check(current.live.ambientPedestrians.actors[0].progress==.4f,"occupied crossing keeps progress");
   Reject(system,"{}","present empty object rejected");Reject(system,"{\"actors\":null}","present null actors rejected");Reject(system,"{\"actors\":[]}","present count mismatch rejected");Reject(system,"{\"actors\":[{\"id\":\"unknown\"}]}","unknown actor rejected");Reject(system,"{\"actors\":[{\"id\":\"crossing\",\"progress\":0.4}]}","invalid crossing state rejected");
   Check(BusinessProgress.ParseCheckpoint(JsonUtility.ToJson(current)).live.ambientPedestrians.actors[0].crossing,"new snapshot survives shipping serializer");
   Debug.Log("CAMPAIGN_STARTUP_TESTS_OK checks="+checks);
  }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
 public static void InspectLegacy(){
  var missing=JsonUtility.FromJson<LiveCampaignCheckpoint>("{\"mapRevision\":\"legacy\"}");
  var explicitNull=JsonUtility.FromJson<LiveCampaignCheckpoint>("{\"ambientPedestrians\":null}");
  Debug.Log("CAMPAIGN_LEGACY_JSON missing="+(missing.ambientPedestrians==null?"null":missing.ambientPedestrians.actors==null?"actors-null":"actors-"+missing.ambientPedestrians.actors.Length)+" explicitNull="+(explicitNull.ambientPedestrians==null?"null":explicitNull.ambientPedestrians.actors==null?"actors-null":"actors-"+explicitNull.ambientPedestrians.actors.Length));
  if(missing.ambientPedestrians!=null||explicitNull.ambientPedestrians!=null)throw new Exception("Legacy campaign optional ambient snapshot becomes a fake nonnull snapshot");
 }
}
