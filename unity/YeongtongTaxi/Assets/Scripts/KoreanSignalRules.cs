// Operation is an explicit junction setting, never inferred from lane count.
public enum LeftTurnMode { ProtectedShared, Permissive }
public static class KoreanSignalRules {
    // Prototype timings, not statutory timing requirements. Each clearance is two seconds.
    public static SignalColor Signal(float time,LeftTurnMode mode,int axis,int sign){
        int phases=mode==LeftTurnMode.Permissive?2:4;
        float cycle=17*phases,t=(time%cycle+cycle)%cycle;
        int approach=mode==LeftTurnMode.Permissive?axis:axis*2+(sign>0?0:1);
        float local=t-approach*17;
        return local>=0&&local<12?SignalColor.Green:local>=12&&local<15?SignalColor.Yellow:SignalColor.Red;
    }
    // No dedicated right-turn signal in this prototype. Clearance includes other traffic.
    public static bool MayEnter(float time,LeftTurnMode mode,int axis,int sign,int turn,bool stopped,bool trafficClear,bool pedestrianClear){
        var signal=Signal(time,mode,axis,sign);
        if(turn==0)return signal==SignalColor.Green&&trafficClear;
        if(!trafficClear||!pedestrianClear)return false;
        if(turn<0)return signal==SignalColor.Green;
        return signal!=SignalColor.Red||stopped;
    }
}
