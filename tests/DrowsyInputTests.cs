using System;
public static class DrowsyInputTests {
 public static void Main(){
  var d=new Drowsiness{visionSeconds=30.42};
  if(!d.ControlsLost)throw new Exception("Short eye closure must interrupt input before blackout");
  d.visionSeconds=33;if(d.ControlsLost)throw new Exception("Input must resume after short closure");
  d.Recover();if(d.ControlsLost)throw new Exception("Sleep must restore input");
  foreach(double peak in new[]{30.42,50.665,70.91,91.155,111.4}){d.visionSeconds=peak;if(!d.Microsleep)throw new Exception("Every deep blink interrupts input");d.visionSeconds=peak+5;if(d.Microsleep)throw new Exception("Every blink reopens");}
  d.visionSeconds=150;if(d.Microsleep||d.ControlsLost)throw new Exception("Final closure uses existing safety stop policy");
  d.visionSeconds=180;if(!d.ControlsLost||d.Microsleep)throw new Exception("Blackout remains distinct from short microsleep");
  Console.WriteLine("DROWSY_INPUT_OK checks=15");
 }
}
