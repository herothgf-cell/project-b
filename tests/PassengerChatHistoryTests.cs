using System;
class PassengerChatHistoryTests {
 static int checks;static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 static void Main(){
  var h=new PassengerChatHistory();string first="기사: 오늘은 어떤 하루를 보내셨나요?\n승객: 목적지까지 잘 부탁드려요.";
  Check(PassengerChatHistory.Speech(first)=="목적지까지 잘 부탁드려요.","only passenger speech is presented");
  Check(h.Next(new[]{first,"다른 이야기"})==first,"first authored dialogue preserved");h.Remember(first);
  Check(h.Next(new[]{first,"승객: 목적지까지 잘 부탁드려요.","다른 이야기"})=="다른 이야기","same visible speech cannot repeat under another key");
  var restored=new PassengerChatHistory();restored.Restore(h.Capture(),first);Check(restored.Next(new[]{first})==null,"save restore retains used dialogue");
  restored.Remember("다른 이야기");Check(restored.Next(new[]{first,"다른 이야기"})==null,"exhausted pool stays silent");
  restored.Restore(null,first);Check(restored.Next(new[]{first})==null,"legacy last reply migrates without replay");
  Check(new PassengerChatHistory().Next(new[]{first})==first,"new passenger has fresh history");
  var longLine=new string('가',4000);Check(PassengerChatHistory.Speech(longLine)==longLine,"long configured speech is preserved");
  h.Remember("기사: 질문만");Check(h.Next(new[]{"기사: 질문만","새 이야기"})=="새 이야기","empty passenger speech skipped");
  bool rejected=false;try{h.Restore(new string[17],null);}catch(ArgumentException){rejected=true;}Check(rejected,"oversized snapshot rejected");
  Console.WriteLine("PASSENGER_CHAT_HISTORY_OK checks="+checks);
  Check(PassengerPresentation.Name("offer-123")==PassengerPresentation.Name("offer-123"),"name survives the same saved offer identity");
  var names=new System.Collections.Generic.HashSet<string>();for(int i=0;i<100;i++)names.Add(PassengerPresentation.Name("offer-"+i));Check(names.Count>=20,"offer identities produce varied names");
  Check(PassengerPresentation.Speech("손님  “고맙습니다.”")=="“고맙습니다.”","quoted customer label removed");
  Check(PassengerPresentation.Speech("승객: 안녕하세요\n손님 · 반갑습니다")=="안녕하세요\n반갑습니다","line speaker labels removed");
  Check(PassengerPresentation.Speech("손님이 잠들었습니다 · 승객 만족도 70")=="손님이 잠들었습니다 · 승객 만족도 70","meaningful customer references preserved");
  Check(PassengerPresentation.Speech(longLine)==longLine,"custom long speech remains verbatim");
  Console.WriteLine("PASSENGER_PRESENTATION_OK total="+checks);
 }
}
