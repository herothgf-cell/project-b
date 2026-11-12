using System;
public static class DriveMath {
    public const float ParkingSteerAngle=42f;
    // Manual parking turns are tighter; H retains its calibrated bicycle geometry.
    // Fade out the gentle-curve allowance by60kph; H keeps its calibrated geometry.
    public static float ManualSteerAngle(float speedKph) {
        float speed=Math.Abs(speedKph);
        float blend=Math.Max(0,Math.Min(1,(30-speed)/16));
        float parking=Math.Min(52,SteerAngle(speedKph)*(1+.55f*blend));
        // A gradual city-speed curve; H keeps the original bicycle limit.
        float city=speed<=20?52-speed*.6f:speed<=35?40-(speed-20)*(23f/15):17+(SteerAngle(50)-17)*Math.Min(1,(speed-35)/15);
        float original=speed>=50?parking:Math.Max(parking,city);
        // Smooth 20% allowance at 47.5km/h, zero below35 and above60.
        float t=Math.Max(0,Math.Min(1,(speed-35)/25));
        float wave=(float)Math.Sin(Math.PI*t);
        return original*(1+.2f*wave*wave);
    }
    public struct Demand { public float motor, brake; }
    public static bool MayAssistManualYaw(int grounded,float upright,bool assisted,bool human,bool frozenSteering){return grounded>=2&&upright>.8f&&!assisted&&human&&!frozenSteering;}
    public static float ManualYawAcceleration(float speedMs,float centerAngle,float yawRate) {
        float speed=Math.Abs(speedMs),kph=speed*3.6f;
        if(speed<.5f||kph>=60||Math.Abs(centerAngle)<.1f)return 0;
        // At city cruising speeds, assist gentle bends without forcing a tight
        // intersection turn: cap the target lateral acceleration at5m/s2.
        float limit=1.4f+(Math.Min(1.4f,5f/speed)-1.4f)*Math.Max(0,Math.Min(1,(kph-30)/10));
        float target=Math.Max(-limit,Math.Min(limit,(float)(speedMs/2.6f*Math.Tan(centerAngle*Math.PI/180))));
        float sign=Math.Sign(target),deficit=Math.Abs(target)-yawRate*sign;
        if(deficit<=0)return 0;
        float gain=kph<=30?4.5f*Math.Max(0,Math.Min(1,(40-kph)/20)):
            kph<=50?2.25f-(kph-30)*(1.45f/20):.8f*(60-kph)/10;
        return sign*Math.Min(5.2f,deficit*gain);
    }
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
        if (throttle * speedMs < -0.5f) return new Demand { brake = Math.Min(1,Math.Abs(throttle))*.30f };
        if ((throttle > 0 && speedMs >= 25) || (throttle < 0 && speedMs <= -8)) return new Demand();
        if (Math.Abs(throttle) < .01f) return new Demand { brake = Math.Abs(speedMs) < .3f ? .25f : .015f };
        // 550Nm per rear wheel gives about 2.3m/s2 before drag, leaving time to read city corners.
        return new Demand { motor = Math.Max(-1,Math.Min(1,throttle))*.5f };
    }
}
