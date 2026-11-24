using System;
using System.Collections.Generic;
using UnityEngine;

public static class ReviewedRoadFacilities {
 [Serializable] public sealed class Catalog {public string mapId;public Facility[] facilities;}
 [Serializable] public sealed class Facility {public string id,kind,sourceUrl,captureMonth,checkedAt,observed,estimated;public string[] northMarkIds,southMarkIds;public float hatchFromX,hatchToX,barrierFromX,barrierToX,barrierWidth,barrierHeight,cushionLength,cushionWidth,cushionHeight;public Opening[] openings;public ExplicitSegment[] segments;}
 [Serializable] public sealed class Opening {public float fromX,toX;}
 [Serializable] public sealed class ExplicitSegment {public string kind;public MapPoint[] points;public float width,height;}
 public sealed class Line {public Vector3 a,b;public float width,height;public string facilityId,kind;public bool alternateColour;}
 public sealed class Layout {public readonly List<Line> barriers=new List<Line>();public readonly List<Line> hatches=new List<Line>();public readonly List<Line> cushions=new List<Line>();public readonly List<Line> bollards=new List<Line>();public readonly List<Line> bumps=new List<Line>();}
 static List<Vector2> ClearIntervals(float from,float to,Opening[] openings){
  var result=new List<Vector2>();if(to<=from)return result;
  var ordered=new List<Opening>(openings??new Opening[0]);ordered.Sort((a,b)=>a.fromX.CompareTo(b.fromX));float at=from;
  foreach(var opening in ordered){if(opening.toX<=at||opening.fromX>=to)continue;float start=Mathf.Clamp(opening.fromX,from,to);if(start>at+.001f)result.Add(new Vector2(at,start));at=Mathf.Max(at,Mathf.Clamp(opening.toX,from,to));}
  if(at<to-.001f)result.Add(new Vector2(at,to));return result;
 }
 static bool Boundary(MapData map,string[] ids,float x,out Vector3 point){
  foreach(string id in ids??new string[0])foreach(var mark in map.hdMarks??new HdMark[0]){
   if(mark.id!=id||mark.points==null)continue;
    for(int i=1;i<mark.points.Length;i++){MapPoint a=mark.points[i-1],b=mark.points[i];if(Mathf.Abs(b.x-a.x)<.0001f||x<Mathf.Min(a.x,b.x)-.001f||x>Mathf.Max(a.x,b.x)+.001f)continue;
    point=new Vector3(x,0,Mathf.Lerp(a.z,b.z,(x-a.x)/(b.x-a.x)));return true;
   }
  }point=Vector3.zero;return false;
 }
 static void Pair(MapData map,Facility facility,float x,out Vector3 north,out Vector3 south){
  if(!Boundary(map,facility.northMarkIds,x,out north)||!Boundary(map,facility.southMarkIds,x,out south))throw new InvalidOperationException("Reviewed facility source marking missing at x="+x+": "+facility.id);
  if(Vector3.Distance(north,south)<.5f)throw new InvalidOperationException("Reviewed facility strip too narrow at x="+x+": "+facility.id);
 }
 static Line Segment(Vector3 a,Vector3 b,float width,float height,string id){return new Line{a=a,b=b,width=width,height=height,facilityId=id};}
 // Flexible posts are discrete points, not continuous rail obstacles. Width is the post diameter.
 // The nominal 2m spacing is a visual estimate; vertices do not restart it. Separate polylines preserve openings.
 static void PlanBollards(Layout layout,ExplicitSegment segment,string id){
  if(segment.width<=0||segment.height<=0||float.IsNaN(segment.width)||float.IsNaN(segment.height)||float.IsInfinity(segment.width)||float.IsInfinity(segment.height))throw new InvalidOperationException("Reviewed flexible bollard lacks dimensions: "+id);
  float walked=0,next=0;Vector3 last=segment.points[0].V();bool placed=false;
  for(int i=1;i<segment.points.Length;i++){
   Vector3 a=segment.points[i-1].V(),b=segment.points[i].V();float length=Vector3.Distance(a,b);if(length<=.00001f)continue;
   while(next<=walked+length){
    Vector3 point=Vector3.Lerp(a,b,(next-walked)/length);var post=Segment(point,point,segment.width,segment.height,id);post.kind="flexible-bollard";layout.bollards.Add(post);last=point;placed=true;next+=2f;
   }walked+=length;
  }
  Vector3 end=segment.points[segment.points.Length-1].V();
  if(walked>0&&(!placed||Vector3.Distance(last,end)>.001f)){var post=Segment(end,end,segment.width,segment.height,id);post.kind="flexible-bollard";layout.bollards.Add(post);}
 }
 public static Layout Plan(MapData map,Catalog catalog){
  var layout=new Layout();if(map==null||catalog==null||map.mapId!=catalog.mapId)return layout;
  foreach(var facility in catalog.facilities??new Facility[0]){
   if(facility.kind=="paired-median"){
    if(string.IsNullOrEmpty(facility.sourceUrl)||facility.barrierWidth<=0||facility.barrierHeight<=0)throw new InvalidOperationException("Reviewed facility lacks evidence or dimensions: "+facility.id);
    foreach(var interval in ClearIntervals(facility.barrierFromX,facility.barrierToX,facility.openings)){
     for(float x=interval.x;x<interval.y-.001f;){float end=Mathf.Min(interval.y,x+4f);Vector3 n0,s0,n1,s1;Pair(map,facility,x,out n0,out s0);Pair(map,facility,end,out n1,out s1);
      layout.barriers.Add(Segment((n0+s0)*.5f,(n1+s1)*.5f,facility.barrierWidth,facility.barrierHeight,facility.id));x=end;
     }
    }
    if(facility.cushionLength>0){float from=facility.barrierFromX-facility.cushionLength;var clear=ClearIntervals(from,facility.barrierFromX,facility.openings);
     if(from>=facility.hatchFromX&&clear.Count==1&&Mathf.Abs(clear[0].x-from)<.001f&&Mathf.Abs(clear[0].y-facility.barrierFromX)<.001f){Vector3 n0,s0,n1,s1;Pair(map,facility,from,out n0,out s0);Pair(map,facility,facility.barrierFromX,out n1,out s1);
      layout.cushions.Add(Segment((n0+s0)*.5f,(n1+s1)*.5f,facility.cushionWidth,facility.cushionHeight,facility.id));
     }
    }
    foreach(var interval in ClearIntervals(facility.hatchFromX,facility.hatchToX,facility.openings)){
     for(float x=interval.x;x<interval.y-.001f;x+=2.5f){float end=Mathf.Min(interval.y,x+1.5f);Vector3 n0,s0,n1,s1;Pair(map,facility,x,out n0,out s0);Pair(map,facility,end,out n1,out s1);
      layout.hatches.Add(Segment(Vector3.Lerp(n0,s0,.055f),Vector3.Lerp(s1,n1,.055f),.12f,.018f,facility.id));
     }
    }
   }
   foreach(var explicitSegment in facility.segments??new ExplicitSegment[0]){
    if(explicitSegment.points==null||explicitSegment.points.Length<2)continue;
    if(explicitSegment.kind=="flexible-bollard"){PlanBollards(layout,explicitSegment,facility.id);continue;}
    // Speed bump (2026-11-24): a painted, slightly raised strip across the road; no collider (SpeedBumps judges it).
    if(explicitSegment.kind=="speed-bump"){var bump=Segment(explicitSegment.points[0].V(),explicitSegment.points[explicitSegment.points.Length-1].V(),explicitSegment.width,explicitSegment.height,facility.id);bump.kind="speed-bump";layout.bumps.Add(bump);continue;}
    bool temporary=explicitSegment.kind=="temporary-barrier",alternateColour=false;
    for(int i=1;i<explicitSegment.points.Length;i++){var line=Segment(explicitSegment.points[i-1].V(),explicitSegment.points[i].V(),explicitSegment.width,explicitSegment.height,facility.id);
     if(explicitSegment.kind=="post-rail"||explicitSegment.kind=="pedestrian-rail"||temporary){
      float length=Vector3.Distance(line.a,line.b);if(length<.01f)continue;
      int count=Mathf.CeilToInt(length/2);
      for(int part=0;part<count;part++){
       var module=Segment(Vector3.Lerp(line.a,line.b,(float)part/count),Vector3.Lerp(line.a,line.b,(float)(part+1)/count),line.width,line.height,line.facilityId);
       module.kind=explicitSegment.kind;
       if(temporary){module.alternateColour=alternateColour;alternateColour=!alternateColour;}
       layout.barriers.Add(module);
      }
     }
     else if(explicitSegment.kind=="barrier")layout.barriers.Add(line);else if(explicitSegment.kind=="hatch")layout.hatches.Add(line);
    }
   }
  }
  return layout;
 }
 static void Beam(string name,Line line,float y,float width,float height,Material material,Transform parent,bool solid){
  var delta=line.b-line.a;if(delta.sqrMagnitude<.0001f)return;
  var object3d=WorldBuilder.Box(name,(line.a+line.b)*.5f+Vector3.up*y,new Vector3(width,height,delta.magnitude),material,parent,solid);
  object3d.transform.localRotation=Quaternion.LookRotation(delta.normalized,Vector3.up);
 }
 public static void Draw(MapData map,Transform parent,bool collisions){
  var asset=Resources.Load<TextAsset>("eungye-road-facilities");if(!asset)return;
  var catalog=JsonUtility.FromJson<Catalog>(asset.text);Draw(Plan(map,catalog),parent,collisions);
 }
 static void BollardCylinder(string name,Vector3 position,float diameter,float height,Material material,Transform parent,bool solid){
  var post=GameObject.CreatePrimitive(PrimitiveType.Cylinder);post.name=name;post.transform.SetParent(parent,false);post.transform.localPosition=position;post.transform.localScale=new Vector3(diameter,height*.5f,diameter);post.GetComponent<Renderer>().sharedMaterial=material;
  if(!solid){var collider=post.GetComponent<Collider>();collider.enabled=false;if(Application.isPlaying)UnityEngine.Object.Destroy(collider);else UnityEngine.Object.DestroyImmediate(collider);}
 }
 public static void Draw(Layout layout,Transform parent,bool collisions){
  if(layout==null||layout.barriers.Count==0&&layout.hatches.Count==0&&layout.cushions.Count==0&&layout.bollards.Count==0&&layout.bumps.Count==0)return;
  if(layout.bumps.Count>0){
   // Yellow hump with white chevron-like bands, as Korean speed humps are painted. Visual only.
   var hump=WorldBuilder.Mat("과속방지턱 노랑",new Color(.95f,.72f,.08f));var stripe=WorldBuilder.Mat("과속방지턱 흰 띠",new Color(.95f,.95f,.9f));
   foreach(var bump in layout.bumps){var across=bump.b-bump.a;float length=across.magnitude;if(length<.1f)continue;var dir=across/length;var along=new Vector3(-dir.z,0,dir.x);
    var root=new GameObject("과속방지턱 "+bump.facilityId);root.transform.SetParent(parent,false);root.transform.localPosition=(bump.a+bump.b)*.5f;root.transform.localRotation=Quaternion.LookRotation(along,Vector3.up);
    WorldBuilder.Box("과속방지턱 몸체",new Vector3(0,bump.height*.5f,0),new Vector3(length,bump.height,bump.width),hump,root.transform,false);
    WorldBuilder.Box("과속방지턱 윗면",new Vector3(0,bump.height+.004f,0),new Vector3(length,.008f,bump.width*.55f),hump,root.transform,false);
    for(float x=-length*.5f+.6f;x<length*.5f-.3f;x+=1.2f)WorldBuilder.Box("과속방지턱 흰 띠",new Vector3(x,bump.height+.01f,0),new Vector3(.45f,.01f,bump.width*.92f),stripe,root.transform,false);
   }
  }
  if(layout.bollards.Count>0){
   var orange=WorldBuilder.Mat("Reviewed flexible bollard orange",new Color(1f,.27f,.035f));var white=WorldBuilder.Mat("Reviewed flexible bollard reflector",new Color(.96f,.96f,.91f));
   foreach(var post in layout.bollards){
    BollardCylinder("주황 탄성 분리봉 "+post.facilityId,post.a+Vector3.up*post.height*.5f,post.width,post.height,orange,parent,collisions);
    foreach(float band in new[]{.60f,.81f})BollardCylinder("탄성 분리봉 흰 반사띠 "+post.facilityId,post.a+Vector3.up*post.height*band,post.width*1.025f,post.height*.12f,white,parent,false);
   }
  }
  var steel=WorldBuilder.Mat("Reviewed W-beam",new Color(.57f,.59f,.60f));var highlight=WorldBuilder.Mat("Reviewed W-beam flange",new Color(.75f,.76f,.72f));var yellow=WorldBuilder.Mat("Reviewed safety hatch",new Color(.96f,.7f,.09f));var cushion=WorldBuilder.Mat("Reviewed crash cushion",new Color(.94f,.62f,.10f));
  foreach(var line in layout.hatches)Beam("안전지대 빗금 "+line.facilityId,line,.085f,line.width,line.height,yellow,parent,false);
  var dark=WorldBuilder.Mat("Reviewed post rail",new Color(.08f,.08f,.07f));var posts=new HashSet<Vector2Int>();
  Material temporaryRed=null,temporaryWhite=null;
  if(layout.barriers.Exists(line=>line.kind=="temporary-barrier")){
   temporaryRed=WorldBuilder.Mat("Reviewed temporary barrier red",new Color(.82f,.17f,.13f));
   temporaryWhite=WorldBuilder.Mat("Reviewed temporary barrier white",new Color(.90f,.88f,.81f));
  }
  foreach(var line in layout.barriers){
   if(line.kind=="temporary-barrier"){
    var material=line.alternateColour?temporaryWhite:temporaryRed;
    Beam("적백 임시 방호벽 받침 "+line.facilityId,line,line.height*.14f,line.width,line.height*.28f,material,parent,collisions);
    Beam("적백 임시 방호벽 몸체 "+line.facilityId,line,line.height*.64f,line.width*.65f,line.height*.72f,material,parent,collisions);
    continue;
   }
   if(line.kind=="post-rail"||line.kind=="pedestrian-rail"){
    bool pedestrian=line.kind=="pedestrian-rail";
    string name=pedestrian?"보도 난간":"중앙분리 포스트 난간";
    if(collisions){var delta=line.b-line.a;var block=new GameObject(name+" 충돌 "+line.facilityId);block.transform.SetParent(parent,false);block.transform.localPosition=(line.a+line.b)*.5f+Vector3.up*line.height*.5f;block.transform.localRotation=Quaternion.LookRotation(delta.normalized);block.AddComponent<BoxCollider>().size=new Vector3(line.width,line.height,delta.magnitude);}
    Beam(name+" 상단 "+line.facilityId,line,line.height*.88f,line.width*.45f,.055f,dark,parent,false);
    Beam(name+" 하단 "+line.facilityId,line,line.height*.45f,line.width*.45f,.045f,dark,parent,false);
    foreach(var point in new[]{line.a,line.b}){var key=new Vector2Int(Mathf.RoundToInt(point.x*100),Mathf.RoundToInt(point.z*100));if(!posts.Add(key))continue;
     float postWidth=pedestrian?.065f:.14f;
     WorldBuilder.Box(pedestrian?"보도 난간 기둥 "+line.facilityId:"중앙분리 검정 기둥 "+line.facilityId,
      point+Vector3.up*line.height*.5f,new Vector3(postWidth,line.height,postWidth),dark,parent,false);
     if(!pedestrian)foreach(float band in new[]{.35f,.75f})WorldBuilder.Box("중앙분리 노랑 반사띠 "+line.facilityId,point+Vector3.up*line.height*band,new Vector3(.15f,line.height*.18f,.15f),yellow,parent,false);
    }continue;
   }
   Beam("중앙분리 난간 "+line.facilityId,line,line.height*.5f,line.width*.55f,line.height,steel,parent,collisions);
   Beam("중앙분리 난간 윗날개 "+line.facilityId,line,line.height*.83f,line.width,.06f,highlight,parent,false);
   Beam("중앙분리 난간 아랫날개 "+line.facilityId,line,line.height*.54f,line.width,.055f,highlight,parent,false);
   foreach(var point in new[]{line.a,line.b}){var key=new Vector2Int(Mathf.RoundToInt(point.x*100),Mathf.RoundToInt(point.z*100));if(!posts.Add(key))continue;
    WorldBuilder.Box("중앙분리 난간 기둥 "+line.facilityId,point+Vector3.up*line.height*.5f,new Vector3(.13f,line.height,.13f),steel,parent,collisions);
   }
  }
  foreach(var line in layout.cushions)Beam("중앙분리 충격흡수 "+line.facilityId,line,line.height*.5f,line.width,line.height,cushion,parent,collisions);
 }
}
