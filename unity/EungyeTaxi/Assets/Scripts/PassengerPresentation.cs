using System;
using System.Text.RegularExpressions;

// Existing offer identity gives the same arbitrary name after boarding, restore and LAN replication.
public static class PassengerPresentation {
 static readonly string[] names={"김민준","이서연","박지우","최도윤","정하린","강유진","조수빈","윤지호","장예린","임서준","한지민","오시우","서하윤","신도현","권수아","황지안","안준서","송채원","전민서","홍도겸","유다은","고서진","문주원","양은우","배소윤","백현우","남지윤","심수현","노태윤","하예원"};
 static readonly Regex speaker=new Regex(@"(?m)^(?:손님|승객)[ \t]*(?:(?:[:：·])[ \t]*|(?=[""“]))");
 public static string Name(string identity){if(string.IsNullOrEmpty(identity))return "승객";uint hash=2166136261;unchecked{foreach(char c in identity){hash^=c;hash*=16777619;}}return names[(int)(hash%(uint)names.Length)];}
 public static string Speech(string line){var rows=speaker.Replace(line??"","").Split('\n');for(int i=0;i<rows.Length;i++){string value=rows[i].Trim();if(value.Length>=2){char first=value[0],last=value[value.Length-1];if(first=='“'&&last=='”'||first=='"'&&last=='"'||first=='「'&&last=='」'||first=='『'&&last=='』')value=value.Substring(1,value.Length-2);}rows[i]=value;}return string.Join("\n",rows);}
}
