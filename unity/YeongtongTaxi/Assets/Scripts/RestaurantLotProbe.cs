using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
// One initial placement, followed by actual pedal/steering physics through the facility.
public sealed class RestaurantLotProbe:MonoBehaviour {
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-restaurantLotTest")>=0)new GameObject("Restaurant facility check").AddComponent<RestaurantLotProbe>();}
 bool ok=true;readonly StringBuilder report=new StringBuilder();void Check(bool pass,string reason){ok&=pass;report.AppendLine((pass?"PASS ":"FAIL ")+reason);Directory.CreateDirectory("artifacts");File.WriteAllText("artifacts/restaurant-lot-player.txt",report.ToString());}
 IEnumerator Start(){yield return new WaitForSecondsRealtime(2);var s=FindObjectOfType<DriveSession>();var b=s.business;b.Depart();b.enabledCampaign=false;Time.timeScale=4;s.paused=false;s.jobs.ClearForDay();var site=s.services.Restaurant;Check(site!=null,"restaurant plot exists");if(site==null){Application.Quit(1);yield break;}
  var taxi=s.taxi;taxi.SetSpawn(site.approach.Kerb+Vector3.up*.65f,Quaternion.LookRotation(site.approach.Direction));taxi.ResetTaxi();taxi.automated=true;taxi.autoBrake=true;yield return new WaitForSeconds(.5f);
  Check(!s.services.AtRestaurant()&&!s.services.CanUse(1),"roadside cannot eat");s.services.Toggle();int cash=b.ledger.Account.Cash;Check(!s.services.RequestAction(1)&&b.ledger.Account.Cash==cash,"roadside purchase rejected without charge");Check(s.services.Guide(1),"F7 restaurant guide works");Check(s.jobs.nav.hdGoal==site.approach.laneEdge&&Mathf.Abs(s.jobs.nav.hdGoalAt-site.approach.laneAt)<.01f,"navigation ends at entrance approach");Time.timeScale=4;
  var helper=gameObject.AddComponent<FuelStationProbe>();helper.externallyDriven=true;helper.session=s;var entry=new List<Vector3>{taxi.transform.position};entry.AddRange(site.enter);yield return helper.Drive(entry,"restaurant entrance");Check(helper.Passed,"entrance physics no curb or side contacts");Check(s.services.AtRestaurant(),"actual taxi parked in off-road bay");
  Check(!site.CanDine(site.parking+site.direction*6,site.direction,0)&&!site.CanDine(site.parking,-site.direction,0),"outside bay and wrong direction rejected");Check(!s.services.AtFuelPump()&&!s.services.CanUse(2)&&!s.services.CanUse(3),"restaurant is not fuel home parking or sleep");
  s.services.Toggle();cash=b.ledger.Account.Cash;double elapsed=b.ledger.Elapsed;float fuel=b.ledger.Account.Fuel;double hunger=b.ledger.Driver.satiety;Check(s.services.RequestAction(1),"parked meal confirmation available");Check(s.services.ConfirmAction(true)&&b.ledger.Account.Cash==cash-3000&&Math.Abs(b.ledger.Elapsed-elapsed-50)<.01&&b.ledger.Driver.satiety>hunger&&Math.Abs(b.ledger.Account.Fuel-fuel)<.001,"parked meal charges once and recovers hunger with engine off");if(s.services.open)s.services.Toggle();Time.timeScale=4;
  var exit=new List<Vector3>{taxi.transform.position};exit.AddRange(site.leave);var path=s.jobs.nav.hd.paths[site.approach.laneEdge];exit.Add(path.Position(site.approach.laneAt+35));yield return helper.Drive(exit,"restaurant exit");Check(helper.Passed,"exit physics no curb or side contacts");Check(!s.services.AtRestaurant(),"driving out disables meals");
  var root=s.world.transform.Find("은계 남부 식당");Check(root&&root.Find("식당 건물")&&root.Find("식당 간판")&&root.Find("식당 주차 안내"),"building sign and parking guidance instantiated");var markers=s.GetComponent<FullMapOverlay>().VisibleMarkers();Check(markers.Exists(m=>m.label=="식당"&&Vector3.Distance(m.position,site.parking)<.1f),"map marker identifies parking plot");Check(s.traffic.cars.Count>=48,"ambient traffic retained");
  Debug.Log(report.ToString());Application.Quit(ok?0:1);
 }
}
