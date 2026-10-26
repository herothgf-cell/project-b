using System;
using System.Reflection;
using UnityEngine;

// Focused regression for the shipping serializer and the score/receipt boundary.
public static class PassengerScoreRecoveryTests {
 static int checks;
 static void Check(bool ok,string name){checks++;if(!ok)throw new Exception("PASSENGER_SCORE_RECOVERY_FAIL "+name);}
 public static void Run(){
  var ledger=new MannerLedger();ledger.Penalize("brake",2,1,0);
  var recover=typeof(MannerLedger).GetMethod("RecoverStable");Check(recover!=null,"stable recovery entry point");
  Check((int)recover.Invoke(ledger,new object[]{2})==2&&ledger.Score==100,"motion loss recovers only once");
  var view=PassengerScoreView.Live(68,0,70,ledger.Score,0);Check(view.value==68&&view.required==70,"live gauge uses recovered M and still compares R");
  var reward=PassengerReward.CalculateDetailed(100,70,5000,MannerLedger.Payment(5000,100,70),0,true,60,0,0,0,false);
  Check(reward.satisfaction==60&&reward.baseFare==5000&&reward.bonus==0,"S60 below R70 keeps full M100 base and no bonus");
  var reduced=PassengerReward.CalculateDetailed(68,70,5000,MannerLedger.Payment(5000,68,70),4,true,100,0,0,0,false);
  Check(reduced.baseFare==3500&&reduced.satisfaction==72&&reduced.bonus>0,"M below R pays 70 percent while S above R earns bonus");
  var late=PassengerReward.CalculateDetailed(100,70,5000,0,30,false,60,0,0,0,false);
  Check(late.baseFare==0&&late.bonus==0&&late.satisfaction==90,"late arrival pays zero despite high M and S");
  var result=new PassengerResult{initialFare=5000,quoted=5000,baseFare=5000,paid=5000,bonus=0,score=100,satisfaction=60,required=70,bonusReason="요구 만족도 미달"};
  Check(RideReceiptSummary.Money(result).Split('\n').Length==3,"compact receipt remains three money lines");
  var rule=typeof(RideReceiptSummary).GetMethod("Rule");Check(rule!=null,"receipt explains separate M and S criteria");
  var explanation=(string)rule.Invoke(null,new object[]{result});Check(explanation.Contains("M 100")&&explanation.Contains("S 60")&&explanation.Contains("R 70"),"receipt makes S60/R70 result explicit");
  var taxi=new GameObject("score recovery snapshot");try{taxi.AddComponent<Rigidbody>();var manners=taxi.AddComponent<DrivingManners>();manners.ledger.Penalize("brake",4,1,0);recover.Invoke(manners.ledger,new object[]{2});var snapshot=JsonUtility.FromJson<DrivingMannersSnapshot>(JsonUtility.ToJson(manners.CaptureLive()));var restored=taxi.AddComponent<DrivingManners>();restored.RestoreLive(snapshot);Check(restored.ledger.Score==98&&restored.ledger.MotionDebt==2&&restored.ledger.StableRecovered==2,"JsonUtility restores recovery debt and trip cap");}finally{UnityEngine.Object.DestroyImmediate(taxi);}
  Debug.Log("PASSENGER_SCORE_RECOVERY_TESTS_OK checks="+checks);
 }
}
