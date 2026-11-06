using UnityEngine;
public static partial class TaxiArt {
 public static void Exterior(Transform parent){
  var prefab=Resources.Load<GameObject>("TaxiConcept/Body");if(!prefab){ProceduralExterior(parent);return;}
  var model=Object.Instantiate(prefab,parent,false);model.name="Taxi exterior";
  Text(model.transform,"TAXI front","TAXI",new Vector3(0,1.69f,-.095f),.055f,Color.black,true);
  Text(model.transform,"TAXI rear","TAXI",new Vector3(0,1.69f,-.405f),.055f,Color.black,false);
  Text(model.transform,"Rear registration","서울 1234",new Vector3(0,.54f,-2.026f),.033f,Color.black,false);
  Text(model.transform,"Front registration","서울 1234",new Vector3(0,.24f,2.058f),.031f,Color.black,true);
 }
 public static Transform Wheel(Transform parent){var prefab=Resources.Load<GameObject>("TaxiConcept/Wheel");if(!prefab)return ProceduralWheel(parent);var model=Object.Instantiate(prefab,parent,false);model.name="Tyre visual";return model.transform;}
}
