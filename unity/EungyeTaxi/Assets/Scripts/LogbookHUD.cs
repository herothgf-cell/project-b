using UnityEngine;

public static class LogbookHUD {
 static GUIStyle title, note, detail, muted, button, active, pageButton, inactiveTab;
 static readonly string[] OtherTabs={"이야기","손님","동네","기록"};
 static string[][] noteLines;
 static BusinessProgress openedCampaign;
 static int selectedPage;

 static void Styles(){
  if(title!=null)return;
  KoreanText.PrepareSkin();
  title=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=24};
  note=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=20,wordWrap=true};
  detail=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=17,wordWrap=true};
  muted=new GUIStyle(detail){fontSize=15};muted.normal.textColor=CasualUI.Muted;
  button=KoreanText.Button(16);
  active=CasualUI.Action(16);
  pageButton=KoreanText.Button(18);
  pageButton.alignment=TextAnchor.MiddleLeft;
  pageButton.padding=new RectOffset(16,16,0,0);
  inactiveTab=new GUIStyle(muted){alignment=TextAnchor.MiddleCenter};
  noteLines=new string[FatherStoryState.Pages.Length][];
  for(int i=0;i<noteLines.Length;i++)noteLines[i]=FatherStoryState.Pages[i].text.Split('\n');
 }

 public static void Draw(DriveSession session){
  var b=session.business;
  if(!b.Story.FoundNotebook){b.LogbookOpen=false;return;}
  if(openedCampaign!=b){openedCampaign=b;selectedPage=0;}
  if(!b.Story.HasPage(selectedPage))selectedPage=0;
  Styles();

  var oldMatrix=GUI.matrix;
  var oldColor=GUI.color;
  var oldDepth=GUI.depth;
  GUI.depth=-175;
  GUI.matrix=Matrix4x4.identity;
  GUI.color=CasualUI.Canvas;
  GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
  GUI.color=oldColor;
  float scale=Mathf.Min(Screen.width/1200f,Screen.height/720f);
  GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1200*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);

  CasualUI.Header(new Rect(15,15,830,55),"기사 수첩 · 아버지",5);
  DrawNavigation(b);
  DrawSpread(b.Story);
  GUI.Label(new Rect(26,579,810,44),"아버지가 30년 몰던 차의 글러브박스에서 나온 수첩.",detail);
  DrawTabs(b);

  GUI.matrix=oldMatrix;
  GUI.depth=oldDepth;
 }

 static void DrawNavigation(BusinessProgress b){
  if(b.RoomAvailable&&b.ArchivedRoom==null){
   if(GUI.Button(new Rect(470,22,115,41),"숙소",button))OpenRoom(b,false);
   if(GUI.Button(new Rect(595,22,115,41),"차고",button))OpenRoom(b,true);
  }
  GUI.Label(new Rect(720,22,120,41),"수첩 · 보는 중",active);
 }

 static void OpenRoom(BusinessProgress b,bool garage){
  b.ArchivedRoom=null;
  b.RoomOpen=true;
  b.GarageTab=garage;
  b.LogbookOpen=false;
 }

 static void DrawSpread(FatherStoryState story){
  CasualUI.Panel(new Rect(25,89,810,474),CasualUI.Warm);
  CasualUI.RoundedFill(new Rect(421,101,2,450),CasualUI.Border);
  var pages=FatherStoryState.Pages;
  var page=pages[selectedPage];
  GUI.Label(new Rect(48,107,350,43),PageTitle(selectedPage),title);
  var lines=noteLines[selectedPage];
  for(int i=0;i<lines.Length;i++){
   float y=155+i*50;
   GUI.Label(new Rect(48,y,348,48),lines[i],note);
   CasualUI.Divider(new Rect(48,y+46,348,1));
  }
  GUI.Label(new Rect(48,317,350,30),"— "+page.date,muted);

  CasualUI.Panel(new Rect(48,366,350,168));
  GUI.Label(new Rect(64,382,318,32),"아버지의 운행 팁",detail);
  GUI.Label(new Rect(64,417,318,50),page.hint,detail);
  GUI.Label(new Rect(64,482,318,30),"열림 · "+page.unlock,muted);

  GUI.Label(new Rect(445,107,360,43),"페이지",title);
  for(int i=0;i<pages.Length;i++){
   var row=new Rect(445,160+i*75,365,64);
   if(i==selectedPage){
    CasualUI.RoundedFill(row,CasualUI.Soft);
    GUI.Label(new Rect(row.x+16,row.y,row.width-32,row.height),PageTitle(i)+" · 읽는 중",detail);
   }else if(story.HasPage(i)){
    if(GUI.Button(row,PageTitle(i),pageButton))selectedPage=i;
   }else{
    CasualUI.Panel(row,CasualUI.Warm);
    GUI.Label(new Rect(row.x+16,row.y+5,row.width-32,26),PageTitle(i),detail);
    GUI.Label(new Rect(row.x+16,row.y+32,row.width-32,24),"열림 조건 · "+pages[i].unlock,muted);
   }
  }
 }

 static string PageTitle(int index){return "p."+(index+1)+"  "+FatherStoryState.Pages[index].title;}

 static void DrawTabs(BusinessProgress b){
  CasualUI.Panel(new Rect(865,15,320,690));
  CasualUI.Header(new Rect(875,25,300,48),"수첩 탭",5);
  int count=0;
  for(int i=0;i<FatherStoryState.Pages.Length;i++)if(b.Story.HasPage(i))count++;
  GUI.Label(new Rect(885,92,280,45),"아버지 · "+count+"/"+FatherStoryState.Pages.Length,active);
  for(int i=0;i<OtherTabs.Length;i++){
   var tab=new Rect(885,147+i*50,280,42);
   CasualUI.RoundedFill(tab,CasualUI.Soft);
   GUI.Label(tab,OtherTabs[i],inactiveTab);
  }
  bool canReturnToRoom=b.RoomOpen&&b.RoomAvailable;
  bool canPrepare=canReturnToRoom&&b.ArchivedRoom==null&&!b.ledger.Account.Room.finalized&&b.ledger.State==BusinessState.Summary;
  var close=new Rect(885,canPrepare?595:647,280,40);
  string closeLabel=canReturnToRoom?(b.GarageTab?"차고로 돌아가기":"숙소로 돌아가기"):"수첩 닫기";
  if(GUI.Button(close,closeLabel,button))b.LogbookOpen=false;
  if(canPrepare&&GUI.Button(new Rect(885,647,280,40),"다음 날 준비",active)&&b.BeginNextCampaignDay())b.LogbookOpen=false;
  if(b.SaveFailed)GUI.Label(new Rect(885,535,280,54),b.saveStatus,muted);
 }
}
