using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

// Executes the production pickup selector, with the current catalog's real
// coordinates supplied by the runner. Route eligibility belongs to the native
// OfferVarietyProbe, not this spatial/selection policy test.
class OfferVarietyTests {
 static int checks;
 static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
 static int Main(string[] args){try{Run(args);Console.WriteLine("OFFER_VARIETY_TESTS_OK checks="+checks);return 0;}catch(Exception e){Console.WriteLine("FAIL "+e.Message);return 1;}}
 static void Run(string[] args){
  var method=typeof(OneDayOfferPolicy).GetMethod("SpreadPickup");
  Check(method!=null,"new boards must prefer fresh pickups when equally spread alternatives exist");
  var choiceType=method.GetParameters()[0].ParameterType.GetElementType();
  Func<int,int,float,float,object> choice=(id,cell,x,z)=>Activator.CreateInstance(choiceType,new object[]{id,cell,x,z});
  Func<object[],Array> array=items=>{var values=Array.CreateInstance(choiceType,items.Length);for(int i=0;i<items.Length;i++)values.SetValue(items[i],i);return values;};
  Func<object[],object[],int[],bool,int,int> select=(pool,selected,prior,rotate,generation)=>(int)method.Invoke(null,new object[]{array(pool),array(selected),prior,rotate,generation});
  var a=choice(0,0,0,0);var b=choice(1,1,400,0);var c=choice(2,2,800,0);
  Check(select(new[]{a,b,c},new object[0],new[]{0},false,0)==1,"recent best-ranked pickup yields to fresh candidate");
  Check(select(new[]{a,b,c},new[]{a},new[]{1},false,0)==2,"recent pickup yields within a different-cell spatial pass");
  Check(select(new[]{a,b},new object[0],new[]{0,1},false,0)==0,"all-recent constrained pool remains usable in rank order");
  Check(select(new[]{a},new[]{a},new int[0],false,0)==-1,"never duplicate one pickup in the same board");
  var nearby=choice(3,0,10,0);
  Check(select(new[]{nearby,b},new[]{a},new[]{1},false,0)==1,"freshness cannot weaken available 120m/cell separation");
  Check(select(new[]{nearby},new[]{a},new[]{3},false,0)==0,"sparse pool safely relaxes spatial constraints");
  Check(select(new object[0],new object[0],new int[0],false,0)==-1,"empty eligible pool produces no fabricated stop");
  var lines=File.ReadAllLines(args[0]);var actual=new List<object>();var ids=new List<string>();
  foreach(var line in lines){var p=line.Split('\t');actual.Add(choice(actual.Count,int.Parse(p[1]),float.Parse(p[2],System.Globalization.CultureInfo.InvariantCulture),float.Parse(p[3],System.Globalization.CultureInfo.InvariantCulture)));ids.Add(p[0]);}
  Check(actual.Count>=16,"current Eungye catalog has enough real endpoints for distribution test");
  for(int phase=0;phase<4;phase++){
   int[] previous=new int[0];var seen=new HashSet<int>();int repeats=0;
   for(int generation=0;generation<48;generation++){
    var ranked=new List<int>();for(int i=0;i<actual.Count;i++)ranked.Add(i);
    int g=generation;ranked.Sort((x,y)=>PassengerDemandPolicy.OfferPriority("distribution:"+g,ids[x],ids[(x+5)%ids.Count],phase).CompareTo(PassengerDemandPolicy.OfferPriority("distribution:"+g,ids[y],ids[(y+5)%ids.Count],phase)));
    var pool=ranked.ConvertAll(i=>actual[i]).ToArray();var selected=new List<object>();var picked=new List<int>();
    for(int slot=0;slot<3;slot++){int index=select(pool,selected.ToArray(),previous,slot==0,generation);Check(index>=0,"three available spatially distinct pickups");var item=pool[index];int pickup=(int)choiceType.GetField("Index").GetValue(item);if(Array.IndexOf(previous,pickup)>=0)repeats++;seen.Add(pickup);picked.Add(pickup);selected.Add(item);}
    previous=picked.ToArray();
   }
   Check(repeats==0,"fresh alternatives prevent successive-board repeats phase="+phase+" repeats="+repeats);
   Check(seen.Count>=20,"48 generations retain broad real-catalog coverage phase="+phase+" seen="+seen.Count);
   Console.WriteLine("OFFER_VARIETY_DISTRIBUTION phase="+phase+" generations=48 seen="+seen.Count+" repeats="+repeats);
  }
 }
}
