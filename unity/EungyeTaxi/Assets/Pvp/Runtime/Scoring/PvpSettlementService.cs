using System;
namespace Pvp.Competition {
public static class PvpSettlementService {
 // All validation/arithmetic completes before the ledger and terminal passenger stores.
 public static bool TryComplete(PvpPassengerView passenger,PvpScoreLedger ledger,int tick){
  if(passenger.Status==PvpPassengerStatus.Completed)return ledger.HasTrip(passenger.TripId);
  if(passenger.Status!=PvpPassengerStatus.Onboard)return false;
  if(!ledger.Complete(passenger.Owner,passenger.TripId,passenger.QuotedFare,tick))throw new InvalidOperationException("Trip/ledger mismatch");
  passenger.Status=PvpPassengerStatus.Completed;passenger.Revision++;passenger.Reason="정산 완료";return true;
 }
}
}
