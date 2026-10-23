"""Review game-only layby geometry against the canonical project scene.

No field legality is inferred. This creates fictional asphalt bays and explicit
sidewalk extensions on ground clear of buildings/services/water, connected to
the existing game sidewalk; it never edits the canonical project map/input.
Run with --write to refresh both catalog copies, otherwise validate the saved
catalog and print its geometry evidence. Python standard library only.
"""
import argparse
import collections
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "config/maps/project/eungye.project.json"
CATALOG = ROOT / "config/maps/eungye-passenger-stops.json"
RESOURCE = ROOT / "unity/YeongtongTaxi/Assets/Resources/eungye-passenger-stops.json"


def point(p): return (p["x"], p["z"])
def add(p, d, t): return (p[0] + d[0] * t, p[1] + d[1] * t)
def distance(a, b): return math.hypot(a[0] - b[0], a[1] - b[1])
def segment(p, a, b):
    d = (b[0] - a[0], b[1] - a[1])
    t = max(0, min(1, ((p[0] - a[0]) * d[0] + (p[1] - a[1]) * d[1]) / max(1e-10, d[0] ** 2 + d[1] ** 2)))
    return distance(p, add(a, d, t))


def inside(p, ring):
    result = False
    for a, b in zip(ring, ring[1:] + ring[:1]):
        if segment(p, a, b) < 1e-6: return True
        if (a[1] > p[1]) != (b[1] > p[1]) and p[0] < (b[0] - a[0]) * (p[1] - a[1]) / (b[1] - a[1]) + a[0]: result = not result
    return result


def overlaps(a, b):
    if inside(a[0], b) or inside(b[0], a): return True
    def cross(p, q, r): return (q[0]-p[0])*(r[1]-p[1])-(q[1]-p[1])*(r[0]-p[0])
    for p, q in zip(a, a[1:]+a[:1]):
        for r, s in zip(b, b[1:]+b[:1]):
            if max(p[0],q[0]) < min(r[0],s[0]) or max(r[0],s[0]) < min(p[0],q[0]) or max(p[1],q[1]) < min(r[1],s[1]) or max(r[1],s[1]) < min(p[1],q[1]): continue
            if cross(p,q,r)*cross(p,q,s) <= 0 and cross(r,s,p)*cross(r,s,q) <= 0: return True
    return False


def rectangle(p, d, width, length):
    side = (d[1], -d[0])
    return [add(add(p,d,a*length/2),side,b*width/2) for a,b in ((-1,-1),(-1,1),(1,1),(1,-1))]


class Index:
    def __init__(self): self.cells = collections.defaultdict(list)
    def put(self, item, ring, pad=0):
        for x in range(math.floor((min(p[0] for p in ring) - pad) / 32), math.floor((max(p[0] for p in ring) + pad) / 32) + 1):
            for z in range(math.floor((min(p[1] for p in ring) - pad) / 32), math.floor((max(p[1] for p in ring) + pad) / 32) + 1): self.cells[x, z].append(item)
    def at(self, p): return self.cells[math.floor(p[0] / 32), math.floor(p[1] / 32)]


class Review:
    def __init__(self, package):
        self.package = package
        self.doc = package["document"]
        self.base = package["baseline"]
        self.lanes = {l["id"]: l for l in self.doc["lanes"]}
        self.roads, self.walks, self.blockers = Index(), Index(), Index()
        for o in self.doc["scene"]["objects"]:
            if not o.get("enabled", True): continue
            ring = [point(p) for p in o["points"]]
            if o["kind"] == "road":
                for a, b in zip(ring, ring[1:]):
                    item = (a, b, o["width"] / 2)
                    self.roads.put(item, [a, b], o["width"] / 2 + .2)
            elif o["kind"] == "sidewalk": self.walks.put((o["id"], ring), ring)
            elif o["kind"] in ("building", "water", "parking", "restaurant", "rest"): self.blockers.put((o["id"], ring), ring, 1)
        self.junctions = [point(j) for j in self.base["junctionRules"]]
        self.crossings = [point(c) for c in self.base.get("osmCrossings", [])] + [point(c) for c in self.doc["facilities"] if c["kind"] in ("crosswalk", "stopLine")]
        self.reverse = collections.defaultdict(list)
        for lane in self.lanes.values():
            for nxt in lane["next"]: self.reverse[nxt].append(lane["id"])
        seed = self.base["hdEdges"][self.base["stops"][0]["laneEdge"]]["id"]
        def reachable(reverse):
            seen, todo = {seed}, [seed]
            while todo:
                cur = todo.pop()
                for nxt in (self.reverse[cur] if reverse else self.lanes[cur]["next"]):
                    if nxt not in seen and not self.lanes[nxt]["disabled"]: seen.add(nxt); todo.append(nxt)
            return seen
        self.connected = reachable(False) & reachable(True)

    def road(self, p, margin=0): return any(segment(p, a, b) <= half + margin for a, b, half in self.roads.at(p))
    def walk(self, p): return next((name for name, ring in self.walks.at(p) if inside(p, ring)), None)
    def blocked(self, p): return any(inside(p, ring) or any(segment(p, a, b) < .5 for a, b in zip(ring, ring[1:] + ring[:1])) for _, ring in self.blockers.at(p))
    def footprint_blocked(self, footprint):
        checked = set()
        for x in range(math.floor(min(p[0] for p in footprint)/32), math.floor(max(p[0] for p in footprint)/32)+1):
            for z in range(math.floor(min(p[1] for p in footprint)/32), math.floor(max(p[1] for p in footprint)/32)+1):
                for name, ring in self.blockers.cells[x,z]:
                    if name not in checked and overlaps(footprint, ring): return True
                    checked.add(name)
        return False
    def clear(self, p): return not self.road(p, -.03) and not self.blocked(p)

    def sample(self, lane, at):
        pts = [point(p) for p in lane["points"]]
        for a, b in zip(pts, pts[1:]):
            length = distance(a, b)
            if at <= length:
                d = ((b[0] - a[0]) / length, (b[1] - a[1]) / length)
                return add(a, d, at), d
            at -= length
        return pts[-1], ((pts[-1][0] - pts[-2][0]) / distance(pts[-1], pts[-2]), (pts[-1][1] - pts[-2][1]) / distance(pts[-1], pts[-2]))

    def check(self, c):
        lane = self.lanes.get(c["laneId"])
        if lane is None or lane["disabled"] or lane["turn"] or lane["change"]: return "lane"
        if lane["id"] not in self.connected: return "disconnected"
        pos, direction = self.sample(lane, c["laneAt"])
        if distance(pos, (c["x"], c["z"])) > .3: return "lane geometry changed"
        side = (direction[1], -direction[0])
        bay = add(pos, side, c["bayOffset"])
        if min(distance(bay, j) for j in self.junctions) < 38: return "junction"
        if self.crossings and min(distance(bay, p) for p in self.crossings) < 25: return "crosswalk"
        if any(distance(bay, point(s)) < 55 for s in self.base["stops"]): return "existing stop"
        for along in (-10, -8, -6, -4, -2, 0, 2, 4, 6, 8, 10):
            q, d = self.sample(lane, c["laneAt"] + along)
            if d[0] * direction[0] + d[1] * direction[1] < .995: return "bend"
            for across in (-1.35, 0, 1.35):
                if not self.clear(add(add(bay, direction, along), side, across)): return "bay occupied"
        passenger = (c["passengerX"], c["passengerZ"])
        for center, width, length in ((bay, 2.8, 20), (passenger, 1.9, 28), (add(pos,side,c["bayOffset"]/2),c["bayOffset"]+3,22)):
            if self.footprint_blocked(rectangle(center,direction,width,length)): return "polygon obstacle overlap"
        lo, hi = 2, c["bayOffset"] + 3.35
        for a in (-13,13):
            if self.footprint_blocked(rectangle(add(add(pos,direction,a),side,(lo+hi)/2),direction,hi-lo,2)): return "connector overlap"
        # A game sidewalk extension is constructed here, attached to the existing
        # walkway at the pocket ends. It must fit entirely on unobstructed land.
        def within(q): return 3 < q[0] < self.base["width"] - 3 and 3 < q[1] < self.base["depth"] - 3
        for a in (-14, -12, -10, -8, -4, 0, 4, 8, 10, 12, 14):
            for b in (-.9, 0, .9):
                q = add(add(passenger, direction, a), side, b)
                if not within(q) or self.road(q, -.03) or self.blocked(q): return "sidewalk"
        if not any(self.walk(add(add(pos, direction, a), side, b)) for a in (-12, 12) for b in (2, 3, 4, 5)): return "no existing sidewalk connection"
        if not self.road(pos): return "approach asphalt"
        # Driver reaches the bay across clear ground from this outer lane, no sidewalk crossing.
        for along in (-11, -8, 0, 8, 11):
            for across in (-1.5, 0, .5, 1, 1.5, 2, 2.5, c["bayOffset"], c["bayOffset"] + 1.5):
                q = add(add(pos, direction, along), side, across)
                if not within(q) or self.blocked(q): return "entry obstruction"
        for along in (-14, -13, -12, 12, 13, 14):
            for across in (2, 3, 4, 5, c["bayOffset"] + 3.35):
                q = add(add(pos, direction, along), side, across)
                if not within(q) or self.road(q, -.03) or self.blocked(q): return "walk connector"
        return None

    def generate(self):
        options = []
        for lane in self.lanes.values():
            if lane["disabled"] or lane["turn"] or lane["change"]: continue
            length = sum(distance(point(a), point(b)) for a, b in zip(lane["points"], lane["points"][1:]))
            for at in range(28, int(length) - 27, 12):
                pos, d = self.sample(lane, at)
                if min(distance(pos, j) for j in self.junctions) < 38: continue
                for offset in (3.2, 3.5, 3.8):
                    side = (d[1], -d[0])
                    bay = add(pos, side, offset)
                    for p_offset in (2.4,):
                        passenger = add(bay, side, p_offset)
                        if self.blocked(passenger): continue
                        c = dict(laneId=lane["id"], laneAt=at, x=pos[0], z=pos[1], dx=d[0], dz=d[1], bayOffset=offset, passengerX=passenger[0], passengerZ=passenger[1], sidewalkId="game-extension:"+lane["id"])
                        if not self.check(c): options.append(c); break
                    if options and options[-1]["laneId"] == lane["id"] and options[-1]["laneAt"] == at: break
        # Farthest-point selection spreads independent bays across the district.
        chosen = []
        while options and len(chosen) < 12:
            score = lambda c: min([distance((c["x"], c["z"]), (a["x"], a["z"])) for a in chosen] or [distance((c["x"], c["z"]), (700, 800))])
            best = max(options, key=score)
            if chosen and score(best) < 90: break
            chosen.append(best)
            options = [c for c in options if distance((c["x"], c["z"]), (best["x"], best["z"])) > 90]
        for i, c in enumerate(chosen):
            c.update(stopId=f"EG-GAME-{i+1:02}", name=f"게임 승하차 {i+1}", category="outer-link" if c["z"] < 400 or c["z"] > 1300 else "neighborhood", evidenceStatus="game-only-field-unverified", evidence="Canonical project polygon review: fictional asphalt layby and continuous sidewalk extension; no real facility or legal stopping claim.")
        return dict(schema=1, mapId="eungye", mapRevision=self.package["contentHash"], revision="eungye-game-laybys-2026-10-23-v1", candidates=chosen)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--write", action="store_true")
    parser.add_argument("--report", type=Path, help="optional ignored artifact JSON evidence report")
    args = parser.parse_args()
    review = Review(json.loads(SOURCE.read_text(encoding="utf-8-sig")))
    if args.write:
        data = review.generate()
        assert len(data["candidates"]) >= 4, "No useful safe game layby catalog; do not publish uncertain curb points"
        if CATALOG.exists():
            previous = json.loads(CATALOG.read_text(encoding="utf-8-sig"))["candidates"]
            retained = {(c["laneId"], c["laneAt"]): (c["stopId"],c["name"]) for c in previous}
            next_id = max(int(c["stopId"].split("-")[-1]) for c in previous) + 1
            for c in data["candidates"]:
                key = c["laneId"], c["laneAt"]
                if key in retained: c["stopId"], c["name"] = retained[key]
                else:
                    c["stopId"],c["name"] = f"EG-GAME-{next_id:02}",f"게임 승하차 {next_id}"
                    next_id += 1
        payload = json.dumps(data, ensure_ascii=False, indent=2) + "\n"
        CATALOG.write_text(payload, encoding="utf-8")
        RESOURCE.write_text(payload, encoding="utf-8")
    data = json.loads(CATALOG.read_text(encoding="utf-8-sig"))
    assert RESOURCE.read_bytes() == CATALOG.read_bytes(), "runtime/config mismatch"
    ids = set()
    for c in data["candidates"]:
        assert c["stopId"] not in ids
        ids.add(c["stopId"])
        issue = review.check(c)
        assert not issue, (c["stopId"], issue)
        assert c["evidenceStatus"] == "game-only-field-unverified"
        print(f"{c['stopId']} lane={c['laneId']} at={c['laneAt']} bayOffset={c['bayOffset']} sidewalk={c['sidewalkId']}")
    print(f"PASSENGER_STOP_GEOMETRY_OK candidates={len(ids)} existing=12 revision={data['revision']} sha256={hashlib.sha256(CATALOG.read_bytes()).hexdigest()}")
    if args.report:
        report = dict(catalogRevision=data["revision"], mapRevision=data["mapRevision"], evidenceStatus="game-only-field-unverified", existingStopCount=len(review.base["stops"]), additionalStopCount=len(ids), pedestrianStripWidth=1.9, manualDrivingVerified=False, fieldLegalityVerified=False, candidates=[])
        for c in data["candidates"]:
            bay = (c["x"]+c["dz"]*c["bayOffset"],c["z"]-c["dx"]*c["bayOffset"])
            report["candidates"].append(dict(stopId=c["stopId"],laneId=c["laneId"],laneAt=c["laneAt"],bayX=bay[0],bayZ=bay[1],minJunctionCenterDistance=min(distance(bay,j) for j in review.junctions),minCrossingCenterDistance=min(distance(bay,j) for j in review.crossings),directedMainNetworkRoundtrip=c["laneId"] in review.connected,geometryIssue=review.check(c)))
        args.report.parent.mkdir(parents=True,exist_ok=True)
        args.report.write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")


if __name__ == "__main__": main()
