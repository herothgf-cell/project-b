using System;
using System.Collections.Generic;
using System.Globalization;

[Serializable] public sealed class RainSettings {
 public bool enabled;public float intensity=.65f,wiperSeconds=1.2f,brakeMultiplier=.9f,trafficSpeedMultiplier=.85f;public int boardingDirt=2;
 public static RainSettings Capture(){return new RainSettings{enabled=RainPolicy.Enabled,intensity=(float)ContentTuning.Number("weather.rain.intensity",.65),wiperSeconds=RainPolicy.WiperSeconds,brakeMultiplier=(float)ContentTuning.Number("weather.rain.brakeMultiplier",.9),trafficSpeedMultiplier=(float)ContentTuning.Number("weather.rain.trafficSpeedMultiplier",.85),boardingDirt=ContentTuning.Int("weather.rain.boardingDirt",2)};}
 public RainSettings Copy(){return (RainSettings)MemberwiseClone();}
 static bool Range(float value,float min,float max){return !float.IsNaN(value)&&!float.IsInfinity(value)&&value>=min&&value<=max;}
 public void Validate(){if(!Range(intensity,0,1)||!Range(wiperSeconds,.5f,3)||!Range(brakeMultiplier,.75f,1)||!Range(trafficSpeedMultiplier,.6f,1)||boardingDirt<0||boardingDirt>10)throw new ArgumentException("날씨 설정 범위 오류");}
 public bool Same(RainSettings other){return other!=null&&enabled==other.enabled&&intensity==other.intensity&&wiperSeconds==other.wiperSeconds&&brakeMultiplier==other.brakeMultiplier&&trafficSpeedMultiplier==other.trafficSpeedMultiplier&&boardingDirt==other.boardingDirt;}
 public string Profile(string original){
  Validate();var p=ContentJson.Object(ContentJson.Parse(original));var values=ContentJson.Array(p["values"]);
  var changes=new Dictionary<string,string>{{"enabled",enabled?"true":"false"},{"intensity",intensity.ToString("R",CultureInfo.InvariantCulture)},{"wiperSeconds",wiperSeconds.ToString("R",CultureInfo.InvariantCulture)},{"brakeMultiplier",brakeMultiplier.ToString("R",CultureInfo.InvariantCulture)},{"trafficSpeedMultiplier",trafficSpeedMultiplier.ToString("R",CultureInfo.InvariantCulture)},{"boardingDirt",boardingDirt.ToString(CultureInfo.InvariantCulture)}};
  foreach(var pair in changes){string id="weather.rain."+pair.Key;bool found=false;foreach(object item in values){var row=ContentJson.Object(item);if((string)row["id"]==id){row["value"]=pair.Value;found=true;break;}}if(!found)values.Add(new Dictionary<string,object>{{"id",id},{"value",pair.Value}});}
  p["contentHash"]=ContentTuning.ComputeHash(p);string json=ContentJson.Stringify(p);ContentTuning.ValidateJson(json);return json;
 }
 public void Install(){Validate();if(!Same(Capture()))ContentTuning.UseJson(Profile(ContentTuning.CaptureJson()));}
}
