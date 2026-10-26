using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
// Run -threeDayTest, then -threeDayReloadTest in separate processes.
[DefaultExecutionOrder(-32000)]
public class ThreeDayCampaignProbe:MonoBehaviour {
 DriveSession s;bool ok=true;readonly StringBuilder report=new StringBuilder();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]static void Install(){var args=Environment.GetCommandLineArgs();if(Array.IndexOf(args,"-threeDayTest")>=0||Array.IndexOf(args,"-threeDayReloadTest")>=0){Time.timeScale=0;var host=new GameObject("Three day campaign probe");DontDestroyOnLoad(host);host.AddComponent<ThreeDayCampaignProbe>();}}
 void Update(){Time.timeScale=0;}
 void LateUpdate(){Time.timeScale=0;}
 void Check(bool value,string text){ok&=value;report.AppendLine((value?"PASS ":"FAIL ")+text);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/three-day-runtime"+(Array.IndexOf(Environment.GetCommandLineArgs(),"-threeDayReloadTest")>=0?"-reload":"")+".txt","success="+ok+"\n"+report);}
 CampaignCheckpoint Saved(){return JsonUtility.FromJson<CampaignCheckpoint>(File.ReadAllText(s.business.SavePath));}
 IEnumerator Start(){
  s=FindObjectOfType<DriveSession>();var b=s.business;b.enabled=false;Time.timeScale=0;
  yield return new WaitForSecondsRealtime(2);b.enabled=true;Time.timeScale=0;s.taxi.enabled=false;s.taxi.body.isKinematic=true;s.taxi.body.detectCollisions=false;
  yield return null; // Let the production Start restore the independent law journal while physics remains frozen.
  Check(b.ledger.OneDay&&b.ledger.Settings.days==3&&b.SavePath.EndsWith("three-day-test.json"),"three-day modern mode has separate save path");
  if(Array.IndexOf(Environment.GetCommandLineArgs(),"-threeDayReloadTest")>=0){
   Check(b.ledger.Day==2&&b.ledger.Operating&&b.ledger.Income==1234&&b.ledger.Elapsed==110,"fresh process restores active day2");Check(b.ledger.Account.Cash==57734&&Math.Abs(b.ledger.Account.Fuel-55f)<.001&&b.ledger.Driver.satiety==95&&Math.Abs(b.ledger.Driver.fatigue-27.333333)<.001,"day2 cash fuel hunger and overnight fatigue restored together");Check(s.law.ledger.TotalScore==85,"law record survives day boundary and process restart");
   Check(b.ComparisonVersion.EndsWith("mixed"),"campaign retains prior mixed traffic configuration across fresh process");
   b.ledger.RequestClose();yield return null;yield return null;Check(b.ledger.State==BusinessState.Summary&&b.NextDay(),"day2 summary proceeds to day3");b.ledger.RecordFare("day3",1000);b.ledger.RequestClose();yield return null;yield return null;
   Check(b.ledger.State==BusinessState.Completed&&b.ledger.TotalIncome==52234&&b.ledger.TotalExpenses==4500&&b.ledger.TotalNet==47734,"day3 completes using cumulative actual income expenses and net");Check(b.completedDays.Count==1&&b.completedDays[0].days==3&&b.completedDays[0].totalNet==47734,"only full campaign archived with totals");
   Check(b.SaveCheckpoint()&&b.completedDays.Count==1,"repeat save cannot duplicate completed campaign");Check(b.RequestRestart()&&!b.ConfirmRestart(false)&&b.ledger.Day==3,"restart cancellation preserves completed campaign");var final=Saved();var restored=EconomyLedger.RestoreCampaign(final.days,final.account);Check(restored.State==BusinessState.Completed&&restored.TotalNet==47734,"final saved checkpoint restores campaign completion");
  }else{
   Check(b.ledger.Day==1&&b.ledger.SecondsLeft==1200,"first campaign day starts with twenty minute budget");s.traffic.mitOptions.SetEnabled(!s.traffic.mitOptions.Enabled,false);yield return null;Check(b.ComparisonVersion.EndsWith("mixed"),"traffic switch marks campaign mixed");b.ledger.RecordFare("first",50000);b.ledger.Advance(300,1000);b.ledger.Driver.fatigue=80;b.ledger.RequestClose();yield return null;yield return null;
   Check(b.ledger.State==BusinessState.Summary&&b.ledger.TotalNet==49500&&b.completedDays.Count==0,"high day1 income stays daily summary without completed archive");var law=LawEvaluator.Exit("three-day-law",1,true,true,true,true);s.law.ledger.Record(law);
   string settled=File.ReadAllText(b.SavePath);Directory.CreateDirectory(b.SavePath+".tmp");try{Check(!b.BeginNextCampaignDay()&&b.ledger.Day==1&&b.ShowingSummary&&File.ReadAllText(b.SavePath)==settled,"failed next-day save preserves current summary and file");}finally{Directory.Delete(b.SavePath+".tmp");}
   Check(b.BeginNextCampaignDay()&&b.ledger.Day==2,"daily summary starts next campaign day");Check(b.ledger.Driver.satiety==55&&b.ledger.Driver.fatigue==20&&b.ledger.Elapsed==0,"overnight sleep recovers sixty fatigue and preserves hunger");
   int boundaryCash=b.ledger.Account.Cash;float boundaryFuel=b.ledger.Account.Fuel;b.ledger.RecordFare("discard-active",1234);b.ledger.UseMeal("discard-meal");b.ledger.Advance(60,100);Check(b.SaveCheckpoint(),"active day save succeeds");var saved=Saved();var restored=EconomyLedger.RestoreCampaign(saved.days,saved.account);Check(restored.Day==2&&restored.Income==1234&&restored.Account.Cash==boundaryCash+1234-3000&&restored.Account.Fuel<boundaryFuel&&restored.Elapsed==110&&restored.Driver.satiety>55,"active v3 save retains day2 income services fuel and time");
  }
  Application.Quit(ok?0:1);
 }
}
