using System;
using System.Collections.Generic;

// Explicit, reviewed list of NGII source markings that are kept as map data but
// not painted, because a reviewed game carriageway already paints that stretch.
// Never infer this from proximity: each id is listed with its reason.
public static class ReviewedMarkSuppression {
 // J7973_3744 north stem (Eungye-ro between the stop line and ~12m north).
 // May 2026 panorama F3LdEIaemBy-g7GXluq28Q shows three lanes per direction
 // on this stem; the driven graph (local:8017/8074/8131 northbound,
 // local:8032/7975/7918 southbound) paints those lanes, a single yellow
 // centre and the reviewed stop line. The older NGII stub draws a second,
 // offset set (3.5m dividers, double yellow, yellow kerb hooks and a
 // slip-road stop line) that does not match those lanes. Stop line
 // B2218G610880 is left to ReplacedStopLineMask, junction guides (525) stay.
 public const string J7973NorthEvidence="https://map.naver.com/p?c=19.00,0,0,0,adh&p=F3LdEIaemBy-g7GXluq28Q,0,-16,80,Float";
 public static readonly string[] J7973North={
  "B2218G612771","B2218G612774","B2218G610881","B222BC004833", // offset double yellow centre
  "B2218G612772","B2218G612773","B2218G612776",                // southbound 3.5m dividers
  "B2218G612775","B222BC004832","B222BC004835",                // northbound dashed divider
  "B2218G610882","B2218G610883","B2218G610885","B2218G610886","B2218G610887","B2218G610888","B2218G610884", // NW kerb hooks/slip stop
  "B2218G612768","B2218G612769","B2218G612770","B222BC004834"  // NE kerb hooks
 };
 static readonly HashSet<string> eungye=new HashSet<string>(J7973North,StringComparer.Ordinal);
 public static bool Hides(MapData map,HdMark mark){return map!=null&&mark!=null&&map.mapId=="eungye"&&eungye.Contains(mark.id);}
}
