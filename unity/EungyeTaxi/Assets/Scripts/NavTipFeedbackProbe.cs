using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
// Opt-in disposable Player fixture. Never runs during ordinary play or saves it.
public sealed class NavTipFeedbackProbe:MonoBehaviour {
 bool ok=true;readonly StringBuilder report=new StringBuilder();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-navTipFeedbackTest")>=0)new GameObject("Nav TIP feedback probe").AddComponent<NavTipFeedbackProbe>();}
 void Check(bool value,string message){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+message);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/nav-tip-runtime.txt","success="+ok+"\n"+report);}
 IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.4f);yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("artifacts/nav-tip-"+name+".png",image.EncodeToPNG());Destroy(image);}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);var s=FindObjectOfType<DriveSession>();while(!s.StartupReady)yield return null;yield return null;
  Screen.SetResolution(1600,900,FullScreenMode.Windowed);s.business.enabled=false;s.business.preparing=false;s.business.enabledCampaign=true;s.business.ledger=new EconomyLedger(PrototypeSettings.ForOneDay());s.jobs.ClearForDay();s.paused=false;Time.timeScale=1;s.taxi.enabled=false;s.taxi.body.isKinematic=true;s.jobs.enabled=false;s.requests.enabled=false;s.law.enabled=false;s.manners.enabled=false;s.GetComponent<StopOccupancySystem>().enabled=false;
  var zones=s.GetComponent<ServiceLawZones>();s.services.notice="생활시설·도로 상황은 TIP에서 확인합니다.";
  Check(KoreanText.SignFont.material!=KoreanText.BoldFont.material&&KoreanText.SignFont.material.mainTexture!=KoreanText.BoldFont.material.mainTexture,"sign and HUD bold materials/atlases are independent after startup");
  foreach(int width in new[]{1280,1920}){float height=width*9f/16;var hidden=CockpitUI.SlidingTipBounds(width,height,false,true,0);var shown=CockpitUI.SlidingTipBounds(width,height,false,true,1);Check(Mathf.Abs(hidden.xMax)<.01f&&shown.x>0&&shown.yMax<=height-232*Mathf.Min(1,width/1600f,height/900f)+.1f,"sliding panel/tab bounds "+width);}
  yield return Capture("open");zones.ToggleTip();yield return new WaitForSecondsRealtime(.4f);Check(!zones.TipExpanded,"TIP hidden without pausing");yield return Capture("hidden");zones.ToggleTip();yield return new WaitForSecondsRealtime(.4f);
  var j=s.jobs;int p=j.Catalog.LegacyCount;var pickup=j.stops[p];s.taxi.transform.SetPositionAndRotation(pickup.Kerb+Vector3.up*.65f,Quaternion.LookRotation(pickup.Direction));s.taxi.body.position=s.taxi.transform.position;s.taxi.body.rotation=s.taxi.transform.rotation;
  j.offers[0]=new JobOffer{pickup=p,drop=p+1,fare=5000,seconds=600,requiredScore=70,distance=j.nav.Distance(j.stops[p],j.stops[p+1])};Check(j.Accept(0),"occupancy TIP uses an active real catalog call");
  var occupancy=s.GetComponent<StopOccupancySystem>();occupancy.Restore(new StopOccupancySnapshot{version=1,active=true,eventId="tip-fixture",stopId=j.Catalog.Entry(p).StopId,remaining=26});Check(occupancy.AffectsActive&&occupancy.Resolve(false),"TIP wait action retains occupancy protection");yield return Capture("occupied");
  j.ClearForDay();s.services.notice="";s.taxi.transform.SetPositionAndRotation(new Vector3(807.6996f,.65f,607.1715f),Quaternion.Euler(0,3.280868f,0));s.taxi.body.position=s.taxi.transform.position;s.taxi.body.rotation=s.taxi.transform.rotation;
  s.drivingCamera.enabled=false;var camera=Camera.main;camera.transform.position=s.taxi.transform.position-s.taxi.transform.forward*8+Vector3.up*3;camera.transform.LookAt(s.taxi.transform.position+s.taxi.transform.forward*35+Vector3.up*3);
  var labels=FindObjectsOfType<TextMesh>().Where(t=>t.text.Contains("유턴\n")).ToArray();Check(labels.Length>0&&labels.All(t=>t.font==KoreanText.SignFont&&t.font!=KoreanText.Font),"conditional Korean signs use isolated bold font atlas");
  Check(labels.All(t=>t.GetComponent<Renderer>().localBounds.size.x>1&&t.GetComponent<Renderer>().localBounds.size.y>.5f),"actual sign glyph meshes have readable physical area");yield return Capture("signs");
  var catalog=j.Catalog.Stops;var a=catalog.First(t=>t.name.Contains("국도42")&&t.name.EndsWith("1"));var b=catalog.First(t=>t.name=="게임 승하차 2");var route=j.nav.hd.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt);Check(route!=null&&route.path.length<1000&&route.edges.Any(i=>j.nav.hd.edges[i].id=="A2218G611778:0"),"actual Player uses western captured U-turn");
  File.WriteAllText("artifacts/nav-tip-runtime.txt","success="+ok+"\n"+report);Application.Quit(ok?0:1);
 }
}
