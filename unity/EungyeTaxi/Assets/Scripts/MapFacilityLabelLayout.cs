using System.Collections.Generic;
using UnityEngine;

public partial class FullMapOverlay {
 // Keep the badge's measured content separate from its panel padding. In
 // particular, a wrapped workshop caption is taller than a single line.
 GUIStyle FacilityLabelStyle(){return new GUIStyle(text){fontSize=MapFont(12),alignment=TextAnchor.MiddleCenter,padding=new RectOffset(0,0,0,0)};}
 List<Rect> LayoutFacilityLabels(Rect map,GUIStyle style,List<Rect> occupied){
  var result=new List<Rect>();
  foreach(var place in Places()){
   var p=Project(place.position,map);if(!map.Contains(p)||string.IsNullOrEmpty(place.capabilityLabel))continue;
   var content=new GUIContent(place.capabilityLabel);float width=Mathf.Min(map.width,Mathf.Clamp(style.CalcSize(content).x+12,84,140));
   var size=new Vector2(width,Mathf.Ceil(style.CalcHeight(content,Mathf.Max(1,width-12)))+8);
   var box=PlaceFacilityLabel(p,size,map,occupied);result.Add(box);if(box.width>0)occupied.Add(box);
  }
  return result;
 }
 static Rect FacilityLabelCandidate(float x,float y,Vector2 size,Rect bounds){return new Rect(Mathf.Clamp(x,bounds.xMin,bounds.xMax-size.x),Mathf.Clamp(y,bounds.yMin,bounds.yMax-size.y),size.x,size.y);}
 static bool FacilityLabelClear(Rect candidate,List<Rect> occupied){foreach(var other in occupied)if(candidate.Overlaps(new Rect(other.x-3,other.y-3,other.width+6,other.height+6)))return false;return true;}
 static Rect PlaceFacilityLabel(Vector2 anchor,Vector2 size,Rect map,List<Rect> occupied){
  if(size.x>map.width||size.y>map.height)return Rect.zero;
  // Prefer a nearby side of the facility, keeping a visible gap from its icon.
  Rect best=Rect.zero;float score=float.PositiveInfinity;
  foreach(float x in new[]{anchor.x-size.x/2,anchor.x+22,anchor.x-size.x-22})foreach(float y in new[]{anchor.y-size.y-22,anchor.y+22,anchor.y-size.y/2}){
   var box=FacilityLabelCandidate(x,y,size,map);if(!FacilityLabelClear(box,occupied))continue;float distance=(box.center-anchor).sqrMagnitude;if(distance<score){best=box;score=distance;}
  }
  if(best.width>0)return best;
  // Crowded calls can block every immediate side. Search outward only when
  // needed, rather than scanning the complete map on every GUI event.
  float limit=Mathf.Max(map.width,map.height)+Mathf.Max(size.x,size.y);
  for(float radius=16;radius<=limit;radius+=16){
   for(float offset=-radius;offset<=radius;offset+=16)for(int side=0;side<4;side++){
    var shift=side==0?new Vector2(offset,-radius):side==1?new Vector2(offset,radius):side==2?new Vector2(-radius,offset):new Vector2(radius,offset);
    var box=FacilityLabelCandidate(anchor.x+shift.x-size.x/2,anchor.y+shift.y-size.y/2,size,map);if(!FacilityLabelClear(box,occupied))continue;float distance=(box.center-anchor).sqrMagnitude;if(distance<score){best=box;score=distance;}
   }
   if(best.width>0)return best;
  }
  // In an entirely occupied viewport the icon and its tooltip remain usable.
  return Rect.zero;
 }
 List<Rect> FacilityLabelObstacles(Rect map){
  var occupied=new List<Rect>{new Rect(8,8,152,48),new Rect(map.width-64,map.height-182,52,152)};
  if(session.taxi){var p=MapPresentation.PlayerAnchor(map,Project(session.taxi.transform.position,map));occupied.Add(new Rect(p.x-42,p.y-42,84,84));occupied.Add(new Rect(Mathf.Clamp(p.x-74,map.xMin,map.xMax-148),Mathf.Clamp(p.y+31,map.yMin,map.yMax-31),148,28));}
  if(session.simulationMatch)foreach(var bot in session.simulationMatch.Bots)if(bot&&bot.taxi){var p=Project(bot.taxi.transform.position,map);if(map.Contains(p)){occupied.Add(new Rect(p.x-13,p.y-13,26,26));occupied.Add(new Rect(p.x+12,p.y-12,100,24));}}
  if(ShowFacilities)foreach(var place in Places()){var p=Project(place.position,map);if(map.Contains(p))occupied.Add(new Rect(p.x-16,p.y-16,32,32));}
  var jobs=session.jobs;
  if(jobs.Active&&jobs.current!=null)foreach(int index in ActiveStopPins)ReserveStopPin(index,map,occupied);
  else if(!jobs.Active){for(int i=0;i<jobs.offers.Length;i++)if(jobs.offers[i]!=null)occupied.Add(MapPresentation.PinHitBounds(CallPinPosition(i,map),26));if(selectedOffer!=null){ReserveStopPin(selectedOffer.pickup,map,occupied);ReserveStopPin(selectedOffer.drop,map,occupied);}}
  foreach(var marker in VisibleMarkers())if(marker.named&&(marker.label=="정체·정차"||marker.label=="일시 점유")){var p=Project(marker.position,map);if(map.Contains(p))occupied.Add(new Rect(p.x-8,p.y-8,16,16));}
  foreach(var zone in VisibleLawZones()){var p=Project(zone.junction.center,map);if(map.Contains(p))occupied.Add(new Rect(p.x-8,p.y-8,16,16));}
  return occupied;
 }
 void ReserveStopPin(int index,Rect map,List<Rect> occupied){var p=Project(session.jobs.Active?session.jobs.EffectiveStop(index).V():session.jobs.stops[index].V(),map);if(!map.Contains(p))return;occupied.Add(StopPinLabel(p,map));occupied.Add(new Rect(p.x-32,p.y-38,64,70));}
}
