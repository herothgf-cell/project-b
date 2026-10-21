using System;
class PrototypeCompletionTests {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static object Call(object o,string name,params object[] args){var method=o.GetType().GetMethod(name);Check(method!=null,"Missing behavior: "+name);return method.Invoke(o,args);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e.Message);Environment.Exit(1);}} static void Run(){
  var law=new LawLedger();Check((string)Call(law,"DaySummary",1)=="평가 기록 없음","no opportunity is not a scored day");
  var bypass=LawEvaluator.Exit("b",1,true,false,false,false);law.Record(bypass);Check(((string)Call(law,"DaySummary",1)).Contains("유효 0"),"bypass excludes valid opportunity");
  law.Record(LawEvaluator.Exit("c",1,true,true,true,true));var summary=(string)Call(law,"DaySummary",1);Check(summary.Contains("사건 2")&&summary.Contains("위반 2")&&summary.Contains("당일 감점 15점"),"event count distinct from rule count and penalty");
  Check((string)Call(law,"DaySummary",2)=="평가 기록 없음","new day keeps old events but no score evidence");
  var request=new PassengerRequest();request.Begin("a",RequestKind.Quiet);request.DeferSafety();Check((string)Call(request,"ResponseText")=="안전상 보류 · 감점 없음","defer feedback");request.Accept();Check((string)Call(request,"ResponseText")=="안전상 보류 · 감점 없음","response cannot be overwritten");request.Begin("b",RequestKind.Eta);request.Accept();Check(((string)Call(request,"ResponseText")).Contains("예상"),"ETA acknowledgement");
  var practice=new PracticeResult{events=1,score=100};Check((string)Call(practice,"EvaluationText")=="유효 평가 없음","nonempty no-opportunity practice is not a score");
  practice.validFindings=1;Check(((string)Call(practice,"EvaluationText")).StartsWith("100점"),"valid practice may show score");
  Console.WriteLine("PASS prototype completion history and request states");
 }
}
