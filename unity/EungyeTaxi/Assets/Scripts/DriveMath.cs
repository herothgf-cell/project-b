using System;
public static class DriveMath {
    public const float ParkingSteerAngle=42f;
    // Manual parking turns are tighter; H retains its calibrated bicycle geometry.
    // Fade out the extra steering by 30kph to preserve the high-speed grip limit.
    public static float ManualSteerAngle(float speedKph) {
        float blend=Math.Max(0,Math.Min(1,(30-Math.Abs(speedKph))/16));
        return Math.Min(48,SteerAngle(speedKph)*(1+.14f*blend));
    }
    public struct Demand { public float motor, brake; }
    public static float SteerAngle(float speedKph) {
        float speed=Math.Abs(speedKph)/3.6f;
        return Math.Min(ParkingSteerAngle,(float)(Math.Atan(5f*2.6f/Math.Max(.1f,speed*speed))*180/Math.PI));
    }
    public static float WheelSteerAngle(float centerAngle,bool leftWheel) {
        if(Math.Abs(centerAngle)<.001f)return 0;
        // H computes the bicycle center angle. Ackermann preserves that path curvature
        // while giving the inside and outside wheels their own 1.7m-track radii.
        double radius=2.6/Math.Tan(Math.Abs(centerAngle)*Math.PI/180);
        bool inside=leftWheel?centerAngle<0:centerAngle>0;
        return Math.Sign(centerAngle)*(float)(Math.Atan(2.6/Math.Max(.1,radius+(inside?-.85:.85)))*180/Math.PI);
    }
    public static float TravelAcceleration(float longitudinalAcceleration,float speedMs){return speedMs<0?-longitudinalAcceleration:longitudinalAcceleration;}
    public static bool HarshPassengerMotion(float acceleration,float speedMs,bool necessaryBraking) {
        // Actual tyre braking filters to about -6.2m/s2; ordinary S at 30kph stays
        // above -5.8. Keep the excessive case reachable without charging for a normal stop.
        return Math.Abs(speedMs)>2&&(acceleration>4.5f||acceleration< -6f&&!necessaryBraking);
    }
    public static float CornerSpeed(float steering) {
        double angle=Math.Min(1,Math.Abs(steering))*ParkingSteerAngle*Math.PI/180;
        return (float)Math.Sqrt(5*2.6/Math.Max(.001,Math.Tan(angle)));
    }
    public static Demand CornerAssist(Demand demand,float speedMs,float steering,bool physicalSteering=false) {
        // Both control modes normalize input against their speed-dependent wheel limit.
        // Use the same physical angle as TaxiController, not a 42-degree parking turn.
        steering*=(physicalSteering?SteerAngle(speedMs*3.6f):ManualSteerAngle(speedMs*3.6f))/ParkingSteerAngle;
        // Keep the existing lateral-grip limit, but stop accelerating away from a tight turn.
        if(speedMs<=.5f||Math.Abs(steering)<.2f)return demand;
        float target=CornerSpeed(steering);
        if(demand.motor>0)demand.motor*=Math.Max(0,Math.Min(1,target-speedMs));
        if(speedMs>target)demand.brake=Math.Max(demand.brake,Math.Min(.08f,(speedMs-target)*.04f));
        return demand;
    }
    public static Demand Resolve(float throttle, float speedMs, bool handbrake) {
        if (handbrake) return new Demand { brake = 1f };
        // Digital S is an ordinary service stop; Space remains the full emergency brake.
        if (throttle * speedMs < -0.5f) return new Demand { brake = Math.Min(1,Math.Abs(throttle))*.18f };
        if ((throttle > 0 && speedMs >= 25) || (throttle < 0 && speedMs <= -8)) return new Demand();
        if (Math.Abs(throttle) < .01f) return new Demand { brake = Math.Abs(speedMs) < .3f ? .25f : .015f };
        // 550Nm per rear wheel gives about 2.3m/s2 before drag, leaving time to read city corners.
        return new Demand { motor = Math.Max(-1,Math.Min(1,throttle))*.5f };
    }
}
