using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// PREVIEW ONLY: P12/P21 east internal north-south column. No save/apply entrypoint.
// School-side east-west streets, raised red intersections and speed bumps are
// deliberately outside this appearance edit. Existing road width/graph survive.
public static class PinkEastUnmarkedMapEdit {
 public const string Prefix="reviewed:pink-east-unmarked:";
 public static readonly string[] Lanes={"local:8785:940:8785:1578","local:8737:1578:8737:940","local:8785:1734:8786:2476","local:8738:2476:8737:1734"};
 static readonly string[] Panos={"gEvfQOQQDG-H9fxvrEZTzg","MeIlWffu_buIxQ-OejM9_Q","7CSwOsWRasQzj6WkIyfWlg","L3JLYc575PGByNv2y_uphA","G3KSddqwo_ffWB4DzcG45w"};
 static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException("PINK_EAST_UNMARKED_EDIT: "+why);}
 static AssistedRoadNetwork.Path Path(RoadMapLane lane){return new AssistedRoadNetwork.Path(lane.points.Select((p,i)=>p.V(lane.heights!=null&&i<lane.heights.Length?lane.heights[i]:0)).ToArray());}
 static RoadMapEvidence Evidence(int segment){return new RoadMapEvidence{state="observed",appearanceConfirmed=true,month="2026-05",checkedAt="2026-11-13",url="https://map.naver.com/p?c=19,0,0,0,adh&p="+Panos[segment==0?0:4]+",0,-15,80,Float",note="P12/P21 internal east N-S column: both directions have no painted centre/divider/direction arrows. Contiguous May2026 panorama chain: "+string.Join(" -> ",Panos)+". Last public camera anchor x875.9173/z230.9603. Only these two segments are confirmed; school E-W centreline retained. startAt/endAt are GLOBAL original-lineage stations; suppression width is existing opposing-centre spacing plus0.30m paint allowance, a game geometry bound, not surveyed road width. Raised red intersections/speed bumps are observed but NOT implemented here."};}
 static float SuppressionWidth(RoadMapLane lane,RoadMapLane opposite){
  var path=Path(lane);var other=Path(opposite);float separation=0;
  for(float at=0;at<=path.length;at+=2){float error;other.Project(path.Position(at),out error);separation=Mathf.Max(separation,error);}
  float endError;other.Project(path.Position(path.length),out endError);separation=Mathf.Max(separation,endError);
  Check(separation>0&&separation<8,"unexpected existing opposite-centre spacing "+lane.id);
  return Mathf.Max(lane.width,separation+.30f);
 }
 public static RoadMapDocument Preview(ProjectMapPackage package){
  if(package==null||package.document==null)throw new ArgumentNullException("package");var d=package.document.Copy();
  Check(!d.facilities.Any(f=>f.id.StartsWith(Prefix,StringComparison.Ordinal)),"preview already present or partially applied");var added=new List<RoadMapFacility>();
  for(int i=0;i<Lanes.Length;i++){
   var lane=d.lanes.Single(l=>l.id==Lanes[i]);var opposite=d.lanes.Single(l=>l.id==Lanes[i^1]);
   Check(!lane.disabled&&lane.turn==0&&lane.change==0&&lane.points!=null&&lane.points.Length>=2,"expected intact active through lane "+lane.id);
   Check(d.lanes.Count(l=>!l.disabled&&RoadMapAuthoringOperations.Origin(l)==lane.id)==1,"unexpected split lineage; review segment scopes first "+lane.id);
   Check(!d.facilities.Any(f=>f.laneId==lane.id&&f.kind=="lanePresentation"),"existing lane presentation needs explicit reconciliation "+lane.id);
   var path=Path(lane);var midpoint=path.Position(path.length*.5f);var evidence=Evidence(i/2);
   added.Add(new RoadMapFacility{id=Prefix+"lines:"+lane.id,kind="lanePresentation",type=ReviewedUnmarkedRoadPaint.FeatureType,laneId=lane.id,junctionId=lane.ruleId,rule=false,startAt=lane.sourceOffset,endAt=lane.sourceOffset+path.length,width=SuppressionWidth(lane,opposite),length=1,scale=1,x=midpoint.x,z=midpoint.z,evidence=evidence});
   added.Add(new RoadMapFacility{id=Prefix+"arrows:"+lane.id,kind="lanePresentation",type=LaneDirectionPaint.NoArrowsType,laneId=lane.id,junctionId=lane.ruleId,rule=false,x=lane.points.Last().x,z=lane.points.Last().z,evidence=Evidence(i/2)});
  }
  d.facilities=d.facilities.Concat(added).ToArray();return d;
 }
}
