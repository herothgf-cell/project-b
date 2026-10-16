using System;

// Input edges and traffic advice are independent of rendering and physics.
public sealed class AssistedDrivingPolicy {
    bool armed=true;
    public int Pulse(float value) {
        if(Math.Abs(value)<.2f){armed=true;return 0;}
        if(!armed||Math.Abs(value)<.55f)return 0;
        armed=false;return value<0?-1:1;
    }
    public void Reset(){armed=true;}
    public static bool CanChange(int lane,int count,int side,bool busy,float speed) {
        return !busy&&speed>.5f&&(side==-1||side==1)&&lane+side>=0&&lane+side<count;
    }
    public static bool RearRisk(float gap,float rearSpeed,float ownSpeed) {
        return gap>0&&rearSpeed>ownSpeed&&gap/(rearSpeed-ownSpeed)<3;
    }
}
