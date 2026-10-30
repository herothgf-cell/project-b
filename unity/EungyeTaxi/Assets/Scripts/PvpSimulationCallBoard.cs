using System;
using System.Collections.Generic;
using UnityEngine;

// A single stable array is displayed and selected by every participant.
// Accepted offers are removed by identity, then replaced with new native jobs.
public sealed class PvpSimulationCallBoard {
    public const int Capacity=8;
    public JobOffer[] Offers {get;private set;}
    readonly PvpSimulationLedger ledger;
    readonly Func<int,JobOffer> create;
    readonly Dictionary<int,float> resting=new Dictionary<int,float>();
    int serial;
    readonly int attemptsPerPass;
    public PvpSimulationCallBoard(PvpSimulationLedger ledger,Func<int,JobOffer> create):this(ledger,create,32){}
    public PvpSimulationCallBoard(PvpSimulationLedger ledger,Func<int,JobOffer> create,int stopCount) {
        if(ledger==null||create==null)throw new ArgumentNullException();
        this.ledger=ledger;this.create=create;attemptsPerPass=Mathf.Max(Capacity,stopCount*2);
        Offers=new JobOffer[Capacity];Replenish();
    }
    public bool Contains(JobOffer offer){return offer!=null&&Array.IndexOf(Offers,offer)>=0;}
    public void Replenish(){
        if(ledger.Finished)return;
        int budget=attemptsPerPass;
        for(int slot=0;slot<Offers.Length;slot++){
            if(Offers[slot]!=null)continue;
            while(budget-->0){
                int id=serial++;var offer=create(id);if(offer==null||offer.pickup==offer.drop||float.IsInfinity(offer.distance)||float.IsNaN(offer.distance)||ledger.IsClaimed(offer.pickup))continue;
                float until;if(resting.TryGetValue(offer.pickup,out until)&&Time.time<until)continue;
                bool duplicate=false;foreach(var prior in Offers)if(prior!=null&&prior.pickup==offer.pickup){duplicate=true;break;}
                if(duplicate)continue;
                offer.candidateSeed="shared-call:"+id;Offers[slot]=offer;break;
            }
        }
    }
    public bool TryClaim(JobOffer offer,int driver){
        int slot=Array.IndexOf(Offers,offer);
        if(offer==null||slot<0||!ledger.TryClaim(offer.pickup,driver))return false;
        Offers[slot]=null;Replenish();return true;
    }
    public void Release(int pickup,int driver){
        if(!ledger.IsClaimed(pickup)||!ledger.Available(pickup,driver))return;
        ledger.Release(pickup,driver);resting[pickup]=Time.time+60;Replenish();
    }
}
