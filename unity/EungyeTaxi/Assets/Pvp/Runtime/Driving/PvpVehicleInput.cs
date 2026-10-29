namespace Pvp.Driving {
/// <summary>Camera-independent drive request. Reverse is explicit; brake never selects reverse.</summary>
public readonly struct PvpVehicleInput {
    public readonly float Throttle, Brake, Steering;
    public readonly bool ReverseRequested, Connected;
    public PvpVehicleInput(float throttle,float brake,float steering,bool reverseRequested=false,bool connected=true) {
        Throttle=throttle;Brake=brake;Steering=steering;ReverseRequested=reverseRequested;Connected=connected;
    }
}
}
