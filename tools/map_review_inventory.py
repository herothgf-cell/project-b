"""Refresh the complete Eungye junction review ledger without losing observations.

Requires pyproj. Reads generated inputs; never rewrites source maps.
"""
import json
import math
from pathlib import Path
from pyproj import Transformer

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'config/maps/eungye-intersection-review.json'


def refresh():
    data = json.loads((ROOT / 'unity/EungyeTaxi/Assets/Resources/eungye-hybrid-map.json').read_text(encoding='utf-8-sig'))
    old = json.loads(OUTPUT.read_text(encoding='utf-8-sig')) if OUTPUT.exists() else {}
    previous = {j['id']: j for j in old.get('junctions', [])}
    transformer = Transformer.from_crs(5186, 4326, always_xy=True)
    settings = json.loads((ROOT / 'config/maps/eungye.json').read_text(encoding='utf-8-sig'))
    x0, z0 = settings['bounds'][:2]
    configured = json.loads((ROOT / 'config/maps/eungye-traffic-overrides.json').read_text(encoding='utf-8-sig'))
    configured_ids = {j['id'] for j in configured['junctions']}
    entries = []
    for rule in data['junctionRules']:
        entry = previous.get(rule['id'], {})
        lon, lat = transformer.transform(x0 + rule['x'], z0 + rule['z'])
        approaches = {}
        for edge in data['hdEdges']:
            if edge.get('disabled') or edge.get('change') or edge['id'].startswith('join:'):
                continue
            joins = [data['hdEdges'][i] for i in edge['next'] if data['hdEdges'][i]['id'].startswith('join:') and data['hdEdges'][i].get('ruleId') == rule['id']]
            if not joins:
                continue
            a, b = edge['points'][-2:]
            bearing = (math.degrees(math.atan2(b['x']-a['x'], b['z']-a['z'])) + 180) % 360
            side = ['N', 'NE', 'E', 'SE', 'S', 'SW', 'W', 'NW'][int((bearing + 22.5) // 45) % 8]
            arm = approaches.setdefault(side, dict(direction=side, sourceId=edge['sourceId'], lanes=[], status='unreviewed', observations=[]))
            arm['lanes'].append(dict(id=edge['id'], family=edge['toNode'].removesuffix(':end'), number=edge['number'], graphTurns=''.join(letter for letter, turn in [('L', -1), ('S', 0), ('R', 1)] if any(j['turn'] == turn for j in joins))))
        old_arms = {a['direction']: a for a in entry.get('approaches', [])}
        for arm in approaches.values():
            arm['lanes'].sort(key=lambda lane: lane['number'])
            saved = old_arms.get(arm['direction'], {})
            for key in ('status', 'observations', 'checkedAt', 'panoramas', 'captureMonth', 'remaining', 'applied'):
                if key in saved:
                    arm[key] = saved[key]
        entries.append(dict(id=rule['id'], x=rule['x'], z=rule['z'], latitude=round(lat, 7), longitude=round(lon, 7), searchUrl=f'https://map.naver.com/p/search/{lat:.7f}%2C%20{lon:.7f}', configured=rule['id'] in configured_ids, classification='routable-junction' if approaches else 'no-active-join', approaches=list(approaches.values())))
    result = dict(schemaVersion=1, coordinateSystem='NGII EPSG:5186 + config bounds origin; pyproj EPSG:4326', source='eungye-hybrid-map.json unchanged baseline; runtime reviewed pockets recorded separately', note='Graph turns are provisional game movements, never field verification. No-active-join entries remain in the inventory.', junctions=entries)
    OUTPUT.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f"junctions={len(entries)} routable={sum(bool(e['approaches']) for e in entries)} approaches={sum(len(e['approaches']) for e in entries)}")


if __name__ == '__main__':
    refresh()
