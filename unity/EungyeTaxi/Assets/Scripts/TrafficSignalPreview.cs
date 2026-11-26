public partial class TrafficSystem {
 // Querying HUD state cannot create, step, or restore an authoritative clock.
 public NextSignalPreview PreviewNextSignal(TrafficJunction junction,int axis,int sign){return PreviewLight(junction,axis,sign,SignalPhasePreview.AnyTurn);}
 // turn: only that movement's change counts (the light the driver watches, 2026-11-26); while it is not green the
 // demand-actuated forecast takes the latest case so the countdown never rises.
 public NextSignalPreview PreviewLight(TrafficJunction junction,int axis,int sign,int turn){
  if(junction==null||junction.scenarioSignal||!junction.signalized)return NextSignalPreview.Unknown;
  if(Adaptive(junction)){
   DemandSignalClock clock;if(!demandClocks.TryGetValue(junction,out clock))return NextSignalPreview.Unknown;
   bool waiting=turn!=SignalPhasePreview.AnyTurn&&MovementSignal(junction,axis,turn,sign)!=SignalColor.Green;
   return SignalPhasePreview.AdaptiveFor(junction.gameProgram,clock.Capture(junction.name),axis,sign,ambientPedestrians&&ambientPedestrians.CrossingOccupied(junction),turn,waiting);
  }
  var program=junction.editorProgram??junction.gameProgram;
  return SignalPhasePreview.ProgramFor(program,axis,sign,junction.editorProgram!=null?Clock:Clock+junction.offset,turn);
 }
}
