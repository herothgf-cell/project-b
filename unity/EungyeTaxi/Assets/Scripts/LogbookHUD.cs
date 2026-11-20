using UnityEngine;

public static class LogbookHUD {
 static GUIStyle title,note,detail,muted,button,active,pageButton,cardTitle,endingLabel,lockedLabel;
 static readonly string[] TabNames={"아버지","이야기","손님","동네","기록"};
 static readonly string[] EndingNames={"좋은 결말","보통 결말","아쉬운 결말"};
 static readonly Color[] EndingColors={CasualUI.Green,CasualUI.Muted,CasualUI.Danger};
 static PassengerPresetDefinition[] presets=System.Array.Empty<PassengerPresetDefinition>();
 static string presetProfile;
 static string[][] noteLines;
 static BusinessProgress openedCampaign;
 static CampaignLogbook openedLogbook;
 static int tab,selectedPage,selectedEnding,selectedStop;
 static string selectedStory,selectedPreset;
 static Vector2 detailScroll;

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
  cardTitle=new GUIStyle(detail){font=KoreanText.BoldFont,fontSize=16,wordWrap=false};
  endingLabel=new GUIStyle(cardTitle){fontSize=15,alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}};
  lockedLabel=new GUIStyle(muted){alignment=TextAnchor.MiddleCenter};
  noteLines=new string[FatherStoryState.Pages.Length][];
  for(int i=0;i<noteLines.Length;i++)noteLines[i]=FatherStoryState.Pages[i].text.Split('\n');
 }

 public static void Draw(DriveSession session){
  var b=session.business;
  if(!b.Story.FoundNotebook){b.LogbookOpen=false;return;}
  if(openedCampaign!=b||openedLogbook!=b.Logbook){
   openedCampaign=b;openedLogbook=b.Logbook;
   tab=selectedPage=selectedStop=0;
   selectedStory=selectedPreset=null;
   detailScroll=Vector2.zero;
  }
  if(!b.Story.HasPage(selectedPage))selectedPage=0;
  Styles();
  RefreshPresets();

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

  CasualUI.Header(new Rect(15,15,830,55),"기사 수첩 · "+TabNames[tab],5);
  DrawNavigation(b);
  switch(tab){
   case 1:DrawStories(b.Logbook);break;
   case 2:DrawPresets(b.Logbook);break;
   case 3:LogbookNeighborhood.Draw(session,ref selectedStop);break;
   case 4:DrawAchievements(b.Logbook);break;
   default:
    DrawSpread(b.Story);
    string memo=b.Logbook.StudentMemo(b.ledger.Day,session.jobs.StudentMinute);
    GUI.Label(new Rect(26,579,810,44),memo.Length>0?memo:"아버지가 30년 몰던 차의 글러브박스에서 나온 수첩.",detail);
    break;
  }
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

 static Rect GridCard(int index,int columns,float totalHeight,int count){
  const float gap=8;
  int rows=(count+columns-1)/columns;
  float width=(830-gap*(columns-1))/columns;
  float height=(totalHeight-gap*(rows-1))/rows;
  return new Rect(15+index%columns*(width+gap),89+index/columns*(height+gap),width,height);
 }

 static void DrawStories(CampaignLogbook logbook){
  var stories=PassengerStories.All;
  PassengerStoryDefinition chosen=null;
  for(int i=0;i<stories.Count;i++){
   var story=stories[i];
   var card=GridCard(i,5,474,stories.Count);
   CasualUI.Panel(card);
   GUI.Label(new Rect(card.x+10,card.y+7,card.width-20,35),new GUIContent(UiReadability.Ellipsis(story.title,cardTitle,card.width-20),story.title),cardTitle);
   for(int ending=0;ending<EndingNames.Length;ending++){
    var cell=new Rect(card.x+10,card.y+57+ending*54,card.width-20,45);
    bool unlocked=logbook.HasEnding(story.id,ending);
    if(!unlocked){
     CasualUI.RoundedFill(cell,CasualUI.Soft);
     GUI.Label(cell,new GUIContent("?",EndingNames[ending]+" · 아직 보지 못한 결말"),lockedLabel);
     continue;
    }
    if(GUI.Button(cell,new GUIContent("",EndingNames[ending]+" 읽기"),GUIStyle.none)){
     selectedStory=story.id;
     selectedEnding=ending;
     detailScroll=Vector2.zero;
    }
    if(selectedStory==story.id&&selectedEnding==ending){
     CasualUI.RoundedFill(new Rect(cell.x-2,cell.y-2,cell.width+4,cell.height+4),CasualUI.Ink);
     chosen=story;
    }
    CasualUI.RoundedFill(cell,EndingColors[ending]);
    GUI.Label(cell,EndingNames[ending],endingLabel);
   }
  }
  if(chosen!=null)DrawCollectionDetail(chosen.title+" · "+EndingNames[selectedEnding],logbook.EndingLine(chosen.id,selectedEnding));
  else GUI.Label(new Rect(25,576,810,36),"결말 "+stories.Count*EndingNames.Length+"칸 중 "+logbook.EndingCount+"칸",detail);
 }

 static void RefreshPresets(){
  string profile=ContentTuning.TextOrCatalog("passenger.presets");
  if(profile==presetProfile)return;
  presetProfile=profile;
  presets=profile.Length>0?PassengerPresetCatalog.Parse(profile):System.Array.Empty<PassengerPresetDefinition>();
 }

 static int KnownPresetCount(CampaignLogbook logbook){
  if(logbook.PresetCount==0)return 0;
  int count=0;
  foreach(var preset in presets)if(logbook.HasPreset(preset.id))count++;
  return count;
 }

 static void DrawPresets(CampaignLogbook logbook){
  PassengerPresetDefinition chosen=null;
  for(int i=0;i<presets.Length;i++){
   var preset=presets[i];
   var card=GridCard(i,5,474,presets.Length);
   if(!logbook.HasPreset(preset.id)){
    CasualUI.Panel(card,CasualUI.Soft);
    GUI.Label(card,preset.enabled&&preset.weight>0?"손님 "+(i+1).ToString("00")+" · ?":"등장 꺼짐",lockedLabel);
    continue;
   }
   if(GUI.Button(card,GUIContent.none,button)){selectedPreset=preset.id;detailScroll=Vector2.zero;}
   float imageSize=Mathf.Min(40,card.height-16);
   CasualUI.Portrait(new Rect(card.x+8,card.center.y-imageSize*.5f,imageSize,imageSize),CasualUI.PortraitVariant(preset.id));
   var name=new Rect(card.x+54,card.y+4,card.width-62,card.height-8);
   GUI.Label(name,new GUIContent(UiReadability.Ellipsis(preset.name,cardTitle,name.width),preset.name),cardTitle);
   if(selectedPreset==preset.id){
    chosen=preset;
    CasualUI.RoundedFill(new Rect(card.x+5,card.yMax-5,card.width-10,4),CasualUI.Green);
   }
  }
  if(chosen!=null)DrawCollectionDetail(chosen.name,PassengerPresentation.Speech(chosen.intro));
  else GUI.Label(new Rect(25,576,810,36),"처음 태운 손님의 유형이 열립니다.",detail);
 }

 static void DrawCollectionDetail(string heading,string content){
  var panel=new Rect(15,580,830,117);
  CasualUI.Panel(panel);
  GUI.Label(new Rect(panel.x+14,panel.y+8,panel.width-28,28),new GUIContent(UiReadability.Ellipsis(heading,cardTitle,panel.width-28),heading),cardTitle);
  if(string.IsNullOrEmpty(content))return;
  var view=new Rect(panel.x+14,panel.y+40,panel.width-28,68);
  float width=view.width-19,height=Mathf.Max(view.height,detail.CalcHeight(new GUIContent(content),width));
  detailScroll=GUI.BeginScrollView(view,detailScroll,new Rect(0,0,width,height));
  GUI.Label(new Rect(0,0,width,height),content,detail);
  GUI.EndScrollView();
 }

 static void DrawAchievements(CampaignLogbook logbook){
  var names=CampaignLogbook.AchievementNames;
  for(int i=0;i<names.Length;i++){
   var card=GridCard(i,3,600,names.Length);
   bool unlocked=logbook.HasAchievement(i);
   CasualUI.Panel(card,unlocked?CasualUI.Card:CasualUI.Soft);
   HudGlyph.DrawBadge(new Rect(card.x+12,card.y+12,29,29),7,unlocked?CasualUI.Green:CasualUI.Muted);
   GUI.Label(new Rect(card.x+51,card.y+11,card.width-64,30),names[i],cardTitle);
   GUI.Label(new Rect(card.x+14,card.y+51,card.width-28,52),CampaignLogbook.AchievementHint(i),muted);
   GUI.Label(new Rect(card.x+14,card.yMax-34,card.width-28,23),unlocked?"달성":"미달성",muted);
  }
 }

 static void DrawTabs(BusinessProgress b){
  CasualUI.Panel(new Rect(865,15,320,690));
  CasualUI.Header(new Rect(875,25,300,48),"수첩 탭",5);
  int count=0;
  for(int i=0;i<FatherStoryState.Pages.Length;i++)if(b.Story.HasPage(i))count++;
  var logbook=b.Logbook;
  string[] counts={count+"/"+FatherStoryState.Pages.Length,logbook.EndingCount+"/"+(PassengerStories.All.Count*EndingNames.Length),
   KnownPresetCount(logbook)+"/"+presets.Length,"★ "+logbook.StarTotal,logbook.AchievementCount+"/"+CampaignLogbook.AchievementNames.Length};
  for(int i=0;i<TabNames.Length;i++){
   var row=new Rect(885,92+i*56,280,45);
   if(i==tab)GUI.Label(row,TabNames[i]+" · "+counts[i],active);
   else if(GUI.Button(row,TabNames[i]+" · "+counts[i],button)){tab=i;detailScroll=Vector2.zero;}
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
