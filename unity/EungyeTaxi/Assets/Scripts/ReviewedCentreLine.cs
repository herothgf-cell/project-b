using System;
using System.Linq;
using UnityEngine;

// 2026-11-26 map review: straight centre lines where the lane-based yellow line wiggles because the
// opposing lanes shift (left pockets starting on one side, an inner lane starting on the other).
// A "lanePresentation" facility of this type is a band (x,z = centre, yaw = road direction, length
// along, width across). Yellow centre-line pieces drawn inside it are moved onto the band's axis.
// The centre-line law indexes the drawn pieces (TrafficSystem -> CentreLineCrossing), so it follows
// the straight line too. tools/map_review_r59_plan.py only places a band where the straight axis
// stays clear of every lane in both directions, so ordinary driving never crosses it.
public static class ReviewedCentreLine {
 public const string Type="검토 중앙선 직선";
 public static Action<string,Vector3,Vector3,float,float,bool> Wrap(MapData map,Action<string,Vector3,Vector3,float,float,bool> mark){
  var bands=(map?.editorDocument?.facilities??Array.Empty<RoadMapFacility>()).Where(f=>f!=null&&f.kind=="lanePresentation"&&f.type==Type).ToArray();
  if(bands.Length==0)return mark;
  return (name,p,dir,width,length,yellow)=>{
   if(yellow&&name=="Centre line")foreach(var band in bands){
    var forward=Quaternion.Euler(0,band.yaw,0)*Vector3.forward;var right=TrafficSystem.Right(forward);
    var delta=p-band.Position;delta.y=0;float along=Vector3.Dot(delta,forward),across=Vector3.Dot(delta,right);
    if(Mathf.Abs(along)>band.length*.5f||Mathf.Abs(across)>band.width*.5f||Mathf.Abs(Vector3.Dot(dir,forward))<.7f)continue;
    var q=band.Position+forward*along;q.y=p.y;
    mark(name,q,Vector3.Dot(dir,forward)<0?-forward:forward,width,length,yellow);return;
   }
   mark(name,p,dir,width,length,yellow);
  };
 }
}
