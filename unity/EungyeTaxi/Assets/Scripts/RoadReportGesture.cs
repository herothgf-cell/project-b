using UnityEngine;
public sealed class RoadReportGesture {
 Vector2 start;bool active,moved;
 public bool Active {get{return active;}}
 public bool Moved {get{return moved;}}
 public void Down(Vector2 point){start=point;active=true;moved=false;}
 public void Move(Vector2 point){if(active&&(point-start).sqrMagnitude>36)moved=true;}
 public bool Up(Vector2 point,Rect viewport){Move(point);bool click=active&&!moved&&viewport.Contains(point);active=false;return click;}
 public void Cancel(){active=false;moved=false;}
}
