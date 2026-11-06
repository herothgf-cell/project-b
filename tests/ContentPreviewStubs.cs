using System;
namespace UnityEngine {
 public class RuntimeInitializeOnLoadMethodAttribute:Attribute {public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type){}}
 public enum RuntimeInitializeLoadType{BeforeSceneLoad,AfterSceneLoad,SubsystemRegistration}
 public class TextAsset {public string text;}
 public static class Resources {public static T Load<T>(string path) where T:class{return new TextAsset{text=System.IO.File.ReadAllText("unity/EungyeTaxi/Assets/Resources/"+path+".json")} as T;}}
 public static class Application {public static bool isEditor=true;public static string dataPath=System.IO.Path.GetFullPath("unity/EungyeTaxi/Assets");public static string temporaryCachePath=System.IO.Path.GetTempPath();public static void Quit(int code){}}
 public static class Debug {public static void Log(object s){Console.WriteLine(s);}public static void LogWarning(object s){Console.WriteLine(s);}public static void LogError(object s){Console.WriteLine(s);}}
 public static class JsonUtility {static System.Web.Script.Serialization.JavaScriptSerializer serializer=new System.Web.Script.Serialization.JavaScriptSerializer();public static T FromJson<T>(string s){return serializer.Deserialize<T>(s);}public static string ToJson(object o,bool pretty=false){return serializer.Serialize(o);}}
 public class MonoBehaviour {protected static void DontDestroyOnLoad(object o){}}
 public class GameObject {public GameObject(string name){}public T AddComponent<T>() where T:new(){return new T();}}
 public struct Rect {public Rect(float x,float y,float w,float h){}}
 public static class Screen {public static int width=1280,height=720;}
 public class Skin {public object font,box,label;}
 public class GUIStyle {public bool wordWrap;public GUIStyle(object previous){}}
 public static class GUI {public static int depth;public static Skin skin=new Skin();public static bool Button(Rect rect,string s){return false;}}
 public static class GUILayout {public static void BeginArea(Rect r,object style){}public static void Label(string s,object style=null){}public static void EndArea(){}}
 public static class Mathf {public static float Min(float a,float b){return Math.Min(a,b);}public static float Max(float a,float b){return Math.Max(a,b);}}
}
public static class KoreanText {public static object Font;}
public class RuntimeCheckEntry {public static int Main(){try{ContentTuningChecks.Run();return 0;}catch(Exception e){Console.WriteLine("CHECK_FAILURE "+e.Message);return 1;}}}
