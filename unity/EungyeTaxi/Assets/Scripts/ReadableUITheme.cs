using UnityEngine;
// Mutate the shared default skin before the existing IMGUI screens copy styles.
[DefaultExecutionOrder(-10000)]
public class ReadableUITheme:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){new GameObject("Readable Korean UI").AddComponent<ReadableUITheme>();}
 void OnGUI(){KoreanText.PrepareSkin();}
}
