using System;
using System.Collections.Generic;
using UnityEngine;

public enum FeedKind {Passenger,Law,Fuel,Destination,Situation,System,Tip}

// One chat-style row in the left drive notice panel. The kind decides its card shape.
public class FeedItem {
 public FeedKind kind;public string text,trip;public int delta,minute=-1;public bool scored;
 public string Line {get{switch(kind){
  case FeedKind.Passenger:return "만족도 · "+text;
  case FeedKind.Law:return "운전 · "+text;
  case FeedKind.Tip:return "TIP · "+text;
  default:return text;}}}
 public string Title {get{int at=text.IndexOf(" · ",StringComparison.Ordinal);return at<0?text:text.Substring(0,at);}}
 public string Body {get{int at=text.IndexOf(" · ",StringComparison.Ordinal);return at<0?"":text.Substring(at+3);}}
}

// A button drawn inside a pinned card (fuel guide, occupied stop choices, ride evaluation).
public class FeedButton {public string label,note;public bool enabled=true,primary;public Action click;}

public class DriveNotifications : MonoBehaviour {
 public DriveSession session;
 class Notice {public string text;public bool positive,passenger;public float since;}
 const float NoticeSeconds=4;
 const int FeedLimit=12;
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
   int after=PassengerScoreView.Calculate(view.start,view.drive,passenger.request,passenger.time,passenger.comfort);if(after!=passenger.value)Push(session.manners.lastEvent,after-passenger.value,true);
   int requested=PassengerScoreView.Calculate(view.start,view.drive,view.request,passenger.time,passenger.comfort);if(requested!=after)Push(requested>after?"요청 이행":"요청 미이행",requested-after,true);
   int comforted=PassengerScoreView.Calculate(view.start,view.drive,view.request,passenger.time,view.comfort);if(comforted!=requested)Push("안정 주행 안심",comforted-requested,true);requested=comforted;
   if(view.value!=requested)Push("도착 시간",view.value-requested,true);passenger=view;
  }
  if(lawRevision!=ledger.Revision){var events=ledger.Events;int prefix=100;for(int i=0;i<Mathf.Min(lawCount,events.Length);i++)prefix=LawLedger.ScoreAfter(prefix,events[i]);if(prefix!=score)Push(lawCount==events.Length?ScoreFeedback.LawReason(ledger.LastChanged,prefix-score):"법규 사건 갱신",prefix-score);score=prefix;for(int i=lawCount;i<events.Length;i++){int next=LawLedger.ScoreAfter(score,events[i]);if(next!=score)Push(ScoreFeedback.LawReason(events[i],next-score),next-score);score=next;}lawCount=events.Length;lawRevision=ledger.Revision;}
 }
 void Push(string reason,int delta,bool passenger=false){
  string text=ScoreFeedback.Text(reason,delta);
  Add(new FeedItem{kind=passenger?FeedKind.Passenger:FeedKind.Law,text=ScoreFeedback.Reason(reason),delta=delta,scored=true,trip=passenger?observedTrip:null});
  visible.Insert(0,new Notice{text=text,positive=delta>0,passenger=passenger,since=Time.unscaledTime});if(visible.Count>4)visible.RemoveAt(visible.Count-1);
 }

 // ---------- feed model ----------
 readonly List<FeedItem> items=new List<FeedItem>();
 public const string HungerTip="포만감이 25 아래면 피로가 1.5배, 10 아래면 2배 빨리 쌓입니다. 기사식당이나 김밥집에서 식사하세요.";
 static readonly string[] Tips={"시설 이용 · 정차 후 F","지도 · M","배고프면 피로가 빨리 쌓여요","퇴근 · 숙소 칸에서 F","자세한 규칙 · F1 가이드"};
 int fuelBand,hungerBand,tipIndex,revision,drawnRevision=-1;
 float fuelUntil,nextTip;
 FeedItem fuelItem;
 string lastBusiness,lastService,lastDestination,lastContext;
 Vector2 feedScroll;bool atBottom=true;
 public bool ScoresOnly;
 public int Unseen {get;private set;}
 public int TipsPosted {get;private set;}
 public IReadOnlyList<FeedItem> Items {get{return items;}}
 public IReadOnlyList<string> Feed {get{return items.ConvertAll(i=>i.Line);}}
 public bool FuelActionVisible {get{return FuelLow&&Time.time<fuelUntil;}}
 int Minute(){
  if(session==null||session.simulationMatch||session.business==null)return -1;var e=session.business.ledger;
  if(e==null||e.Settings==null||e.Settings.daySeconds<=0)return -1;return 480+(int)(e.Elapsed*720/e.Settings.daySeconds);
 }
 void Add(FeedItem item){
  item.minute=Minute();
  // Only the newest TIP is kept so repeating tips never crowd out real events.
  if(item.kind==FeedKind.Tip){items.RemoveAll(i=>i.kind==FeedKind.Tip);TipsPosted++;}
  items.Add(item);if(items.Count>FeedLimit)items.RemoveAt(0);revision++;
  if(!atBottom||!Expanded)Unseen++;
 }
 bool Expanded {get{var zones=GetComponent<ServiceLawZones>();return !zones||zones.TipExpanded;}}
 void Changed(ref string previous,string line,FeedKind kind){if(line==previous)return;previous=line;if(!string.IsNullOrEmpty(line))Add(new FeedItem{kind=kind,text=line});}
 // 08 chapter 2: praise, tips and the clean-run streak are read from the manners state, which the LAN
 // host replicates, so a guest sees the same cards. Repeated praise in one trip fades (level 2 card and
 // sound, level 1 sound only, level 0 silent). Simulation bots never play sound or post cards.
 int goodSeen=-1,tipSeen=-1,streakSeen=-1;AudioSource chime;static AudioClip praiseClip,tipClip;
 void ObserveGoodDriving(){
  var m=session.manners;if(!m||session.IsSimulationBot)return;
  if(goodSeen<0){goodSeen=m.goodSerial;tipSeen=m.tipSerial;streakSeen=m.cleanStreak;return;}
  if(m.goodSerial!=goodSeen){goodSeen=m.goodSerial;
   if(m.goodLevel==2)Add(new FeedItem{kind=FeedKind.Situation,text="칭찬 · "+GoodDrivingRules.Label(m.goodKind)+(string.IsNullOrEmpty(m.goodLine)?"":" · “"+m.goodLine+"”")});
   if(m.goodLevel>=1)Chime(false,m.goodLevel==2?.32f:.22f);
  }
  if(m.tipSerial!=tipSeen){tipSeen=m.tipSerial;if(m.tipAmount>0){Add(new FeedItem{kind=FeedKind.Situation,text="팁 +"+m.tipAmount.ToString("N0")+"원 · “"+m.tipLine+"”"});Chime(true,.36f);}}
  if(m.cleanStreak!=streakSeen){if(m.cleanStreak>=2&&m.cleanStreak>streakSeen)Add(new FeedItem{kind=FeedKind.Situation,text="깔끔한 운송 "+m.cleanStreak+"회째"});streakSeen=m.cleanStreak;}
 }
 public int PraiseCardsSeen {get{return goodSeen;}}
 public void Card(string text){if(!string.IsNullOrEmpty(text)&&!session.IsSimulationBot)Add(new FeedItem{kind=FeedKind.Situation,text=text});}
 void Chime(bool tip,float volume){
  if(!chime){chime=gameObject.AddComponent<AudioSource>();chime.playOnAwake=false;chime.spatialBlend=0;}
  if(tip){if(!tipClip)tipClip=Bell("팁 효과음",new[]{1047f,1319f,1568f},.11f);chime.PlayOneShot(tipClip,volume);}
  else{if(!praiseClip)praiseClip=Bell("칭찬 효과음",new[]{1319f,1760f},.10f);chime.PlayOneShot(praiseClip,volume);}
 }
 static AudioClip Bell(string name,float[] notes,float step){
  const int rate=44100;int length=(int)(rate*(step*notes.Length+.45f));var data=new float[length];
  for(int n=0;n<notes.Length;n++){int start=(int)(rate*step*n);
   for(int i=start;i<length;i++){float t=(i-start)/(float)rate;float envelope=Mathf.Exp(-t*7f)*Mathf.Clamp01(t*400);
    data[i]+=.28f*envelope*(Mathf.Sin(2*Mathf.PI*notes[n]*t)+.35f*Mathf.Sin(4*Mathf.PI*notes[n]*t));}}
  var clip=AudioClip.Create(name,length,1,rate,false);clip.SetData(data,0);return clip;
 }

 public void ObserveFeed(){
  if(!session.StartupReady||session.paused||session.business.preparing||session.business.ShowingSummary)return;
  var account=session.business.ledger.Account;
  int band=!FuelLow?0:account.Fuel<=5?3:account.Fuel<=10?2:1;
  if(fuelItem!=null&&(band==0||band>fuelBand||Time.time>=fuelUntil)){
   items.Remove(fuelItem);fuelItem=null;revision++;
  }
  if(band>fuelBand){
   fuelItem=new FeedItem{kind=FeedKind.Fuel,text="연료 부족 "+account.Fuel.ToString("0")+"%",delta=Mathf.RoundToInt(account.Fuel)};Add(fuelItem);
   fuelUntil=Time.time+12;
  }
  fuelBand=band;
  // Low satiety speeds up fatigue (DriverCondition.Advance): explain it as soon as it starts.
  var driver=session.business.ledger.Driver;int hunger=!session.business.enabledCampaign||driver==null?0:driver.satiety<=10?2:driver.satiety<=25?1:0;
  if(hunger>hungerBand){Add(new FeedItem{kind=FeedKind.Tip,text=hunger==2?"포만감 10 이하 · 피로 2배":"포만감 25 이하 · 피로 1.5배"});nextTip=Time.time+20;}
  hungerBand=hunger;
  ObserveGoodDriving();
  Changed(ref lastBusiness,session.business.notice,FeedKind.System);
  Changed(ref lastService,session.services.notice,FeedKind.System);
  Changed(ref lastDestination,session.services.destination,FeedKind.Destination);
  if(Time.time>=nextTip){
   var zones=GetComponent<ServiceLawZones>();
   string tip=null;
   if(zones&&zones.Assigned!=null&&!zones.Completed){
    float distance=Vector3.Distance(session.taxi.transform.position,zones.Assigned.junction.center);
    if(distance<260)tip=zones.AssignedTip(distance);
   }
   if(tip==null){tip=Tips[tipIndex];tipIndex=(tipIndex+1)%(session.services.HasHomeBase?Tips.Length:Tips.Length-1);}
   Add(new FeedItem{kind=FeedKind.Tip,text=tip});nextTip=Time.time+20;
  }
 }

 // ---------- drawing ----------
 static readonly Color PositiveChip=new Color(.87f,.95f,.91f),NegativeChip=new Color(.98f,.89f,.89f),FuelBorder=new Color(.89f,.69f,.29f),CriticalFill=new Color(.99f,.91f,.92f),CriticalBorder=new Color(.90f,.54f,.57f),SystemFill=new Color(1,1,1,.6f);
 GUIStyle body,bold,small,chip,systemText;float s;
 void Styles(){
  s=CockpitUI.Scale;
  body=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=UiReadability.Font(14),wordWrap=true,padding=new RectOffset(0,0,0,0),margin=new RectOffset(0,0,0,0)};body.normal.textColor=CasualUI.Ink;
  bold=new GUIStyle(body){font=KoreanText.BoldFont};
  small=new GUIStyle(body){fontSize=UiReadability.Font(12)};small.normal.textColor=CasualUI.Muted;
  systemText=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};
  chip=new GUIStyle(bold){fontSize=UiReadability.Font(12),wordWrap=false,alignment=TextAnchor.MiddleCenter};
 }
 static string Delta(int d){return (d>0?"+":"−")+Mathf.Abs(d);}
 float ChipWidth(int d){return chip.CalcSize(new GUIContent(Delta(d))).x+12*s;}
 void Chip(Rect r,int d){CasualUI.RoundedFill(r,d>0?PositiveChip:NegativeChip);chip.normal.textColor=d>0?CasualUI.Green:CasualUI.Danger;GUI.Label(r,Delta(d),chip);}
 static void Outline(Rect r,Color border,Color fill,float line=1){CasualUI.RoundedFill(r,border);CasualUI.RoundedFill(new Rect(r.x+line,r.y+line,r.width-2*line,r.height-2*line),fill);}
 bool Dim(FeedItem i){return i.kind==FeedKind.Passenger&&i.trip!=observedTrip;}
 bool Grouped(FeedItem previous,FeedItem i){return previous!=null&&previous.kind==FeedKind.Passenger&&i.kind==FeedKind.Passenger&&previous.trip==i.trip;}
 bool Divider(FeedItem previous,FeedItem i){return i.minute>=0&&(previous==null?false:previous.minute>=0&&i.minute-previous.minute>=10);}
 static string Clock(int minute){return (minute/60%24)+":"+(minute%60).ToString("00");}

 float Height(FeedItem i,float width,List<FeedButton> buttons){
  float pad=6*s,buttonRow=buttons!=null&&buttons.Count>0?30*s+(Notes(buttons)?small.lineHeight+2*s:0):0;
  switch(i.kind){
   case FeedKind.Passenger:{float max=width-34*s-pad*2-ChipWidth(i.delta)-6*s;return Mathf.Max(26*s,body.CalcHeight(new GUIContent(i.text),Mathf.Min(max,body.CalcSize(new GUIContent(i.text)).x+1))+pad*2);}
   case FeedKind.Law:return body.CalcHeight(new GUIContent(i.text),width-ChipWidth(i.delta)-40*s)+pad*2;
   case FeedKind.Fuel:return bold.lineHeight+pad*2+(buttonRow>0?buttonRow+2*s:10*s);
   case FeedKind.Destination:{float h=bold.CalcHeight(new GUIContent(i.Title),width-36*s);if(i.Body!="")h+=small.CalcHeight(new GUIContent(i.Body+"  M"),width-36*s);return h+pad*2;}
   case FeedKind.Situation:{float h=bold.CalcHeight(new GUIContent(i.Title),width-36*s);if(i.Body!="")h+=body.CalcHeight(new GUIContent(i.Body),width-36*s);return h+pad*2+(buttonRow>0?buttonRow+4*s:0);}
   case FeedKind.System:return small.CalcHeight(new GUIContent(i.text),width-28*s)+6*s;
   default:return small.CalcHeight(new GUIContent("TIP  "+i.text),width-34*s)+pad*2;
  }
 }
 static bool Notes(List<FeedButton> b){foreach(var x in b)if(!string.IsNullOrEmpty(x.note)&&!x.enabled)return true;return false;}

 void DrawItem(Rect r,FeedItem i,bool grouped,List<FeedButton> buttons){
  float pad=6*s;var old=GUI.color;if(Dim(i))GUI.color=new Color(1,1,1,.55f);
  switch(i.kind){
   case FeedKind.Passenger:{
    if(!grouped){Rect av=new Rect(r.x,r.yMax-26*s,26*s,26*s);CasualUI.RoundedFill(av,new Color(.25f,.34f,.47f));HudGlyph.Draw(new Rect(av.x+4*s,av.y+4*s,18*s,18*s),6,Color.white);}
    float cw=ChipWidth(i.delta),max=r.width-34*s-pad*2-cw-6*s,tw=Mathf.Min(max,body.CalcSize(new GUIContent(i.text)).x+1);
    Rect b=new Rect(r.x+32*s,r.y,tw+cw+pad*2+6*s,r.height);Outline(b,CasualUI.Border,CasualUI.Card);
    GUI.Label(new Rect(b.x+pad,b.y+pad,tw,b.height-pad*2),i.text,body);Chip(new Rect(b.xMax-pad-cw,b.center.y-9*s,cw,18*s),i.delta);break;}
   case FeedKind.Law:{
    Color band=i.delta<0?CasualUI.Danger:CasualUI.Green;Outline(r,CasualUI.Border,CasualUI.Card);{var c=GUI.color;GUI.color=band;GUI.DrawTexture(new Rect(r.x+2,r.y+4*s,4*s,r.height-8*s),Texture2D.whiteTexture);GUI.color=c;}
    HudGlyph.Draw(new Rect(r.x+10*s,r.center.y-8*s,16*s,16*s),i.delta<0?2:7,band);
    float cw=ChipWidth(i.delta);GUI.Label(new Rect(r.x+32*s,r.y+pad,r.width-cw-40*s,r.height-pad*2),i.text,body);Chip(new Rect(r.xMax-pad-cw,r.center.y-9*s,cw,18*s),i.delta);break;}
   case FeedKind.Fuel:{
    bool critical=i.delta<=5;Outline(r,critical?CriticalBorder:FuelBorder,critical?CriticalFill:CasualUI.Warm,1.5f);Color tone=critical?CasualUI.Danger:CasualUI.Amber;
    HudGlyph.Draw(new Rect(r.x+pad,r.y+pad,18*s,18*s),8,tone);bold.normal.textColor=tone;GUI.Label(new Rect(r.x+pad+24*s,r.y+pad,r.width-pad*2-24*s,bold.lineHeight),(critical?"연료 거의 없음 ":"연료 부족 ")+i.delta+"%",bold);bold.normal.textColor=CasualUI.Ink;
    float row=r.y+pad+bold.lineHeight+2*s,inner=r.width-pad*2;
    if(buttons!=null&&buttons.Count>0){float bw=inner*.46f;CasualUI.Bar(new Rect(r.x+pad,row+10*s,inner-bw-8*s,6*s),i.delta/100f,tone);Buttons(new Rect(r.xMax-pad-bw,row,bw,r.height),buttons,new Rect(r.x+pad,row,inner,r.height));}
    else CasualUI.Bar(new Rect(r.x+pad,row+3*s,inner,6*s),i.delta/100f,tone);break;}
   case FeedKind.Destination:{
    CasualUI.RoundedFill(r,CasualUI.Blue);HudGlyph.Draw(new Rect(r.x+pad,r.y+pad,18*s,18*s),3,Color.white);
    var title=new GUIStyle(bold);title.normal.textColor=Color.white;var sub=new GUIStyle(small);sub.normal.textColor=new Color(1,1,1,.88f);
    float w=r.width-36*s,h=title.CalcHeight(new GUIContent(i.Title),w);GUI.Label(new Rect(r.x+30*s,r.y+pad,w,h),i.Title,title);
    if(i.Body!="")GUI.Label(new Rect(r.x+30*s,r.y+pad+h,w,r.height-h-pad*2),i.Body+"  [M 지도]",sub);break;}
   case FeedKind.Situation:{
    Outline(r,CasualUI.Ink,CasualUI.Card,1.5f);HudGlyph.Draw(new Rect(r.x+pad,r.y+pad,18*s,18*s),2,CasualUI.Amber);
    float h=bold.CalcHeight(new GUIContent(i.Title),r.width-36*s);GUI.Label(new Rect(r.x+30*s,r.y+pad,r.width-36*s,h),i.Title,bold);
    if(i.Body!=""){float bh=body.CalcHeight(new GUIContent(i.Body),r.width-36*s);GUI.Label(new Rect(r.x+30*s,r.y+pad+h,r.width-36*s,bh),i.Body,body);h+=bh;}
    Buttons(new Rect(r.x+pad,r.y+pad+h+4*s,r.width-pad*2,r.height),buttons);break;}
   case FeedKind.System:{
    float w=Mathf.Min(r.width-8*s,systemText.CalcSize(new GUIContent(i.text)).x+20*s);Rect b=new Rect(r.center.x-w/2,r.y+1*s,w,r.height-2*s);CasualUI.RoundedFill(b,SystemFill);
    GUI.Label(new Rect(b.x+8*s,b.y,b.width-16*s,b.height),i.text,systemText);break;}
   default:{
    Outline(r,CasualUI.Border,CasualUI.Soft);
    GUI.Label(new Rect(r.x+pad,r.y+pad,r.width-pad*2,r.height-pad*2),"<b>TIP</b>  "+i.text,new GUIStyle(small){richText=true});break;}
  }
  GUI.color=old;
 }
 void Buttons(Rect area,List<FeedButton> buttons,Rect? noteArea=null){
  if(buttons==null||buttons.Count==0)return;Rect na=noteArea??area;float gap=5*s,w=(area.width-gap*(buttons.Count-1))/buttons.Count;string note=null;
  for(int k=0;k<buttons.Count;k++){var b=buttons[k];var style=b.primary?CasualUI.Action(UiReadability.Font(13)):KoreanText.Button(UiReadability.Font(13));
   bool was=GUI.enabled;GUI.enabled=was&&b.enabled;if(GUI.Button(new Rect(area.x+k*(w+gap),area.y,w,26*s),b.label,style)&&b.click!=null)b.click();GUI.enabled=was;
   if(!b.enabled&&!string.IsNullOrEmpty(b.note))note=b.note;}
  if(note!=null)GUI.Label(new Rect(na.x,area.y+28*s,na.width,small.lineHeight),note,systemText);
 }

 public void DrawFilter(Rect r){
  if(body==null)Styles();float w=(r.width-4*s)/2;
  for(int k=0;k<2;k++){bool on=(k==1)==ScoresOnly;Rect b=new Rect(r.x+k*(w+4*s),r.y,w,r.height);
   if(on)CasualUI.RoundedFill(b,CasualUI.Ink);else Outline(b,CasualUI.Border,CasualUI.Soft);
   var st=new GUIStyle(chip);st.normal.textColor=on?Color.white:CasualUI.Muted;GUI.Label(b,k==0?"전체":"점수",st);
   if(GUI.Button(b,GUIContent.none,GUIStyle.none)&&ScoresOnly!=(k==1)){ScoresOnly=k==1;drawnRevision=-1;}}
 }

 // context: the situation line ServiceLawZones observes. Actionable cards (fuel, situation with buttons) stay pinned under the scrolling history.
 public void DrawFeed(Rect bounds,string context,FeedKind contextKind=FeedKind.System,List<FeedButton> contextButtons=null,FeedButton fuelButton=null){
  Styles();
  if(context!=lastContext){lastContext=context;if(!string.IsNullOrEmpty(context))Add(new FeedItem{kind=contextKind,text=context});}
  FeedItem contextItem=null;for(int k=items.Count-1;k>=0;k--)if(items[k].text==context&&(items[k].kind==FeedKind.Situation||items[k].kind==FeedKind.System)){contextItem=items[k];break;}
  bool pinSituation=contextItem!=null&&contextButtons!=null&&contextButtons.Count>0;
  bool pinFuel=fuelItem!=null&&fuelButton!=null&&FuelActionVisible;
  var fuelButtons=pinFuel?new List<FeedButton>{fuelButton}:null;
  float gap=5*s,width=Mathf.Max(1,bounds.width-Mathf.Max(10,GUI.skin.verticalScrollbar.fixedWidth)-4*s);
  // pinned cards
  float pinned=0;
  if(pinFuel)pinned+=Height(fuelItem,bounds.width,fuelButtons)+gap;
  if(pinSituation)pinned+=Height(contextItem,bounds.width,contextButtons)+gap;
  Rect view=new Rect(bounds.x,bounds.y,bounds.width,Mathf.Max(12*s,bounds.height-pinned));
  float y=view.yMax+gap;
  if(pinFuel){float h=Height(fuelItem,bounds.width,fuelButtons);DrawItem(new Rect(bounds.x,y,bounds.width,h),fuelItem,false,fuelButtons);y+=h+gap;}
  if(pinSituation){float h=Height(contextItem,bounds.width,contextButtons);DrawItem(new Rect(bounds.x,y,bounds.width,h),contextItem,false,contextButtons);}
  // history
  var shown=new List<FeedItem>();foreach(var i in items){if(pinFuel&&i==fuelItem||pinSituation&&i==contextItem)continue;if(ScoresOnly&&!i.scored)continue;shown.Add(i);}
  float total=0;FeedItem previous=null;var heights=new float[shown.Count];
  for(int k=0;k<shown.Count;k++){if(Divider(previous,shown[k]))total+=small.lineHeight+gap;heights[k]=Height(shown[k],width,null);total+=heights[k]+gap;previous=shown[k];}
  float height=Mathf.Max(total,view.height);
  // User request 2026-11-26: the feed always stays scrolled to the newest item (also when a pinned card or a
  // longer context line changes the height without a new item).
  feedScroll.y=Mathf.Max(0,height-view.height);Unseen=0;drawnRevision=revision;
  feedScroll=GUI.BeginScrollView(view,feedScroll,new Rect(0,0,width,height),false,false,GUIStyle.none,GUI.skin.verticalScrollbar);feedScroll.x=0;
  float top=height-total;previous=null;
  for(int k=0;k<shown.Count;k++){var i=shown[k];
   if(Divider(previous,i)){GUI.Label(new Rect(0,top,width,small.lineHeight),Clock(i.minute),systemText);top+=small.lineHeight+gap;}
   DrawItem(new Rect(0,top,width,heights[k]),i,Grouped(previous,i),null);top+=heights[k]+gap;previous=i;}
  GUI.EndScrollView();
  atBottom=feedScroll.y>=height-view.height-4*s;if(atBottom)Unseen=0;
  if(!atBottom&&Unseen>0){string label="새 알림 "+Unseen+" ▼";float w=chip.CalcSize(new GUIContent(label)).x+20*s;Rect pill=new Rect(view.center.x-w/2,view.yMax-28*s,w,22*s);
   CasualUI.RoundedFill(pill,CasualUI.Blue);var st=new GUIStyle(chip);st.normal.textColor=Color.white;GUI.Label(pill,label,st);
   if(GUI.Button(pill,GUIContent.none,GUIStyle.none)){feedScroll.y=Mathf.Max(0,height-view.height);atBottom=true;Unseen=0;}}
 }
}
