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
 // Traced against the r49 source-only captures and the matching overhead views.
 // IDs are individual NGII polylines. The longer J7973 east kerb and the
 // source median on each side street remain where they supply the road edge or centre.
 static readonly HashSet<string> reviewedYellow=new HashSet<string>(StringComparer.Ordinal){
  // J7973 west: old channelising forks and disconnected median tips at the stop.
  "B2218G610890","B222BC004831","B2218G610936","B2218G610313",
  "B2218G610988","B2218G613334","B2218G610995",
  "B222BC004110","B222BC004111","B2218G613341",
  // J7973 east: the channelising forks cut the new islands and cross the stop bar.
  "B2218G610878","B2218G610877","B2218G610876","B222BC004099","B2218G610869",
  "B2218G613328","B222BC004418","B222BC004299","B2218G610445","B2218G610446",
  "B222BC004742",
  // J7973 south: old slip entry and exit run through the crossings and lane interiors.
  "B2218G610439","B2218G610440","B2218G610441","B2218G611003","B222BC004113",
  "B222BC004117","B222BC004118","B222BC004119",
  // J866 west/east: short imported kerb and median pieces among the repainted lanes.
  "B2218G612700","B2218G610361","B222BC004821","B222BC004820",
  "B2218G610430","B222BC004273","B222BC004267",
  // J866 north: two source divider tips project onto the sidewalk at the corners.
  "B2218G610358","B2218G610360",
  // J866 south: the generated approach already supplies both edges and its centre.
  "B2218G612709","B222BC004818","B222BC004819","B2218G611089","B2218G612712",
  "B2218G612710","B2218G612711","B2218G611088"
 };
 public static bool Hides(MapData map,HdMark mark){
  if(map==null||map.mapId!="eungye"||mark==null)return false;
  if(eungye.Contains(mark.id)||reviewedYellow.Contains(mark.id))return true;
  // The import splits this one survey ID into a stop-side piece (x=774m)
  // and a separate upstream piece (x=752m). Only the first was visible here.
  return mark.id=="B2218G613337"&&mark.points!=null&&mark.points.Length>0&&mark.points[0].x>770;
 }
}
