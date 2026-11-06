using System;
using UnityEngine;

public static class NumericChoiceInputChecks {
 static int checks;
 static void Check(bool ok,string why){checks++;if(!ok)throw new Exception("NUMERIC_CHOICE: "+why);}
 public static void Run(){
  checks=0;int frame=int.MinValue+100;
  foreach(RequestKind kind in Enum.GetValues(typeof(RequestKind))){
   var choices=PassengerRequestChoices.For(kind);
   for(int i=0;i<choices.Length;i++){
    int row=i;
    Check(NumericChoiceInput.Label(row)==(row+1).ToString(),kind+" contiguous label");
    Check(NumericChoiceInput.Read(choices.Length,n=>true,k=>k==NumericChoiceInput.Key(row,false),frame++)==row,kind+" top row selects displayed reply "+choices[row].reply);
    Check(NumericChoiceInput.Read(choices.Length,n=>true,k=>k==NumericChoiceInput.Key(row,true),frame++)==row,kind+" keypad selects displayed reply");
   }
  }
  Check(NumericChoiceInput.Read(2,n=>true,k=>k==KeyCode.Alpha2,frame++)==1,"two-option final row is 2");
  Check(NumericChoiceInput.Read(2,n=>true,k=>k==KeyCode.Alpha3,frame++)==-1,"out-of-range key ignored");
  Check(NumericChoiceInput.Read(3,n=>n!=1,k=>k==KeyCode.Alpha2,frame++)==-1,"disabled row is not remapped");
  foreach(var key in new[]{KeyCode.Y,KeyCode.I,KeyCode.O,KeyCode.U})Check(NumericChoiceInput.Read(4,n=>true,k=>k==key,frame++)==-1,"legacy key ignored "+key);
  Check(NumericChoiceInput.Read(3,n=>true,k=>k==KeyCode.Alpha1||k==KeyCode.Alpha2,frame)==0,"simultaneous keys choose once");
  Check(NumericChoiceInput.Read(3,n=>true,k=>true,frame)==-1,"same-frame next panel blocked");
  Check(!NumericChoiceInput.TryConsume(frame),"same-frame mouse blocked");
  Check(NumericChoiceInput.Read(3,n=>false,k=>true,++frame)==-1,"unavailable choices ignored");
  Debug.Log("NUMERIC_CHOICE_CHECKS_OK checks="+checks);
 }
}
