using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;


public sealed class ProjectMapEditSession : IDisposable {
 [Serializable] sealed class Draft { public string fingerprint; public ProjectMapObject[] objects; }
 readonly string root;
 readonly MapData baseline;
 readonly RoadMapDocument original;
 readonly Dictionary<string,ProjectMapObject> index;
 readonly Dictionary<string,ProjectMapEditObjectHandle> handles=new Dictionary<string,ProjectMapEditObjectHandle>();
 readonly Dictionary<string,string> observed=new Dictionary<string,string>();
 readonly Dictionary<string,string> saved=new Dictionary<string,string>();
 readonly HashSet<string> dirtyIds=new HashSet<string>();
 string fingerprint;
 bool disposed,ownsDraft;
 public event Action<string[]> Changed;
 public MapData Baseline {get{return baseline;}}
 // Explicit snapshot operation. Do not call Document/Compose during a Scene handle drag.
 public RoadMapDocument Document {get{var doc=original.Copy();doc.scene.objects=original.scene.objects.Select(o=>GetObject(o.id)).ToArray();return doc;}}
 public MapData InitialMap {get;private set;}
 public IEnumerable<ProjectMapObject> Objects {get{return index.Keys.Select(GetObject);}}
 public IEnumerable<string> ObjectIds {get{return index.Keys;}}
 public int Count {get{return index.Count;}}
 public int EditedObjectCount {get{return dirtyIds.Count;}}
 public int EditedHandleCount {get{return handles.Count;}}
 public bool Dirty {get{return dirtyIds.Count>0;}}
 public string DraftPath {get{return Path.Combine(root,"Library/ProjectMapEditor/session-draft.json");}}
 public bool HasDraft {get{return File.Exists(DraftPath);}}
 public static ProjectMapEditSession Open(string root){return new ProjectMapEditSession(root);}
 ProjectMapEditSession(string projectRoot){
  root=Path.GetFullPath(projectRoot);string text=File.ReadAllText(ProjectMapStore.SourcePath(root));fingerprint=ProjectMapPackage.Digest(text);var package=ProjectMapPackage.Parse(text);baseline=package.baseline;original=package.document;
  index=original.scene.objects.ToDictionary(o=>o.id);foreach(var pair in index)saved[pair.Key]=JsonUtility.ToJson(pair.Value);
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
 // Translation/rotation are horizontal; scale is measured in the object's current world X/Z axes.
 public void Transform(string id,Vector3 translation,float yawDegrees,Vector2 scaleXZ,float heightScale=1){
  if(!Finite(translation.x)||!Finite(translation.y)||!Finite(translation.z)||translation.y!=0||!Finite(yawDegrees)||!Finite(scaleXZ.x)||!Finite(scaleXZ.y)||scaleXZ.x<=0||scaleXZ.y<=0||!Finite(heightScale)||heightScale<=0)throw new ArgumentException("수평 이동과 양수 배율만 지원합니다.");
  var value=GetObject(id);var center=ProjectMapScene.Center(value.points);var rotation=Quaternion.Euler(0,yawDegrees,0);
  Func<Vector3,Vector3> transform=p=>center+rotation*new Vector3((p.x-center.x)*scaleXZ.x,0,(p.z-center.z)*scaleXZ.y)+translation;
  foreach(var p in value.points){var point=transform(p.V());p.x=point.x;p.z=point.z;}
  if(value.IsService){var service=transform(value.Position);value.serviceX=service.x;value.serviceZ=service.z;value.yaw=Mathf.Repeat(value.yaw+yawDegrees,360);}
  value.height*=heightScale;ApplyObject(value);
 }
 public void SetVertex(string id,int vertex,Vector3 position){if(!Finite(position.x)||!Finite(position.z))throw new ArgumentException("유효한 좌표가 필요합니다.");var o=GetObject(id);o.points[vertex].x=position.x;o.points[vertex].z=position.z;if(o.kind!="road")ProjectMapScene.Triangulate(o.points);ApplyObject(o);}
 public void SetService(string id,Vector3 position,float yaw,string laneId,float laneAt){var o=GetObject(id);if(!o.IsService||position.y!=0)throw new ArgumentException("수평 서비스 정차점이 필요합니다.");o.serviceX=position.x;o.serviceZ=position.z;o.yaw=yaw;o.laneId=laneId;o.laneAt=laneAt;ApplyObject(o);}
 void TrackDirty(string id,string json){if(json==saved[id])dirtyIds.Remove(id);else dirtyIds.Add(id);}
 void Notify(string[] ids){var changed=Changed;if(changed!=null)changed(ids);}
 void OnUndoRedo(){var changed=new List<string>();foreach(var p in handles){string json=JsonUtility.ToJson(p.Value.value);if(observed[p.Key]!=json){observed[p.Key]=json;TrackDirty(p.Key,json);changed.Add(p.Key);}}if(changed.Count>0)Notify(changed.ToArray());}
 public MapData Compose(){return RoadMapComposer.Compose(baseline,Document);}
 public void Save(){
  if(HasDraft&&!ownsDraft)throw new IOException("복구하지 않은 지도 초안이 남아 있습니다. 복구하거나 명시적으로 버린 후 저장하세요.");
  // The Store lock checks the complete source fingerprint, including external baseline changes.
  var doc=Document;doc.revision++;var package=ProjectMapPackage.Create(baseline,doc);fingerprint=ProjectMapStore.Save(root,package,fingerprint);
  foreach(var p in index.Keys)saved[p]=JsonUtility.ToJson(GetObject(p));dirtyIds.Clear();original.revision=doc.revision;original.savedAt=package.document.savedAt;DiscardDraft();
 }
 public void WriteDraft(){if(!Dirty){if(ownsDraft)DiscardDraft();return;}if(HasDraft&&!ownsDraft)throw new IOException("복구하지 않은 초안을 덮어쓸 수 없습니다. 복구하거나 명시적으로 버리세요.");if(HasDraft){var previous=JsonUtility.FromJson<Draft>(File.ReadAllText(DraftPath));if(previous==null||previous.fingerprint!=fingerprint)throw new IOException("다른 기준 지도의 복구 초안이 남아 있습니다. 기존 초안을 보존하세요.");}var draft=new Draft{fingerprint=fingerprint,objects=handles.Where(p=>JsonUtility.ToJson(p.Value.value)!=saved[p.Key]).Select(p=>Copy(p.Value.value)).ToArray()};Directory.CreateDirectory(Path.GetDirectoryName(DraftPath));string temp=DraftPath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(draft));if(File.Exists(DraftPath))File.Replace(temp,DraftPath,null);else File.Move(temp,DraftPath);ownsDraft=true;}
 public void RecoverDraft(){if(!HasDraft)return;var draft=JsonUtility.FromJson<Draft>(File.ReadAllText(DraftPath));if(draft==null||draft.fingerprint!=fingerprint)throw new IOException("초안의 기준 프로젝트 지도가 변경되었습니다. 초안을 보존하고 원본을 확인하세요.");foreach(var value in draft.objects??new ProjectMapObject[0])ApplyObject(value,"지도 초안 복구");ownsDraft=true;}
 public void DiscardDraft(){if(File.Exists(DraftPath))File.Delete(DraftPath);ownsDraft=false;}
 void BeforeReload(){WriteDraft();}
 public void Dispose(){if(disposed)return;try{WriteDraft();}finally{disposed=true;Undo.undoRedoPerformed-=OnUndoRedo;AssemblyReloadEvents.beforeAssemblyReload-=BeforeReload;foreach(var handle in handles.Values){Undo.ClearUndo(handle);UnityEngine.Object.DestroyImmediate(handle);}handles.Clear();}}
}
