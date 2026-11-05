public partial class TrafficSystem {
 // Querying HUD state cannot create, step, or restore an authoritative clock.
 public NextSignalPreview PreviewNextSignal(TrafficJunction junction,int axis,int sign){
  if(junction==null||junction.scenarioSignal||!junction.signalized)return NextSignalPreview.Unknown;
  if(Adaptive(junction)){
   DemandSignalClock clock;if(!demandClocks.TryGetValue(junction,out clock))return NextSignalPreview.Unknown;
   return SignalPhasePreview.Adaptive(junction.gameProgram,clock.Capture(junction.name),axis,sign,ambientPedestrians&&ambientPedestrians.CrossingOccupied(junction));
  }
  var program=junction.editorProgram??junction.gameProgram;
  return SignalPhasePreview.Program(program,axis,sign,junction.editorProgram!=null?Clock:Clock+junction.offset);
 }
}
