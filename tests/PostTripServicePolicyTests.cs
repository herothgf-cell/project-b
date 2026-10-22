using System;
class PostTripServicePolicyTests {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
  var routes=new float[]{1000,500,800,600};
  Check(PostTripServicePolicy.Recommend(60,80,10,900,routes)==-1,"healthy driver can continue work");
  Check(PostTripServicePolicy.Recommend(5,80,10,900,routes)==0,"low fuel recommends station");
  Check(PostTripServicePolicy.Recommend(60,15,10,900,routes)==1,"hungry driver recommends meal");
  Check(PostTripServicePolicy.Recommend(60,80,80,900,routes)==3,"tired driver recommends rest");
  Check(PostTripServicePolicy.Recommend(60,80,10,120,routes)==2,"near closing recommends home parking");
  Check(PostTripServicePolicy.Recommend(60,10,90,900,new float[]{1000,10000,800,50})==3,"route access affects competing needs");
  Check(PostTripServicePolicy.Recommend(5,80,10,900,new float[]{float.PositiveInfinity,500,800,600})==-1,"unreachable fuel is never recommended");
  Check(PostTripServicePolicy.Recommend(60,10,10,49,routes)==2,"meal cannot fit the remaining day");
  Console.WriteLine("PASS post-trip service needs, route distance, closing and unreachable routes");
 }
}
