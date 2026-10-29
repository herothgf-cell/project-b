using System;
using UnityEngine;
namespace Pvp.Driving {
/// <summary>Offline planar candidate, advanced by the caller's fixed tick. No wheel/contact simulation.
/// Same state and tick sequence replay locally; cross-platform network determinism is not promised.</summary>
public sealed class PvpVehicleMotor {
    public const float MaxTickSeconds=0.1f;
    const float ReverseThreshold=0.35f;
    PvpVehicleState state;
    public PvpVehicleMotor(PvpVehicleProfile profile=PvpVehicleProfile.A) { SetProfile(profile); }
    public PvpVehicleState Capture() { return state; }
    public void Restore(in PvpVehicleState snapshot) {
        ValidateProfile(snapshot.Profile);
        if(!Finite(snapshot.Position.x)||!Finite(snapshot.Position.y)||!Finite(snapshot.Position.z)||
           Math.Abs(snapshot.Position.x)>10000000||Math.Abs(snapshot.Position.y)>10000000||Math.Abs(snapshot.Position.z)>10000000||
           !Finite(snapshot.YawDegrees)||!Finite(snapshot.Speed)||Math.Abs(snapshot.Speed)>100||!Finite(snapshot.Steering)||Math.Abs(snapshot.Steering)>1)
            throw new ArgumentException("Snapshot must contain finite bounded vehicle state",nameof(snapshot));
        state=snapshot;
    }
    /// <summary>Switches future tuning without teleporting, rotating, or instantly changing speed/steering.</summary>
    public void SetProfile(PvpVehicleProfile profile) { ValidateProfile(profile);state.Profile=profile; }
    public void Simulate(in PvpVehicleInput input,float dt) {
        if(!Finite(dt)||dt<=0||dt>MaxTickSeconds) throw new ArgumentOutOfRangeException(nameof(dt),"Use a fixed tick in (0, 0.1] seconds");
        if(!Finite(input.Throttle)||!Finite(input.Brake)||!Finite(input.Steering)) throw new ArgumentException("Drive input must be finite",nameof(input));
        var tuning=state.Profile==PvpVehicleProfile.A?Candidate.A:Candidate.B;
        float throttle=input.Connected?Clamp(input.Throttle,0,1):0;
        float brake=input.Connected?Clamp(input.Brake,0,1):0;
        float steer=input.Connected?Clamp(input.Steering,-1,1):0;
        var next=state;
        next.Steering=steer+(state.Steering-steer)*(float)Math.Exp(-tuning.SteeringResponse*dt);
        float oldSpeed=state.Speed;
        bool reverse=input.Connected&&input.ReverseRequested;
        bool opposite=reverse?oldSpeed>ReverseThreshold:oldSpeed < -ReverseThreshold;
        if(brake>0) next.Speed=Toward(oldSpeed,0,tuning.Braking*brake*dt);
        else if(throttle>0&&opposite) next.Speed=Toward(oldSpeed,0,tuning.Braking*throttle*dt);
        else if(throttle>0) {
            float target=reverse?-tuning.ReverseSpeed:tuning.ForwardSpeed;
            // Preserve velocity when changing profile; excess speed bleeds off over time.
            float rate=Math.Abs(oldSpeed)>Math.Abs(target)?tuning.Coast:tuning.Acceleration*throttle;
            next.Speed=Toward(oldSpeed,target,rate*dt);
        } else next.Speed=Toward(oldSpeed,0,(input.Connected?tuning.Coast:tuning.InputLossDeceleration)*dt);
        float speed=(oldSpeed+next.Speed)*0.5f;
        float absolute=Math.Abs(speed);
        // Rises from zero at rest, then limits yaw rate at speed. No stationary spin.
        float turnRate=tuning.LowSpeedYaw*Math.Min(absolute/4f,1f)/(1f+(absolute/10f)*(absolute/10f));
        float yawDelta=next.Steering*turnRate*Math.Sign(speed)*dt;
        double heading=(state.YawDegrees+yawDelta*0.5f)*Math.PI/180.0;
        next.Position+=new Vector3((float)Math.Sin(heading),0,(float)Math.Cos(heading))*speed*dt;
        next.YawDegrees=NormalizeYaw(state.YawDegrees+yawDelta);
        state=next;
    }
    static bool Finite(float v) { return !float.IsNaN(v)&&!float.IsInfinity(v); }
    static float Clamp(float v,float min,float max) { return Math.Max(min,Math.Min(max,v)); }
    static float Toward(float v,float target,float step) { return v<target?Math.Min(v+step,target):Math.Max(v-step,target); }
    static float NormalizeYaw(float yaw) { yaw%=360;return yaw<0?yaw+360:yaw; }
    static void ValidateProfile(PvpVehicleProfile profile) { if(profile!=PvpVehicleProfile.A&&profile!=PvpVehicleProfile.B) throw new ArgumentOutOfRangeException(nameof(profile)); }
    readonly struct Candidate {
        public readonly float Acceleration,Braking,ForwardSpeed,ReverseSpeed,Coast,InputLossDeceleration,LowSpeedYaw,SteeringResponse;
        Candidate(float accel,float braking,float top,float reverse,float coast,float loss,float yaw,float response) {Acceleration=accel;Braking=braking;ForwardSpeed=top;ReverseSpeed=reverse;Coast=coast;InputLossDeceleration=loss;LowSpeedYaw=yaw;SteeringResponse=response;}
        // Experiment values only. A favors launch/low speed; B favors calmer steering/stronger brakes.
        public static readonly Candidate A=new Candidate(8.5f,13f,28f,6f,1.8f,5f,115f,9f);
        public static readonly Candidate B=new Candidate(7.2f,15f,26f,5f,2.2f,5.5f,95f,7f);
    }
}
}
