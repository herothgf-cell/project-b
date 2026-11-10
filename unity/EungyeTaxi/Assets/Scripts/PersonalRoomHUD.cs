using UnityEngine;

// Fixed room view: the backdrop and all purchase layers share a single aspect ratio.
public static class PersonalRoomHUD {
 static GUIStyle text,title,small,button;static Texture2D backdrop;static string preview="",confirmation="";static int style;
 static string Money(int n){return n.ToString("N0")+"원";}
 static void Box(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
 public static void Draw(DriveSession session){
  var b=session.business;var a=b.ledger.Account;var r=b.ArchivedRoom??a.Room;bool read=b.ArchivedRoom!=null||r.finalized;
  if(text==null){text=new GUIStyle(GUI.skin.label){font=KoreanText.Font,fontSize=18,wordWrap=true};small=new GUIStyle(text){fontSize=14};title=new GUIStyle(text){fontSize=28,fontStyle=FontStyle.Bold};button=KoreanText.Button(16);}
  GUI.depth=-170;var saved=GUI.matrix;float scale=Mathf.Min(Screen.width/1200f,Screen.height/720f);GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1200*scale)/2,(Screen.height-720*scale)/2),Quaternion.identity,Vector3.one*scale);
  Box(new Rect(0,0,1200,720),CasualUI.Canvas);
  if(b.ArchivedRoom==null&&b.GarageTab){VehicleGarageHUD.Draw(session);preview=confirmation="";Tabs(b);GUI.matrix=saved;return;}
  if(!backdrop)backdrop=Resources.Load<Texture2D>("personal-room-base");
  var view=new Rect(0,85,850,478);if(backdrop)GUI.DrawTexture(view,backdrop,ScaleMode.StretchToFill);else Box(view,new Color(.65f,.60f,.53f));
  bool lamp=preview=="lamp"||r.lampOwned&&r.lampPlaced;bool rug=preview=="rug"||r.rugOwned&&r.rugPlaced;
  if(rug){int rs=preview=="rug"?style:r.rugStyle;var rc=rs==0?new Color(.44f,.54f,.57f):new Color(.72f,.50f,.35f);CasualUI.RoundedFill(new Rect(280,482,315,50),rc);if(rs==1)for(int i=0;i<7;i++)Box(new Rect(295+i*42,485,10,43),new Color(.88f,.77f,.60f));}
  if(lamp){bool on=preview=="lamp"||r.lampOn;int ls=preview=="lamp"?style:r.lampStyle;if(on)CasualUI.RoundedFill(new Rect(465,290,115,67),new Color(1,.76f,.26f,.20f));Box(new Rect(510,310,7,43),new Color(.30f,.27f,.23f));CasualUI.RoundedFill(new Rect(492,348,45,8),new Color(.46f,.39f,.28f));CasualUI.RoundedFill(new Rect(486,285,55,33),on?new Color(1,.87f,.59f):new Color(.65f,.60f,.49f));if(ls==1)Box(new Rect(486,285,55,5),new Color(.28f,.33f,.36f));}
  CasualUI.Header(new Rect(15,15,830,55),read?"내 공간 · 완료 기록":"영업을 마치고, 내 공간",5);
  GUI.Label(new Rect(25,578,790,48),preview.Length>0?"미리보기 · 닫으면 현재 배치로 돌아갑니다":"작은 방에서, 내가 고른 생활을 하나씩.",text);
  GUI.Label(new Rect(25,635,790,50),b.ArchivedRoom!=null?"이전 회차의 방 · 읽기 전용":"개인 지출 "+Money(a.PersonalSpent)+"  ·  영업 순수익 "+Money(b.ledger.TotalNet),small);
  CasualUI.Panel(new Rect(865,15,320,690));CasualUI.Header(new Rect(875,25,300,48),"나의 물건",5);
  GUI.Label(new Rect(885,82,280,34),b.ArchivedRoom!=null?"이전 회차 기록":"현금 "+Money(a.Cash),new GUIStyle(title){fontSize=23});
  bool incomingEnabled=GUI.enabled;GUI.enabled=incomingEnabled&&confirmation.Length==0;
  Card(b,"lamp","탁상 조명",5000,r.lampOwned,r.lampPlaced,130,read);
  Card(b,"rug","작은 러그",8000,r.rugOwned,r.rugPlaced,305,read);
  if(!read&&r.lampOwned&&GUI.Button(new Rect(885,483,280,32),r.lampOn?"조명 끄기":"조명 켜기",button))b.ChangeRoom("light","lamp");
  GUI.Label(new Rect(885,525,280,65),b.SaveFailed?b.saveStatus:read?"완료한 방은 기록으로 보존됩니다.":"구매는 선택입니다. 새 회차에는 물건이 초기화됩니다.",small);
  if(GUI.Button(new Rect(885,595,280,40),"정산으로 돌아가기",button)){b.RoomOpen=false;b.ArchivedRoom=null;preview=confirmation="";}
  if(!read&&GUI.Button(new Rect(885,647,280,40),b.ledger.State==BusinessState.Completed?"이번 회차 마무리":"다음 날 준비",CasualUI.Action(18))){preview=confirmation="";if(b.ledger.State==BusinessState.Completed)b.ChangeRoom("finish");else b.BeginNextCampaignDay();}
  GUI.enabled=incomingEnabled;
  if(b.ArchivedRoom==null&&confirmation.Length==0)Tabs(b);
  if(confirmation.Length>0&&!read){Box(new Rect(0,0,1200,720),new Color(.07f,.15f,.29f,.42f));CasualUI.Panel(new Rect(280,200,640,310));int price=confirmation=="lamp"?5000:8000;CasualUI.Header(new Rect(295,215,610,50),"내 공간에 들일까요?",5);GUI.Label(new Rect(305,278,590,105),(confirmation=="lamp"?"탁상 조명":"작은 러그")+"  "+Money(price)+"\n남는 현금 "+Money(a.Cash-price)+" · 환불 불가\n미납 "+Money(a.Debt),new GUIStyle(title){fontSize=24});GUI.enabled=a.Debt==0&&a.Cash>=price;
   if(GUI.Button(new Rect(305,410,280,50),"구매 확인",CasualUI.Action(18))){if(b.ChangeRoom("buy",confirmation,style)){preview=confirmation="";}}
   GUI.enabled=true;if(GUI.Button(new Rect(605,410,280,50),"취소",button))confirmation="";}
  GUI.matrix=saved;
 }
 // 방 / 차고 tabs share the personal space; drawn on top of the header strip.
 static void Tabs(BusinessProgress b){
  if(GUI.Button(new Rect(560,22,135,41),b.GarageTab?"방":"방 · 보는 중",b.GarageTab?button:CasualUI.Action(15)))b.GarageTab=false;
  if(GUI.Button(new Rect(705,22,135,41),b.GarageTab?"차고 · 보는 중":"차고",b.GarageTab?CasualUI.Action(15):button))b.GarageTab=true;
 }
 static void Card(BusinessProgress b,string item,string name,int price,bool owned,bool placed,float y,bool read){
  CasualUI.RoundedFill(new Rect(877,y-8,296,153),owned?CasualUI.Soft:CasualUI.Warm);
  HudGlyph.Draw(new Rect(889,y+1,23,23),5,owned?CasualUI.Green:CasualUI.Amber);
  GUI.Label(new Rect(918,y,247,30),name+" · "+(owned?"보유 중":Money(price)),new GUIStyle(text){fontSize=16,fontStyle=FontStyle.Bold});
  if(read)return;
  if(owned){if(GUI.Button(new Rect(885,y+40,135,35),placed?"보관":"놓기",button))b.ChangeRoom("place",item);if(GUI.Button(new Rect(1030,y+40,135,35),"스타일 변경",button)){var r=b.ledger.Account.Room;b.ChangeRoom("style",item,1-(item=="lamp"?r.lampStyle:r.rugStyle));}}
  else{
   if(GUI.Button(new Rect(885,y+40,135,35),"미리보기",button)){if(preview==item)style=1-style;else{preview=item;style=0;}}
   if(GUI.Button(new Rect(1030,y+40,135,35),b.ledger.Account.Room.wished==item?"찜 해제":"찜",button))b.ChangeRoom("wish",item);
   if(GUI.Button(new Rect(885,y+83,280,35),"구매하기",button)){confirmation=item;if(preview!=item)style=0;preview=item;}
  }
 }
}
