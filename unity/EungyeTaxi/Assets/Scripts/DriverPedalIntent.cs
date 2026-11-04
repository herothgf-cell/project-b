using System;
// A service stop ends in a held stop. Release the pedal before changing gear.
public sealed class DriverPedalIntent {
 int brakingDirection;
 public int BrakingDirection {get{return brakingDirection;}}
 public void Clear(){brakingDirection=0;}
 public void Restore(int direction){if(direction< -1||direction>1)throw new ArgumentException("Invalid pedal direction");brakingDirection=direction;}
 public DriveMath.Demand Resolve(float raw,float speed,bool emergency){
  if(Math.Abs(raw)<.01f){brakingDirection=0;return DriveMath.Resolve(raw,speed,emergency);}
  int direction=Math.Sign(raw);
  if(raw*speed<-.5f)brakingDirection=direction;
  if(brakingDirection==direction&&raw*speed>=-.5f)return new DriveMath.Demand{brake=emergency?1:.30f};
  if(brakingDirection!=0&&brakingDirection!=direction)brakingDirection=0;
  return DriveMath.Resolve(raw,speed,emergency);
 }
}
