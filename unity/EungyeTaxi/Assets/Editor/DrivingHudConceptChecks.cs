using System;
using UnityEngine;
public static class DrivingHudConceptChecks {
 public static void Run(){
  var method=typeof(CockpitUI).GetMethod("SpeedBounds");
  if(method==null)throw new Exception("HUD_CONCEPT: measurable persistent central speedometer missing");
  int checks=0;
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1920,1080),new Vector2(2560,1080)}){
   float scale=UiReadability.HudScale(size.x,size.y);var dial=(Rect)method.Invoke(null,new object[]{size.x,size.y});
   var d=PassengerDialogueUI.PanelBounds(size.x,size.y,scale,12000);var dialogue=new Rect(d.x*scale,d.y*scale,d.width*scale,d.height*scale);
   if(dial.Overlaps(dialogue)||Mathf.Abs(dial.center.x-size.x/2)>1||dial.yMax>size.y||dial.yMin<size.y*.65f)throw new Exception("HUD_CONCEPT: long choices obscure speed/indicators "+size);checks++;
   var conditionMethod=typeof(PvpConditionHUD).GetMethod("PanelBounds");var hubMethod=typeof(ServiceStops).GetMethod("PostTripBounds");
   if(conditionMethod==null||hubMethod==null)throw new Exception("HUD_CONCEPT: auxiliary HUD surfaces need shared measured bounds");
   var condition=(Rect)conditionMethod.Invoke(null,new object[]{size.x,size.y});
   var nav=new Rect(size.x/2-310*scale,20*scale,620*scale,164*scale);var standings=new Rect(24*scale,392*scale,300*scale,244*scale);
   foreach(bool pvp in new[]{false,true}){var hub=(Rect)hubMethod.Invoke(null,new object[]{size.x,size.y,pvp});if(hub.Overlaps(nav)||hub.Overlaps(dial)||pvp&&hub.Overlaps(standings))throw new Exception("HUD_CONCEPT: life hub covers guidance or standings "+size);checks++;}
   if(condition.Overlaps(dial)||condition.Overlaps(standings))throw new Exception("HUD_CONCEPT: fatigue advice covers driving controls "+size);checks++;
  }
  Debug.Log("DRIVING_HUD_CONCEPT_OK checks="+checks);
 }
 public static void Build(){Run();NumericChoiceInputChecks.Run();MenuConditionFollowupTests.Run();if(Mathf.Abs(DrivingHudArt.ArcCrop(70)-.5f)>.001f||DrivingHudArt.ArcCrop(35)>=DrivingHudArt.ArcCrop(70)||DrivingHudArt.ArcCrop(-70)!=DrivingHudArt.ArcCrop(70)||DrivingHudArt.ArcCrop(180)!=DrivingHudArt.ArcCrop(140))throw new Exception("Speed arc progression");HudDialogueReadabilityChecks.Build();}
}
