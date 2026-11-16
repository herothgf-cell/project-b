using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Preview never writes. Apply is guarded to the exact reviewed r23 package.
// Subsequent field views established two ring lanes.
// Inner-lane entry/exit assignment remains unconfirmed. Centre below is the
// existing map junction, not the north-side camera position (510.0757,111.5919).
public static class PinkRoundaboutMapEdit {
 public const string Junction="J5074_877";
 public const float BoundaryTrim=8;
 public const string SouthExit="local:5023:746:5021:22";
 public const string RequiredContentHash="1233c5ca6b88d5de3c75d7542ebc294409750bd1d2daa5041658149457f7e42d";
 public static void Apply(){
  string root=ProjectMapBuild.Root,token=ProjectMapStore.Fingerprint(root);
  var before=ProjectMapStore.Load(root);
  Require(before.document.revision==23&&before.contentHash==RequiredContentHash,"Apply requires exact reviewed r23 hash; changed or already-applied map must be reassessed");
  string original=JsonUtility.ToJson(before);
  var after=Preview(before,new Parameters());
  Require(original==JsonUtility.ToJson(before),"preview changed original package");
  after.revision=24;after.savedAt=DateTime.UtcNow.ToString("o");after.projectRevision="";
  PinkRoundaboutMapTests.Validate(before,after);
  Require(original==JsonUtility.ToJson(before),"validation changed original package");
  var candidate=ProjectMapPackage.Create(before.baseline,after);
  Require(candidate.baselineHash==before.baselineHash,"baseline fingerprint changed");
  ProjectMapStore.Save(root,candidate,token);
  var loaded=ProjectMapStore.Load(root);
  Require(loaded.document.revision==24&&loaded.contentHash==candidate.contentHash&&loaded.baselineHash==before.baselineHash,"persisted revision/content/baseline fingerprint mismatch");
  PinkRoundaboutMapTests.Validate(before,loaded.document);
  ProjectMapBuild.Prepare();
  Debug.Log("PINK_ROUNDABOUT_MAP_APPLIED revision="+loaded.document.revision+" contentHash="+loaded.contentHash+" baselineHash="+loaded.baselineHash+" retained-turns=12 stops=39 pairs=1482 two-physical-lanes outer-provisional-routing game-estimate-dimensions");
 }
 [Serializable] public class Parameters {
  public float centreX=507.407013f,centreZ=87.669998f;
  public int ringLaneCount=2;
  public float islandRadius=8,ringCentreRadius=13.25f; // OUTER lane centre. Game estimates only.
  public float laneWidth=3.5f,islandHeight=.18f;
 }
 public static RoadMapFacility Control(){return new RoadMapFacility{
  id="reviewed:pink-roundabout:control",kind=ReviewedRoundaboutControl.Kind,type=ReviewedRoundaboutControl.Type,
  junctionId=Junction,rule=true,x=507.407013f,z=87.669998f,width=30,length=16,
  evidence=new RoadMapEvidence{state="observed",appearanceConfirmed=true,ruleConfirmed=true,month="2026-05",checkedAt="2026-11-13",
   url="https://map.naver.com/p?c=19,0,0,0,adh&p=RxMEWDM2QYHJZQ2HVJCFpg,135,-20,80,Float",
   note="Root firsthand observation: planted island and circular-arrow/회전차량우선 sign. Subsequent May2026 EYpgCQOw and 2024-10 br8am7cNh6e6wRfWRrKHrw views confirm two circulating lanes. Entry/exit lane assignments unconfirmed. Camera north of map junction is not island centre. Dimensions are game estimates."}};}
 public static RoadMapDocument Preview(ProjectMapPackage package,Parameters parameters){
  var a=parameters??new Parameters();Require(a.ringLaneCount==2,"field confirms two circulating lanes");
  Require(a.islandRadius>=6&&a.islandRadius<=10&&a.laneWidth>=3&&a.laneWidth<=3.75f&&a.ringCentreRadius-a.laneWidth*1.5f>=a.islandRadius-.01f,"two lanes must fit outside island");
  Require(a.ringCentreRadius+a.laneWidth*.5f<=18&&a.islandHeight>0&&a.islandHeight<=.3f,"explicit bounded game dimensions required");
  var d=package.document.Copy();Require(!d.facilities.Any(f=>f.kind==ReviewedRoundaboutControl.Kind&&f.junctionId==Junction),"already authored");
  var centre=new Vector3(a.centreX,0,a.centreZ);Require(Vector3.Distance(centre,package.baseline.junctionRules.Single(j=>j.id==Junction).V())<3,"centre must remain aligned with mapped junction, not camera");
  var originalJoins=d.lanes.Where(l=>!l.disabled&&l.ruleId==Junction&&l.id.StartsWith("join:")&&l.change==0).ToArray();Require(originalJoins.Length==12&&originalJoins.All(l=>l.turn>=-1&&l.turn<=1),"expected original twelve L/S/R joins");
  var inputIds=d.lanes.Where(l=>!l.disabled&&l.next.Any(id=>originalJoins.Any(j=>j.id==id))).Select(l=>l.id).ToArray();
  var outputIds=originalJoins.Select(j=>j.next.Single()).Distinct().ToArray();Require(inputIds.Length==4&&outputIds.Length==4,"four approaches required");
  foreach(string id in inputIds){var lane=d.lanes.Single(l=>l.id==id);var path=new AssistedRoadNetwork.Path(lane.points.Select(p=>p.V()).ToArray());float end=path.length-BoundaryTrim;Require(end>20,"approach too short");
   foreach(var stop in package.baseline.stops.Where(s=>s.graphStop&&package.baseline.hdEdges[s.laneEdge].id==id))Require(stop.laneAt<end-5,"bound stop lies in approach adjustment");
   Require(!d.scene.objects.Any(o=>o.enabled&&o.IsService&&o.laneId==id&&o.laneAt>end-5),"service in approach adjustment");
   lane.points=Slice(path,0,end);lane.heights=new float[lane.points.Length];
  }
  foreach(string id in outputIds){var head=d.lanes.Single(l=>l.id==id);string split=LaneStationAuthoring.SplitLineageAt(d,id,head.sourceOffset+BoundaryTrim);
   // Normal lineage encoding, stable task suffix instead of helper's GUID.
   string stable="editor:segment:"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(id)).TrimEnd('=').Replace('+','-').Replace('/','_')+":pink-roundabout";
   string splitNode=d.lanes.Single(l=>l.id==split).fromNode;
   d=RoadMapDocument.Parse(d.ToJson().Replace("\""+split+"\"","\""+stable+"\"").Replace("\""+splitNode+"\"","\"pink-roundabout:split:"+id+"\""));var tail=d.lanes.Single(l=>l.id==stable);
   if(id==SouthExit){
    Require(tail.next.Length==0&&!package.baseline.stops.Any(s=>s.graphStop&&package.baseline.hdEdges[s.laneEdge].id==id)&&!d.scene.objects.Any(o=>o.enabled&&o.IsService&&(o.laneId==id||o.laneId==tail.id)),"south clearance needs new binding review");
    var path=new AssistedRoadNetwork.Path(tail.points.Select(p=>p.V()).ToArray());float endZ=path.points.Last().z;var shifted=new List<MapPoint>();
    for(float at=0;at<path.length;at+=.4f){var v=path.Position(at);float up=Mathf.Clamp01((57-v.z)/14),down=Mathf.Clamp01((v.z-endZ)/(6-endZ));up=up*up*(3-2*up);down=down*down*(3-2*down);v.x+=1.2f*Mathf.Min(up,down);shifted.Add(Point(v));}
    shifted.Add(Point(path.points.Last()));tail.points=shifted.ToArray();tail.heights=new float[shifted.Count];
    tail.evidence=new RoadMapEvidence{state="game",checkedAt="2026-11-13",note="Local source-clearance repair, not streetview-measured: original width3.5m south lane overlaps source buildings4518(z35.57..41.54) and4520(z6.34..12.17). Shift centre east <=1.2m within source20.1m roadway, taper z57→43 and6→original endpoint2.234. Endpoints/sourceOffset8 retained; no stop/service or downstream station binding. Game arc length changes slightly."};
   }
   foreach(var join in d.lanes.Where(l=>originalJoins.Any(o=>o.id==l.id)&&l.next.Contains(id))){join.next=new[]{stable};join.toNode=tail.fromNode;}
  }
  var joins=d.lanes.Where(l=>originalJoins.Any(o=>o.id==l.id)).ToArray();
  foreach(var join in joins){
   var input=d.lanes.Single(l=>!l.disabled&&l.next.Contains(join.id));var output=d.lanes.Single(l=>l.id==join.next.Single());
   Require(input.number==1&&output.number==1,"single-lane entry/exit assumption changed");
   var start=input.points.Last().V();var end=output.points[0].V();var forward=(start-input.points[input.points.Length-2].V()).normalized;var leave=(output.points[1].V()-end).normalized;
   var pts=new List<Vector3>();
   if(join.turn==1){float handle=Vector3.Distance(start,end)*.3905243f;Bezier(pts,start,start+forward*handle,end-leave*handle,end,96);}
   else{
    Vector3 entryCentre,exitCentre;float entryRadius,exitRadius;
    TangentCircle(start,forward,centre,a.ringCentreRadius,out entryCentre,out entryRadius);TangentCircle(end,leave,centre,a.ringCentreRadius,out exitCentre,out exitRadius);
    var first=centre+(entryCentre-centre).normalized*a.ringCentreRadius;var last=centre+(exitCentre-centre).normalized*a.ringCentreRadius;
    Arc(pts,entryCentre,entryRadius,start,first,false);
    Arc(pts,centre,a.ringCentreRadius,first,last,true);
    Arc(pts,exitCentre,exitRadius,last,end,false);
   }
   join.points=pts.Select(Point).ToArray();join.heights=new float[pts.Count];join.width=a.laneWidth;join.evidence=new RoadMapEvidence{state="game",url=Control().evidence.url,month="2026-05",checkedAt="2026-11-13",note="Two physical circulating lanes observed. Existing twelve permissions retained on outer path; NPC lane assignment provisional pending markings. CCW geometry and dimensions are game estimates; no added U or inner-lane graph permissions."};
  }
  float outer=a.ringCentreRadius+a.laneWidth*.5f;var circle=Enumerable.Range(0,96).Select(i=>At(centre,outer,2*Mathf.PI*i/96)).ToArray();
  // Subtract both the circular pavement and the actual vehicle corridors.
  // Entry/exit curves and front/rear corners extend beyond the radial disk;
  // retaining a fragment just outside that disk can still obstruct a turn.
  // Width includes the lane half-width + 0.10m game clearance; 2.30m caps
  // include the 2.25m vehicle half-length used by the production checker.
  var cuts=new List<Vector3[]>{circle};
  foreach(var join in joins){var path=new AssistedRoadNetwork.Path(join.points.Select(p=>p.V()).ToArray());
   for(float at=0;at<path.length;at+=.5f){var start=path.Position(at);var end=path.Position(Mathf.Min(path.length,at+.5f));var heading=(end-start).normalized;
    var side=TrafficSystem.Right(heading)*(join.width*.5f+.10f);var rect=new[]{start-side-heading*2.30f,end-side+heading*2.30f,end+side+heading*2.30f,start+side-heading*2.30f};
    float orientation=0;for(int i=0;i<4;i++)orientation+=rect[i].x*rect[(i+1)%4].z-rect[(i+1)%4].x*rect[i].z;if(orientation<0)Array.Reverse(rect);cuts.Add(rect);
   }
  }
  float minX=cuts.Min(c=>c.Min(v=>v.x)),maxX=cuts.Max(c=>c.Max(v=>v.x)),minZ=cuts.Min(c=>c.Min(v=>v.z)),maxZ=cuts.Max(c=>c.Max(v=>v.z));
  // Only generated sidewalks touched by these local corridors are replaced;
  // retain outside fragments, unrelated authored pavements and every building.
  var extra=new List<ProjectMapObject>();foreach(var sidewalk in d.scene.objects.Where(o=>o.enabled&&o.kind=="sidewalk"&&o.id.StartsWith("sidewalk:generated:"))){
   if(sidewalk.points.All(p=>p.x<minX)||sidewalk.points.All(p=>p.x>maxX)||sidewalk.points.All(p=>p.z<minZ)||sidewalk.points.All(p=>p.z>maxZ))continue;
   var fragments=new List<List<Vector3>>{sidewalk.points.Select(p=>p.V()).ToList()};bool changed=false;
   foreach(var cut in cuts){var next=new List<List<Vector3>>();foreach(var fragment in fragments){var remainder=fragment;var outside=new List<List<Vector3>>();
     for(int i=0;i<cut.Length&&remainder.Count>=3;i++){var piece=Clip(remainder,cut[i],cut[(i+1)%cut.Length],false);if(piece.Count>=3)outside.Add(piece);remainder=Clip(remainder,cut[i],cut[(i+1)%cut.Length],true);}
     if(remainder.Count>=3){changed=true;next.AddRange(outside);}else next.Add(fragment);
    }fragments=next;if(fragments.Count==0)break;
   }
   if(!changed)continue;sidewalk.enabled=false;int n=0;foreach(var fragment in fragments){var copy=JsonUtility.FromJson<ProjectMapObject>(JsonUtility.ToJson(sidewalk));copy.id=sidewalk.id+":roundabout:"+n++;copy.enabled=true;copy.points=fragment.Select(Point).ToArray();extra.Add(copy);}
  }
  // Roads are OPEN centreline ribbons: a repeated closing endpoint is a
  // self-intersection under the normal scene validator. Four quarter arcs meet
  // at identical endpoints and retain a continuous two-lane annular surface.
  var ring=Enumerable.Range(0,128).Select(i=>Point(At(centre,a.ringCentreRadius-a.laneWidth*.5f,2*Mathf.PI*i/128))).ToArray();
  for(int quarter=0;quarter<4;quarter++){
   var arc=Enumerable.Range(0,33).Select(i=>ring[(quarter*32+i)%128]).ToArray();
   extra.Add(new ProjectMapObject{id="reviewed:pink-roundabout:road:"+quarter,kind="road",name="Two-lane circular carriageway quarter (game estimate)",points=arc,width=a.laneWidth*2,lanes=2,oneWay=true,suppressPaint=true,enabled=true});
  }
  // The annulus does not cover the tangent entry/exit flares. Author actual
  // pavement along every preserved movement, using the same 0.10m shoulder
  // clearance as sidewalk clipping. These are visible/collidable scene roads,
  // not a special exemption in footprint validation.
  foreach(var join in joins)extra.Add(new ProjectMapObject{id="reviewed:pink-roundabout:connector-road:"+join.id,kind="road",name="Roundabout entry/exit pavement (game estimate)",points=join.points.Select(p=>new MapPoint{x=p.x,z=p.z}).ToArray(),width=join.width+.20f,lanes=1,oneWay=true,suppressPaint=true,enabled=true});
  extra.Add(new ProjectMapObject{id="reviewed:pink-roundabout:island",kind="sidewalk",name="Planted central island (game estimate)",points=Enumerable.Range(0,96).Select(i=>Point(At(centre,a.islandRadius,2*Mathf.PI*i/96))).ToArray(),height=a.islandHeight,enabled=true});
  // Dashed separator uses authored line facilities; no directional arrows inferred.
  var paint=new List<RoadMapFacility>();float middle=a.ringCentreRadius-a.laneWidth*.5f;
  // Relocate the baseline's provisional crossings/stop planes outside the
  // circular roadway. Their old ~12/14m offsets would lie inside the ring and
  // let an entering car reach circulation before its reservation gate.
  var junction=package.baseline.junctionRules.Single(j=>j.id==Junction);var axisDirection=new Vector3(junction.dx,0,junction.dz).normalized;
  foreach(string id in inputIds){var lane=d.lanes.Single(l=>l.id==id);var endpoint=lane.points.Last().V();var heading=(endpoint-lane.points[lane.points.Length-2].V()).normalized;
   int axis=Mathf.Abs(Vector3.Dot(heading,axisDirection))>.7f?0:1,sign=Vector3.Dot(heading,axis==0?axisDirection:TrafficSystem.Right(axisDirection))>=0?1:-1;
   float roadWidth=junction.approaches.OrderByDescending(ap=>Vector3.Dot(new Vector3(ap.dx,0,ap.dz),-heading)).First().width;
   float setback=Vector3.Dot(centre-endpoint,heading);var crossing=centre-heading*(setback+4);
   paint.Add(new RoadMapFacility{id="reviewed:pink-roundabout:entry-stop:"+id,kind="stopLine",junctionId=Junction,laneId=id,axis=axis,sign=sign,x=endpoint.x,z=endpoint.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,width=roadWidth*.5f,length=.4f,rule=false,evidence=new RoadMapEvidence{state="game",note="Existing provisional stop plane relocated to outer entry boundary for circulating priority; exact field paint unverified."}});
   paint.Add(new RoadMapFacility{id="reviewed:pink-roundabout:crosswalk:"+id,kind="crosswalk",junctionId=Junction,laneId=id,axis=axis,sign=sign,x=crossing.x,z=crossing.z,yaw=Quaternion.LookRotation(heading).eulerAngles.y,width=roadWidth,length=2.4f,rule=false,evidence=new RoadMapEvidence{state="game",note="Existing provisional crossing relocated 4m upstream of the entry boundary, outside circular lanes; exact field position unverified."}});
  }
  for(int i=0;i<24;i++){float angle=2*Mathf.PI*i/24;var pos=At(centre,middle,angle);paint.Add(new RoadMapFacility{id="reviewed:pink-roundabout:dash:"+i,kind="line",x=pos.x,z=pos.z,yaw=Quaternion.LookRotation(Tangent(angle)).eulerAngles.y,width=.12f,length=1.35f,rule=false,evidence=new RoadMapEvidence{state="game",note="Observed dashed circulating-lane separator; exact dash positions are game estimates."}});}
  var control=Control();control.x=a.centreX;control.z=a.centreZ;control.width=outer*2;control.length=a.islandRadius*2;paint.Add(control);
  d.scene.objects=d.scene.objects.Concat(extra).ToArray();d.facilities=d.facilities.Concat(paint).ToArray();return d;
 }
 static void Require(bool ok,string why){if(!ok)throw new ArgumentException("PINK_ROUNDABOUT: "+why);}
 static MapPoint Point(Vector3 p){return new MapPoint{x=p.x,z=p.z};}
 static Vector3 At(Vector3 c,float r,float t){return c+new Vector3(Mathf.Cos(t)*r,0,Mathf.Sin(t)*r);}
 static Vector3 Tangent(float t){return new Vector3(-Mathf.Sin(t),0,Mathf.Cos(t));}
 static void Bezier(List<Vector3> p,Vector3 a,Vector3 b,Vector3 c,Vector3 d,int count){for(int i=p.Count==0?0:1;i<=count;i++){float t=i/(float)count,u=1-t;p.Add(a*u*u*u+b*3*u*u*t+c*3*u*t*t+d*t*t*t);}}
 static MapPoint[] Slice(AssistedRoadNetwork.Path path,float start,float end){var points=new List<MapPoint>{Point(path.Position(start))};float station=0;for(int i=1;i<path.points.Length;i++){station+=Vector3.Distance(path.points[i-1],path.points[i]);if(station>start+.001f&&station<end-.001f)points.Add(Point(path.points[i]));}points.Add(Point(path.Position(end)));return points.ToArray();}
 static void TangentCircle(Vector3 endpoint,Vector3 heading,Vector3 centre,float ringRadius,out Vector3 turnCentre,out float turnRadius){var relative=endpoint-centre;var right=TrafficSystem.Right(heading);turnRadius=(ringRadius*ringRadius-relative.sqrMagnitude)/(2*(Vector3.Dot(relative,right)-ringRadius));Require(turnRadius>=10&&turnRadius<40,"entry/exit tangent turn radius outside safe game range");turnCentre=endpoint+right*turnRadius;}
 static void Arc(List<Vector3> points,Vector3 centre,float radius,Vector3 start,Vector3 end,bool ccw){float from=Mathf.Atan2(start.z-centre.z,start.x-centre.x),to=Mathf.Atan2(end.z-centre.z,end.x-centre.x);if(ccw){while(to<=from)to+=Mathf.PI*2;}else{while(to>=from)to-=Mathf.PI*2;}Require(Mathf.Abs(to-from)<Mathf.PI*1.8f,"unexpected full-circle transition");int n=Mathf.CeilToInt(Mathf.Abs(to-from)*radius/.35f);if(points.Count==0)points.Add(start);for(int i=1;i<n;i++)points.Add(At(centre,radius,Mathf.Lerp(from,to,i/(float)n)));points.Add(end);}
 static List<Vector3> Clip(List<Vector3> polygon,Vector3 a,Vector3 b,bool inside){
  var output=new List<Vector3>();if(polygon.Count<3)return output;Func<Vector3,float> side=p=>(b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x);
  for(int i=0;i<polygon.Count;i++){var p=polygon[i];var q=polygon[(i+1)%polygon.Count];float ps=side(p),qs=side(q);bool pin=inside?ps>=0:ps<=0,qin=inside?qs>=0:qs<=0;if(pin)output.Add(p);if(pin!=qin)output.Add(Vector3.Lerp(p,q,ps/(ps-qs)));}
  for(int i=output.Count-1;i>=0&&output.Count>1;i--)if(Vector3.Distance(output[i],output[(i+1)%output.Count])<.002f)output.RemoveAt(i);
  if(output.Count<3)return new List<Vector3>();float area=0;for(int i=0;i<output.Count;i++)area+=output[i].x*output[(i+1)%output.Count].z-output[(i+1)%output.Count].x*output[i].z;return Mathf.Abs(area)<.005f?new List<Vector3>():output;
 }
}
