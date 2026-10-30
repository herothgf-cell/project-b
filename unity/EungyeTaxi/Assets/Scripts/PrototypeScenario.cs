using System;
using System.Collections.Generic;
using UnityEngine;

// Stops define the trip. Situations are selected on its ordinary shortest directed route.
public sealed class PrototypeScenario {
 public string id;public int pickup,drop,connector,variant,alternate=-1;public bool beforePickup;
 public HdRoadGraph.RoutePath route,alternative;
 public static List<PrototypeScenario> Build(HdRoadGraph hd,MapStop[] stops){
  var result=new List<PrototypeScenario>();if(hd==null||stops.Length<12)return result;
  var routes=new HdRoadGraph.RoutePath[stops.Length,stops.Length];
  for(int a=0;a<stops.Length;a++)for(int b=0;b<stops.Length;b++)if(a!=b&&stops[a].graphStop&&stops[b].graphStop)routes[a,b]=hd.Route(stops[a].laneEdge,stops[a].laneAt,stops[b].laneEdge,stops[b].laneAt);
  int[] pickups={7,9,7,7,7,4,5,6,6},drops={0,4,4,4,6,5,8,3,11},links={1182,1158,1180,1050,1248,1133,1351,1180,1368};
  var occupied=new HashSet<string>();
  for(int i=0;i<9;i++){
   PrototypeScenario best=null;float score=float.MaxValue;int turn=i==8?1:i>=4&&i<=6?-1:0;
   for(int a=0;a<stops.Length;a++)for(int b=0;b<stops.Length;b++){
    var r=routes[a,b];if(r==null)continue;
    for(int k=1;k<r.edges.Count-1;k++){
     int c=r.edges[k];var e=hd.edges[c];int incoming=r.edges[k-1],outgoing=r.edges[k+1];
     if(e.disabled||e.change!=0||e.turn!=turn||!e.id.StartsWith("join:")||!hd.signalizedRules.Contains(e.ruleId)||i<7&&occupied.Contains(e.ruleId)||!Valid(r,c))continue;
     if(i==7&&occupied.Contains(e.ruleId)&&!result.Exists(x=>x.variant==10&&x.connector==c))continue;
     if(hd.edges[incoming].number!=1||hd.paths[incoming].length<65||hd.paths[outgoing].length<32)continue;
     HdRoadGraph.RoutePath alternative=null;int alternate=-1;
     if(i==7){alternative=hd.Route(stops[a].laneEdge,stops[a].laneAt,stops[b].laneEdge,stops[b].laneAt,AvoidWeights(hd,c));if(alternative==null||alternative.edges.Contains(c))continue;
      for(int n=1;n<alternative.edges.Count-1;n++){int x=alternative.edges[n];if(hd.edges[x].id.StartsWith("join:")&&!string.IsNullOrEmpty(hd.edges[x].ruleId)&&Valid(alternative,x)){alternate=x;break;}}if(alternate<0)continue;}
     // Keep the authored passenger endpoints where possible; relocate the game situation instead of detouring the passenger.
     float cost=(a==pickups[i]?0:100000)+(b==drops[i]?0:100000)+r.path.length+(c==links[i]?0:3000);
     // PS05–07 keep their saved trip identity and natural routes. Fictional permissive-left
     // signal overrides belong only to standalone practice, never to normal service.
     if(cost>=score)continue;score=cost;best=new PrototypeScenario{id="PS0"+(i+1),pickup=a,drop=b,connector=c,variant=i<4?i+8:i==7?10:0,route=r,alternative=alternative,alternate=alternate};
    }
   }
   if(best!=null){result.Add(best);if(i<7)occupied.Add(hd.edges[best.connector].ruleId);}
  }
  return result;
 }
 public static bool Valid(HdRoadGraph.RoutePath r,int connector){if(r==null||r.path.length<85)return false;int k=r.edges.IndexOf(connector);return k>0&&k<r.edges.Count-1&&r.edges.LastIndexOf(connector)==k&&r.ends[k-1]>=65&&r.path.length-r.ends[k]>=20;}
 public static float[] AvoidWeights(HdRoadGraph hd,int connector){var w=new float[hd.edges.Length];for(int i=0;i<w.Length;i++)w[i]=hd.paths[i].length;w[connector]=float.PositiveInfinity;return w;}
}
