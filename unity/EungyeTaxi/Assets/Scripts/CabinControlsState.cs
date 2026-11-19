using System;

[Serializable]
public sealed class CabinControlsState {
 public const int Warm=0,Cool=1,Off=2;
 public bool MeterOn;
 public int Vacancy;
 public int Temperature=Off;
 public bool OffDuty {get{return Vacancy==2;}}

 public bool ToggleMeter(bool occupied){
  if(MeterOn==occupied)return false;
  MeterOn=occupied;
  return true;
 }
 public void CycleVacancy(){Vacancy=(Vacancy+1)%3;}
 public void CycleTemperature(){Temperature=Temperature==Warm?Off:Temperature==Off?Cool:Warm;}
 public int Payable(int fare){
  if(MeterOn||fare<=0)return fare;
  return Math.Max(1,(int)Math.Floor(fare*ContentTuning.Number("cabin.meter.missedFareRate",.5)));
 }
 public string NewCallBlock(bool automatic){
  if(OffDuty)return "휴무 중입니다. K로 빈차등을 바꾸세요.";
  return !automatic&&MeterOn?"미터기를 끈 뒤 새 호출을 받을 수 있습니다. J를 누르세요.":"";
 }
 public void Validate(){
  if(Vacancy<0||Vacancy>2||Temperature<Warm||Temperature>Off)throw new ArgumentException("Invalid cabin controls");
 }
 public CabinControlsState Copy(){return (CabinControlsState)MemberwiseClone();}
}
