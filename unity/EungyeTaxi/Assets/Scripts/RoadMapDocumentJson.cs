using System;
using System.IO;
using System.Text;

// Locate a direct JSON member without confusing escaped strings or nested
// objects. Unity serializes null inline classes as {}, so the document codec
// explicitly preserves null versus an intentionally empty authored scene.
internal static class RoadMapDocumentJson {
 static void White(string json,ref int i){while(i<json.Length&&char.IsWhiteSpace(json[i]))i++;}
 static string String(string json,ref int i){if(i>=json.Length||json[i++]!='"')throw new InvalidDataException("JSON 속성 문자열 오류");var result=new StringBuilder();while(i<json.Length){char c=json[i++];if(c=='"')return result.ToString();if(c!='\\'){result.Append(c);continue;}if(i>=json.Length)break;c=json[i++];switch(c){case 'u':if(i+4>json.Length)throw new InvalidDataException("JSON 유니코드 오류");result.Append((char)Convert.ToInt32(json.Substring(i,4),16));i+=4;break;case 'n':result.Append('\n');break;case 'r':result.Append('\r');break;case 't':result.Append('\t');break;case 'b':result.Append('\b');break;case 'f':result.Append('\f');break;default:result.Append(c);break;}}throw new InvalidDataException("닫히지 않은 JSON 문자열");}
 static int End(string json,int i){White(json,ref i);if(i>=json.Length)throw new InvalidDataException("JSON 값 누락");if(json[i]=='"'){String(json,ref i);return i;}if(json[i]=='{'||json[i]=='['){int depth=0;do{char c=json[i];if(c=='"'){String(json,ref i);continue;}if(c=='{'||c=='[')depth++;if(c=='}'||c==']')depth--;i++;}while(i<json.Length&&depth>0);if(depth!=0)throw new InvalidDataException("닫히지 않은 JSON 값");return i;}while(i<json.Length&&json[i]!=','&&json[i]!='}'&&!char.IsWhiteSpace(json[i]))i++;return i;}
 public static bool Member(string json,string name,out int start,out int end){start=end=-1;int i=0;White(json,ref i);if(i>=json.Length||json[i++]!='{')throw new InvalidDataException("JSON 객체가 필요합니다");bool found=false;while(i<json.Length){White(json,ref i);if(i<json.Length&&json[i]=='}')return found;string key=String(json,ref i);White(json,ref i);if(i>=json.Length||json[i++]!=':')throw new InvalidDataException("JSON 속성 구분 오류");White(json,ref i);int value=i;i=End(json,i);if(key==name){if(found)throw new InvalidDataException("중복 JSON 속성: "+name);found=true;start=value;end=i;}White(json,ref i);if(i<json.Length&&json[i]==','){i++;continue;}if(i<json.Length&&json[i]=='}')return found;throw new InvalidDataException("JSON 객체 구분 오류");}throw new InvalidDataException("닫히지 않은 JSON 객체");}
}
