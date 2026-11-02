using System;
using System.Collections.Generic;
using UnityEngine;

// A single stable array is displayed and selected by every participant.
// Accepted offers are removed by identity, then replaced with new native jobs.
public sealed class PvpSimulationCallBoard {
    public const int Capacity=8;
    public JobOffer[] Offers {get;private set;}
    public JobOffer[] Roadside {get;private set;}
    readonly PvpSimulationLedger ledger;
    readonly Func<int,JobOffer> create;
    readonly Dictionary<int,float> resting=new Dictionary<int,float>();
    int serial;float retryAt;
    readonly int attemptsPerPass;
    public PvpSimulationCallBoard(PvpSimulationLedger ledger,Func<int,JobOffer> create):this(ledger,create,32,0){}
    public PvpSimulationCallBoard(PvpSimulationLedger ledger,Func<int,JobOffer> create,int stopCount):this(ledger,create,stopCount,0){}
    public PvpSimulationCallBoard(PvpSimulationLedger ledger,Func<int,JobOffer> create,int stopCount,int roadsideCount) {
        if(ledger==null||create==null)throw new ArgumentNullException();
        this.ledger=ledger;this.create=create;attemptsPerPass=Mathf.Max(Capacity,stopCount*2);
        Offers=new JobOffer[Capacity];Roadside=new JobOffer[Mathf.Clamp(roadsideCount,0,3)];Replenish();
    }
    public bool Contains(JobOffer offer){return offer!=null&&(Array.IndexOf(Offers,offer)>=0||Array.IndexOf(Roadside,offer)>=0);}
    public void Replenish(){
        if(ledger.Finished||Roadside.Length>0&&Time.time<retryAt)return;
        int budget=attemptsPerPass*(Roadside.Length>0?2:1);
        foreach(var pool in new[]{Offers,Roadside})for(int slot=0;slot<pool.Length;slot++){
            if(pool[slot]!=null)continue;
            while(budget-->0){
                int id=serial++;var offer=create(id);if(offer==null||offer.pickup==offer.drop||float.IsInfinity(offer.distance)||float.IsNaN(offer.distance)||ledger.IsClaimed(offer.pickup))continue;
                float until;if(resting.TryGetValue(offer.pickup,out until)&&Time.time<until)continue;
                bool duplicate=false;foreach(var list in new[]{Offers,Roadside})foreach(var prior in list)if(prior!=null&&prior.pickup==offer.pickup)duplicate=true;
                if(duplicate)continue;
                offer.candidateSeed="shared-call:"+id;pool[slot]=offer;break;
            }
        }
        if(budget<=0&&Roadside.Length>0)retryAt=Time.time+2;
    }
    public bool CanGenerateAt(int pickup){if(ledger.IsClaimed(pickup))return false;float until;if(resting.TryGetValue(pickup,out until)&&Time.time<until)return false;foreach(var o in Offers)if(o!=null&&o.pickup==pickup)return false;foreach(var o in Roadside)if(o!=null&&o.pickup==pickup)return false;return true;}
    public void ApplyNetworkView(JobOffer[] offers,JobOffer[] roadside){if(offers==null||offers.Length!=8||roadside==null||roadside.Length!=3)throw new ArgumentException("network call board");for(int i=0;i<Offers.Length;i++)Offers[i]=Stable(Offers[i],offers[i]);for(int i=0;i<Roadside.Length;i++)Roadside[i]=Stable(Roadside[i],roadside[i]);}
    static JobOffer Stable(JobOffer old,JobOffer incoming){if(incoming!=null&&string.IsNullOrEmpty(incoming.candidateSeed))incoming=null;return old!=null&&incoming!=null&&old.candidateSeed==incoming.candidateSeed?old:incoming;}
    public JobOffer RoadsideAt(int pickup){return Array.Find(Roadside,o=>o!=null&&o.pickup==pickup);}
    public bool TryClaim(JobOffer offer,int driver){
        var pool=Array.IndexOf(Offers,offer)>=0?Offers:Roadside;
        int slot=Array.IndexOf(pool,offer);
        if(offer==null||slot<0||!ledger.TryClaim(offer.pickup,driver))return false;
        pool[slot]=null;retryAt=0;Replenish();return true;
    }
    public void Release(int pickup,int driver){
        if(!ledger.IsClaimed(pickup)||!ledger.Available(pickup,driver))return;
        ledger.Release(pickup,driver);retryAt=0;resting[pickup]=Time.time+60;Replenish();
    }
}
