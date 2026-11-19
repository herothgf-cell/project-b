using System;
using System.Collections.Generic;

[Serializable]
public sealed class LogbookEnding {
 public string storyId,line;
 public int outcome;
}

[Serializable]
public sealed class CampaignLogbook {
 public const int CurrentVersion=1,NeighborhoodSize=12;
 public int version=CurrentVersion;
 public List<string> passengerPresets=new List<string>();
 public List<LogbookEnding> endings=new List<LogbookEnding>();
 public int[] neighborhood=new int[NeighborhoodSize];
 public int assignmentDay,achievementBits,completedRides,cleanStreak;
 public List<string> assignedStories=new List<string>();
 public static readonly string[] AchievementNames={
  "첫 영업 운송","비 오는 날의 첫 손님","무위반 하루","긴 운송","첫 번째 이야기","좋은 결말",
  "동네가 익숙해졌다","꾸준한 영업","세 가지 결말","안전한 길","금요일 밤의 손님","좋은 이야기들"
 };
 public static string AchievementHint(int id){
  switch(id){
   case 0:return "운송 1건 완료";
   case 1:return "비 카드에서 운송 완료";
   case 2:return "무위반 하루 마감";
   case 3:return (ContentTuning.Int("story.logbook.longRideMeters",3000)/1000f).ToString("0.###")+"km 이상 운송 완료";
   case 4:return "손님 이야기 결말 1개 기록";
   case 5:return "좋은 결말 1개 기록";
   case 6:return "동네 한 곳의 운송 기록 3회";
   case 7:return "누적 운송 "+ContentTuning.Int("story.logbook.volumeRides",10)+"건 완료";
   case 8:return "한 이야기의 결말 3종 기록";
   case 9:return "무위반 "+ContentTuning.Int("story.logbook.cleanDays",3)+"일 연속 마감";
   case 10:return "금요일 카드에서 운송 완료";
   case 11:return "서로 다른 좋은 결말 "+ContentTuning.Int("story.logbook.goodEndings",5)+"개 기록";
   default:return "";
  }
 }
 public int EndingCount {get{return endings.Count;}}
 public bool HasEnding(string storyId,int ending){return endings.Exists(e=>e.storyId==storyId&&e.outcome==ending);}
 public string EndingLine(string storyId,int ending){var item=endings.Find(e=>e.storyId==storyId&&e.outcome==ending);return item==null?"":item.line;}
 public int PresetCount {get{return passengerPresets.Count;}}
 public bool HasPreset(string id){return passengerPresets.Contains(id);}
 public int NeighborhoodCount(string stopId){int i=StopIndex(stopId);return i<0?0:neighborhood[i];}
 public int NeighborhoodStars(string stopId){return Stars(NeighborhoodCount(stopId));}
 public int StarTotal {get{int total=0;foreach(int count in neighborhood)total+=Stars(count);return total;}}
 public int StarredNeighborhoods {get{int total=0;foreach(int count in neighborhood)if(Stars(count)>0)total++;return total;}}
 public bool HasAchievement(int id){return id>=0&&id<AchievementNames.Length&&(achievementBits&(1<<id))!=0;}
 public int AchievementCount {get{int total=0;for(int i=0;i<AchievementNames.Length;i++)if(HasAchievement(i))total++;return total;}}
 public int StoryAssignments(int day){return assignmentDay==day?assignedStories.Count:0;}
 public bool HasStoryAssignment(int day,string id){return assignmentDay==day&&assignedStories.Contains(id);}
 public void RecordStoryAssignment(int day,string id){
  if(day<1||!ValidId(id))return;
  if(day!=assignmentDay){assignmentDay=day;assignedStories.Clear();}
  if(!assignedStories.Contains(id))assignedStories.Add(id);
 }
 public void RecordPreset(string id){if(ValidId(id)&&!HasPreset(id))passengerPresets.Add(id);}

 public void ObserveRide(PassengerResult ride,bool rainy,bool friday){
  RecordPreset(ride.passengerPresetId);
  if(ValidId(ride.storyId)&&ride.storyEnding>=0&&ride.storyEnding<3&&!HasEnding(ride.storyId,ride.storyEnding)){
   endings.Add(new LogbookEnding{storyId=ride.storyId,outcome=ride.storyEnding,line=ride.storyLine??""});
   Unlock(4);
   if(ride.storyEnding==0)Unlock(5);
   if(HasEnding(ride.storyId,0)&&HasEnding(ride.storyId,1)&&HasEnding(ride.storyId,2))Unlock(8);
   if(endings.FindAll(e=>e.outcome==0).Count>=ContentTuning.Int("story.logbook.goodEndings",5))Unlock(11);
  }
  if(ride.outcome!="운송 완료")return;
  completedRides++;
  AddNeighborhood(ride.pickupStopId);AddNeighborhood(ride.dropStopId);
  Unlock(0);
  if(rainy)Unlock(1);
  if(ride.rideDistance>=ContentTuning.Int("story.logbook.longRideMeters",3000))Unlock(3);
  if(StarredNeighborhoods>0)Unlock(6);
  if(completedRides>=ContentTuning.Int("story.logbook.volumeRides",10))Unlock(7);
  if(friday)Unlock(10);
 }
 public void ObserveSettlement(bool legal){
  cleanStreak=legal?cleanStreak+1:0;
  if(legal)Unlock(2);
  if(cleanStreak>=ContentTuning.Int("story.logbook.cleanDays",3))Unlock(9);
 }
 void Unlock(int id){achievementBits|=1<<id;}
 public static int NeighborhoodThreshold(int stars){return stars==1?3:stars==2?10:stars==3?25:0;}
 static int Stars(int count){return count>=NeighborhoodThreshold(3)?3:count>=NeighborhoodThreshold(2)?2:count>=NeighborhoodThreshold(1)?1:0;}
 static int StopIndex(string id){
  if(id==null||id.Length!=10||!id.StartsWith("EG-GAME-",StringComparison.Ordinal)||id[8]<'0'||id[8]>'9'||id[9]<'0'||id[9]>'9')return -1;
  int index=(id[8]-'0')*10+id[9]-'0'-1;
  return index>=0&&index<NeighborhoodSize?index:-1;
 }
 void AddNeighborhood(string id){int index=StopIndex(id);if(index>=0)neighborhood[index]++;}
 static bool ValidId(string id){
  if(string.IsNullOrEmpty(id)||id.Length>48)return false;
  foreach(char c in id)if(!(c>='a'&&c<='z'||c>='A'&&c<='Z'||c>='0'&&c<='9'||c=='-'||c=='_'))return false;
  return true;
 }
 public void Validate(int day){
  if(version!=CurrentVersion||assignmentDay<0||assignmentDay>day||achievementBits<0||achievementBits>=1<<AchievementNames.Length||
    completedRides<0||cleanStreak<0||cleanStreak>day||neighborhood==null||neighborhood.Length!=NeighborhoodSize||
    assignedStories==null||assignedStories.Count>3||assignmentDay==0&&assignedStories.Count>0||passengerPresets==null||endings==null||endings.Count>30)
   throw new ArgumentException("Invalid driver logbook");
  foreach(int count in neighborhood)if(count<0||count>2L*completedRides)throw new ArgumentException("Invalid neighborhood ride count");
  var ids=new HashSet<string>();
  foreach(string id in assignedStories)if(!ValidId(id)||!ids.Add(id))throw new ArgumentException("Invalid story assignment");
  ids.Clear();
  foreach(string id in passengerPresets)if(!ValidId(id)||!ids.Add(id))throw new ArgumentException("Invalid preset collection");
  ids.Clear();
  foreach(var ending in endings)if(ending==null||!ValidId(ending.storyId)||ending.outcome<0||ending.outcome>2||
    ending.line==null||ending.line.Length>4000||!ids.Add(ending.storyId+":"+ending.outcome))throw new ArgumentException("Invalid story ending");
 }
}
