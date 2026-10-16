using System;
class PracticeFeedbackTests {
 static object Build(LawEvent e){var t=typeof(LawLedger).Assembly.GetType("PracticeFeedback");if(t==null)throw new Exception("missing practice feedback report");return t.GetMethod("Build").Invoke(null,new object[]{e});}
 static string Field(object o,string n){return (string)o.GetType().GetField(n).GetValue(o);}
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 static void Main(){try{
  var pending=Build(null);Check(Field(pending,"outcome").Contains("미완료"),"no event cannot pass");
  var o=new RoadObservation{known=true,forward=SignalColor.Red,pedestrian=PedestrianState.None};
  var missed=Build(LawEvaluator.RightTurn("a",1,o,false));Check(Field(missed,"outcome").Contains("보완")&&Field(missed,"issues").Contains("정지선")&&Field(missed,"next").Contains("완전히"),"red missed stop gives location and correction");
  var good=Build(LawEvaluator.RightTurn("b",1,o,true));Check(Field(good,"outcome").Contains("완료")&&Field(good,"good").Contains("정지"),"observed stop is explained");
  o.forward=SignalColor.Green;var green=Build(LawEvaluator.RightTurn("c",1,o,false));Check(!Field(green,"outcome").Contains("100")&&Field(green,"good").Contains("녹색"),"R2 describes observed completion not invented score");
  o.dedicated=true;o.right=SignalColor.Red;var red=Build(LawEvaluator.RightTurn("d",1,o,true));Check(Field(red,"issues").Contains("전용 적색"),"dedicated red explanation");
  o.right=SignalColor.Green;o.pedestrian=PedestrianState.Crossing;var ped=Build(LawEvaluator.RightTurn("e",1,o,true));Check(Field(ped,"issues").Contains("횡단보도")&&Field(ped,"issues").Contains("횡단 중"),"pedestrian passage location and state");
  o.known=false;Check(Field(Build(LawEvaluator.RightTurn("f",1,o,true)),"outcome").Contains("보류"),"unknown observation not success");
  Check(Field(Build(new LawEvent{family="signal",action="직진"}),"outcome").Contains("경로"),"wrong turn distinct from successful right turn");
  Console.WriteLine("PASS practice feedback evidence, explanations and unknown boundaries");
 }catch(Exception e){Console.WriteLine(e.Message);Environment.Exit(1);}}
}
