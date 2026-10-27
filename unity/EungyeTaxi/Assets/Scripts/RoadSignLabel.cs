using UnityEngine;
public class RoadSignLabel : MonoBehaviour {
    Material material;TextMesh caption;
    void Awake(){caption=GetComponent<TextMesh>();material=new Material(Resources.Load<Shader>("RoadSignText"));GetComponent<Renderer>().sharedMaterial=material;LateUpdate();}
    void LateUpdate(){if(!caption||!caption.font)return;var texture=caption.font.material.mainTexture;if(material&&material.mainTexture!=texture)material.mainTexture=texture;}
    void OnDestroy(){if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
}
