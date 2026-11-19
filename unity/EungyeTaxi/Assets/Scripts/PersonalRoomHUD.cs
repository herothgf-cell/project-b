using UnityEngine;

// Fixed room view: the backdrop and all purchase layers share a single aspect ratio.
public static class PersonalRoomHUD {
 const float BackdropWidth=1672,BackdropHeight=941;
 static GUIStyle text,title,small,button;static Texture2D backdrop,morningSky;static string preview="",confirmation="";static int style;
 static readonly Color NightTint=new Color(.53f,.64f,.88f);
 static readonly Color NightOutside=new Color(.035f,.08f,.22f,.28f);
 static readonly Rect[] WindowPanes={new Rect(619,194,217,245),new Rect(857,194,218,245)};
 static readonly string[] MemoGoals={"car","home","none"};
 static readonly string[] MemoTitles={"아버지 차를, 내 차로","이 집을 내 집답게","일단, 오늘을 버티자"};
 static readonly string[] MemoDescriptions={"꾸미기 · 새 차 저축","소품 · 위층 넓은 호실","목표 없이 · 3일차에 다시"};
 static GUIStyle memoName,memoDescription,memoOnWall;
 static Texture2D fatherPhoto;
 static BusinessProgress memoCampaign;
 static int memoDay;
 static bool wallMemoOpen;
 static string Money(int n){return n.ToString("N0")+"원";}
 static readonly System.Collections.Generic.Dictionary<string,Texture2D> layers=new System.Collections.Generic.Dictionary<string,Texture2D>();
 // Item crops are in source pixels and match CROPS in tools/generate_room_items.py.
 static Rect Crop(string name){return name.Contains("rug")?new Rect(500,744,676,176):name.Contains("glow")?new Rect(436,228,568,388):new Rect(660,384,132,136);}
 static Rect RoomRect(Rect view,Rect source){
  float sx=view.width/BackdropWidth,sy=view.height/BackdropHeight;
  return new Rect(view.x+source.x*sx,view.y+source.y*sy,source.width*sx,source.height*sy);
 }
 static void Layer(Rect view,string name,Color tint){
  Texture2D texture;
  if(!layers.TryGetValue(name,out texture)){texture=Resources.Load<Texture2D>(name);layers[name]=texture;}
  if(texture)DrawTexture(RoomRect(view,Crop(name)),texture,tint);
 }
 static void DrawTexture(Rect rect,Texture texture,Color color,ScaleMode mode=ScaleMode.StretchToFill){
  var old=GUI.color;GUI.color=color;
  GUI.DrawTexture(rect,texture,mode,true);
  GUI.color=old;
 }
 static Texture2D MorningSky(){
  if(morningSky)return morningSky;
  const int height=32;
  morningSky=new Texture2D(1,height,TextureFormat.RGBA32,false){name="숙소 아침 창문 빛",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
  var horizon=new Color(1,.91f,.75f,.10f);
  var sky=new Color(.53f,.79f,1,.72f);
  for(int y=0;y<height;y++)morningSky.SetPixel(0,y,Color.Lerp(horizon,sky,y/(height-1f)));
  morningSky.Apply(false,true);
  return morningSky;
 }
 static void DrawRoomView(Rect view,PersonalRoomState room,bool morning,bool showPhoto){
  var tint=morning?Color.white:NightTint;
  if(!backdrop)backdrop=Resources.Load<Texture2D>("personal-room-base");
  if(backdrop){
   DrawTexture(view,backdrop,tint);
   foreach(var pane in WindowPanes){
    Rect window=RoomRect(view,pane);
    if(morning)DrawTexture(window,MorningSky(),Color.white);
    else Box(window,NightOutside);
   }
  }else Box(view,new Color(.65f,.60f,.53f)*tint);
  bool rug=preview=="rug"||room.rugOwned&&room.rugPlaced;
  if(rug)Layer(view,"room-rug-"+(preview=="rug"?style:room.rugStyle),tint);
  bool lamp=preview=="lamp"||room.lampOwned&&room.lampPlaced;
  if(lamp){
   bool on=preview=="lamp"||room.lampOn;
   int lampStyle=preview=="lamp"?style:room.lampStyle;
   if(on)Layer(view,"room-lamp-"+lampStyle+"-glow",new Color(1,1,1,morning?.62f:1));
   Layer(view,"room-lamp-"+lampStyle+(on?"-on":"-off"),on?Color.Lerp(tint,Color.white,.65f):tint);
  }
  if(!showPhoto)return;
  if(!fatherPhoto)fatherPhoto=Resources.Load<Texture2D>("UI/father-photo");
  if(fatherPhoto)DrawTexture(RoomRect(view,new Rect(868,424,61,77)),fatherPhoto,tint,ScaleMode.ScaleToFit);
 }
 static string StyleName(string item,int s){return item=="lamp"?(s==0?"나무 받침·리넨 갓":"짙은 철제 스탠드"):(s==0?"회청색 테두리":"테라코타 줄무늬·술");}
 static void Box(Rect r,Color c){DrawTexture(r,Texture2D.whiteTexture,c);}
 public static bool MorningPreviewAvailable(BusinessProgress b){
  return b.SupportsCareer&&b.preparing&&b.ledger!=null&&b.ledger.State==BusinessState.Open&&b.ledger.Day>1&&!b.restartPrompt&&!b.ResumeBlocked;
 }
 public static void Draw(DriveSession session,bool morning=false){
  var b=session.business;var a=b.ledger.Account;var r=b.ArchivedRoom??a.Room;bool read=morning||b.ArchivedRoom!=null||r.finalized;
  if(text==null){text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=14};title=new GUIStyle(text){fontSize=28,fontStyle=FontStyle.Bold};button=KoreanText.Button(16);}
  if(memoCampaign!=b||memoDay!=b.ledger.Day){memoCampaign=b;memoDay=b.ledger.Day;wallMemoOpen=false;}
  GUI.depth=-170;var saved=GUI.matrix;float scale=Mathf.Min(Screen.width/1200f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1200*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
  Box(new Rect(0,0,1200,720),CasualUI.Canvas);
  if(!morning&&b.ArchivedRoom==null&&b.GarageTab){VehicleGarageHUD.Draw(session);preview=confirmation="";wallMemoOpen=false;var tabsEnabled=GUI.enabled;GUI.enabled=tabsEnabled&&!VehicleGarageHUD.Confirming;Tabs(b);GUI.enabled=tabsEnabled;GUI.matrix=saved;return;}
  if(morning)preview=confirmation="";
  DrawRoomView(new Rect(0,85,850,478),r,morning,b.SupportsCareer&&b.ArchivedRoom==null);
  string roomTitle=morning?HomeBasePresentation.Name+" · 아침":read?"숙소 · 완료 기록":HomeBasePresentation.Name+" · 밤";
  CasualUI.Header(new Rect(15,15,830,55),roomTitle,0);
  bool incomingEnabled=GUI.enabled;GUI.enabled=incomingEnabled&&confirmation.Length==0;
  bool memoAvailable=!read&&b.WallMemoAvailable;
  bool showMemo=memoAvailable&&(wallMemoOpen||b.WallMemoPrompt);
  if(memoAvailable)DrawWallNotes(b,showMemo);
  string monologue=b.ArchivedRoom==null?b.StoryMonologue:"";
  if(!morning)GUI.Label(new Rect(25,578,790,48),preview.Length>0?"미리보기 · "+StyleName(preview,style)+" · 미리보기를 다시 누르면 스타일 전환":!string.IsNullOrEmpty(monologue)?monologue:"작은 방에서, 내가 고른 생활을 하나씩.",text);
  string rent=!morning&&a.HasHomeBase?"  ·  숙소 월세 "+Money(a.Expense(b.ledger.Day,"home-rent")):"";
  GUI.Label(new Rect(25,635,790,50),b.ArchivedRoom!=null?"이전 회차의 방 · 읽기 전용":"개인 지출 "+Money(a.PersonalSpent)+"  ·  영업 순수익 "+Money(b.ledger.TotalNet)+rent,small);
  CasualUI.Panel(new Rect(865,15,320,690));
  if(showMemo)DrawMemoPanel(b);
  else{
   CasualUI.Header(new Rect(875,25,300,48),"나의 물건",5);
   GUI.Label(new Rect(885,82,280,34),b.ArchivedRoom!=null?"이전 회차 기록":"현금 "+Money(a.Cash),new GUIStyle(title){fontSize=23});
   Card(b,"lamp","탁상 조명",5000,r.lampOwned,r.lampPlaced,130,read);
   Card(b,"rug","작은 러그",8000,r.rugOwned,r.rugPlaced,305,read);
   if(!read&&r.lampOwned&&GUI.Button(new Rect(885,483,280,32),r.lampOn?"조명 끄기":"조명 켜기",button))b.ChangeRoom("light","lamp");
   string roomNote=morning?(r.lampOwned&&r.lampPlaced?"탁상 조명 · "+(r.lampOn?"켜짐":"꺼짐"):""):read?"완료한 방은 기록으로 보존됩니다.":"구매는 선택입니다. 새 회차에는 물건이 초기화됩니다.";
   GUI.Label(new Rect(885,525,280,65),b.SaveFailed?b.saveStatus:roomNote,small);
  }
  if(GUI.Button(new Rect(885,595,280,40),morning?"영업 준비로 돌아가기":"정산으로 돌아가기",button)){b.RoomOpen=false;b.ArchivedRoom=null;preview=confirmation="";wallMemoOpen=false;}
  if(!read&&GUI.Button(new Rect(885,647,280,40),b.ledger.State==BusinessState.Completed?"이번 회차 마무리":"다음 날 준비",CasualUI.Action(18))){preview=confirmation="";wallMemoOpen=false;if(b.ledger.State==BusinessState.Completed)b.ChangeRoom("finish");else b.BeginNextCampaignDay();}
  GUI.enabled=incomingEnabled;
  if(!morning&&b.ArchivedRoom==null&&confirmation.Length==0)Tabs(b);
  if(confirmation.Length>0&&!read){Box(new Rect(0,0,1200,720),new Color(.07f,.15f,.29f,.42f));CasualUI.Panel(new Rect(280,200,640,310));int price=confirmation=="lamp"?5000:8000;CasualUI.Header(new Rect(295,215,610,50),"내 공간에 들일까요?",5);GUI.Label(new Rect(305,278,590,105),(confirmation=="lamp"?"탁상 조명":"작은 러그")+"  "+Money(price)+"\n남는 현금 "+Money(a.Cash-price)+" · 환불 불가\n미납 "+Money(a.Debt),new GUIStyle(title){fontSize=24});GUI.enabled=a.Debt==0&&a.Cash>=price;
   if(GUI.Button(new Rect(305,410,280,50),"구매 확인",CasualUI.Action(18))){if(b.ChangeRoom("buy",confirmation,style)){preview=confirmation="";}}
   GUI.enabled=true;if(GUI.Button(new Rect(605,410,280,50),"취소",button))confirmation="";}
  GUI.matrix=saved;
 }
 static void MemoStyles(){
  if(memoName!=null)return;
  memoName=new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=17};
  memoDescription=new GUIStyle(small);memoDescription.normal.textColor=CasualUI.Muted;
  memoOnWall=new GUIStyle(memoName){alignment=TextAnchor.MiddleCenter};
 }
 static void DrawWallNotes(BusinessProgress b,bool showMemo){
  MemoStyles();
  int selected=System.Array.IndexOf(MemoGoals,b.WallMemoChoice);
  if(!showMemo){
   var note=new Rect(328,112,194,54);
   CasualUI.Panel(note,CasualUI.Warm);
   GUI.Label(new Rect(note.x,note.y,194,30),selected<0?"벽 메모":MemoTitles[selected],memoOnWall);
   if(GUI.Button(new Rect(note.x+20,note.y+29,154,23),selected<0?"메모 고르기":"메모 바꾸기",button)){wallMemoOpen=true;preview="";}
   return;
  }
  for(int i=0;i<MemoTitles.Length;i++){
   var note=new Rect(152+i*184,116+(i==1?0:7),173,53);
   CasualUI.Panel(note,i==selected?CasualUI.Card:i==0?CasualUI.Warm:CasualUI.Soft);
   GUI.Label(note,MemoTitles[i],memoOnWall);
  }
 }
 static void DrawMemoPanel(BusinessProgress b){
  MemoStyles();
  CasualUI.Header(new Rect(875,25,300,48),"목표 고르기",5);
  GUI.Label(new Rect(885,78,280,38),"무엇을 위해 달릴까?",title);
  GUI.Label(new Rect(885,121,280,54),b.WallMemoPrompt&&b.ledger.Day==3&&b.WallMemoChoice=="none"?"셋째 날을 마쳤어요. 새 목표를 고르거나 지금 메모를 둘 수 있어요.":"벽에 붙일 메모 한 장을 고르세요.\n언제든 바꿀 수 있어요.",memoDescription);
  for(int i=0;i<MemoGoals.Length;i++){
   var row=new Rect(885,185+i*81,280,70);
   bool chosen=b.WallMemoChoice==MemoGoals[i];
   if(chosen)CasualUI.RoundedFill(new Rect(row.x-2,row.y-2,row.width+4,row.height+4),CasualUI.Green);
   if(GUI.Button(row,GUIContent.none,button)){
    if(chosen){b.DismissWallMemoPrompt();wallMemoOpen=false;}
    else if(b.ChooseCareerGoal(MemoGoals[i]))wallMemoOpen=false;
   }
   GUI.Label(new Rect(row.x+13,row.y+8,row.width-26,27),MemoTitles[i]+(chosen?" · 붙인 메모":""),memoName);
   string description=i==2&&b.ledger.Day>=3?"목표 없이 · 생계 유지":MemoDescriptions[i];
   GUI.Label(new Rect(row.x+13,row.y+37,row.width-26,25),description,memoDescription);
  }
  var ledger=b.ledger;
  var net=ledger.Last?.net??0;
  GUI.Label(new Rect(885,434,280,51),"숙소 월세  −"+Money(ledger.Account.Expense(ledger.Day,"home-rent"))+"\n오늘 순수익  "+(net>0?"+":"")+Money(net),small);
  if(GUI.Button(new Rect(885,494,280,33),"방 물건 보기",button)){wallMemoOpen=false;b.DismissWallMemoPrompt();}
  if(b.SaveFailed)GUI.Label(new Rect(885,536,280,48),b.saveStatus,small);
 }
 // 숙소 / 차고 / 수첩 share this header strip.
 static void Tabs(BusinessProgress b){
  float start=b.Story.FoundNotebook?470:595;
  if(GUI.Button(new Rect(start,22,115,41),b.GarageTab?"숙소":"숙소 · 보는 중",b.GarageTab?button:CasualUI.Action(15)))b.GarageTab=false;
  if(GUI.Button(new Rect(start+125,22,115,41),b.GarageTab?"차고 · 보는 중":"차고",b.GarageTab?CasualUI.Action(15):button))b.GarageTab=true;
  if(b.Story.FoundNotebook&&GUI.Button(new Rect(720,22,120,41),"수첩",button))b.LogbookOpen=true;
 }
 static void Card(BusinessProgress b,string item,string name,int price,bool owned,bool placed,float y,bool read){
  CasualUI.RoundedFill(new Rect(877,y-8,296,153),owned?CasualUI.Soft:CasualUI.Warm);
  HudGlyph.Draw(new Rect(889,y+1,23,23),5,owned?CasualUI.Green:CasualUI.Amber);
  GUI.Label(new Rect(918,y,247,30),name+" · "+(owned?"보유 중":Money(price)),new GUIStyle(text){fontSize=16,fontStyle=FontStyle.Bold});
  if(read)return;
  if(owned){if(GUI.Button(new Rect(885,y+40,135,35),placed?"보관":"놓기",button))b.ChangeRoom("place",item);if(GUI.Button(new Rect(1030,y+40,135,35),"스타일 변경",button)){var r=b.ledger.Account.Room;b.ChangeRoom("style",item,1-(item=="lamp"?r.lampStyle:r.rugStyle));}var cur=b.ledger.Account.Room;GUI.Label(new Rect(889,y+86,276,26),"스타일 · "+StyleName(item,item=="lamp"?cur.lampStyle:cur.rugStyle),small);}
  else{
   if(GUI.Button(new Rect(885,y+40,135,35),"미리보기",button)){if(preview==item)style=1-style;else{preview=item;style=0;}}
   if(GUI.Button(new Rect(1030,y+40,135,35),b.ledger.Account.Room.wished==item?"찜 해제":"찜",button))b.ChangeRoom("wish",item);
   if(GUI.Button(new Rect(885,y+83,280,35),"구매하기",button)){confirmation=item;if(preview!=item)style=0;preview=item;}
  }
 }
}
