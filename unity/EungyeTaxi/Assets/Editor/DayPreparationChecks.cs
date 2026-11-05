using System;
using System.Reflection;
using UnityEngine;

// Reflection keeps the RED phase compilable before the presentation model exists.
public static class DayPreparationChecks {
 static int checks;
 static Type Model {get{return typeof(EconomyLedger).Assembly.GetType("DayPreparationPresentation");}}
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception("DAY_PREPARATION: "+message);Debug.Log("DAY_PREPARATION_PASS "+message);}
 static object Field(object value,string name){var field=value.GetType().GetField(name);Check(field!=null,"presentation exposes "+name);return field.GetValue(value);}
 static object Create(EconomyLedger ledger){Check(Model!=null,"next-day preparation presentation exists");var method=Model.GetMethod("Create");Check(method!=null,"presentation accepts the live economy ledger");return method.Invoke(null,new object[]{ledger});}
 static object Stat(object view,string id){foreach(var stat in (Array)Field(view,"stats"))if((string)Field(stat,"id")==id)return stat;throw new Exception("Missing stat "+id);}
 static int Expense(object view,string kind){foreach(var row in (Array)Field(view,"expenses"))if((string)Field(row,"kind")==kind)return (int)Field(row,"amount");throw new Exception("Missing expense "+kind);}
 static EconomyLedger Fixture(double recovery=60){var settings=PrototypeSettings.ForThreeDays();settings.startCash=0;settings.initialFuel=0;settings.nightlyFatigueRecovery=recovery;return new EconomyLedger(settings);}
 static void Next(EconomyLedger ledger){ledger.RequestClose();Check(ledger.Settle(),"fixture settles the current day");Check(ledger.NextDay(),"fixture advances to preparation boundary");}
 public static void Routing(){
  Check(Model!=null,"next-day preparation presentation exists");var method=Model.GetMethod("ShouldShow");Check(method!=null,"dedicated preparation has an explicit route");
  Func<int,bool,bool,bool,bool> show=(day,preparing,restarting,blocked)=>(bool)method.Invoke(null,new object[]{day,preparing,restarting,blocked});
  Check(!show(1,true,false,false),"first day retains the one-click single menu");
  Check(show(2,true,false,false)&&show(3,true,false,false),"second and third preparation days use the dedicated popup");
  Check(!show(2,false,false,false)&&!show(2,true,true,false)&&!show(2,true,false,true),"live driving, restart and recovery retain their existing routes");
 }
 public static void Settlement(){
  var ledger=Fixture();var account=ledger.Account;
  Check(account.RecoverFuel("empty",1)&&account.Wash("wash",1)&&account.Repair("repair",1,10)&&account.Tow("tow",1)&&account.Meal("meal",1),"fixture accrues actual facility expenses as debt");
  Check(ledger.RecordFare("repayment-fare",1000)&&account.Repay("repayment",1000,1),"fixture repays debt without another expense");
  ledger.Driver.satiety=0;ledger.Driver.fatigue=14;Next(ledger);
  var view=Create(ledger);Check((int)Field(view,"previousDay")==1,"expense source is the immediately previous day");
  // 1,800 fuel + 3,000 meal + 500 parking + 2,000 wash + 2,000 repair + 3,000 tow.
  // Repaying 1,000 lowers outstanding debt to 11,300 without reducing the 12,300 expense.
  Check(account.Debt==11300,"fixture distinguishes outstanding debt after repayment from incurred cost");
  int totalExpense=(int)Field(view,"totalExpense");Check(totalExpense==12300,"cost total includes accrued debt and excludes duplicate debt repayment (actual "+totalExpense+")");
  Check(Expense(view,"fuel")==1800&&Expense(view,"meal")==3000&&Expense(view,"parking")==500,"fuel recovery, meal and actual parking are included");
  Check(Expense(view,"wash")==2000&&Expense(view,"repair")==2000&&Expense(view,"tow")==3000&&Expense(view,"other")==0,"care costs are itemized without invented menu prices");
  Check((Array)Field(view,"stats") is Array&&((Array)Field(view,"stats")).Length==4,"preparation shows precisely cash fuel satiety fatigue");
  var cash=Stat(view,"cash");var fuel=Stat(view,"fuel");var satiety=Stat(view,"satiety");var fatigue=Stat(view,"fatigue");
  Check((double)Field(cash,"value")==0&&(double)Field(cash,"delta")==0&&(bool)Field(cash,"hasDelta"),"zero cash is real and overnight cash change is zero");
  Check((double)Field(fuel,"value")==20&&(double)Field(fuel,"delta")==0,"overnight fuel is carried from the previous close");
  Check((double)Field(satiety,"value")==0&&(double)Field(satiety,"delta")==0&&(bool)Field(satiety,"hasDelta"),"zero satiety is preserved rather than replaced with a default");
  Check((double)Field(fatigue,"value")==0&&(double)Field(fatigue,"delta")==-14,"fatigue delta uses the saved close and recovery clamps at zero");
 }
 public static void PreviousDayAndLegacy(){
  var ledger=Fixture(7);ledger.Driver.satiety=25;ledger.Driver.fatigue=80;Next(ledger);
  var view=Create(ledger);Check((double)Field(Stat(view,"fatigue"),"delta")==-7,"configured seven-point overnight recovery is not hardcoded to sixty");
  ledger.Driver.satiety=0;ledger.Driver.fatigue=5;Next(ledger);view=Create(ledger);
  Check((int)Field(view,"previousDay")==2&&(double)Field(Stat(view,"fatigue"),"delta")==-5,"third-day comparison uses day two rather than day one");
  Check((int)Field(view,"totalExpense")==500,"day-three expense section excludes earlier days' costs");
  ledger.Last.driver=null;view=Create(ledger);
  Check(!(bool)Field(Stat(view,"satiety"),"hasDelta")&&!(bool)Field(Stat(view,"fatigue"),"hasDelta"),"legacy missing driver close is explicitly unknown");
  Check(((string)Field(Stat(view,"satiety"),"deltaText")).Contains("기록 없음"),"unknown change is labeled rather than fabricated");
  Check((bool)Field(Stat(view,"cash"),"hasDelta")&&(bool)Field(Stat(view,"fuel"),"hasDelta"),"missing old driver data does not hide known cash and fuel carryover");
 }
 public static void Bounds(){
  Check(Model!=null,"next-day preparation presentation exists");var method=Model.GetMethod("PanelBounds");Check(method!=null,"popup has a measurable screen layout");
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1920,1080)}){var rect=(Rect)method.Invoke(null,new object[]{size.x,size.y});Check(rect.xMin>=0&&rect.yMin>=0&&rect.xMax<=size.x&&rect.yMax<=size.y,"popup fits "+size.x+"x"+size.y);Check(rect.width>=760&&rect.height>=590,"preparation remains readable at "+size.x);}
 }
 public static void Run(){checks=0;int errors=0;foreach(Action test in new Action[]{Routing,Settlement,PreviousDayAndLegacy,Bounds})try{test();}catch(Exception ex){errors++;Debug.LogException(ex);}if(errors>0)throw new Exception("DAY_PREPARATION failed groups="+errors);Debug.Log("DAY_PREPARATION_OK checks="+checks);}
}
