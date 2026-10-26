using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Isolated screenshot hook for the real completed-ride panel; does not settle or save a ride.
public sealed class ReceiptCaptureProbe:MonoBehaviour {
 const string Flag="-receiptCaptureTest";
 const string Screenshot="artifacts/receipt-compact.png";
 const string Report="artifacts/receipt-capture-test.txt";
 string receipt="",details="",errorMessage="";
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Attach(){if(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)>=0){Application.runInBackground=true;new GameObject("Receipt capture probe").AddComponent<ReceiptCaptureProbe>();}}
 IEnumerator Start(){
  yield return new WaitForSecondsRealtime(2);
  DriveSession session=null;PassengerResult fixture=null;
  try{
   session=FindObjectOfType<DriveSession>();if(!session)throw new Exception("DriveSession missing");
   var business=session.business;if(!business)throw new Exception("BusinessProgress missing");
   business.enabledCampaign=false;business.preparing=false;business.passengers.Clear();
   fixture=new PassengerResult{farePolicyVersion=3,initialFare=4900,quoted=4900,boardingDrivingScore=100,drivingAdjustedFare=5390,baseFare=5290,bonus=160,paid=5450,satisfaction=71,required=60,elapsed=50,score=0,cleanBonus=3,driveDelta=0,satisfactionDelta=8,timeDelta=12,bonusReason="fixture",pickup=session.jobs.stops[0].name,destination=session.jobs.stops[3].name};
   business.passengers.Add(fixture);
   Screen.SetResolution(1280,720,FullScreenMode.Windowed);
   Directory.CreateDirectory(Path.GetDirectoryName(Screenshot));
  }catch(Exception error){errorMessage=error.ToString();Debug.LogException(error);}
  if(string.IsNullOrEmpty(errorMessage))yield return new WaitForSecondsRealtime(.7f);
  if(string.IsNullOrEmpty(errorMessage)){Screen.SetResolution(1920,1080,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.7f);}
  if(string.IsNullOrEmpty(errorMessage)){
   try{
    var panel=session.GetComponent<RideCompletionPanel>();if(!panel||!panel.ShowLast())throw new Exception("RideCompletionPanel.ShowLast failed");
    if(panel.Result!=fixture||!panel.Open)throw new Exception("Panel did not show settled fixture");
    receipt=panel.Receipt;var lines=receipt.Split('\n');if(lines.Length!=3||lines[1]!="보너스  +550원"||receipt.Contains("약정")||receipt.Contains("감액"))throw new Exception("Unexpected compact money summary: "+receipt);
    if(fixture.initialFare!=4900||fixture.quoted!=4900||fixture.drivingAdjustedFare!=5390||fixture.baseFare!=5290||fixture.bonus!=160||fixture.paid!=5450)throw new Exception("Fixture economic fields changed");
   }catch(Exception error){errorMessage=error.ToString();Debug.LogException(error);}
  }
  if(string.IsNullOrEmpty(errorMessage))yield return new WaitForEndOfFrame();
  if(string.IsNullOrEmpty(errorMessage)){try{ScreenCapture.CaptureScreenshot(Path.GetFullPath(Screenshot));}catch(Exception error){errorMessage=error.ToString();Debug.LogException(error);}}
  if(string.IsNullOrEmpty(errorMessage))yield return new WaitForSecondsRealtime(.7f);
  if(string.IsNullOrEmpty(errorMessage)){
   try{if(!File.Exists(Path.GetFullPath(Screenshot)))throw new Exception("Screenshot was not written");var lines=receipt.Split('\n');details="moneyLines="+lines.Length+"\nnetRow="+lines[1]+"\nnoSplitFareDetails=true\ndataAssertionsPassed=true\nvisualValidation=unverified-pixel-review-required\npanelRuleExplanation=source-reviewed-absent\nscreenshot="+Path.GetFullPath(Screenshot)+"\nfixture=version3 initial4900 quoted4900 drivingAdjusted5390 base5290 bonus160 paid5450 satisfaction71/60 elapsed50 score0 clean3 request8 time12";}catch(Exception error){errorMessage=error.ToString();Debug.LogException(error);}
  }
  bool ok=string.IsNullOrEmpty(errorMessage);Directory.CreateDirectory(Path.GetDirectoryName(Report));File.WriteAllText(Path.GetFullPath(Report),"success="+ok+"\n"+(ok?details:"error="+errorMessage)+"\n");
  Debug.Log("RECEIPT_CAPTURE_TEST_"+(ok?"OK":"FAILED"));Application.Quit(ok?0:1);
 }
}
