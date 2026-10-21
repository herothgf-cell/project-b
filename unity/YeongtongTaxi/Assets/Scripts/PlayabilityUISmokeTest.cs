using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Real Player interaction checks; reflection permits running the same probe before the new UI exists.
public class PlayabilityUISmokeTest : MonoBehaviour {
 public DriveSession session;bool ok=true;readonly StringBuilder report=new StringBuilder();
 bool measured;
 void OnGUI(){if(measured||!session||!session.jobs)return;measured=true;var style=KoreanText.Button(13);foreach(var caption in new[]{"시설 정보 보기","시설 표시 끄기","M 전체 지도","+ 확대","− 축소"})Check(style.CalcHeight(new GUIContent(caption),caption.StartsWith("시설")?140:125)<=26,"Korean control fits: "+caption);}
 void Check(bool value,string name){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+name);File.WriteAllText("artifacts/playability-ui-test.txt","success="+ok+"\n"+report);}
 void Update(){if(Time.realtimeSinceStartup>100)Application.Quit(2);}
 IEnumerator Capture(string name){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.GetFullPath("artifacts/"+name+".png"));yield return new WaitForSecondsRealtime(.4f);}
 IEnumerator Start(){
  Directory.CreateDirectory("artifacts");yield return new WaitForSecondsRealtime(1.5f);
  session.business.enabledCampaign=true;session.business.Depart();session.taxi.automated=true;session.taxi.autoBrake=true;
  var overlay=session.GetComponent("FullMapOverlay");Check(overlay!=null,"large whole-map available in normal Eungye");
  if(!overlay){Application.Quit(1);yield break;}
  var type=overlay.GetType();var open=type.GetMethod("Open");var close=type.GetMethod("Close");
  Check(session.world.map.mapId=="eungye","Eungye default build");
  var whole=(FullMapOverlay)overlay;var mini=session.GetComponent<DrivingReadability>();float zoom=mini.ZoomSpan;mini.Zoom(1);Check(mini.ZoomSpan<zoom,"plus zooms minimap in");mini.Zoom(-1);Check(mini.ZoomSpan==zoom,"minus reverses zoom");for(int i=0;i<20;i++)mini.Zoom(1);Check(mini.ZoomSpan==300,"zoom clamps at close range");for(int i=0;i<20;i++)mini.Zoom(-1);Check(mini.ZoomSpan==2300,"zoom clamps at full district");mini.Overview=false;
  whole.ShowFacilities=false;whole.ShowTraffic=false;whole.ShowLaw=false;Check(whole.VisibleMarkers().Count==0&&whole.VisibleLawZones().Count==0,"all category filters remove actual drawn markers");
  whole.ShowFacilities=true;var facilities=whole.VisibleMarkers();Check(facilities.Count==5&&facilities.Exists(m=>m.label=="식당")&&facilities.Exists(m=>m.label=="주차장"),"facilities filter includes three pumps meal parking");
  whole.ShowFacilities=false;whole.ShowTraffic=true;var traffic=whole.VisibleMarkers();Check(traffic.Count>0&&!traffic.Exists(m=>m.label=="식당"||m.label.StartsWith("주유")),"traffic filter shows vehicles independently of facilities");
  whole.ShowLaw=true;Check(whole.VisibleLawZones().Count==7,"one law filter includes all seven zones");whole.ShowLaw=false;Check(whole.VisibleLawZones().Count==0,"one law filter hides all seven zones");whole.ShowLaw=true;whole.ShowFacilities=true;

  Check(session.services.indices[2]==0,"startup temporary parking moved to broad national road");
  var hud=session.GetComponent<DriveHUD>();var caption=typeof(DriveHUD).GetProperty("MannerCaption");var lawCaption=typeof(DriveHUD).GetProperty("LawCaption");
  Check(caption!=null&&lawCaption!=null,"two independent score gauges exposed");
  Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
  yield return Capture("playability-fuel-offers-1280");
  session.jobs.offers[0]=new JobOffer{pickup=0,drop=1,fare=5000,seconds=600,requiredScore=70,distance=500,pickupSeconds=600};session.jobs.Accept(0);int before=session.manners.ledger.Score;
  var e=new LawEvent{id="ui-score-probe",day=session.business.ledger.Day,family="signal"};e.Add("L01",LawVerdict.Violation,20);session.law.ledger.Record(e);
  Check(session.manners.ledger.Score==before&&session.law.ledger.Score(e.day)==80,"law deduction does not change passenger manners");
  if(lawCaption!=null)Check(((string)lawCaption.GetValue(hud,null)).Contains("80")&&((string)lawCaption.GetValue(hud,null)).Contains("운행 점수"),"driving score gauge uses actual score and player-facing name");
  if(caption!=null)Check(((string)caption.GetValue(hud,null)).Contains("탑승 전"),"no fabricated passenger score before boarding");
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1600,900),new Vector2Int(1920,1080)}){
   Screen.SetResolution(size.x,size.y,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.7f);
   bool opened=(bool)open.Invoke(overlay,null);Check(opened&&session.paused&&Time.timeScale==0,"whole map pauses driving "+size.x);
   double elapsed=session.business.ledger.Elapsed;yield return new WaitForSecondsRealtime(.3f);Check(session.business.ledger.Elapsed==elapsed,"map does not consume business time "+size.x);
   var rect=(Rect)type.GetProperty("MapRect").GetValue(overlay,null);Check(rect.width>=500&&rect.height>=500&&rect.xMin>=0&&rect.yMin>=0&&rect.xMax<=Screen.width&&rect.yMax<=Screen.height,"large map fits "+size.x);
   Check(!(bool)session.GetComponent<PracticeSession>().OpenPicker(),"map blocks overlapping practice modal");
   whole.ResetView();whole.Zoom(1);Check(whole.ZoomLevel>1,"whole-map zoom in "+size.x);whole.FocusPlayer();whole.Pan(new Vector2(70,-35));
   yield return Capture("playability-full-map-zoom-"+size.x);for(int n=0;n<20;n++)whole.Zoom(1);Check(whole.ZoomLevel==8,"whole-map maximum bound");for(int n=0;n<20;n++)whole.Zoom(-1);Check(whole.ZoomLevel==1,"whole-map overview bound");whole.ResetView();
   yield return Capture("playability-full-map-"+size.x);close.Invoke(overlay,null);Check(!session.paused&&Time.timeScale==1,"closing map resumes previous drive "+size.x);Check((bool)type.GetProperty("ClosedThisFrame").GetValue(overlay,null),"close frame suppresses duplicate Escape "+size.x);
   yield return Capture("playability-hud-"+size.x);
  }
  var taxi=session.taxi;taxi.enabled=false;taxi.body.useGravity=false;taxi.body.detectCollisions=false;var pickup=session.jobs.stops[session.jobs.current.pickup];taxi.body.velocity=Vector3.zero;taxi.body.position=pickup.Kerb+Vector3.up*.65f;taxi.body.rotation=Quaternion.LookRotation(pickup.Direction);taxi.transform.SetPositionAndRotation(taxi.body.position,taxi.body.rotation);Physics.SyncTransforms();
  Check(session.jobs.Interact()&&session.jobs.trip.State==TripState.OnTrip,"board fixture to display actual passenger score");session.manners.Deduct("ui-probe",3,"테스트 매너 기록",0);if(caption!=null)Check(((string)caption.GetValue(hud,null)).Contains("97"),"passenger gauge uses real occupied score");Check(session.law.ledger.Score(e.day)==80,"manner deduction does not change law score");
  Screen.SetResolution(1280,720,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.7f);yield return Capture("playability-occupied-hud-1280");
  session.paused=true;Time.timeScale=0;Check((bool)open.Invoke(overlay,null),"map can open from pause");close.Invoke(overlay,null);Check(session.paused&&Time.timeScale==0,"closing map preserves previous pause");
  session.paused=false;Time.timeScale=1;session.business.preparing=true;Check(!(bool)open.Invoke(overlay,null),"map cannot open over day preparation");session.business.preparing=false;
  File.WriteAllText("artifacts/playability-ui-test.txt","success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
