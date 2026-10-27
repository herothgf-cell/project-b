using System;
class DrowsyBlinkTests {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static double Loss(double t){return new Drowsiness{visionSeconds=t}.VisionLoss;}
 static void Main(){try{
  Check(Loss(0)==0&&Loss(29)==0,"initial warning leaves view open");
  double previous=0;double[] peaks={30.42,50.665,70.91,91.155,111.4};
  for(int i=0;i<5;i++){double strength=Loss(peaks[i]);Check(strength>.5&&strength>previous,"early closure peak strengthens "+i);Check(Loss(35+i*20)==0,"early eyes reopen between episodes "+i);previous=strength;}
  Check(Loss(120)==0,"final smooth close starts open");double last=0;for(int i=121;i<=180;i++){double loss=Loss(i);Check(loss>=last&&loss<=1,"final close never reopens "+i);last=loss;}Check(last==1,"complete blackout at180");
  var d=new Drowsiness();d.Advance(70.91,true,100);var copy=d.Copy();d.Advance(100,false,100);Check(Math.Abs(d.VisionLoss-copy.VisionLoss)<1e-9,"paused menus do not advance eyelids");copy.Advance(.1,true,100);d.Advance(.1,true,100);Check(d.VisionLoss==copy.VisionLoss,"saved exposure reproduces animation phase");d.Recover();Check(d.VisionLoss==0,"actual recovery opens eyes");
  Console.WriteLine("DROWSY_BLINK_OK checks="+checks);
 }catch(Exception e){Console.WriteLine(e);Environment.Exit(1);}}
}
