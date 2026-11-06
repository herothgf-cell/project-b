using System;
class PersonalRoomTests {
 static int count;static void Check(bool ok,string message){count++;if(!ok)throw new Exception(message);}
 static void Reject(Action a,string message){try{a();}catch(ArgumentException){count++;return;}throw new Exception(message);}
 static void Main(){
  var a=new CampaignAccount(PrototypeSettings.ForThreeDays());a.Fare("one",10000);int net=a.Net;
  Check(a.BuyRoomItem("lamp",1,1),"lamp buy");Check(a.Cash==15000&&a.Net==net&&a.PersonalSpent==5000&&a.Expense(1,null)==0,"personal expense separate");
  Check(a.Room.lampOwned&&a.Room.lampPlaced&&a.Room.lampOn&&a.Room.lampStyle==1,"auto placement");
  Check(!a.BuyRoomItem("lamp",0,1)&&!a.BuyRoomItem("bad",0,1)&&!a.BuyRoomItem("rug",2,1),"duplicates and invalid choices reject");
  var snap=a.Snapshot();a.Room.lampStyle=0;Check(snap.Room.lampStyle==1,"snapshot independent");var restored=CampaignAccount.Restore(snap);restored.Room.lampStyle=0;Check(snap.Room.lampStyle==1,"restore independent");
  Check(restored.BuyRoomItem("rug",0,2)&&restored.PersonalSpent==13000&&restored.PersonalExpense(2)==8000,"rug costs once");
  var poor=new CampaignAccount(PrototypeSettings.ForThreeDays());poor.BuyRoomItem("rug",0,1);Check(!poor.BuyRoomItem("lamp",0,1)&&poor.Cash==2000&&!poor.Room.lampOwned,"insufficient funds no mutation");
  poor.Repair("repair",1,100);Check(poor.Debt>0&&!poor.BuyRoomItem("lamp",0,1),"debt blocks purchase");
  var bad=a.Snapshot();bad.Room.rugOwned=true;Reject(()=>CampaignAccount.Restore(bad),"unpaid ownership rejected");
  var old=new CampaignAccount(PrototypeSettings.ForThreeDays()).Snapshot();old.Room=null;Check(!CampaignAccount.Restore(old).Room.lampOwned,"legacy room absent normalizes");
  var e=new EconomyLedger(PrototypeSettings.ForThreeDays());e.RecordFare("trip",10000);e.RequestClose("test");e.Settle();Check(e.ShowingSummary,"settled");var prior=e.Last.net;var baseTotal=e.TotalNet;
  Check(e.Account.BuyRoomItem("lamp",0,1),"summary purchase");var next=EconomyLedger.RestoreCampaign(e.Snapshot(),e.Account.Snapshot());Check(next.Last.net==prior&&next.TotalNet==baseTotal,"reload operating results unchanged");Check(next.NextDay()&&next.Account.Room.lampOwned,"next day persists");
  var before=next.Account.Snapshot();var candidate=CampaignAccount.Restore(before);candidate.BuyRoomItem("rug",0,2);Check(!next.Account.Room.rugOwned&&next.Account.Cash==before.settings.startCash+10000-500-5000,"failed save candidate isolated");
  next.Account.Room.finalized=true;Check(!next.Account.BuyRoomItem("rug",0,2),"finalized no buy");
  Console.WriteLine("PASS personal room "+count);
 }
}
