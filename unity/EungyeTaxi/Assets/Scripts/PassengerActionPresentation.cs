using System;
public enum PassengerActionKind { None, Wake, Load, Protect, Unload, Food, Secure, Errand }
// UI-only state. Never advances work, changes money, or acknowledges a model event.
public sealed class PassengerActionPresentation {
 public PassengerActionKind kind;public string identity="",title="",message="",condition="",footer="";
 public float remaining,total;public bool safe,ready;
 public bool Active {get{return kind!=PassengerActionKind.None;}}
 public bool HasAction {get{return kind==PassengerActionKind.Wake;}}
 public int OptionCount {get{return HasAction?2:1;}}
 public string Countdown {get{return kind==PassengerActionKind.Wake?(ready?"다시 깨울 수 있습니다":"다시 깨우기까지 "+Math.Ceiling(remaining)+"초"):"남은 작업 "+Math.Ceiling(remaining)+"초";}}
 public float Progress {get{return total<=0?0:Math.Max(0,Math.Min(1,1-remaining/total));}}
 public bool Enabled(int index){return index>=0&&index<OptionCount&&(!HasAction||index!=0||safe&&ready);}
 public string Summary {get{return title+" · "+(!safe?"안전 정차 필요":HasAction&&ready?"깨우기 가능":Math.Ceiling(remaining)+"초");}}
}
public sealed class PassengerActionVisibility {
 string identity="";bool offered;public bool Expanded {get;private set;}
 public bool Observe(string key,bool safe,bool available){if(identity!=(key??"")){identity=key??"";offered=false;Expanded=false;}if(identity.Length==0)return false;if(!offered&&safe&&available){offered=true;Expanded=true;return true;}return false;}
 public void Open(){if(identity.Length==0)return;offered=true;Expanded=true;}
 public void Fold(){offered=true;Expanded=false;}
}
