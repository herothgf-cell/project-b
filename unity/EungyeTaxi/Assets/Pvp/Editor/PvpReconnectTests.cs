using System;
using System.Collections.Generic;
using UnityEngine;
using Pvp.Network;
using Pvp.Passengers;
using Pvp.Driving;
namespace Pvp.Tests.EditMode {
public static class PvpReconnectTests {
 static void Check(bool ok,string label){if(!ok)throw new Exception("PVP_RECONNECT: "+label);}
 public static void Run(){
  Check(typeof(PvpBootstrap).Assembly.GetType("Pvp.Network.PvpReconnectPolicy")!=null,"outage recovery policy missing"); PvpNetworkTests.Run();bool resumed;string reason;
  Check(!PvpReconnectPolicy.ShouldRecover(119,2.38f)&&PvpReconnectPolicy.ShouldRecover(120,2.4f)&&PvpReconnectPolicy.ShouldRecover(0,3.01f),"recover before prediction history exhaustion");
  Check(!PvpReconnectPolicy.IsTerminalRejection("already connected")&&PvpReconnectPolicy.IsTerminalRejection("invalid reconnect ticket")&&PvpReconnectPolicy.IsTerminalRejection("reconnect expired"),"transient duplicate retries; credentials and expiry terminate");
  var registry=new PvpReconnectRegistry(30);
  Check(registry.TryReserve(11,null,0,out resumed,out reason)&&!resumed,"initial reservation");
  Check(!registry.TryReserve(12,null,0,out resumed,out reason),"pending slot cannot be stolen");
  Check(registry.Confirm(11,1),"initial confirmation");string ticket=registry.TicketFor(11);
  Check(ticket.Length==44,"256-bit ticket encoding");
  Check(!registry.TryReserve(12,ticket,2,out resumed,out reason)&&reason=="already connected","duplicate active identity rejected");
  Check(!registry.TryReserve(12,"forged",2,out resumed,out reason)&&reason=="invalid reconnect ticket","forged credential rejected");
  Check(registry.CanDrive(11,1)&&!registry.CanDrive(11,2),"connection and epoch binding");
  Check(registry.Disconnect(11,3)&&!registry.CanDrive(11,1),"disconnected input rejected");
  Check(registry.TryReserve(12,ticket,4,out resumed,out reason)&&resumed,"resume before deadline");
  Check(registry.Confirm(12,4)&&registry.Epoch==2,"new transport epoch");
  Check(!registry.CanDrive(11,1)&&!registry.CanDrive(12,1)&&registry.CanDrive(12,2),"stale connection and epoch rejected");
  Check(registry.Leave(12),"explicit leave");Check(!registry.TryReserve(13,ticket,5,out resumed,out reason)&&reason=="left session","leave revokes credential");
  var expiry=new PvpReconnectRegistry(3);expiry.TryReserve(21,null,0,out resumed,out reason);expiry.Confirm(21,0);string expiredTicket=expiry.TicketFor(21);expiry.Disconnect(21,1);
  Check(expiry.TryReserve(22,expiredTicket,3.9,out resumed,out reason),"reservation just before expiry");
  Check(!expiry.Confirm(22,4),"pending handshake cannot extend expiry");
  Check(!expiry.TryReserve(23,expiredTicket,4,out resumed,out reason)&&reason=="reconnect expired","exact expiry boundary rejected");
  var other=new PvpReconnectRegistry(30);Check(!other.TryReserve(31,ticket,0,out resumed,out reason),"other host session rejects ticket");
  var kinds=new HashSet<PassengerType>();int special=0;for(int i=0;i<PvpPassengerCatalog.Count;i++){var card=PvpPassengerCatalog.At(i);kinds.Add(card.Type);if(card.Type!=PassengerType.General)special++;Check(!string.IsNullOrEmpty(card.TypeLabel)&&!string.IsNullOrEmpty(card.VariantLabel)&&!string.IsNullOrEmpty(card.Dialogue),"shared passenger preview content");Check(PvpPassengerCatalog.At(i).Dialogue==card.Dialogue,"stable preview without advancing campaign effects");}
  Check(kinds.Count==12&&special==34,"general plus11 types and34 special variants");
  bool rejected=false;try{PvpPassengerCatalog.At(-1);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"invalid catalog index");
  var from=new PvpVehicleState{Position=Vector3.zero,YawDegrees=350};var to=new PvpVehicleState{Position=new Vector3(10,0,0),YawDegrees=10};
  Check(PvpRemotePresentation.Step(from,to,0).Position==from.Position,"zero-time presentation");var visual=PvpRemotePresentation.Step(from,to,.05f);Check(visual.Position.x>0&&visual.Position.x<10&&Mathf.Abs(Mathf.DeltaAngle(visual.YawDegrees,10))<20,"bounded position and shortest yaw interpolation");Check(from.Position==Vector3.zero&&to.Position.x==10,"presentation does not mutate authority");
  Debug.Log("PVP_RECONNECT_MODEL_OK credential/duplicate/expiry/epoch/withdrawal/catalog37/interpolation");
 }
}}