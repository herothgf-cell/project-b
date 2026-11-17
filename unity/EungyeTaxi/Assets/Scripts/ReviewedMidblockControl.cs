using System;
using System.Linq;
using UnityEngine;

public static class ReviewedMidblockControl
{
    public const string Kind = "midblockControl";
    public const string Type = "신호 교차로";

    public static HybridJunctionRule[] Rules(HybridJunctionRule[] existing, RoadMapDocument document)
    {
        var rules = existing ?? Array.Empty<HybridJunctionRule>();
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
