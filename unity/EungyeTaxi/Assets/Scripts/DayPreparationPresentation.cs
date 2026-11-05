using System;
using UnityEngine;

// Read-only projection of the next-day boundary. No new checkpoint data is needed.
public sealed class DayPreparationStat {
 public string id,label,valueText,deltaText;public int glyph;public double value,delta;public bool hasDelta;
}
public sealed class DayPreparationExpense {public string kind,label;public int amount;}
public sealed class DayPreparationView {
 public int day,previousDay,totalExpense;public string title,comparisonSource,expenseSource;
 public DayPreparationStat[] stats;public DayPreparationExpense[] expenses;
}
public static class DayPreparationPresentation {
 public static bool ShouldShow(int day,bool preparing,bool restart,bool blocked){return day>1&&preparing&&!restart&&!blocked;}
 public static Rect PanelBounds(float width,float height){float scale=Mathf.Min(1.25f,width/1280f,height/720f);return new Rect((width-760*scale)/2,(height-590*scale)/2,760*scale,590*scale);}
 static DayPreparationStat Stat(string id,string label,int glyph,double value,double? delta,string suffix){
  string format=id=="cash"?"N0":"0.#";
  return new DayPreparationStat{id=id,label=label,glyph=glyph,value=value,valueText=value.ToString(format)+suffix,hasDelta=delta.HasValue,delta=delta.GetValueOrDefault(),deltaText=delta.HasValue?(delta.Value>=0?"+":"−")+Math.Abs(delta.Value).ToString(format)+suffix:"전일 기록 없음"};
 }
 public static DayPreparationView Create(EconomyLedger ledger){
  if(ledger==null||ledger.Account==null||ledger.Driver==null)throw new ArgumentException("Preparation requires an active campaign ledger");
  var last=ledger.Last;bool previous=last!=null&&last.day==ledger.Day-1;var driver=previous?last.driver:null;var account=ledger.Account;int day=previous?last.day:0;
  var view=new DayPreparationView{day=ledger.Day,previousDay=day,title=ledger.Day+"일차 · 영업 준비",comparisonSource=previous?day+"일차 마감 대비":"전일 마감 기록 없음",expenseSource=previous?day+"일차 정산 내역":"전일 정산 기록 없음"};
  // NextDay carries cash/fuel through unchanged. Their end-of-day values are not
  // stored in DayRecord, so use that boundary contract rather than initial values.
  view.stats=new[]{Stat("cash","현금",4,account.Cash,previous?(double?)0:null,"원"),Stat("fuel","연료",8,account.Fuel,previous?(double?)0:null,"%"),Stat("satiety","포만감",9,ledger.Driver.satiety,driver==null?(double?)null:ledger.Driver.satiety-driver.satiety,""),Stat("fatigue","피로",11,ledger.Driver.fatigue,driver==null?(double?)null:ledger.Driver.fatigue-driver.fatigue,"")};
  string[] kinds={"fuel","meal","parking","wash","repair","tow","other"};string[] labels={"주유·연료 회복","식사","주차","세차","수리","견인","기타"};view.expenses=new DayPreparationExpense[kinds.Length];
  int itemized=0;view.totalExpense=previous?account.Expense(day,null):0;
  for(int i=0;i<kinds.Length;i++){
   int cost=previous?(kinds[i]=="other"?view.totalExpense-itemized:account.Expense(day,kinds[i])+(kinds[i]=="fuel"?account.Expense(day,"fuel-recovery"):0)):0;
   view.expenses[i]=new DayPreparationExpense{kind=kinds[i],label=labels[i],amount=cost};itemized+=cost;
  }
  return view;
 }
}
