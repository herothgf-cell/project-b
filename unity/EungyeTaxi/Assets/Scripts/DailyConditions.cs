using System;

public enum DailyConditionKind { Weekday, Rain, Market, Friday, Construction }

public static class DailyConditions {
 static readonly DailyConditionKind[] kinds={DailyConditionKind.Weekday,DailyConditionKind.Rain,DailyConditionKind.Market,DailyConditionKind.Friday,DailyConditionKind.Construction};
 static readonly string[] keys={"weekday","rain","market","friday","construction"};
 static readonly string[] titles={"평일","비 오는 날","상가 행사","금요일","도로 공사"};
 static readonly string[] clues={"평범한 하루","오후부터 비","저녁 상가 행사","회식 많은 밤","{road} 일부 통제"};
 static readonly string[] descriptions={
  "기본 수요","전 지역 손님이 늘고, 탑승 때 차가 쉽게 더러워집니다.","저녁에는 상가에서 출발하는 손님이 많습니다.",
  "밤에는 상가에서 주거 지역으로 가는 손님이 많습니다.","{road} 서행 · {place} 앞 통제 구간을 지도에서 확인하세요."
 };
 public static bool Valid(int kind){return kind>=0&&kind<kinds.Length;}
 static int Index(DailyConditionKind kind){return Valid((int)kind)?(int)kind:0;}
 static string Key(DailyConditionKind kind,string setting){return "day.condition."+keys[Index(kind)]+"."+setting;}
 public static string Title(DailyConditionKind kind){return ContentTuning.Text(Key(kind,"title"),titles[Index(kind)]);}
 public static string Clue(DailyConditionKind kind){var text=ContentTuning.Text(Key(kind,"clue"),clues[Index(kind)]);return kind==DailyConditionKind.Construction?DailyRoadConstruction.Fill(text):text;}
 public static string Description(DailyConditionKind kind){var text=ContentTuning.Text(Key(kind,"description"),descriptions[Index(kind)]);return kind==DailyConditionKind.Construction?DailyRoadConstruction.Fill(text):text;}
 public static DailyConditionKind ForDay(string seed,int day){
  var rotation=(DailyConditionKind[])kinds.Clone();
  uint state=2166136261;
  foreach(char c in seed??""){state^=c;state*=16777619;}
  // A campaign has a stable shuffled rotation, so each rolling four-day window is distinct.
  for(int i=rotation.Length-1;i>0;i--){
   state=unchecked(state*1664525u+1013904223u);
   int current=(int)((state>>8)%(uint)(i+1));
   var value=rotation[current];rotation[current]=rotation[i];rotation[i]=value;
  }
  return rotation[(Math.Max(1,day)-1)%rotation.Length];
 }
 public static double DemandMultiplier(DailyConditionKind kind,string pickup,string destination,int phase){
  if(kind==DailyConditionKind.Rain)return ContentTuning.Number(Key(kind,"demand"),1.3);
  if(kind==DailyConditionKind.Market&&phase==2&&pickup=="commercial")return ContentTuning.Number(Key(kind,"demand"),2);
  if(kind==DailyConditionKind.Friday&&phase>=2&&pickup=="commercial"&&destination=="residential")return ContentTuning.Number(Key(kind,"demand"),2);
  return 1;
 }
 public static double TrafficDestinationMultiplier(DailyConditionKind kind,string destination,int phase){
  return kind==DailyConditionKind.Market&&phase==2&&destination=="commercial"?ContentTuning.Number(Key(kind,"traffic"),1.5):1;
 }
 public static float CallRetrySeconds(DailyConditionKind kind){
  return (float)(3/DemandMultiplier(kind,"neutral","neutral",0));
 }
 public static bool IsRaining(DailyConditionKind kind,double elapsed,double duration){
  return kind==DailyConditionKind.Rain&&duration>0&&elapsed/duration>=(ContentTuning.Number(Key(kind,"startsAt"),12)-8)/12;
 }
 public static double PoolMultiplier(DailyConditionKind kind,PassengerType pool){
  switch(kind){
   case DailyConditionKind.Rain:
    return pool==PassengerType.Luggage?ContentTuning.Number(Key(kind,"umbrella"),2):1;
   case DailyConditionKind.Market:
    if(pool==PassengerType.Luggage||pool==PassengerType.MuddyLuggage)return ContentTuning.Number(Key(kind,"luggage"),2);
    return pool==PassengerType.Food?ContentTuning.Number(Key(kind,"food"),2):1;
   case DailyConditionKind.Friday:
    if(pool==PassengerType.Drunk)return ContentTuning.Number(Key(kind,"drunk"),2);
    return pool==PassengerType.Sleeping?ContentTuning.Number(Key(kind,"sleep"),2):1;
   case DailyConditionKind.Construction:
    return pool==PassengerType.Hurried?ContentTuning.Number(Key(kind,"hurried"),2):1;
   default:return 1;
  }
 }
 public static double PresetMultiplier(DailyConditionKind kind,PassengerPresetDefinition preset){
  return kind==DailyConditionKind.Rain&&preset.Has("item.wetUmbrella")?ContentTuning.Number(Key(kind,"umbrella"),2):1;
 }
}
