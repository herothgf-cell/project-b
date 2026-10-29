using System.Collections.Generic;
using UnityEngine;
namespace Pvp.Map {
public static class PvpExpandedMap {
 public static PvpSignalZone[] LegacyZones(){return new[]{
  new PvpSignalZone{Id="central",ApproachEdge=16,RightTurnEdge=-1,StopFraction=52f/120,EntryFraction=56f/120,GreenTicks=250,RedTicks=250},
  new PvpSignalZone{Id="south",ApproachEdge=0,RightTurnEdge=8,StopFraction=50f/60,EntryFraction=56f/60,GreenTicks=250,RedTicks=250}};}
 public static PvpNpcPath[] LegacyNpcs(){var ring=new[]{0,1,2,3,4,5,6,7};return new[]{new PvpNpcPath{Edges=(int[])ring.Clone(),StartDistance=150},new PvpNpcPath{Edges=(int[])ring.Clone(),StartDistance=300}};}
 public static PvpPedestrianPath[] LegacyPedestrians(){return new[]{new PvpPedestrianPath{Id="central-walk",SignalZone=1,From=new Vector3(-6,0,-65),To=new Vector3(-6,0,-55),Speed=2}};}
 public static PvpMapDefinition Create(){
  var d=PvpMapDefinition.CreateDefault();d.MapId="pvp_district_02";d.Version=2;d.ExpandedDistrict=true;
  var nodes=new List<Vector3>(d.Nodes);nodes.AddRange(new[]{new Vector3(-240,0,-200),new Vector3(240,0,-200),new Vector3(240,0,200),new Vector3(-240,0,200),new Vector3(0,0,-200),new Vector3(240,0,0),new Vector3(0,0,200),new Vector3(-240,0,0),new Vector3(0,0,-220),new Vector3(260,0,0),new Vector3(0,0,220),new Vector3(-260,0,0),new Vector3(160,0,-80),new Vector3(-160,0,80),new Vector3(24,0,120),new Vector3(-24,0,-120),new Vector3(-240,0,80),new Vector3(240,0,-80)});
  var edges=new List<PvpConnection>(d.Connections);int[] from={12,16,13,17,14,18,15,19},to={16,13,17,14,18,15,19,12};for(int i=0;i<8;i++)edges.Add(new PvpConnection(from[i],to[i],16,14));
  int southBay=edges.Count;edges.Add(new PvpConnection(16,20,7));edges.Add(new PvpConnection(20,13,12));int eastBay=edges.Count;edges.Add(new PvpConnection(17,21,7));edges.Add(new PvpConnection(21,14,12));int northBay=edges.Count;edges.Add(new PvpConnection(18,22,7));edges.Add(new PvpConnection(22,15,12));edges.Add(new PvpConnection(19,23,7));edges.Add(new PvpConnection(23,12,12));
  // Outbound connectors continue through separate corner gates instead of almost
  // overlapping the old stop-bay forks. Their final leg aligns with the outer lane.
  edges.Add(new PvpConnection(1,24,10));edges.Add(new PvpConnection(24,29,10));edges.Add(new PvpConnection(3,25,10));edges.Add(new PvpConnection(25,28,10));edges.Add(new PvpConnection(18,26,10));edges.Add(new PvpConnection(26,6,10));edges.Add(new PvpConnection(16,27,10));edges.Add(new PvpConnection(27,0,10));edges.Add(new PvpConnection(29,17,10));edges.Add(new PvpConnection(28,19,10));
  var stops=new List<PvpStop>(d.Stops);string[] central={"중앙공원","도심 환승장","중앙 상가","도심 물류"};for(int i=0;i<4;i++){var s=stops[i];s.Name=central[i];stops[i]=s;}
  string[] names={"남부 주거단지","동부 터미널","북부 상업지구","서부 산업단지"};for(int i=0;i<4;i++)stops.Add(new PvpStop("district-stop-"+i,names[i],20+i,nodes[20+i]));
  d.Nodes=nodes.ToArray();d.Connections=edges.ToArray();d.Stops=stops.ToArray();d.Spawns=new[]{0,1,2,3};
  var zones=new List<PvpSignalZone>(LegacyZones());zones.Add(new PvpSignalZone{Id="outer-south",ApproachEdge=18,RightTurnEdge=southBay,StopFraction=226f/240,EntryFraction=234f/240,GreenTicks=400,RedTicks=250,OffsetTicks=80});zones.Add(new PvpSignalZone{Id="outer-east",ApproachEdge=20,AlternateEdgePlusOne=43,RightTurnEdge=eastBay,StopFraction=186f/200,EntryFraction=194f/200,GreenTicks=450,RedTicks=250,OffsetTicks=200});zones.Add(new PvpSignalZone{Id="outer-north",ApproachEdge=22,RightTurnEdge=northBay,StopFraction=226f/240,EntryFraction=234f/240,GreenTicks=400,RedTicks=250,OffsetTicks=300});d.SignalZones=zones.ToArray();
  var paths=new List<PvpNpcPath>(LegacyNpcs());for(int i=0;i<4;i++)paths.Add(new PvpNpcPath{Edges=new[]{18,19,20,21,22,23,24,25},StartDistance=220+i*440,Speed=8});d.NpcPaths=paths.ToArray();
  var walks=new List<PvpPedestrianPath>(LegacyPedestrians());walks.Add(new PvpPedestrianPath{Id="south-walk",SignalZone=2,From=new Vector3(-6,0,-205),To=new Vector3(-6,0,-195),Speed=2});walks.Add(new PvpPedestrianPath{Id="east-walk",SignalZone=3,From=new Vector3(235,0,-6),To=new Vector3(245,0,-6),Speed=2});walks.Add(new PvpPedestrianPath{Id="north-walk",SignalZone=4,From=new Vector3(6,0,195),To=new Vector3(6,0,205),Speed=2});d.PedestrianPaths=walks.ToArray();
  d.Obstacle=new PvpMovingObstacle{From=new Vector3(-5,0,0),To=new Vector3(5,0,0),PeriodTicks=400,Radius=1.8f};d.Services=new[]{new PvpServicePoint{Id="east-depot",Name="동부 차고지 · 주유",StopIndex=5,DurationTicks=250,CostPoints=2},new PvpServicePoint{Id="west-fuel",Name="서부 주유소",StopIndex=7,DurationTicks=250,CostPoints=2}};
  d.SupplyOrder=new[]{0,4,1,5,2,6,3,7};d.Destinations=new[]{1,3,3,0,6,7,5,4};
  d.Districts=new[]{new PvpDistrict{Name="도심",Center=Vector3.zero,Size=new Vector2(100,100),Color=new Color(.45f,.6f,.7f)},new PvpDistrict{Name="남부 주거",Center=new Vector3(0,0,-150),Size=new Vector2(360,75),Color=new Color(.5f,.7f,.5f)},new PvpDistrict{Name="터미널 지구",Center=new Vector3(180,0,60),Size=new Vector2(100,240),Color=new Color(.75f,.65f,.45f)},new PvpDistrict{Name="북부 상업",Center=new Vector3(0,0,160),Size=new Vector2(360,70),Color=new Color(.55f,.65f,.8f)},new PvpDistrict{Name="물류 산업",Center=new Vector3(-180,0,0),Size=new Vector2(100,300),Color=new Color(.65f,.6f,.55f)}};return d;
 }
}}
