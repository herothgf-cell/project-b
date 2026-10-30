using System;
using System.Reflection;
using UnityEngine;
public static class PvpCallPinTests {
 public static void Run(){
  var root=new GameObject("eight call pins");root.SetActive(false);
  try {
   var s=root.AddComponent<DriveSession>();s.simulationMatch=root.AddComponent<PvpSimulationMatch>();
   s.world=root.AddComponent<WorldBuilder>();s.world.map=new MapData{width=1000,depth=1000};
   s.jobs=root.AddComponent<PassengerJobs>();s.jobs.offers=new JobOffer[8];s.jobs.stops=new MapStop[8];
   var map=root.AddComponent<FullMapOverlay>();map.session=s;var bounds=new Rect(0,0,900,600);
   var project=typeof(FullMapOverlay).GetMethod("Project",BindingFlags.Instance|BindingFlags.NonPublic);
   for(int i=0;i<8;i++){s.jobs.offers[i]=new JobOffer{pickup=i};s.jobs.stops[i]=new MapStop{x=100+i*100,z=500};}
   for(int i=0;i<8;i++){var raw=(Vector2)project.Invoke(map,new object[]{s.jobs.stops[i].V(),bounds});Check(Vector2.Distance(raw,map.CallPinPosition(i,bounds))<.1f,"spaced call "+i+" remains at its real pickup, regardless of slot number");}
   for(int i=0;i<8;i++)s.jobs.stops[i].x=400+i;
   var placed=new Rect[8];
   for(int i=0;i<8;i++){var raw=(Vector2)project.Invoke(map,new object[]{s.jobs.stops[i].V(),bounds});var p=map.CallPinPosition(i,bounds);placed[i]=new Rect(p.x-15,p.y-15,30,30);Check(bounds.Contains(placed[i].min)&&bounds.Contains(placed[i].max)&&Vector2.Distance(raw,p)<=110,"crowded call remains near real pickup and clickable");for(int j=0;j<i;j++)Check(!placed[i].Overlaps(placed[j]),"eight call buttons do not overlap");}
   for(int i=0;i<8;i++)s.jobs.stops[i].x=100+i*100;
   var taxiRoot=new GameObject("human marker");taxiRoot.transform.SetParent(root.transform);s.taxi=taxiRoot.AddComponent<TaxiController>();s.taxi.transform.position=s.jobs.stops[0].V();
   var rivalRoot=new GameObject("bot marker");rivalRoot.transform.SetParent(root.transform);var rival=rivalRoot.AddComponent<DriveSession>();rival.taxi=rivalRoot.AddComponent<TaxiController>();rival.taxi.transform.position=s.jobs.stops[1].V();
   ((System.Collections.Generic.List<DriveSession>)typeof(PvpSimulationMatch).GetField("bots",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(s.simulationMatch)).Add(rival);
   for(int i=0;i<2;i++){var raw=(Vector2)project.Invoke(map,new object[]{s.jobs.stops[i].V(),bounds});var pin=map.CallPinPosition(i,bounds);Check(!new Rect(pin.x-15,pin.y-15,30,30).Overlaps(new Rect(raw.x-15,raw.y-15,30,30)),"call number remains visible beside human/bot marker "+i);}
   Debug.Log("PVP_CALL_PIN_OK 8 real anchors, 8 crowded pins, human/bot visibility");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
 static void Check(bool value,string why){if(!value)throw new Exception("PVP_CALL_PIN: "+why);}
}