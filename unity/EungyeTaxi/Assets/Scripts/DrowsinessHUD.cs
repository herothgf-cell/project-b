using UnityEngine;
// Covers only the world edges; HUD and menu interaction remain accessible.
public class DrowsinessHUD : MonoBehaviour {
 public BusinessProgress business;
 void OnGUI(){
  if(!business||!business.DrowsinessRunning)return;
  var d=business.ledger.Driver.drowsiness;if(d==null||d.phase==0)return;
  GUI.depth=10;
  if(d.Tunnel){GUI.color=new Color(0,0,0,.94f);float w=Screen.width,h=Screen.height;GUI.DrawTexture(new Rect(0,0,w*.28f,h),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(w*.72f,0,w*.28f,h),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(w*.28f,0,w*.44f,h*.25f),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(w*.28f,h*.75f,w*.44f,h*.25f),Texture2D.whiteTexture);GUI.color=Color.white;}
  GUI.depth=-40;var rect=new Rect(Screen.width/2f-230,Screen.height*.65f,460,55);KoreanText.Panel(rect);GUI.Label(rect,d.Warning?"졸음 예고 · 안전하게 정차하세요":d.ControlsLost?"졸음 · 잠시 조작 불가":"졸음 · 시야 축소",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=22,alignment=TextAnchor.MiddleCenter});
 }
}
