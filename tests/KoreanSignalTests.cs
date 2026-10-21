using System;
class KoreanSignalTests {
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main(){
        foreach(var mode in new[]{LeftTurnMode.Permissive,LeftTurnMode.ProtectedShared})for(float t=-69;t<140;t+=.25f)for(int a=0;a<2;a++)foreach(int sign in new[]{-1,1}){
            float wait=KoreanSignalRules.SecondsToChange(t,mode,a,sign);var current=KoreanSignalRules.Signal(t,mode,a,sign);
            Check(wait>0&&KoreanSignalRules.Signal(t+wait-.01f,mode,a,sign)==current&&KoreanSignalRules.Signal(t+wait+.01f,mode,a,sign)!=current,"countdown matches next actual lamp change");
        }
        Check(KoreanSignalRules.MayEnter(1,LeftTurnMode.Permissive,0,1,-1,false,true,true),"signed permissive green allows clear left");
        Check(!KoreanSignalRules.MayEnter(1,LeftTurnMode.Permissive,0,1,-1,false,false,true),"permissive left yields to opposing traffic");
        Check(!KoreanSignalRules.MayEnter(18,LeftTurnMode.Permissive,0,1,-1,true,true,true),"red never permits permissive left");
        Check(!KoreanSignalRules.MayEnter(13,LeftTurnMode.Permissive,0,1,-1,true,true,true),"yellow does not permit new left entry");
        Check(!KoreanSignalRules.MayEnter(18,LeftTurnMode.Permissive,0,1,1,false,true,true),"red right requires stop");
        Check(KoreanSignalRules.MayEnter(18,LeftTurnMode.Permissive,0,1,1,true,true,true),"red right after stop with clear path");
        Check(!KoreanSignalRules.MayEnter(18,LeftTurnMode.Permissive,0,1,1,true,true,false),"right yields to pedestrian");
        Check(!KoreanSignalRules.MayEnter(18,LeftTurnMode.Permissive,0,1,0,true,true,true),"stop never grants red straight");
        for(float t=0;t<68;t+=.25f){int open=0;for(int a=0;a<2;a++)foreach(int s in new[]{-1,1})if(KoreanSignalRules.Signal(t,LeftTurnMode.ProtectedShared,a,s)==SignalColor.Green)open++;Check(open<=1,"shared-lane protected phase isolates one approach");}
        Check(KoreanSignalRules.MayEnter(1,LeftTurnMode.ProtectedShared,0,1,-1,false,true,true),"protected shared lane permits left with simultaneous straight");
        Check(!KoreanSignalRules.MayEnter(1,LeftTurnMode.ProtectedShared,0,-1,0,false,true,true),"opposing straight red during protected left");
        Console.WriteLine("PASS Korean signal permissions, yielding and shared-lane phase safety");
    }
}
