using System;
using System.Text.RegularExpressions;

// Existing offer identity gives the same name, face and age band after boarding, restore and LAN replication.
// 2026-11-24 play-test round 5: the face, the name and the story did not agree (an old woman's face was called
// a student). The face now comes first, and the name and age band follow from it; story passengers draw only
// from faces that fit their story.
public struct PassengerPersona {
 public int portrait;public string name,age;public bool male;
 public string Label {get{return age+" "+(male?"남성":"여성");}}
}
public static class PassengerPresentation {
 // passenger-portraits.png: 0 young man with glasses, 1 young woman, 2 grey-haired man in his fifties, 3 woman in her seventies.
 public static readonly string[] Ages={"20대","20대","50대","70대"};
 public static readonly bool[] Male={true,false,true,false};
 static readonly string[][] names={
  new[]{"김민준","최도윤","윤지호","임서준","오시우","신도현","안준서","홍도겸","백현우","노태윤","박지훈","정우진"},
  new[]{"이서연","정하린","강유진","조수빈","장예린","한지민","서하윤","권수아","송채원","전민서","유다은","배소윤"},
  new[]{"김영호","박성철","이상훈","최정수","정만수","강동석","조병철","윤재근","장기석","임종훈"},
  new[]{"김순자","이영자","박정순","최말순","정옥자","강영숙","조복례","윤춘자","장금순","한경희"}};
 public const string StoryTag="|story:";
 static readonly Regex speaker=new Regex(@"(?m)^(?:손님|승객)[ \t]*(?:(?:[:：·])[ \t]*|(?=[""“]))");
 // Faces that fit each story (story.<id>.title in the content catalog) and each passenger preset.
 static int[] Fits(string key){
  switch(key??""){
   case "last-train":case "first-interview":case "rough-day":return new[]{0,1};
   case "feverish-child":return new[]{1,2};
   case "moving-day":return new[]{0,1,2};
   case "night-shift":case "night-shift-rest":return new[]{0,2};
   case "wedding-guest":return new[]{1,0};
   case "plant-gift":case "plant-visit":case "sleepy_gardener":return new[]{3,2};
   case "after-dinner":case "after-gathering":return new[]{2,0};
   case "trip-departure":return new[]{0,1,3};
   case "interview-appointment":return new[]{0,1};
   case "neighborhood-story":return new[]{3,2};
   default:return new[]{0,1,2,3};
  }
 }
 public static bool Fits(int face,string story){return Array.IndexOf(Fits(story),face)>=0;}
 public static string Identity(string baseIdentity,string story){return string.IsNullOrEmpty(story)?baseIdentity:baseIdentity+StoryTag+story;}
 static uint Hash(string s){uint hash=2166136261;unchecked{foreach(char c in s??""){hash^=c;hash*=16777619;}}return hash;}
 public static PassengerPersona Persona(string identity){
  if(string.IsNullOrEmpty(identity))return new PassengerPersona{portrait=0,name="승객",age=Ages[0],male=true};
  string story="";int tag=identity.IndexOf(StoryTag,StringComparison.Ordinal);string id=identity;
  if(tag>=0){story=identity.Substring(tag+StoryTag.Length);id=identity.Substring(0,tag);}
  // The face seen before boarding (no story yet) is kept when it fits the story chosen at boarding.
  var fits=Fits(story);uint h=Hash(id);int face=(int)(h%4u);if(Array.IndexOf(fits,face)<0)face=fits[(int)((h/4)%(uint)fits.Length)];var list=names[face];
  return new PassengerPersona{portrait=face,name=list[(int)((h/7)%(uint)list.Length)],age=Ages[face],male=Male[face]};
 }
 // A preset card in the logbook has no ride; its face still fits the preset.
 public static PassengerPersona PresetPersona(string presetId){return Persona(Identity("preset:"+presetId,presetId));}
 public static string Name(string identity){return string.IsNullOrEmpty(identity)?"승객":Persona(identity).name;}
 public static string Speech(string line){var rows=speaker.Replace(line??"","").Split('\n');for(int i=0;i<rows.Length;i++){string value=rows[i].Trim();if(value.Length>=2){char first=value[0],last=value[value.Length-1];if(first=='“'&&last=='”'||first=='"'&&last=='"'||first=='「'&&last=='」'||first=='『'&&last=='』')value=value.Substring(1,value.Length-2);}rows[i]=value;}return string.Join("\n",rows);}
}
