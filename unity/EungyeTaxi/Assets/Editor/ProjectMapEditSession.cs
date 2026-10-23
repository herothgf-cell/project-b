using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


public sealed class ProjectMapEditSession : IDisposable {
 [Serializable] sealed class Draft { public string fingerprint; public ProjectMapObject[] objects; public string trafficJson; }
 readonly string root;
 readonly MapData baseline;
 readonly RoadMapDocument original;
 readonly Dictionary<string,ProjectMapObject> index;
 readonly Dictionary<string,ProjectMapEditObjectHandle> handles=new Dictionary<string,ProjectMapEditObjectHandle>();
 readonly Dictionary<string,string> observed=new Dictionary<string,string>();
 readonly Dictionary<string,string> saved=new Dictionary<string,string>();
 readonly HashSet<string> dirtyIds=new HashSet<string>();
 ProjectMapTrafficHandle trafficHandle;
 string observedTraffic,savedTraffic;
 string fingerprint;
 bool disposed,ownsDraft,suppressNotifications;
 public event Action<string[]> Changed;
 public event Action TrafficChanged;
 // Borrowed read-only snapshot. Mutate only through EditTraffic. No scene geometry is retained.
 public RoadMapDocument Traffic {get{trafficHandle.value.scene=null;return trafficHandle.value;}}
 public MapData Baseline {get{return baseline;}}
 // Explicit snapshot operation. Do not call Document/Compose during a Scene handle drag.
 public RoadMapDocument Document {get{var doc=original.Copy();var traffic=Traffic.Copy();doc.lanes=traffic.lanes;doc.facilities=traffic.facilities;doc.signals=traffic.signals;doc.scene.objects=index.Keys.Select(GetObject).ToArray();RoadMapAuthoringOperations.RemapServiceBindings(doc);return doc;}}
 public MapData InitialMap {get;private set;}
 public IEnumerable<ProjectMapObject> Objects {get{return index.Keys.Select(GetObject);}}
 public IEnumerable<string> ObjectIds {get{return index.Keys;}}
 public int Count {get{return index.Count;}}
 public int EditedObjectCount {get{return dirtyIds.Count;}}
 public int EditedHandleCount {get{return handles.Count;}}
 public bool Dirty {get{return dirtyIds.Count>0||observedTraffic!=savedTraffic;}}
 public string DraftPath {get{return Path.Combine(root,"Library/ProjectMapEditor/session-draft.json");}}
 public bool HasDraft {get{return File.Exists(DraftPath);}}
 public static ProjectMapEditSession Open(string root){return new ProjectMapEditSession(root);}
 ProjectMapEditSession(string projectRoot){
  root=Path.GetFullPath(projectRoot);string text=File.ReadAllText(ProjectMapStore.SourcePath(root));fingerprint=ProjectMapPackage.Digest(text);var package=ProjectMapPackage.Parse(text);baseline=package.baseline;original=package.document;
  index=original.scene.objects.ToDictionary(o=>o.id);foreach(var pair in index)saved[pair.Key]=JsonUtility.ToJson(pair.Value);
  trafficHandle=ScriptableObject.CreateInstance<ProjectMapTrafficHandle>();trafficHandle.hideFlags=HideFlags.HideAndDontSave;trafficHandle.value=original.Copy();trafficHandle.value.scene=null;observedTraffic=savedTraffic=trafficHandle.value.ToJson();
  InitialMap=package.Compose();EungyeConnectivityRepair.Apply(InitialMap,baseline);Undo.undoRedoPerformed+=OnUndoRedo;AssemblyReloadEvents.beforeAssemblyReload+=BeforeReload;
 }
 static ProjectMapObject Copy(ProjectMapObject o){return JsonUtility.FromJson<ProjectMapObject>(JsonUtility.ToJson(o));}
 public ProjectMapObject GetObject(string id){ProjectMapEditObjectHandle handle;return Copy(handles.TryGetValue(id,out handle)?handle.value:index[id]);}
 ProjectMapEditObjectHandle Handle(string id){ProjectMapEditObjectHandle handle;if(!handles.TryGetValue(id,out handle)){handle=ScriptableObject.CreateInstance<ProjectMapEditObjectHandle>();handle.hideFlags=HideFlags.HideAndDontSave;handle.value=Copy(index[id]);handles.Add(id,handle);observed[id]=JsonUtility.ToJson(handle.value);}return handle;}
 static bool Finite(float n){return !float.IsNaN(n)&&!float.IsInfinity(n);}
 public void ApplyObject(ProjectMapObject value,string undoLabel="지도 객체 편집"){
  if(value==null||value.id==null||!index.ContainsKey(value.id))throw new ArgumentException("기존 객체 ID가 필요합니다.");
  var old=GetObject(value.id);if(value.kind!=old.kind)throw new ArgumentException("객체 종류를 변경할 수 없습니다.");
  if(value.points==null||value.points.Length<(value.kind=="road"?2:3)||value.points.Any(p=>p==null||!Finite(p.x)||!Finite(p.z)||!Finite(p.elevation))||new[]{value.height,value.width,value.yaw,value.serviceX,value.serviceZ,value.laneAt}.Any(n=>!Finite(n))||value.width<=0||value.height<0)throw new ArgumentException("유효한 좌표·양수 크기가 필요합니다.");
  if(value.points.Length==old.points.Length)for(int i=0;i<value.points.Length;i++)if(value.points[i].elevation!=old.points[i].elevation)throw new ArgumentException("수직 이동은 지원하지 않습니다.");
  if(JsonUtility.ToJson(old)==JsonUtility.ToJson(value))return;
  var handle=Handle(value.id);Undo.RecordObject(handle,undoLabel);handle.value=Copy(value);EditorUtility.SetDirty(handle);observed[value.id]=JsonUtility.ToJson(handle.value);TrackDirty(value.id,observed[value.id]);try{Notify(new[]{value.id});}catch{handle.value=old;observed[value.id]=JsonUtility.ToJson(old);TrackDirty(value.id,observed[value.id]);EditorUtility.SetDirty(handle);throw;}
 }
 public void EditTraffic(Action<RoadMapDocument> edit,string undoLabel="도로 편집"){
  if(edit==null)throw new ArgumentNullException("edit");
  var candidate=Traffic.Copy();edit(candidate);candidate.scene=null;ValidateTraffic(candidate);
  string json=candidate.ToJson();if(json==observedTraffic)return;
  var previous=Traffic;string previousJson=observedTraffic;
  Undo.RegisterCompleteObjectUndo(trafficHandle,undoLabel);trafficHandle.value=candidate;observedTraffic=json;EditorUtility.SetDirty(trafficHandle);
  try{NotifyTraffic();}catch{trafficHandle.value=previous;observedTraffic=previousJson;EditorUtility.SetDirty(trafficHandle);throw;}
 }
 public void ImportDocument(RoadMapDocument document){
  if(document==null)throw new ArgumentNullException("document");
  // Validate the complete candidate against this session's immutable baseline before touching Undo/state.
  var candidate=document.Copy();ProjectMapPackage.Create(baseline,candidate);
  var imported=candidate.scene.objects.ToDictionary(o=>o.id);
  foreach(var pair in imported){ProjectMapObject old;if(index.TryGetValue(pair.Key,out old)){old=GetObject(pair.Key);var value=pair.Value;if(old.kind!=value.kind)throw new ArgumentException("기존 객체 종류를 변경할 수 없습니다.");if(old.points.Length==value.points.Length&&old.points.Where((point,i)=>point.elevation!=value.points[i].elevation).Any())throw new ArgumentException("수직 이동은 지원하지 않습니다.");}}
  AtomicEdit(()=>{
   foreach(var value in imported.Values){if(!index.ContainsKey(value.id))AddTombstone(value);ApplyObject(value,"지도 JSON 가져오기");}
   foreach(var id in index.Keys.Where(id=>!imported.ContainsKey(id)).ToArray()){var removed=GetObject(id);removed.enabled=false;ApplyObject(removed,"지도 JSON 가져오기");}
   EditTraffic(d=>{d.lanes=candidate.lanes;d.facilities=candidate.facilities;d.signals=candidate.signals;},"지도 JSON 가져오기");
  },"지도 JSON 가져오기");
 }
 void AtomicEdit(Action edit,string label){
  var previous=index.Keys.ToDictionary(id=>id,GetObject);var previousJson=previous.ToDictionary(p=>p.Key,p=>JsonUtility.ToJson(p.Value));var previousHandles=new HashSet<string>(handles.Keys);var previousObserved=new Dictionary<string,string>(observed);var previousDirty=dirtyIds.ToArray();var previousTraffic=Traffic.Copy();string previousTrafficJson=observedTraffic;
  Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName(label);suppressNotifications=true;
  try{
   edit();var changed=index.Keys.Where(id=>!previousJson.ContainsKey(id)||JsonUtility.ToJson(GetObject(id))!=previousJson[id]).ToArray();
   suppressNotifications=false;if(changed.Length>0)Notify(changed);if(observedTraffic!=previousTrafficJson)NotifyTraffic();Undo.CollapseUndoOperations(group);
  }catch{
   suppressNotifications=true;try{Undo.FlushUndoRecordObjects();Undo.RevertAllDownToGroup(group);}catch{/* Restore explicitly even when a third-party Undo callback fails. */}
   foreach(var id in handles.Keys.Where(id=>!previousHandles.Contains(id)).ToArray()){var handle=handles[id];Undo.ClearUndo(handle);UnityEngine.Object.DestroyImmediate(handle);handles.Remove(id);}
   foreach(var id in index.Keys.Where(id=>!previous.ContainsKey(id)).ToArray()){index.Remove(id);saved.Remove(id);}
   foreach(var pair in previous){ProjectMapEditObjectHandle handle;if(handles.TryGetValue(pair.Key,out handle)){handle.value=Copy(pair.Value);EditorUtility.SetDirty(handle);}}
   observed.Clear();foreach(var pair in previousObserved)observed[pair.Key]=pair.Value;
   dirtyIds.Clear();foreach(var id in previousDirty)dirtyIds.Add(id);trafficHandle.value=previousTraffic;observedTraffic=previousTrafficJson;EditorUtility.SetDirty(trafficHandle);
   suppressNotifications=false;try{Notify(previous.Keys.ToArray());NotifyTraffic();}catch{}throw;
  }finally{suppressNotifications=false;Undo.IncrementCurrentGroup();}
 }
 public void RestoreAll(){var document=original.Copy();var traffic=RoadMapDocument.Parse(savedTraffic);document.lanes=traffic.lanes;document.facilities=traffic.facilities;document.signals=traffic.signals;document.scene.objects=index.Keys.Select(id=>JsonUtility.FromJson<ProjectMapObject>(saved[id])).ToArray();ImportDocument(document);}
 static void ValidateTraffic(RoadMapDocument doc){
  if(doc.lanes==null||doc.facilities==null||doc.signals==null)throw new ArgumentException("도로 목록이 필요합니다.");
  var ids=new HashSet<string>();foreach(var lane in doc.lanes){if(lane==null||string.IsNullOrEmpty(lane.id)||!ids.Add(lane.id)||lane.points==null||lane.points.Length<2||lane.points.Any(p=>p==null||!Finite(p.x)||!Finite(p.z)||!Finite(p.elevation))||!Finite(lane.width)||lane.width<=0||!Finite(lane.sourceOffset)||lane.heights==null||lane.heights.Any(h=>!Finite(h))||lane.next==null)throw new ArgumentException("유효한 차로 ID·좌표·폭이 필요합니다.");}
  ids.Clear();foreach(var f in doc.facilities){if(f==null||string.IsNullOrEmpty(f.id)||!ids.Add(f.id)||new[]{f.x,f.z,f.height,f.yaw,f.width,f.length,f.scale,f.startAt,f.endAt,f.speed}.Any(n=>!Finite(n))||f.width<=0||f.length<=0||f.scale<=0||f.height<0)throw new ArgumentException("유효한 시설 ID·좌표·크기가 필요합니다.");}
  ids.Clear();foreach(var p in doc.signals){if(p==null||string.IsNullOrEmpty(p.junctionId)||!ids.Add(p.junctionId)||p.groups==null||p.stages==null||p.groups.Any(g=>g==null||string.IsNullOrEmpty(g.id)||g.laneIds==null)||p.groups.Select(g=>g.id).Distinct().Count()!=p.groups.Length||p.stages.Any(t=>t==null||t.groups==null||new[]{t.green,t.yellow,t.clearance}.Any(n=>!Finite(n)||n<0)))throw new ArgumentException("유효한 신호 그룹·시간이 필요합니다.");}
 }
 void NotifyTraffic(){if(suppressNotifications)return;var changed=TrafficChanged;if(changed!=null)changed();}
 // Translation/rotation are horizontal; scale is measured in the object's current world X/Z axes.
 public void Transform(string id,Vector3 translation,float yawDegrees,Vector2 scaleXZ,float heightScale=1){
  if(!Finite(translation.x)||!Finite(translation.y)||!Finite(translation.z)||translation.y!=0||!Finite(yawDegrees)||!Finite(scaleXZ.x)||!Finite(scaleXZ.y)||scaleXZ.x<=0||scaleXZ.y<=0||!Finite(heightScale)||heightScale<=0)throw new ArgumentException("수평 이동과 양수 배율만 지원합니다.");
  var value=GetObject(id);var center=ProjectMapScene.Center(value.points);var rotation=Quaternion.Euler(0,yawDegrees,0);
  Func<Vector3,Vector3> transform=p=>center+rotation*new Vector3((p.x-center.x)*scaleXZ.x,0,(p.z-center.z)*scaleXZ.y)+translation;
  foreach(var p in value.points){var point=transform(p.V());p.x=point.x;p.z=point.z;}
  if(value.IsService){var service=transform(value.Position);value.serviceX=service.x;value.serviceZ=service.z;value.yaw=Mathf.Repeat(value.yaw+yawDegrees,360);}
  value.height*=heightScale;ApplyObject(value);
 }
 public string CreateObject(string kind,Vector3 position){
  if(!new[]{"road","building","sidewalk","parking","restaurant","rest","water","land"}.Contains(kind)||!Finite(position.x)||!Finite(position.z))throw new ArgumentException("지원하는 객체 종류와 좌표가 필요합니다.");
  position=new Vector3(Mathf.Clamp(position.x,4,Mathf.Max(4,baseline.width-4)),0,Mathf.Clamp(position.z,4,Mathf.Max(4,baseline.depth-4)));
  var value=new ProjectMapObject{id="editor:"+Guid.NewGuid().ToString("N"),kind=kind,name="새 "+kind,width=4,height=kind=="building"?8:kind=="sidewalk"?.14f:0,enabled=false,serviceX=position.x,serviceZ=position.z};
  value.points=kind=="road"?new[]{new MapPoint{x=position.x,z=position.z-3},new MapPoint{x=position.x,z=position.z+3}}:ProjectMapScene.Rectangle(position,Vector3.forward,6,6);
  if(value.IsService){var lane=Traffic.lanes.Where(l=>!l.disabled&&l.points!=null&&l.points.Length>1).OrderBy(l=>(l.points[0].V()-position).sqrMagnitude).FirstOrDefault();if(lane!=null)value.laneId=lane.id;}
  AddTombstone(value);value.enabled=true;ApplyObject(value,"지도 객체 추가");return value.id;
 }
 void AddTombstone(ProjectMapObject value){var initial=Copy(value);initial.enabled=false;index.Add(initial.id,initial);saved[initial.id]=JsonUtility.ToJson(initial);}
 public void DeleteObject(string id){var value=GetObject(id);value.enabled=false;ApplyObject(value,"지도 객체 삭제");}
 public void AddVertex(string id,int after){var value=GetObject(id);if(after<0||after>=value.points.Length||value.kind=="road"&&after==value.points.Length-1)throw new ArgumentOutOfRangeException("after");var a=value.points[after];var b=value.points[(after+1)%value.points.Length];var points=value.points.ToList();points.Insert(after+1,new MapPoint{x=(a.x+b.x)*.5f,z=(a.z+b.z)*.5f,elevation=(a.elevation+b.elevation)*.5f});value.points=points.ToArray();if(value.kind!="road")ProjectMapScene.Triangulate(value.points);ApplyObject(value,"꼭짓점 추가");}
 public void DeleteVertex(string id,int vertex){var value=GetObject(id);if(vertex<0||vertex>=value.points.Length)throw new ArgumentOutOfRangeException("vertex");if(value.points.Length<=(value.kind=="road"?2:3))throw new ArgumentException("최소 꼭짓점 수를 유지하세요.");value.points=value.points.Where((p,i)=>i!=vertex).ToArray();if(value.kind!="road")ProjectMapScene.Triangulate(value.points);ApplyObject(value,"꼭짓점 삭제");}
 public void SetVertex(string id,int vertex,Vector3 position){if(!Finite(position.x)||!Finite(position.z))throw new ArgumentException("유효한 좌표가 필요합니다.");var o=GetObject(id);o.points[vertex].x=position.x;o.points[vertex].z=position.z;if(o.kind!="road")ProjectMapScene.Triangulate(o.points);ApplyObject(o);}
 public void SetService(string id,Vector3 position,float yaw,string laneId,float laneAt){var o=GetObject(id);if(!o.IsService||position.y!=0)throw new ArgumentException("수평 서비스 정차점이 필요합니다.");o.serviceX=position.x;o.serviceZ=position.z;o.yaw=yaw;o.laneId=laneId;o.laneAt=laneAt;ApplyObject(o);}
 void TrackDirty(string id,string json){if(json==saved[id])dirtyIds.Remove(id);else dirtyIds.Add(id);}
 void Notify(string[] ids){if(suppressNotifications)return;var changed=Changed;if(changed!=null)changed(ids);}
 void OnUndoRedo(){var changed=new List<string>();foreach(var p in handles){string json=JsonUtility.ToJson(p.Value.value);if(observed[p.Key]!=json){observed[p.Key]=json;TrackDirty(p.Key,json);changed.Add(p.Key);}}if(changed.Count>0)Notify(changed.ToArray());string trafficJson=Traffic.ToJson();if(trafficJson!=observedTraffic){observedTraffic=trafficJson;NotifyTraffic();}}
 public MapData Compose(){return RoadMapComposer.Compose(baseline,Document);}
 public void Save(){
  if(HasDraft&&!ownsDraft)throw new IOException("복구하지 않은 지도 초안이 남아 있습니다. 복구하거나 명시적으로 버린 후 저장하세요.");
  // The Store lock checks the complete source fingerprint, including external baseline changes.
  var doc=Document;doc.revision++;var package=ProjectMapPackage.Create(baseline,doc);fingerprint=ProjectMapStore.Save(root,package,fingerprint);
  foreach(var p in index.Keys)saved[p]=JsonUtility.ToJson(GetObject(p));dirtyIds.Clear();savedTraffic=observedTraffic;original.revision=doc.revision;original.savedAt=package.document.savedAt;DiscardDraft();
 }
 public void WriteDraft(){if(!Dirty){if(ownsDraft)DiscardDraft();return;}if(HasDraft&&!ownsDraft)throw new IOException("복구하지 않은 초안을 덮어쓸 수 없습니다. 복구하거나 명시적으로 버리세요.");if(HasDraft){var previous=JsonUtility.FromJson<Draft>(File.ReadAllText(DraftPath));if(previous==null||previous.fingerprint!=fingerprint)throw new IOException("다른 기준 지도의 복구 초안이 남아 있습니다. 기존 초안을 보존하세요.");}var draft=new Draft{fingerprint=fingerprint,trafficJson=observedTraffic!=savedTraffic?observedTraffic:null,objects=handles.Where(p=>JsonUtility.ToJson(p.Value.value)!=saved[p.Key]).Select(p=>Copy(p.Value.value)).ToArray()};Directory.CreateDirectory(Path.GetDirectoryName(DraftPath));string temp=DraftPath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(draft));if(File.Exists(DraftPath))File.Replace(temp,DraftPath,null);else File.Move(temp,DraftPath);ownsDraft=true;}
 public void RecoverDraft(){
  if(!HasDraft)return;var draft=JsonUtility.FromJson<Draft>(File.ReadAllText(DraftPath));if(draft==null||draft.fingerprint!=fingerprint)throw new IOException("초안의 기준 프로젝트 지도가 변경되었습니다. 초안을 보존하고 원본을 확인하세요.");
  RoadMapDocument traffic=null;if(!string.IsNullOrEmpty(draft.trafficJson)){traffic=RoadMapDocument.Parse(draft.trafficJson);if(traffic==null||traffic.mapId!=Traffic.mapId||traffic.baseHash!=Traffic.baseHash||traffic.schema!=Traffic.schema)throw new IOException("초안의 도로 기준 지도가 다릅니다.");ValidateTraffic(traffic);}
  var values=draft.objects??new ProjectMapObject[0];var draftIds=new HashSet<string>();foreach(var value in values){
   if(value==null||string.IsNullOrEmpty(value.id)||!draftIds.Add(value.id)||!new[]{"road","building","sidewalk","parking","restaurant","rest","water","land"}.Contains(value.kind))throw new IOException("초안 객체 ID/종류가 유효하지 않습니다.");
   if(value.points==null||value.points.Length<(value.kind=="road"?2:3)||value.points.Any(p=>p==null||!Finite(p.x)||!Finite(p.z)||!Finite(p.elevation))||new[]{value.height,value.width,value.yaw,value.serviceX,value.serviceZ,value.laneAt}.Any(n=>!Finite(n))||value.width<=0||value.height<0)throw new IOException("초안 객체 좌표/크기가 유효하지 않습니다.");
   if(index.ContainsKey(value.id)){var old=GetObject(value.id);if(old.kind!=value.kind||old.points.Length==value.points.Length&&old.points.Where((p,i)=>p.elevation!=value.points[i].elevation).Any())throw new IOException("초안의 기존 객체 종류/높이가 다릅니다.");}
  }
  AtomicEdit(()=>{if(traffic!=null)EditTraffic(d=>{d.lanes=traffic.lanes;d.facilities=traffic.facilities;d.signals=traffic.signals;},"도로 초안 복구");foreach(var value in values){if(!index.ContainsKey(value.id))AddTombstone(value);ApplyObject(value,"지도 초안 복구");}},"지도 초안 복구");ownsDraft=true;
 }
 public void DiscardDraft(){if(File.Exists(DraftPath))File.Delete(DraftPath);ownsDraft=false;}
 void BeforeReload(){WriteDraft();}
 public void Dispose(){if(disposed)return;WriteDraft();disposed=true;Undo.undoRedoPerformed-=OnUndoRedo;AssemblyReloadEvents.beforeAssemblyReload-=BeforeReload;foreach(var handle in handles.Values){Undo.ClearUndo(handle);UnityEngine.Object.DestroyImmediate(handle);}handles.Clear();Undo.ClearUndo(trafficHandle);UnityEngine.Object.DestroyImmediate(trafficHandle);}
}
