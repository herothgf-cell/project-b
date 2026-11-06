using System;
using UnityEngine;
public static class AccidentUiRevisionChecks {
 static void Check(bool value,string reason){if(!value)throw new Exception("ACCIDENT_UI "+reason);}
 static AccidentState State(string kind,string severity){var asm=typeof(AccidentState).Assembly;var policy=asm.GetType("AccidentSeverityPolicy");var k=Enum.Parse(asm.GetType("AccidentKind"),kind);var s=Enum.Parse(asm.GetType("AccidentSeverity"),severity);var definition=policy.GetMethod("Resolve").Invoke(null,new[]{k,s});var state=new AccidentState();typeof(AccidentState).GetMethod("BeginResolved").Invoke(state,new[]{(object)(kind+severity),"fixture",definition});return state;}
 public static void Run(){
  var type=typeof(AccidentState).Assembly.GetType("AccidentPresentation");Check(type!=null,"compact result presentation exists");
  var minor=State("Person","Minor");var major=State("Person","Major");
  Check((string)type.GetMethod("FeeText").Invoke(null,new object[]{minor})=="3,000원","minor fee uses selected event");Check((string)type.GetMethod("FeeText").Invoke(null,new object[]{major})=="15,000원","major fee is not old fixed text");
  Check((string)type.GetMethod("TimeLossText").Invoke(null,new object[]{major})=="−4:00","large time loss is a readable duration");
  Check((string)type.GetMethod("ResumeAction").Invoke(null,new object[]{minor,false})=="운행 재개","minor returns to current location");Check((string)type.GetMethod("ResumeAction").Invoke(null,new object[]{major,false})=="공업사에서 재개","major cannot promise roadside resume");
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1920,1080)}){var r=(Rect)type.GetMethod("Bounds").Invoke(null,new object[]{size.x,size.y});Check(r.x>=0&&r.y>=0&&r.xMax<=size.x&&r.yMax<=size.y,"compact panel fits target screen");}
  var score=typeof(AccidentState).Assembly.GetType("AccidentScore");Check(score!=null,"game driving score has explicit accident path");var apply=score.GetMethod("Apply");var ledger=new LawLedger();Check((bool)apply.Invoke(null,new object[]{ledger,major,1})&&ledger.TotalScore==60,"major person event actually deducts driving score40");Check(!(bool)apply.Invoke(null,new object[]{ledger,major,1})&&ledger.TotalScore==60,"same event does not deduct twice");
  var copy=major.Copy();typeof(AccidentState).GetField("scoreApplied").SetValue(copy,false);Check(!(bool)apply.Invoke(null,new object[]{ledger,copy,1})&&ledger.TotalScore==60,"replayed event with missing flag still deduplicates by ledger id");
  var legacy=new AccidentState();legacy.Begin("legacy","ambient:legacy");var oldScore=new LawLedger();Check(!(bool)apply.Invoke(null,new object[]{oldScore,legacy,1})&&oldScore.TotalScore==100,"legacy pending event does not get retroactive deduction");
  Debug.Log("ACCIDENT_UI_REVISION_OK presentation / score / replay / legacy / resolutions");
 }
}
