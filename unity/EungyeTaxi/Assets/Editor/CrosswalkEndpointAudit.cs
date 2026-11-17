using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CrosswalkEndpointAudit {
 public static void Run(){
  GeneratedCrosswalkGeometryTests.Run();
  AuthoredCrosswalkGeometryTests.Run();
  var package=ProjectMapStore.Load(ProjectMapBuild.Root);var map=package.Compose();EungyeConnectivityRepair.Apply(map,package.baseline);
  var zones=ReviewedCrosswalkGeometry.Build(map);var lines=new List<string>{"junction,axis,sign,kind,old_width,new_width,center_shift,resolved,x,z"};int generated=0,changed=0,unresolved=0,authored=0;
  foreach(var j in map.junctionRules){if(!j.crosswalk)continue;var axis0=new Vector3(j.dx,0,j.dz).normalized;float[] widths={3,3};foreach(var a in j.approaches){var d=new Vector3(a.dx,0,a.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(d,axis0))>.7f?0:1;widths[axis]=Mathf.Max(widths[axis],a.width*.5f);}
   foreach(var a in j.approaches){var d=new Vector3(a.dx,0,a.dz).normalized;int axis=Mathf.Abs(Vector3.Dot(d,axis0))>.7f?0:1;int sign=Vector3.Dot(d,axis==0?axis0:TrafficSystem.Right(axis0))>=0?1:-1;
    var explicitCross=map.editorDocument.facilities.FirstOrDefault(f=>f.junctionId==j.id&&f.kind=="crosswalk"&&f.axis==axis&&f.sign==sign);
    if(explicitCross!=null)continue;
    var original=j.V()-d*(widths[1-axis]+TrafficJunction.CrosswalkCentreOffset);var center=original;float half=a.width*.5f;bool resolved=GeneratedCrosswalkGeometry.Fit(map,ref center,d,ref half);generated++;if(!resolved)unresolved++;if(Mathf.Abs(half*2-a.width)>.05f||(center-original).magnitude>.05f)changed++;
    lines.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},generated,{3:F3},{4:F3},{5:F3},{6},{7:F3},{8:F3}",j.id,axis,sign,a.width,half*2,(center-original).magnitude,resolved,center.x,center.z));
    if(j.id=="J1927_5181"&&a.dx>.9f&&(!resolved||half*2<a.width+1))throw new Exception("J1927 west crossing still stops inside expanded pavement");
    if(!zones.Any(z=>!z.authored&&(z.center-center).sqrMagnitude<.001f&&Mathf.Abs(z.halfWidth-half)<.001f))throw new Exception("Paint exclusion differs from fitted stripe geometry: "+j.id);
   }
  }
  int authoredChanged=0;foreach(var source in map.editorDocument.facilities.Where(f=>f.kind=="crosswalk")){
   var effective=GeneratedCrosswalkGeometry.Effective(map,source);authored++;bool fitted=!ReferenceEquals(effective,source);if(fitted)authoredChanged++;
   if(!zones.Any(z=>z.authored&&(z.center-effective.Position).sqrMagnitude<.001f&&Mathf.Abs(z.halfWidth*2-effective.width)<.001f))throw new Exception("Authored stripe/exclusion mismatch: "+source.id);
   lines.Add(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4:F3},{5:F3},{6:F3},true,{7:F3},{8:F3}",source.id,source.axis,source.sign,fitted?"authored_fitted":"authored_preserved",source.width,effective.width,(effective.Position-source.Position).magnitude,effective.x,effective.z));
  }
  string output=Path.Combine(ProjectMapBuild.Root,"artifacts/color-road-survey/crosswalk-endpoints.csv");File.WriteAllLines(output,lines);
  Debug.Log("CROSSWALK_ENDPOINT_AUDIT generated="+generated+" changed="+changed+" unresolved="+unresolved+" authored="+authored+" authored_fitted="+authoredChanged+" report="+output);
 }
}
