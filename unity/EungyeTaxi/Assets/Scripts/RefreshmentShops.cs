using System.Collections.Generic;
using UnityEngine;

public partial class ServiceStops {
 public sealed class RefreshmentShop {
  public Refreshment Item;
  public MapStop Stop;
  public int StopIndex;
 }
 readonly List<RefreshmentShop> refreshmentShops=new List<RefreshmentShop>();
 PassengerStopCatalog refreshmentCatalog;
 public IReadOnlyList<RefreshmentShop> RefreshmentShops {
  get{
   var catalog=session.jobs.Catalog;
   if(ReferenceEquals(refreshmentCatalog,catalog))return refreshmentShops;
   refreshmentCatalog=catalog;refreshmentShops.Clear();
   if(catalog!=null&&session.world.map.mapId=="eungye"&&session.business.ledger.OneDay&&!Pvp&&!PracticeSession.Enabled){
    foreach(int kind in new[]{Refreshment.Snack,Refreshment.Coffee}){
     var item=Refreshment.For(kind);
     int index=catalog.IndexOf(item.StopId);
     if(index>=0)refreshmentShops.Add(new RefreshmentShop{Item=item,Stop=catalog.Stops[index],StopIndex=index});
    }
   }
   return refreshmentShops;
  }
 }
 public RefreshmentShop LocalRefreshment(){
  var taxi=session.taxi;
  foreach(var shop in RefreshmentShops){
   if(Mathf.Abs(taxi.transform.position.y-shop.Stop.Kerb.y)>1.5f)continue;
   // Round 7: anywhere in front of the shop (its pocket, about 9 m round the stop) once stopped.
   if(taxi.body.velocity.magnitude<FacilityZone.Stopped&&FacilityZone.Near(shop.Stop.Kerb,taxi.transform.position,9)
      &&!session.jobs.StopOccupied(shop.StopIndex))return shop;
  }
  return null;
 }
 bool CanBuyRefreshment(Refreshment item){
  var ledger=session.business.ledger;
  return LocalRefreshment()?.Item==item&&!session.jobs.Active&&ledger.State==BusinessState.Open&&
   !ledger.Driver.drowsiness.Blackout&&ledger.Account.Cash>=item.Price;
 }
 bool BuyRefreshment(Refreshment item){
  var business=session.business;
  if(!business.ledger.UseRefreshment(session.law.ids.Next("refreshment"),item.Kind))return false;
  CompleteServiceNavigation();Close();business.SaveCheckpoint();
  notice=item.Menu+" 이용 완료 · "+item.Price.ToString("N0")+"원 · "+item.Seconds+"초 경과";
  noticeUntil=Time.unscaledTime+5;return true;
 }
 void AddRefreshmentOptions(List<FacilityOption> options,int kind){
  foreach(var shop in RefreshmentShops){
   if(kind==Refreshment.Coffee&&shop.Item.Kind!=kind)continue;
   var stop=shop.Stop;var bay=stop.Kerb;
   var entry=new[]{stop.V(),bay};var exit=new[]{bay,stop.V()+stop.Direction*7};
   options.Add(Option(shop.Item.Kind,shop.Item.Shop,stop,bay,stop.Direction,entry,exit));
  }
 }
 void DrawRefreshmentSigns(){
  foreach(var shop in RefreshmentShops){
   bool food=shop.Item.Kind==Refreshment.Snack;
   var stop=shop.Stop;
   // Shops occupy a building behind the pocket; the street stall remains only as a fallback.
   if(RefreshmentStorefronts.Draw(session.world.map,session.world.transform,stop,shop.Item.Shop,food))continue;
   var root=new GameObject(shop.Item.Shop);
   root.transform.SetParent(session.world.transform,false);
   root.transform.position=stop.Passenger+stop.Direction*5+TrafficSystem.Right(stop.Direction)*1.15f;
   root.transform.rotation=Quaternion.LookRotation(stop.Direction);
   var accent=WorldBuilder.Mat(shop.Item.Shop,food?new Color(.70f,.22f,.12f):new Color(.12f,.35f,.28f));
   var trim=WorldBuilder.Mat("매대 아이보리",new Color(.96f,.86f,.67f));
   WorldBuilder.Box("포장 매대",new Vector3(0,.52f,0),new Vector3(.75f,1,1.65f),accent,root.transform,false);
   WorldBuilder.Box("주문대",new Vector3(0,1.04f,0),new Vector3(.9f,.1f,1.8f),trim,root.transform,false);
   foreach(float z in new[]{-.72f,.72f})WorldBuilder.Box("차양 기둥",new Vector3(.3f,1.58f,z),new Vector3(.055f,1.05f,.055f),trim,root.transform,false);
   WorldBuilder.Box("차양",new Vector3(0,2.13f,0),new Vector3(1.15f,.14f,1.95f),accent,root.transform,false);
   var label=new GameObject("주문 안내");label.transform.SetParent(root.transform,false);
   label.transform.localPosition=Vector3.up*2.85f;
   var text=label.AddComponent<TextMesh>();text.text=shop.Item.Shop+"\n빈차 정차 후 F";
   text.fontSize=44;text.characterSize=.052f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;
   KoreanText.Apply(text);label.AddComponent<FaceCamera>();
  }
 }
}
