using System;
class MannerTests {
 static void Check(bool c,string m){if(!c)throw new Exception(m);}
 static void Main(){var m=new MannerLedger();Check(m.Score==100,"initial");Check(m.Penalize("red",12,1,3),"first event");Check(!m.Penalize("red",12,2,3),"duplicate event");Check(m.Score==88,"one deduction");Check(m.Penalize("red",12,5,3),"later event");m.Penalize("hit",200,6,1);Check(m.Score==0,"clamp");m.Reset();Check(m.Score==100,"reset");Check(MannerLedger.Payment(4900,59,60)==3430,"exact won rounding");Check(MannerLedger.Payment(5000,69,70)==3500,"below requirement");Check(MannerLedger.Payment(5000,70,70)==5000,"met requirement");
  Check(DriveMath.TravelAcceleration(-4,-3)==4,"reverse acceleration");Check(DriveMath.TravelAcceleration(6,-3)==-6,"reverse braking");
  foreach(float speed in new[]{40f,60f,90f}){double v=speed/3.6;double lateral=v*v*Math.Tan(DriveMath.SteerAngle(speed)*Math.PI/180)/2.6;Check(lateral<=5.01,"lateral limit at "+speed);}
  Console.WriteLine("PASS: score deduplication / passenger requirement / steering lateral bounds");}
}
