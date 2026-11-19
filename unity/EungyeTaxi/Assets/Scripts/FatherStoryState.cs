using System;
using System.Collections.Generic;

public sealed class FatherStoryPage {
 public readonly string title,text,date,unlock,hint;
 public FatherStoryPage(string title,string text,string date,string unlock,string hint){
  this.title=title;this.text=text;this.date=date;this.unlock=unlock;this.hint=hint;
 }
}

[Serializable]
public sealed class FatherStoryAnswer {
 public int day,question,choice;
 public string trip;
}

[Serializable]
public sealed class FatherStoryState {
 public const int CurrentVersion=1;
 public int version=CurrentVersion;
 public bool introSeen,firstVacancy,goalHalf,goalDone,firstPurchase;
 public int pages,wallPromptDay,questionDay,questionIndex;
 public string wallChoice="",questionTrip="";
 public List<FatherStoryAnswer> answers=new List<FatherStoryAnswer>();
 public bool FoundNotebook {get{return HasPage(0);}}

 public static readonly FatherStoryPage[] Pages={
  new FatherStoryPage("아침 장사","아침엔 아파트 단지 앞이 제일 바쁘다.\n수인로는 8시 반 넘으면 막힌다.\n첫 손님한테는 먼저 인사.","1994. 3. 은계","1일차 첫 운송 완료","아침 주거 지역 손님 힌트"),
  new FatherStoryPage("비 오는 날","비 오는 날엔 우산 싸맬 봉투를 챙겨.","2003. 7. 은계","비 오는 날 마감 또는 동네 ★ 3곳","젖은 우산은 탑승 전에 감싸 두기"),
  new FatherStoryPage("금요일 밤","금요일 밤 손님은 말이 많다. 들어주면 된다.","2014. 6. 은계","첫 금요일 카드","음주·수다 승객의 이야기를 들어 주기"),
  new FatherStoryPage("다시 타고 싶은 차","빨리 가는 것보다 다시 타고 싶은 차.","2021. 4. 은계","무위반 하루","안전 운행으로 남기는 하루 기록"),
  new FatherStoryPage("다음 사람에게","이 차 오래 탔다. 누가 몰든 자기 길을 찾길.","2024. 10. 은계","목표 50%","아버지가 남긴 마지막 메모")
 };

 public bool HasPage(int page){return page>=0&&page<Pages.Length&&(pages&(1<<page))!=0;}
 public bool Unlock(int page){
  if(page<0||page>=Pages.Length||HasPage(page))return false;
  pages|=1<<page;return true;
 }
 public void NeighborhoodProgress(int starred){if(starred>=3)Unlock(1);}
 public void FridayOperated(){Unlock(2);}
 public bool Ask(int day,int variant,string trip){
  if(day<2||questionDay>=day||string.IsNullOrEmpty(trip))return false;
  questionDay=day;questionIndex=variant%3;questionTrip=trip;return true;
 }
 public bool Pending(string trip){
  return !string.IsNullOrEmpty(trip)&&trip==questionTrip&&(answers.Count==0||answers[answers.Count-1].day<questionDay);
 }
 public bool Answer(string trip,int choice){
  if(choice<0||choice>2||!Pending(trip))return false;
  answers.Add(new FatherStoryAnswer{day=questionDay,question=questionIndex,choice=choice,trip=trip});return true;
 }
 public bool UndoAnswer(){
  if(answers.Count==0)return false;
  answers.RemoveAt(answers.Count-1);return true;
 }
 public int Satisfaction(string trip){
  return !string.IsNullOrEmpty(trip)&&answers.Count>0&&answers[answers.Count-1].trip==trip?ContentTuning.Int("backstory.reply.satisfaction",1):0;
 }
 public string Question(){return ContentTuning.TextOrCatalog("backstory.question."+questionIndex);}
 public string Reply(int choice){return ContentTuning.TextOrCatalog("backstory.question."+questionIndex+".reply."+choice);}
 public FatherStoryState Copy(){
  var result=(FatherStoryState)MemberwiseClone();
  result.answers=new List<FatherStoryAnswer>(answers);
  return result;
 }
 public void Validate(int day){
  if(version!=CurrentVersion||pages<0||pages>=1<<Pages.Length||wallPromptDay<0||wallPromptDay>day||questionDay<0||questionDay>day||questionIndex<0||questionIndex>2||
     wallChoice!="car"&&wallChoice!="home"&&wallChoice!="none"&&wallChoice!=""||(questionTrip??"").Length>200||questionDay>0&&string.IsNullOrEmpty(questionTrip))
   throw new ArgumentException("Invalid father story state");
  if(answers==null)answers=new List<FatherStoryAnswer>();
  int previous=1;
  foreach(var answer in answers){
   if(answer==null||answer.day<=previous||answer.day>questionDay||answer.question<0||answer.question>2||answer.choice<0||answer.choice>2||
      string.IsNullOrEmpty(answer.trip)||answer.trip.Length>200||answer.day==questionDay&&(answer.question!=questionIndex||answer.trip!=questionTrip))
    throw new ArgumentException("Invalid father story answer");
   previous=answer.day;
  }
 }
}
