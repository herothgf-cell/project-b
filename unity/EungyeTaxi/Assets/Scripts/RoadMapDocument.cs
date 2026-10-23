using System;
using System.Linq;
using UnityEngine;

[Serializable] public class RoadMapEvidence {public string state="game",url="",month="",checkedAt="",note="";public bool appearanceConfirmed,ruleConfirmed;}
[Serializable] public class RoadMapLane {
 public string id,sourceId,fromNode,toNode,ruleId,roadName,status;public bool disabled;public int number,turn,change;public float width=3.5f,sourceOffset;public MapPoint[] points;public float[] heights=new float[0];public string[] next=new string[0];public RoadMapEvidence evidence=new RoadMapEvidence();
 public static RoadMapLane From(HdEdge e,HdEdge[] edges){return new RoadMapLane{id=e.id,sourceId=e.sourceId,fromNode=e.fromNode,toNode=e.toNode,ruleId=e.ruleId,roadName=e.roadName,status=e.status,disabled=e.disabled,number=e.number,turn=e.turn,change=e.change,points=e.points.Select(p=>new MapPoint{x=p.x,z=p.z}).ToArray(),heights=e.points.Select(p=>p.elevation).ToArray(),sourceOffset=e.sourceOffset,next=(e.next??new int[0]).Select(i=>edges[i].id).ToArray()};}
}
[Serializable] public class RoadMapFacility {
 public string id,kind="sign",type="안내",junctionId="",laneId="",groupId="",targetId="",text="게임 설정";public bool rule;public float x,z,height=3,yaw,width=3,length=2.4f,scale=1,startAt,endAt,speed=30;public int axis,sign=1;public RoadMapEvidence evidence=new RoadMapEvidence();
 public Vector3 Position {get{return new Vector3(x,0,z);}}
}
[Serializable] public class RoadMapDocument {
 public ProjectMapScene scene;public string projectRevision="";public int schema=1,revision=1;public string mapId,baseHash,id=Guid.NewGuid().ToString("N"),name="내 은계 수정본",savedAt="";public RoadMapLane[] lanes=new RoadMapLane[0];public RoadMapFacility[] facilities=new RoadMapFacility[0];public RoadMapSignalProgram[] signals=new RoadMapSignalProgram[0];
 public static RoadMapDocument Create(MapData map){return new RoadMapDocument{mapId=map.mapId,baseHash=RoadMapRuntime.Hash(map),lanes=map.hdEdges.Select(e=>RoadMapLane.From(e,map.hdEdges)).ToArray()};}
 public string ToJson(bool pretty=false){var json=JsonUtility.ToJson(this,pretty);if(scene!=null)return json;int start,end;if(RoadMapDocumentJson.Member(json,"scene",out start,out end))json=json.Substring(0,start)+"null"+json.Substring(end);return json;}
 public static RoadMapDocument Parse(string json){var result=JsonUtility.FromJson<RoadMapDocument>(json);if(result==null)return null;int start,end;if(!RoadMapDocumentJson.Member(json,"scene",out start,out end)||json.Substring(start,end-start).Trim()=="null")result.scene=null;return result;}
 public RoadMapDocument Copy(){return Parse(ToJson());}
 public string Version {get{return name+" · r"+revision;}}
}
public sealed class RoadMapEditHistory {
 readonly System.Collections.Generic.Stack<string> undo=new System.Collections.Generic.Stack<string>(),redo=new System.Collections.Generic.Stack<string>();
 public RoadMapEditHistory(RoadMapDocument document){}
 public bool CanUndo {get{return undo.Count>0;}} public bool CanRedo {get{return redo.Count>0;}}
 public void Commit(RoadMapDocument document){undo.Push(document.ToJson());if(undo.Count>30){var keep=undo.Take(30).Reverse().ToArray();undo.Clear();foreach(var item in keep)undo.Push(item);}redo.Clear();}
 public RoadMapDocument Undo(RoadMapDocument document){if(!CanUndo)return document;redo.Push(document.ToJson());return RoadMapDocument.Parse(undo.Pop());}
 public RoadMapDocument Redo(RoadMapDocument document){if(!CanRedo)return document;undo.Push(document.ToJson());return RoadMapDocument.Parse(redo.Pop());}
}
