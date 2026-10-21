using System;
using System.Collections.Generic;
using UnityEngine;

// Fixed normal-business courses, validated against the same directed graph used by navigation/NPCs.
public sealed class PrototypeScenario {
 public string id;public int pickup,drop,connector,variant,alternate=-1;public bool beforePickup;
 public HdRoadGraph.RoutePath route,alternative;
 public static List<PrototypeScenario> Build(HdRoadGraph hd,MapStop[] stops){
  var result=new List<PrototypeScenario>();if(hd==null||stops.Length<12)return result;
  int[] pickup={7,9,7,7,7,4,5},drop={0,4,4,4,6,5,8},links={1182,1158,1180,1050,1248,1133,1351};
  for(int i=0;i<7;i++){
   var s=new PrototypeScenario{id="PS0"+(i+1),pickup=pickup[i],drop=drop[i],connector=links[i],variant=i+8,beforePickup=false};
   if(!ConnectorValid(hd,s.connector,i>=4?-1:0))continue;
   var a=stops[s.beforePickup?(i==0?7:9):s.pickup];var b=stops[s.beforePickup?s.pickup:s.drop];
   s.route=hd.Via(a.laneEdge,a.laneAt,s.connector,b.laneEdge,b.laneAt);
   if(!Valid(s.route,s.connector)||hd.Route(stops[s.pickup].laneEdge,stops[s.pickup].laneAt,stops[s.drop].laneEdge,stops[s.drop].laneAt)==null)continue;
   result.Add(s);
  }
  var exit=new PrototypeScenario{id="PS08",pickup=6,drop=3,connector=1180,variant=10};
  if(ConnectorValid(hd,exit.connector,0)){
   var a=stops[exit.pickup];var b=stops[exit.drop];exit.route=hd.Via(a.laneEdge,a.laneAt,exit.connector,b.laneEdge,b.laneAt);
   var weights=AvoidWeights(hd,exit.connector);exit.alternative=hd.Route(a.laneEdge,a.laneAt,b.laneEdge,b.laneAt,weights);
   if(Valid(exit.route,exit.connector)&&exit.alternative!=null&&!exit.alternative.edges.Contains(exit.connector)){
    for(int k=1;k<exit.alternative.edges.Count-1;k++){int e=exit.alternative.edges[k];if(e!=exit.connector&&hd.edges[e].id.StartsWith("join:")&&hd.edges[e].ruleId!=null&&hd.edges[e].ruleId.Length>0&&hd.edges[e].change==0){exit.alternate=e;break;}}
    if(exit.alternate>=0)result.Add(exit);
   }
  }
  // Prefer the documented J7204 right turn, but reject loops after it. Select a compatible curb if needed.
  PrototypeScenario right=null;float score=float.MaxValue;
  foreach(int c in new[]{1368,1392}){
   if(!ConnectorValid(hd,c,1))continue;
   for(int d=0;d<stops.Length;d++){
    if(d==6)continue;var a=stops[6];var b=stops[d];var r=hd.Via(a.laneEdge,a.laneAt,c,b.laneEdge,b.laneAt);var after=hd.Route(c,hd.paths[c].length,b.laneEdge,b.laneAt);
    if(!Valid(r,c)||after==null||after.path.length>600||after.edges.Contains(c)&&after.edges.Count>1&&after.edges.LastIndexOf(c)>0)continue;
    float cost=after.path.length+(d==11?0:500);if(cost>=score)continue;score=cost;
    right=new PrototypeScenario{id="PS09",pickup=6,drop=d,connector=c,variant=0,route=r};
   }
  }
  if(right!=null)result.Add(right);return result;
 }
 public static bool Valid(HdRoadGraph.RoutePath r,int connector){if(r==null||r.path.length<85)return false;int k=r.edges.IndexOf(connector);return k>0&&k<r.edges.Count-1&&r.edges.LastIndexOf(connector)==k&&r.ends[k-1]>=65&&r.path.length-r.ends[k]>=20;}
 static bool ConnectorValid(HdRoadGraph hd,int e,int turn){return e>=0&&e<hd.edges.Length&&!hd.edges[e].disabled&&hd.edges[e].change==0&&hd.edges[e].turn==turn;}
 public static float[] AvoidWeights(HdRoadGraph hd,int connector){var w=new float[hd.edges.Length];for(int i=0;i<w.Length;i++)w[i]=hd.paths[i].length;w[connector]=float.PositiveInfinity;return w;}
}
