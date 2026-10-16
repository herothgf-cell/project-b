using System;
public static class DriveMath {
    public struct Demand { public float motor, brake; }
    public static float SteerAngle(float speedKph) {
        float speed=Math.Abs(speedKph)/3.6f;
        return Math.Min(32f,(float)(Math.Atan(5f*2.6f/Math.Max(.1f,speed*speed))*180/Math.PI));
    }
    public static float TravelAcceleration(float longitudinalAcceleration,float speedMs){return speedMs<0?-longitudinalAcceleration:longitudinalAcceleration;}
    public static float CornerSpeed(float steering) {
        double angle=Math.Min(1,Math.Abs(steering))*32*Math.PI/180;
        return (float)Math.Sqrt(5*2.6/Math.Max(.001,Math.Tan(angle)));
    }
    public static Demand CornerAssist(Demand demand,float speedMs,float steering,bool physicalSteering=false) {
        // Assisted steering is normalized against the speed-dependent wheel limit.
        // Convert it back to the actual wheel angle before applying the corner cap.
        if(physicalSteering)steering*=SteerAngle(speedMs*3.6f)/32f;
        // Keep the existing lateral-grip limit, but stop accelerating away from a tight turn.
        if(speedMs<=.5f||Math.Abs(steering)<.2f)return demand;
        float target=CornerSpeed(steering);
        if(demand.motor>0)demand.motor*=Math.Max(0,Math.Min(1,(target-speedMs)/1.5f));
        if(speedMs>target)demand.brake=Math.Max(demand.brake,Math.Min(.08f,(speedMs-target)*.04f));
        return demand;
    }
    public static Demand Resolve(float throttle, float speedMs, bool handbrake) {
        if (handbrake) return new Demand { brake = 1f };
        if (throttle * speedMs < -0.5f) return new Demand { brake = Math.Abs(throttle) };
        if ((throttle > 0 && speedMs >= 25) || (throttle < 0 && speedMs <= -8)) return new Demand();
        if (Math.Abs(throttle) < .01f) return new Demand { brake = Math.Abs(speedMs) < .3f ? .25f : .015f };
        return new Demand { motor = throttle };
    }
}
