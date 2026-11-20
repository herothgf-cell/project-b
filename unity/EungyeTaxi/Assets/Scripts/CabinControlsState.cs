using System;

public enum CabinAction { Meter, Window, Cooler, Warmer }

[Serializable]
public sealed class CabinControlsState {
 public const int Warm=0,Cool=1,Off=2;
 public bool MeterOn;
 // Vacancy and Temperature are the fields used by pre-thermostat saves.
 public int Vacancy;
 public int Temperature=Off;
 public bool WindowOpen;
 public int Setpoint;
 public float IndoorDegrees;
 public const int Neutral=22,Minimum=18,Maximum=28;
 public int Target {get{return Setpoint!=0?Setpoint:Temperature==Warm?24:Temperature==Cool?20:Neutral;}}
 public float Actual {get{return IndoorDegrees==0?Target:IndoorDegrees;}}
 public int Comfort {get{return Actual>=Neutral+.5f?Warm:Actual<=Neutral-.5f?Cool:Off;}}
 public string ClimateLabel {get{return Target>Actual+.15f?"난방":Target<Actual-.15f?"냉방":"유지";}}

 public bool ToggleMeter(bool occupied){
  if(MeterOn==occupied)return false;
  MeterOn=occupied;
  return true;
 }
 public bool AdjustTemperature(int step){
  int next=Math.Max(Minimum,Math.Min(Maximum,Target+step));
  if(next==Target)return false;
  IndoorDegrees=Actual;
  Setpoint=next;
  return true;
 }
 public void AdvanceClimate(float seconds){
  // Open glass exchanges air with the neutral outside air while HVAC keeps running.
  float air=Actual;
  float equilibrium=WindowOpen?(Target+Neutral)*.5f:Target;
  float delta=equilibrium-air;
  IndoorDegrees=air+Math.Sign(delta)*Math.Min(Math.Abs(delta),seconds*.65f);
 }
 public int Payable(int fare){
  if(MeterOn||fare<=0)return fare;
  return Math.Max(1,(int)Math.Floor(fare*ContentTuning.Number("cabin.meter.missedFareRate",.5)));
 }
 public string NewCallBlock(bool automatic){
  return !automatic&&MeterOn?"미터기를 끈 뒤 새 호출을 받을 수 있습니다. J를 누르세요.":"";
 }
 public void Validate(){
  if(Vacancy<0||Vacancy>2||Temperature<Warm||Temperature>Off||
     Setpoint!=0&&(Setpoint<Minimum||Setpoint>Maximum)||
     float.IsNaN(IndoorDegrees)||float.IsInfinity(IndoorDegrees)||
     IndoorDegrees!=0&&(IndoorDegrees<Minimum||IndoorDegrees>Maximum))throw new ArgumentException("Invalid cabin controls");
 }
 public CabinControlsState Copy(){return (CabinControlsState)MemberwiseClone();}
}
