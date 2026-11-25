#!/usr/bin/env bash
# Player regressions after a map review batch: 77-junction audit, ground poles in lanes, district traffic.
# Usage: bash tools/map_review_regress.sh <exe> <out dir>
set -u
E=$1; O=$2; rm -rf "$O"; mkdir -p "$O/game" "$O/dump"
timeout 1500 "$E" -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditSettle 1 -mapAuditObserve 1 -mapAuditOutput "$(cygpath -w "$O/game")" -batchmode -logFile "$O/game/player.log" >/dev/null 2>&1; echo audit=$?
grep SUMMARY "$O/game/map-audit.txt"
timeout 900 "$E" -singlePlayer -contentTest -dayLightDayProbe -mapAudit -mapAuditLimit 0 -mapAuditDump "1050,875,2000" -mapAuditOutput "$(cygpath -w "$O/dump")" -batchmode -logFile "$O/dump/player.log" >/dev/null 2>&1; echo dump=$?
PYTHONIOENCODING=utf-8 python tools/map_poles_in_lanes.py "$O/dump/map-audit.txt"
timeout 900 "$E" -singlePlayer -contentTest -districtTrafficTest -batchmode -logFile "$O/district.log" >/dev/null 2>&1; echo district=$?
cp artifacts/district-traffic-test.txt "$O/district.txt"
grep -a "^FAIL" "$O/district.txt" | cut -c1-60
grep -a -A1 "^STALLED" "$O/district.txt" | grep -a -o "stop=[^ ]*.*signal=[A-Za-z]*" | sed 's/ahead.*signalized=True//'
