using System;
using Pvp.Driving;
using UnityEngine;
namespace Pvp.Tests.EditMode {
public static class PvpVehicleTests {
    static void Check(bool ok,string message) { if(!ok) throw new InvalidOperationException(message); }
    static void Reject(Action action) { bool rejected=false; try { action(); } catch(ArgumentException) { rejected=true; } Check(rejected,"Invalid argument accepted"); }
    static void Tick(PvpVehicleMotor m,PvpVehicleInput input,int count,float dt=0.02f) { for(int i=0;i<count;i++) m.Simulate(in input,dt); }
    public static void Run() {
        var gas=new PvpVehicleInput(1,0,0);var m=new PvpVehicleMotor();Tick(m,gas,100);Check(m.Capture().Speed>10,"Acceleration absent");
        var turn=new PvpVehicleInput(1,0,0.7f);Tick(m,turn,20);var snapshot=m.Capture();Tick(m,turn,80);var expected=m.Capture();m.Restore(in snapshot);Tick(m,turn,80);var replay=m.Capture();
        Check(expected.Position.Equals(replay.Position)&&expected.YawDegrees==replay.YawDegrees&&expected.Steering==replay.Steering&&expected.Speed==replay.Speed,"Snapshot replay diverged");
        var normal=new PvpVehicleMotor();var clamped=new PvpVehicleMotor();Tick(normal,new PvpVehicleInput(1,0,1),50);Tick(clamped,new PvpVehicleInput(5,-2,4),50);Check(normal.Capture().Position.Equals(clamped.Capture().Position),"Input clamp failed");
        m=new PvpVehicleMotor();Tick(m,new PvpVehicleInput(1,1,0),100);Check(m.Capture().Speed==0,"Brake priority failed");Tick(m,new PvpVehicleInput(0,1,0),100);Check(m.Capture().Speed==0,"Brake caused auto reverse");Tick(m,new PvpVehicleInput(1,0,0,true),50);Check(m.Capture().Speed<0,"Explicit reverse absent");
        m=new PvpVehicleMotor();Tick(m,gas,100);float forward=m.Capture().Speed;m.Simulate(new PvpVehicleInput(1,0,0,true),0.02f);Check(m.Capture().Speed>0&&m.Capture().Speed<forward,"High speed reverse flipped velocity");
        float before=m.Capture().Speed;Tick(m,new PvpVehicleInput(1,0,1,false,false),20);Check(m.Capture().Speed<before,"Disconnected input retained drive");var unchanged=m.Capture();
        foreach(float dt in new[]{0f,-1f,0.101f,float.NaN,float.PositiveInfinity}) Reject(()=>m.Simulate(gas,dt));
        Reject(()=>m.Simulate(new PvpVehicleInput(float.NaN,0,0),0.02f));Reject(()=>m.Simulate(new PvpVehicleInput(0,float.PositiveInfinity,0),0.02f));Reject(()=>m.Simulate(new PvpVehicleInput(0,0,float.NaN),0.02f));Check(m.Capture().Position.Equals(unchanged.Position)&&m.Capture().Speed==unchanged.Speed,"Rejected input changed state");
        var invalid=unchanged;invalid.Position.x=float.NaN;Reject(()=>m.Restore(in invalid));Reject(()=>m.SetProfile((PvpVehicleProfile)99));m.SetProfile(PvpVehicleProfile.B);Check(m.Capture().Speed==unchanged.Speed,"Profile switch instantly changed speed");
        var coast=new PvpVehicleMotor();Tick(coast,gas,30);float coastingSpeed=coast.Capture().Speed;Tick(coast,new PvpVehicleInput(0,0,0),20);Check(coast.Capture().Speed<coastingSpeed&&coast.Capture().Speed>=0,"Input release must coast toward rest");var overspeed=new PvpVehicleMotor();var over=overspeed.Capture();over.Speed=28;overspeed.Restore(in over);overspeed.SetProfile(PvpVehicleProfile.B);overspeed.Simulate(gas,0.02f);Check(overspeed.Capture().Speed>26&&overspeed.Capture().Speed<28,"Profile switch must gradually reduce excess speed");var slow=new PvpVehicleMotor();var fast=new PvpVehicleMotor();var s=slow.Capture();s.Speed=5;slow.Restore(in s);s=fast.Capture();s.Speed=25;fast.Restore(in s);Tick(slow,new PvpVehicleInput(0,0,1),25);Tick(fast,new PvpVehicleInput(0,0,1),25);Check(Math.Abs(slow.Capture().YawDegrees)>Math.Abs(fast.Capture().YawDegrees),"High speed steering unbounded");
        var still=new PvpVehicleMotor();Tick(still,new PvpVehicleInput(0,0,1),50);Check(still.Capture().YawDegrees==0,"Rotates in place");
        var fine=new PvpVehicleMotor();var coarse=new PvpVehicleMotor();Tick(fine,new PvpVehicleInput(0,0,1),100,0.01f);Tick(coarse,new PvpVehicleInput(0,0,1),10,0.1f);Check(Math.Abs(fine.Capture().Steering-coarse.Capture().Steering)<0.00001f,"Steering smoothing frame dependent");
        foreach(PvpVehicleProfile profile in new[]{PvpVehicleProfile.A,PvpVehicleProfile.B}) {var c=new PvpVehicleMotor(profile);Tick(c,gas,150);var atThree=c.Capture();var brakingStart=atThree;brakingStart.Position=Vector3.zero;brakingStart.YawDegrees=0;brakingStart.Speed=20;c.Restore(in brakingStart);float travel=0;int ticks=0;while(c.Capture().Speed>0&&ticks<1000) {var prior=c.Capture().Position;c.Simulate(new PvpVehicleInput(1,1,0),0.02f);travel+=(c.Capture().Position-prior).magnitude;ticks++;}Check(ticks<1000,"Braking never stopped");Console.WriteLine("PVP_VEHICLE_METRIC candidate="+profile+" speedAt3s="+atThree.Speed+" brakingEntrySpeed=20 stopDistance="+travel+" stopSeconds="+(ticks*0.02f));var low=new PvpVehicleMotor(profile);var high=new PvpVehicleMotor(profile);var turnStart=low.Capture();turnStart.Speed=5;low.Restore(in turnStart);turnStart.Speed=25;high.Restore(in turnStart);Tick(low,new PvpVehicleInput(0,0,1),25);Tick(high,new PvpVehicleInput(0,0,1),25);Console.WriteLine("PVP_VEHICLE_TURN candidate="+profile+" low5mpsYawAt0.5s="+low.Capture().YawDegrees+" high25mpsYawAt0.5s="+high.Capture().YawDegrees);}
        Console.WriteLine("PVP_VEHICLE_TESTS_OK");
    }
}
}
