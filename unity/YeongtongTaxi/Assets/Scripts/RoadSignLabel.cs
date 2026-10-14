using UnityEngine;
public class RoadSignLabel : MonoBehaviour {
    Material material;
    void Awake(){material=new Material(Resources.Load<Shader>("RoadSignText"));GetComponent<Renderer>().sharedMaterial=material;}
    void LateUpdate(){material.mainTexture=KoreanText.Font.material.mainTexture;}
    void OnDestroy(){if(material)Destroy(material);}
}
