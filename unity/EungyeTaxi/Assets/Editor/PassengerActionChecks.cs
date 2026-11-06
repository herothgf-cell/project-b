using System;
using UnityEngine;
public static class PassengerActionChecks {
 static int count;static void Check(bool value,string message){count++;if(!value)throw new Exception("PASSENGER_ACTION: "+message);}
 public static void Run(){
  var gate=new PassengerActionVisibility();Check(!gate.Observe("ride:wake",false,true)&&!gate.Expanded,"moving does not auto open");Check(!gate.Observe("ride:wake",true,false),"other modal defers opening");Check(gate.Observe("ride:wake",true,true)&&gate.Expanded,"safe opens once");gate.Fold();for(int i=0;i<20;i++)Check(!gate.Observe("ride:wake",true,true)&&!gate.Expanded,"timer refresh preserves fold");gate.Open();Check(gate.Expanded,"manual reopen");gate.Observe("",true,true);Check(!gate.Expanded,"completion closes");Check(gate.Observe("second:wake",true,true),"next ride same action opens");
  foreach(PassengerActionKind kind in Enum.GetValues(typeof(PassengerActionKind))){var state=new PassengerActionPresentation{kind=kind,remaining=5,total=10,safe=true};Check(state.Active==(kind!=PassengerActionKind.None),"kind active");Check(Math.Abs(state.Progress-.5f)<.001,"work progress");if(kind==PassengerActionKind.Wake){Check(!state.Enabled(0)&&state.Enabled(1),"waiting wake disabled but fold enabled");state.ready=true;Check(state.Enabled(0),"ready wake enabled");state.safe=false;Check(!state.Enabled(0),"unsafe wake disabled");}else Check(state.Enabled(0)&&!state.Enabled(1),"work only offers fold");Check(!state.Enabled(-1)&&!state.Enabled(9),"invalid index blocked");}
  foreach(var size in new[]{new Vector2(1280,720),new Vector2(1920,1080),new Vector2(2560,1080)}){float scale=UiReadability.HudScale(size.x,size.y);var r=PassengerActionUI.Bounds(size.x,size.y);r=new Rect(r.x*scale,r.y*scale,r.width*scale,r.height*scale);Check(!r.Overlaps(CockpitUI.SpeedBounds(size.x,size.y)),"speed remains visible");Check(!r.Overlaps(new Rect(24*scale,392*scale,300*scale,244*scale)),"PVP standings remain visible");Check(r.y>=0&&r.x>=0&&r.xMax<=size.x&&r.yMax<=size.y,"popup contained");}
  Debug.Log("PASSENGER_ACTION_CHECKS_OK checks="+count);
 }
 public static void Build(){Run();DrivingHudConceptChecks.Build();}
}
