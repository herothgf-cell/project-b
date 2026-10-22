using UnityEngine;
public class RoadSignLabel : MonoBehaviour {
    static Material shared;static int users;Material material;
    void Awake(){if(!shared)shared=new Material(Resources.Load<Shader>("RoadSignText"));material=shared;users++;GetComponent<Renderer>().sharedMaterial=material;}
    void LateUpdate(){var texture=KoreanText.Font.material.mainTexture;if(material&&material.mainTexture!=texture)material.mainTexture=texture;}
    void OnDestroy(){if(--users==0&&shared){Destroy(shared);shared=null;}}
}
