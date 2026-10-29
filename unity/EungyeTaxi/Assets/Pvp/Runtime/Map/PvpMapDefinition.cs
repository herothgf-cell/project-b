using System;
using UnityEngine;
namespace Pvp.Map {
[Serializable] public struct PvpConnection { public int From,To; public float NominalSpeed; public PvpConnection(int from,int to,float nominalSpeed=18){From=from;To=to;NominalSpeed=nominalSpeed;} }
[Serializable] public struct PvpStop { public string Id,Name; public int Node; public Vector3 Position; public int NodeIndex {get{return Node;}} public PvpStop(string id,string name,int node,Vector3 position){Id=id;Name=name;Node=node;Position=position;} }
[CreateAssetMenu(menuName="PVP/Map Definition")]
public sealed class PvpMapDefinition:ScriptableObject {
 public string MapId="pvp_circuit_01"; public int Version=1; public float RoadWidth=8;
 public Vector3[] Nodes; public PvpConnection[] Connections; public PvpStop[] Stops; public int[] Spawns;
 public static PvpMapDefinition CreateDefault(){
  var d=CreateInstance<PvpMapDefinition>();
  d.Nodes=new[]{new Vector3(-60,0,-60),new Vector3(60,0,-60),new Vector3(60,0,60),new Vector3(-60,0,60),new Vector3(0,0,-60),new Vector3(60,0,0),new Vector3(0,0,60),new Vector3(-60,0,0),new Vector3(0,0,-72),new Vector3(72,0,0),new Vector3(0,0,72),new Vector3(-72,0,0)};
  d.Connections=new[]{new PvpConnection(0,4),new PvpConnection(4,1),new PvpConnection(1,5),new PvpConnection(5,2),new PvpConnection(2,6),new PvpConnection(6,3),new PvpConnection(3,7),new PvpConnection(7,0),new PvpConnection(4,8),new PvpConnection(8,1),new PvpConnection(5,9),new PvpConnection(9,2),new PvpConnection(6,10),new PvpConnection(10,3),new PvpConnection(7,11),new PvpConnection(11,0),new PvpConnection(4,6,7),new PvpConnection(5,6,7)};
  d.Stops=new[]{new PvpStop("stop_c01_park_s","Park",8,d.Nodes[8]),new PvpStop("stop_c01_terminal_e","Terminal",9,d.Nodes[9]),new PvpStop("stop_c01_market_n","Market",10,d.Nodes[10]),new PvpStop("stop_c01_warehouse_w","Warehouse",11,d.Nodes[11])};d.Spawns=new[]{0,1,2,3};return d;
 }
}}
