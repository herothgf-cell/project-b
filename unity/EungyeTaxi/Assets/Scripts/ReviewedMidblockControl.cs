using System;
using System.Linq;
using UnityEngine;

public static class ReviewedMidblockControl
{
    public const string Kind = "midblockControl";
    public const string Type = "신호 교차로";

    // 2026-11-27 map review: a junction the source map lacks (a new road crossing or meeting an existing one).
    // Its rule has an approach for every heading in the marker's `axis` bit mask (1 = +heading, 2 = -heading,
    // 4 = +right, 8 = -right; 0 = all four): width = carriageway across the heading axis, length = across the
    // other axis. text "신호" makes it signalised (game signal program), anything else unsignalised with stop lines.
    public const string JunctionType = "검토 교차로";

    public static HybridJunctionRule[] Rules(HybridJunctionRule[] existing, RoadMapDocument document)
    {
        var rules = existing ?? Array.Empty<HybridJunctionRule>();
        var added = document.facilities.Where(f => f != null && f.kind == Kind && f.type == JunctionType).Select(marker =>
        {
            var direction = Quaternion.Euler(0, marker.yaw, 0) * Vector3.forward;
            var across = TrafficSystem.Right(direction);
            int mask = marker.axis == 0 ? 15 : marker.axis;
            var arms = new[] { direction, -direction, across, -across };
            return new HybridJunctionRule
            {
                id = marker.junctionId, x = marker.x, z = marker.z, dx = direction.x, dz = direction.z,
                status = "provisional", signalMode = marker.text == "신호" ? 2 : 0, crosswalk = true, stopLine = true,
                facilityStatus = "verified", facilityEvidence = marker.evidence?.url, facilityCheckedAt = marker.evidence?.checkedAt,
                approaches = Enumerable.Range(0, 4).Where(i => (mask & (1 << i)) != 0).Select(i =>
                    new HybridApproach { dx = arms[i].x, dz = arms[i].z, width = i < 2 ? marker.width : marker.length }).ToArray()
            };
        }).ToArray();
        if (added.Length > 0) rules = rules.Concat(added).ToArray();
        var markers = document.facilities.Where(f => f != null && f.kind == Kind && f.type == Type).ToArray();
        if (markers.Length == 0) return rules;
        return rules.Concat(markers.Select(marker =>
        {
            var direction = Quaternion.Euler(0, marker.yaw, 0) * Vector3.forward;
            return new HybridJunctionRule
            {
                id = marker.junctionId, x = marker.x, z = marker.z, dx = direction.x, dz = direction.z,
                status = "provisional", signalMode = 2, crosswalk = true, stopLine = true,
                facilityStatus = "verified", facilityEvidence = marker.evidence?.url, facilityCheckedAt = marker.evidence?.checkedAt,
                approaches = new[] { direction, -direction }.Select(d =>
                    new HybridApproach { dx = d.x, dz = d.z, width = marker.width }).ToArray()
            };
        })).ToArray();
    }
}
