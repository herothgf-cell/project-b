using System;
using UnityEngine;
public static class RoadAnnotationEditTests {
 public static void Run(){
  var type=typeof(RoadScreenshotEditor);
  if(type.GetMethod("HandleShortcut")==null||type.GetMethod("TextBounds")==null||type.GetMethod("EditTextAt")==null)throw new Exception("Annotation keyboard undo and text area editing are missing");
  var editor=new RoadScreenshotEditor(null);var add=type.GetMethod("AddAnnotation");var set=type.GetMethod("SetSelectedText");
  var a=new RoadScreenshotEditor.Annotation{tool=2,text="short",color=Color.yellow};a.points.Add(new Vector2(.2f,.2f));add.Invoke(editor,new object[]{a});
  var style=new GUIStyle{font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),fontSize=20,wordWrap=true,padding=new RectOffset(4,4,2,2)};var picture=new Rect(0,0,600,400);
  var rect=(Rect)type.GetMethod("TextBounds").Invoke(null,new object[]{a,picture,style});if(rect.width>=200||rect.height>=50)throw new Exception("Short text retained oversized background");
  if(!(bool)type.GetMethod("EditTextAt").Invoke(editor,new object[]{new Vector2(rect.xMax-1,rect.center.y),picture,style}))throw new Exception("Right side of text is not editable");
  set.Invoke(editor,new object[]{"edited"});var ev=new Event{type=EventType.KeyDown,keyCode=KeyCode.Z,modifiers=EventModifiers.Control};type.GetMethod("HandleShortcut").Invoke(editor,new object[]{ev});
  if(editor.Items.Count!=1||editor.Items[0].text!="short"||ev.type!=EventType.Used)throw new Exception("Ctrl+Z did not undo text edit");
  editor.Undo();if(editor.Items.Count!=0)throw new Exception("Undo did not remove added text");
  Debug.Log("ROAD_ANNOTATION_EDIT_OK checks=4");
 }
 public static void Build(){Run();DevRoadReportTests.Build();}
}
