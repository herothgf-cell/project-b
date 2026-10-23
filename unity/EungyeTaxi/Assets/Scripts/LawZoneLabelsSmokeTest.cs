using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class LawZoneLabelsSmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);File.WriteAllText("artifacts/law-zone-labels-test.txt","success="+ok+"\n"+report);}
 void Update(){if(Time.realtimeSinceStartup>120){Check(false,"watchdog");Application.Quit(1);}}
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/"+name+".png"));yield return new WaitForSecondsRealtime(.3f);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);session.business.enabledCampaign=true;session.business.Depart();session.taxi.automated=true;session.taxi.autoBrake=true;
  var map=GetComponent<DrivingReadability>();var zones=GetComponent<ServiceLawZones>();var hud=GetComponent<DriveHUD>();var jobs=session.jobs;
  Check(session.world.map.mapId=="eungye"&&zones.zones.Count==7&&!PracticeSession.Enabled,"normal Eungye with seven zones");
  var found=new HashSet<int>();for(int round=0;round<84;round++){jobs.Generate();for(int i=0;i<3;i++){var z=zones.ZoneFor(jobs.offers[i]);if(z==null)continue;found.Add(z.variant);Check(!hud.OfferText(i).Contains(z.Title),"call card hides route situation explanation "+z.variant);}}
  Check(found.Count==7,"all seven actual routed situations retained behind call cards");
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1600,900)}){
   Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.6f);map.Overview=true;map.LayoutLawZones();
   map.Facilities=true;map.LayoutLawZones();Check(map.ZoneLabels.Count==7,"all seven names on overview "+size.x);
   foreach(var a in map.ZoneLabels){Check(a.text.Contains(a.zone.MapTitle)&&a.rect.xMin>=0&&a.rect.yMin>=0&&a.rect.xMax<=280&&a.rect.yMax<=280,"name visible and inside minimap "+a.zone.variant);foreach(var b in map.ZoneLabels)if(a!=b)Check(!a.rect.Overlaps(b.rect),"names do not overlap "+a.zone.variant+"/"+b.zone.variant);}
   yield return Capture("law-zone-overview-"+size.x);
  }
  Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.6f);
  Check(jobs.Accept(0),"select ordinary call");map.LayoutLawZones();Check(map.ZoneLabels.Exists(x=>x.selected&&x.zone==zones.ZoneFor(jobs.current)),"pickup journey highlights the called situation before boarding");
  map.Overview=false;map.LayoutLawZones();var selected=map.ZoneLabels.Find(x=>x.selected);Check(selected!=null&&selected.text.Contains(selected.zone.MapTitle),"nearby map retains selected zone name including offscreen destination");
  foreach(var item in map.ZoneLabels)Check(!item.rect.Overlaps(map.MarkerTextBounds(jobs.stops[jobs.current.drop].V())),"law name does not cover drop-off text");
  yield return Capture("law-zone-selected");
  File.WriteAllText("artifacts/law-zone-labels-test.txt","success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
