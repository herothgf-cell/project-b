using System.Collections.Generic;
using UnityEngine;

public class DriveNotifications : MonoBehaviour {
 public DriveSession session;
 class Notice {public string text;public bool positive,passenger;public float since;}
 const float NoticeSeconds=4;
 readonly List<Notice> visible=new List<Notice>();LawLedger ledger;int score,lawCount,lawRevision;bool ready;string observedTrip;PassengerScoreView passenger;
 public bool FuelLow {get{return session.business.enabledCampaign&&session.business.ledger.Account!=null&&session.business.ledger.Account.Fuel<=15;}}
 public string VisibleText {get{return visible.Count==0?"":visible[0].text;}}
 public string[] VisibleTexts {get{return visible.ConvertAll(n=>(n.passenger?"만족도":"운전")+" | "+n.text).ToArray();}}
 public static Rect NoticeBounds(float width,float height,bool firstPerson){return CockpitUI.SlidingTipBounds(width,height,false,true,1,false,firstPerson);}
 void LateUpdate(){
  if(!session||!session.law||!session.business)return;
  var journal=session.GetComponent<DrivingScoreJournal>();if(journal&&!journal.Loaded)return;
  RefreshScores();
  ObserveFeed();
 }
 // Values are observed only. Every new notice is immediately visible, without a FIFO delay.
 public void RefreshScores(){
  visible.RemoveAll(n=>Time.unscaledTime-n.since>=NoticeSeconds);
  var jobs=session.jobs;var view=jobs.ScoreView;string trip=jobs.HasPassengerScore?session.law.tripId:null;
  if(!ready||ledger!=session.law.ledger){ledger=session.law.ledger;score=ledger.TotalScore;lawCount=ledger.Count;lawRevision=ledger.Revision;observedTrip=trip;passenger=view;visible.Clear();ready=true;return;}
  if(trip!=observedTrip){visible.RemoveAll(n=>n.passenger);observedTrip=trip;passenger=view;}
  else if(trip!=null){
   int after=PassengerScoreView.Calculate(view.start,view.drive,passenger.request,passenger.time,passenger.comfort);if(after!=passenger.value)Push(ScoreFeedback.Text(session.manners.lastEvent,after-passenger.value),after>passenger.value,true);
   int requested=PassengerScoreView.Calculate(view.start,view.drive,view.request,passenger.time,passenger.comfort);if(requested!=after)Push(ScoreFeedback.Text(requested>after?"요청 이행":"요청 미이행",requested-after),requested>after,true);
   int comforted=PassengerScoreView.Calculate(view.start,view.drive,view.request,passenger.time,view.comfort);if(comforted!=requested)Push(ScoreFeedback.Text("안정 주행 안심",comforted-requested),comforted>requested,true);requested=comforted;
   if(view.value!=requested)Push(ScoreFeedback.Text("도착 시간",view.value-requested),view.value>requested,true);passenger=view;
  }
  if(lawRevision!=ledger.Revision){var events=ledger.Events;int prefix=100;for(int i=0;i<Mathf.Min(lawCount,events.Length);i++)prefix=LawLedger.ScoreAfter(prefix,events[i]);if(prefix!=score)Push(ScoreFeedback.Text(lawCount==events.Length?ScoreFeedback.LawReason(ledger.LastChanged,prefix-score):"법규 사건 갱신",prefix-score),prefix>score);score=prefix;for(int i=lawCount;i<events.Length;i++){int next=LawLedger.ScoreAfter(score,events[i]);if(next!=score)Push(ScoreFeedback.Text(ScoreFeedback.LawReason(events[i],next-score),next-score),next>score);score=next;}lawCount=events.Length;lawRevision=ledger.Revision;}
 }
 void Push(string text,bool good,bool passenger=false){Append((passenger?"만족도":"운전")+" · "+text);visible.Insert(0,new Notice{text=text,positive=good,passenger=passenger,since=Time.unscaledTime});if(visible.Count>4)visible.RemoveAt(visible.Count-1);}
 readonly List<string> feed=new List<string>();
 static readonly string[] Tips={"정차한 시설은 F로 이용하세요.","M 지도에서 승차지와 목적지를 확인할 수 있습니다.","숙소로 돌아오면 주차 칸에 정차하고 F로 퇴근하세요."};
 int fuelBand,tipIndex,revision,drawnRevision=-1;
 float fuelUntil,nextTip;
 string fuelLine;
 string lastBusiness,lastService,lastDestination,lastContext;
 Vector2 feedScroll;
 public IReadOnlyList<string> Feed {get{return feed;}}
 public bool FuelActionVisible {get{return FuelLow&&Time.time<fuelUntil;}}
 void Append(string line){feed.Add(line);if(feed.Count>12)feed.RemoveAt(0);revision++;}
 void Changed(ref string previous,string line){if(line==previous)return;previous=line;if(!string.IsNullOrEmpty(line))Append(line);}
 public void ObserveFeed(){
  if(!session.StartupReady||session.paused||session.business.preparing||session.business.ShowingSummary)return;
  var account=session.business.ledger.Account;
  int band=!FuelLow?0:account.Fuel<=5?3:account.Fuel<=10?2:1;
  if(fuelLine!=null&&(band==0||band>fuelBand||Time.time>=fuelUntil)){
   feed.Remove(fuelLine);fuelLine=null;revision++;
  }
  if(band>fuelBand){
   fuelLine="연료 부족 "+account.Fuel.ToString("0")+"% / 주유가 필요합니다.";Append(fuelLine);
   fuelUntil=Time.time+12;
  }
  fuelBand=band;
  Changed(ref lastBusiness,session.business.notice);
  Changed(ref lastService,session.services.notice);
  Changed(ref lastDestination,session.services.destination);
  if(Time.time>=nextTip){
   var zones=GetComponent<ServiceLawZones>();
   string tip=null;
   if(zones&&zones.Assigned!=null&&!zones.Completed){
    float distance=Vector3.Distance(session.taxi.transform.position,zones.Assigned.junction.center);
    if(distance<260)tip=zones.AssignedTip(distance);
   }
   if(tip==null){tip=Tips[tipIndex];tipIndex=(tipIndex+1)%(session.services.HasHomeBase?Tips.Length:Tips.Length-1);}
   Append("TIP · "+tip);nextTip=Time.time+20;
  }
 }
 public void DrawFeed(Rect bounds,string context){
  Changed(ref lastContext,context);
  var style=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=UiReadability.Font(14),wordWrap=true};
  style.normal.textColor=CasualUI.Ink;
  float width=Mathf.Max(1,bounds.width-26),total=0;
  foreach(var line in feed)total+=style.CalcHeight(new GUIContent(line),width)+5;
  float height=Mathf.Max(total,bounds.height);
  if(drawnRevision!=revision){feedScroll.y=Mathf.Max(0,height-bounds.height);drawnRevision=revision;}
  feedScroll=GUI.BeginScrollView(bounds,feedScroll,new Rect(0,0,width,height));
  float y=0;foreach(var line in feed){
   float row=style.CalcHeight(new GUIContent(line),width);
   GUI.Label(new Rect(2,y,width,row),line,style);y+=row+5;
  }
  GUI.EndScrollView();
 }
}
