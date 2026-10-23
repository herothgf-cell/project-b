"""Validate NGII NGI/NDA/XML packages and export one local, planar driving map.

No reprojection or automatic datum inference is performed. Use `--help` for CLI.
Only region-independent planar geometry helpers are shared.
"""
import argparse
import csv
import hashlib
import io
import json
import math
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter
from pathlib import Path

from map_geometry import clip_segment, densify, triangulate
from ngii_shapefile import archive_metadata, xml_metadata, parse_archive

ROOT = Path(__file__).resolve().parents[1]


class ImportFailure(ValueError):
    """An input needs correction or review before it can become a map."""


def require(condition, message):
    if not condition:
        raise ImportFailure(message)


def inventory(raw):
    """Inventory every basename, including incomplete/orphan packages, recursively."""
    raw = Path(raw)
    groups = {}
    for path in sorted(raw.rglob('*')):
        if path.is_file() and path.suffix.lower() in ('.ngi', '.nda', '.xml', '.zip'):
            key = str(path.with_suffix('').relative_to(raw))
            groups.setdefault(key, {})[path.suffix.lower()] = path
    result = []
    for stem, files in sorted(groups.items()):
        item = {'package': stem, 'tile': None, 'metadata': {}, 'files': [], 'errors': []}
        is_shp = '.zip' in files
        item['format'] = 'SHP-ZIP' if is_shp else 'NGI-NDA'
        for suffix in (('.zip', '.xml') if is_shp else ('.ngi', '.nda', '.xml')):
            if suffix not in files:
                item['errors'].append(f'Missing {suffix} companion for {stem}')
        for suffix, path in sorted(files.items()):
            item['files'].append({'path': path.relative_to(raw).as_posix(), 'type': suffix,
                                  'bytes': path.stat().st_size, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
        if '.xml' in files:
            try:
                if is_shp:
                    item.update(archive_metadata(files['.zip']))
                    item['metadata'], item['metadataWarning'] = xml_metadata(files['.xml'])
                    require(item['tile'] == item['metadata']['도엽번호'], 'XML tile differs from SHP boundary')
                else:
                    xml = ET.parse(files['.xml']).getroot()
                    item['metadata'] = {node.tag.split('}')[-1]: (node.text or '').strip() for node in xml.iter() if len(node) == 0}
                item['tile'] = item['metadata'].get('도엽번호')
                if not item['tile']:
                    item['errors'].append('XML has no 도엽번호 tile ID')
            except (ET.ParseError, UnicodeError, ValueError) as exc:
                item['errors'].append(f'Invalid XML: {exc}')
        result.append(item)
    return result


def read_layers(path):
    text = path.read_text(encoding='cp949', errors='strict')
    layers = {}
    require('<LAYER_START>' in text, f'{path.name}: no NGII layers')
    for block in text.split('<LAYER_START>')[1:]:
        match = re.search(r'\$LAYER_NAME\s+"([^"]+)"', block)
        require(match is not None and '<DATA>' in block, f'{path.name}: malformed layer')
        name = match[1]
        require(name not in layers, f'{path.name}: duplicate layer {name}')
        header, data = block.split('<DATA>', 1)
        require('<END>' in data, f'{path.name}/{name}: missing DATA terminator')
        records = {}
        for rid, body in re.findall(r'\$RECORD\s+(\d+)\s*\n(.*?)(?=\$RECORD|\Z)', data.split('<END>', 1)[0], re.S):
            require(rid not in records, f'{path.name}/{name}: duplicate record {rid}')
            records[rid] = body.strip()
        layers[name] = (header, records)
    return layers


def geometry(body, context):
    lines = [line.strip() for line in body.splitlines() if line.strip()]
    require(bool(lines), f'{context}: empty geometry')
    kind = lines[0].split('(')[0]

    def point(line):
        values = tuple(float(v) for v in line.split())
        require(len(values) == 2 and all(math.isfinite(v) for v in values), f'{context}: expected finite 2D coordinates')
        return values

    try:
        if kind in ('POINT', 'TEXT'):
            parts = [[point(lines[1])]]
        elif kind == 'LINESTRING':
            count = int(lines[1])
            require(count >= 2, f'{context}: short line')
            parts = [[point(lines[i]) for i in range(2, 2 + count)]]
        elif kind == 'POLYGON':
            require(lines[1].startswith('NUMPARTS '), f'{context}: expected NUMPARTS')
            count = int(lines[1].split()[1])
            require(count > 0, f'{context}: empty polygon')
            parts, pos = [], 2
            for _ in range(count):
                size = int(lines[pos])
                require(size >= 3, f'{context}: short polygon ring')
                pos += 1
                parts.append([point(lines[i]) for i in range(pos, pos + size)])
                pos += size
        else:
            raise ImportFailure(f'{context}: unsupported geometry {kind}')
    except (IndexError, ValueError) as exc:
        if isinstance(exc, ImportFailure):
            raise
        raise ImportFailure(f'{context}: malformed geometry: {exc}') from exc
    return kind, parts


def parse_package(raw, package):
    paths = {f['type']: Path(raw) / f['path'] for f in package['files']}
    if package.get('format') == 'SHP-ZIP':
        try:
            return parse_archive(paths['.zip'], package['tile']), package['bounds']
        except ValueError as exc:
            raise ImportFailure(str(exc)) from exc
    ngi, nda = read_layers(paths['.ngi']), read_layers(paths['.nda'])
    require(set(ngi) == set(nda), f"{package['tile']}: NGI/NDA layers differ")
    features = []
    for name, (header, records) in ngi.items():
        attr_header, attr_records = nda[name]
        require(set(records) == set(attr_records), f"{package['tile']}/{name}: geometry/attribute IDs differ")
        fields = re.findall(r'ATTRIB\("([^"]+)"', attr_header)
        require(len(fields) == len(set(fields)), f'{name}: duplicate attribute columns')
        dim = re.search(r'DIM\((\d+)\)', header)
        require(dim is None or dim[1] == '2', f'{name}: only DIM(2) supported')
        for rid, body in records.items():
            context = f"{package['tile']}:{name}:{rid}"
            rows = list(csv.reader(io.StringIO(attr_records[rid]), skipinitialspace=True))
            require(len(rows) == 1 and len(rows[0]) == len(fields), f'{context}: attribute column count mismatch')
            attributes = dict(zip(fields, (v.strip() for v in rows[0])))
            if 'ID' in attributes:
                require(attributes['ID'] == rid, f'{context}: attribute ID disagrees with record ID')
            kind, parts = geometry(body, context)
            features.append({'id': context, 'record': rid, 'tile': package['tile'], 'layer': name,
                             'kind': kind, 'parts': parts, 'attributes': attributes})
    boundary = [p for f in features if f['layer'] == 'H0010000' for part in f['parts'] for p in part]
    require(bool(boundary), f"{package['tile']}: missing H0010000 sheet boundary; cannot infer crop")
    bounds = [min(p[0] for p in boundary), min(p[1] for p in boundary), max(p[0] for p in boundary), max(p[1] for p in boundary)]
    require(bounds[0] < bounds[2] and bounds[1] < bounds[3], f"{package['tile']}: degenerate boundary")
    return features, bounds


def check_crs(requests, packages):
    definitions = []
    for item in requests:
        crs = item.get('crs', {})
        status = crs.get('status')
        require(status in ('verified', 'provisional'), f"{item['tile']}: CRS definition needs explicit review")
        require(crs.get('evidence', '').strip(), f"{item['tile']}: CRS evidence is required")
        require(crs.get('units') == 'metre', f"{item['tile']}: CRS units must explicitly be metre")
        if len(requests) > 1:
            require(status == 'verified', 'Multi-sheet merge requires verified CRS definitions for every tile; GRS80 alone is insufficient')
        if status == 'verified':
            require(bool(crs.get('definition', '').strip()), f"{item['tile']}: verified CRS requires full definition")
        definitions.append(crs.get('definition', ''))
    require(len(set(definitions)) == 1, 'CRS definitions differ; reproject with a separately reviewed process before importing')
    source_wkts = {p['wkt'] for p in packages if p.get('wkt')}
    require(len(source_wkts) <= 1, 'Source SHP PRJ CRS definitions differ')
    for item, package in zip(requests, packages):
        if package.get('format') == 'SHP-ZIP':
            require(item['crs'].get('definition') == package['metadata']['좌표계구분코드'], 'Configured CRS disagrees with explicit XML EPSG')
            require(item['crs'].get('wkt') == package['wkt'], 'Configured CRS WKT disagrees with source PRJ')
        if package.get('metadataWarning'):
            review = item.get('metadataReview', {})
            require(review.get('status') == 'verified' and review.get('evidence'), f"{item['tile']}: malformed XML requires metadataReview evidence")
    datums = {p['metadata'].get('좌표계구분코드', '') for p in packages}
    require(len(datums) == 1, 'XML datum labels differ across sheets; refusing merge')
    return requests[0]['crs']['status']


def inside(x, z, ring):
    hit = False
    for a, b in zip(ring, ring[1:] + ring[:1]):
        if (a['z'] > z) != (b['z'] > z) and x < (b['x'] - a['x']) * (z - a['z']) / (b['z'] - a['z']) + a['x']:
            hit = not hit
    return hit


def components(roads):
    graph = {}
    for road in roads:
        for a, b in zip(road['points'], road['points'][1:]):
            a, b = (round(a['x'], 3), round(a['z'], 3)), (round(b['x'], 3), round(b['z'], 3))
            if a != b:
                graph.setdefault(a, set()).add(b)
                graph.setdefault(b, set()).add(a)
    unseen, groups = set(graph), []
    while unseen:
        stack = [unseen.pop()]
        group = set(stack)
        while stack:
            for point in graph[stack.pop()]:
                if point in unseen:
                    unseen.remove(point)
                    group.add(point)
                    stack.append(point)
        groups.append(group)
    return graph, sorted(groups, key=lambda group: (-len(group), min(group)))


def make_stops(roads, buildings, width, depth, anchor=None):
    """Synthetic gameplay stops, clear of footprint probes; not legal pickup data."""
    graph, groups = components(roads)
    if not groups:
        return []
    largest = groups[0]
    if anchor is not None:
        key = (round(anchor['x'], 3), round(anchor['z'], 3))
        require(key in graph, 'First target must coincide with an exported road vertex')
        largest = next(group for group in groups if key in group)
    junctions = [p for p in largest if len(graph[p]) >= 3]
    footprint_grid = {}
    for index, building in enumerate(buildings):
        xs, zs = [p['x'] for p in building['points']], [p['z'] for p in building['points']]
        for gx in range(math.floor(min(xs) / 64), math.floor(max(xs) / 64) + 1):
            for gz in range(math.floor(min(zs) / 64), math.floor(max(zs) / 64) + 1):
                footprint_grid.setdefault((gx, gz), []).append(index)
    candidates = []
    for road in roads:
        points = road['points']
        for i in range(1, len(points) - 1):
            p = points[i]
            k = (round(p['x'], 3), round(p['z'], 3))
            if k not in largest or len(graph[k]) != 2 or not (25 < p['x'] < width - 25 and 25 < p['z'] < depth - 25):
                continue
            if any(math.dist(k, j) < 20 for j in junctions):
                continue
            a, b = points[i - 1], points[i + 1]
            dx, dz = b['x'] - a['x'], b['z'] - a['z']
            length = math.hypot(dx, dz)
            if length < .01:
                continue
            dx, dz = dx / length, dz / length
            if road['oneWay'] == -1:
                dx, dz = -dx, -dz
            kerb = road['width'] * .5 - 1.5
            probes = [(p['x'] + dz * (kerb + side) + dx * along, p['z'] - dx * (kerb + side) + dz * along)
                      for side in (-1.1, 1.4, 2.1, 2.7) for along in (-2.3, 0, 2.3)]
            if any(inside(x, z, buildings[index]['points']) for x, z in probes for index in footprint_grid.get((math.floor(x / 64), math.floor(z / 64)), [])):
                continue
            candidates.append(dict(x=p['x'], z=p['z'], dx=dx, dz=dz, width=road['width'], sourceRoad=road['id'], status='gameplay-provisional'))
    selected = []
    while candidates and len(selected) < 8:
        item = max(candidates, key=lambda p: min(math.hypot(p['x'] - q['x'], p['z'] - q['z']) for q in selected)) if selected else candidates[0]
        selected.append(item)
        candidates.remove(item)
    for i, item in enumerate(selected):
        item['name'] = 'Stop ' + chr(65 + i)
    return selected


def number(attributes, field, context, integer=False):
    try:
        value = float(attributes[field])
    except (KeyError, ValueError) as exc:
        raise ImportFailure(f'{context}: missing/invalid {field}') from exc
    require(math.isfinite(value) and value >= 0 and (not integer or value.is_integer()), f'{context}: invalid {field}')
    return int(value) if integer else value


def build_map(config, raw):
    requests = config.get('requiredTiles', [])
    require(bool(requests), 'requiredTiles is empty')
    requested_ids = [str(r['tile']) for r in requests]
    require(len(set(requested_ids)) == len(requested_ids), 'requiredTiles contains duplicate tile IDs')
    packages = inventory(raw)
    selected = []
    for item in requests:
        matching = [p for p in packages if p['tile'] == str(item['tile'])]
        if item.get('package'):
            wanted = Path(item['package']).with_suffix('').as_posix()
            matching = [p for p in matching if Path(p['package']).as_posix() == wanted]
        require(bool(matching), f"Missing required tile {item['tile']}; provide its NGI/NDA/XML package under {raw}")
        require(len(matching) == 1, f"Tile {item['tile']} is ambiguous; select its .ngi relative path using package")
        require(not matching[0]['errors'], '; '.join(matching[0]['errors']))
        selected.append(matching[0])
    coordinate_status = check_crs(requests, selected)
    all_features, tile_bounds = [], []
    for package in selected:
        features, bounds = parse_package(raw, package)
        all_features.extend(features)
        tile_bounds.append(bounds)
        package['bounds'] = bounds
        package['featureCount'] = len(features)
        package['layerCounts'] = dict(Counter(f['layer'] for f in features))
    # Sheets must form one connected coverage set. Bbox contact is a coarse check,
    # not proof of a connected road network; graph diagnostics below report that.
    reached = {0}
    while True:
        adjacent = {i for i, a in enumerate(tile_bounds) if any(a[0] <= tile_bounds[j][2] + 2 and a[2] >= tile_bounds[j][0] - 2 and a[1] <= tile_bounds[j][3] + 2 and a[3] >= tile_bounds[j][1] - 2 for j in reached)}
        if adjacent <= reached:
            break
        reached |= adjacent
    require(len(reached) == len(tile_bounds), 'Requested sheets are not adjacent within 2 native metres; keep distant areas in separate maps')
    bounds = [min(b[0] for b in tile_bounds), min(b[1] for b in tile_bounds), max(b[2] for b in tile_bounds), max(b[3] for b in tile_bounds)]
    if config.get('bounds'):
        crop = config['bounds']
        require(len(crop) == 4 and all(isinstance(v, (float, int)) and math.isfinite(v) for v in crop) and bounds[0] <= crop[0] < crop[2] <= bounds[2] and bounds[1] <= crop[1] < crop[3] <= bounds[3], 'Configured bounds must lie inside combined sheet bounding box')
        bounds = crop

    def local(p):
        return {'x': round(p[0] - bounds[0], 4), 'z': round(p[1] - bounds[1], 4)}

    overrides, ignored = {}, []
    features_by_id = {f['id']: f for f in all_features}
    for override in config.get('reviewedOverrides', []):
        if override.get('status') != 'verified':
            ignored.append(override)
            continue
        require(bool(override.get('evidence', '').strip()), 'Verified override requires evidence')
        key = override.get('feature')
        require(key in features_by_id, f'Override references missing feature {key}')
        require(key not in overrides, f'duplicate override {key}')
        require(set(override) <= {'feature', 'status', 'evidence', 'reviewedAt', 'oneWay', 'lanes', 'name', 'width'}, f'{key}: unsupported override field')
        require(features_by_id[key]['layer'] == 'A0020000' or not set(override) & {'oneWay', 'lanes', 'width'}, f'{key}: traffic override must reference a road')
        if 'oneWay' in override:
            require(type(override['oneWay']) is int and override['oneWay'] in (-1, 0, 1), f'{key}: oneWay must be -1/0/1')
        if 'lanes' in override:
            require(type(override['lanes']) is int and override['lanes'] > 0, f'{key}: lanes must be a positive integer')
        if 'width' in override:
            require(isinstance(override['width'], (int, float)) and math.isfinite(override['width']) and override['width'] > 0, f'{key}: width must be positive')
        overrides[key] = override

    roads, buildings, skipped, skipped_roads = [], [], [], []
    road_policy = config.get('roadPolicy', {})
    if road_policy:
        require(road_policy.get('evidence') and road_policy.get('status') == 'gameplay-provisional', 'Road selection policy needs gameplay-provisional status and evidence')
    for f in all_features:
        attrs, override = f['attributes'], overrides.get(f['id'], {})
        provenance = {'tile': f['tile'], 'layer': f['layer'], 'record': f['record'], 'ufid': attrs.get('UFID', '')}
        if override:
            provenance['override'] = override
        if f['layer'] == 'A0020000':
            require(f['kind'] == 'LINESTRING', f"{f['id']}: road must be LINESTRING")
            if attrs.get('도로구분') in road_policy.get('excludeRoadClasses', []):
                skipped_roads.append({'id': f['id'], 'reason': 'source road class excluded from planar local gameplay', 'roadClass': attrs['도로구분']})
                continue
            width = override.get('width', number(attrs, '도로폭', f['id']))
            if width < road_policy.get('minimumWidth', 0):
                skipped_roads.append({'id': f['id'], 'reason': 'below configured gameplay minimum width', 'width': width})
                continue
            require(width > 0, f"{f['id']}: road width must be positive")
            lanes = number(attrs, '차로수', f['id'], integer=True)
            direction = attrs.get('일방통행', '')
            if direction not in ('양방향', '양방통행') and 'oneWay' not in override and road_policy.get('unresolvedDirections') == 'exclude':
                skipped_roads.append({'id': f['id'], 'reason': 'source one-way sign has no verified vertex-order orientation', 'direction': direction})
                continue
            require(direction in ('양방향', '양방통행') or 'oneWay' in override, f"{f['id']}: unreviewed direction '{direction}'; provide evidenced oneWay override")
            template = {'width': width, 'sourceLanes': lanes, 'oneWay': override.get('oneWay', 0), 'name': override.get('name', attrs.get('도로명', '')),
                        'source': dict(provenance, direction=direction)}
            if 'lanes' in override:
                template['lanes'] = override['lanes']
            chains, chain = [], []
            for a, b in zip(f['parts'][0], f['parts'][0][1:]):
                segment = clip_segment(a, b, bounds)
                if segment and math.dist(*segment) > 1e-8:
                    if chain and math.dist(chain[-1], segment[0]) > 1e-7:
                        chains.append(chain)
                        chain = []
                    if not chain:
                        chain.append(segment[0])
                    chain.append(segment[1])
                elif chain:
                    chains.append(chain)
                    chain = []
            if chain:
                chains.append(chain)
            for index, points in enumerate(chains):
                roads.append(dict(template, id=f"{f['id']}:{index}", points=[local(p) for p in densify(points)]))
        elif f['layer'] == 'B0010000':
            if f['kind'] != 'POLYGON' or len(f['parts']) != 1:
                skipped.append({'id': f['id'], 'reason': 'unsupported multipart/hole or non-polygon'})
                continue
            points = list(f['parts'][0])
            if points[-1] == points[0]:
                points.pop()
            points = [p for i, p in enumerate(points) if p != points[i - 1]]
            if not all(bounds[0] + 2 < x < bounds[2] - 2 and bounds[1] + 2 < y < bounds[3] - 2 for x, y in points):
                skipped.append({'id': f['id'], 'reason': 'touches outer map border; no polygon clipping'})
                continue
            try:
                require(len(points) >= 3, 'degenerate ring')
                triangles = triangulate(points)
                require(bool(triangles), 'no roof triangles')
            except ValueError as exc:
                skipped.append({'id': f['id'], 'reason': str(exc)})
                continue
            floors = number(attrs, '층수', f['id'], integer=True)
            buildings.append({'id': f['id'], 'name': override.get('name', attrs.get('명칭') or attrs.get('주기', '')), 'height': max(3, min(36, floors) * 3),
                              'heightStatus': 'estimated-3m-per-floor', 'points': [local(p) for p in points], 'triangles': triangles, 'source': provenance})
    require(bool(roads), 'No roads exported')
    width, depth = round(bounds[2] - bounds[0], 4), round(bounds[3] - bounds[1], 4)
    targets = []
    for target in config.get('targets', []):
        require(target.get('status') in ('verified', 'gameplay-provisional') and target.get('evidence'), 'Target requires status and evidence')
        p = (target['sourceX'], target['sourceY'])
        require(all(math.isfinite(v) for v in p) and bounds[0] <= p[0] <= bounds[2] and bounds[1] <= p[1] <= bounds[3], f"Target {target['name']} outside map")
        targets.append(dict(name=target['name'], **local(p), leftTurnMode=target.get('leftTurnMode', 0), status=target['status'], evidence=target['evidence']))
    stops = make_stops(roads, buildings, width, depth, targets[0] if targets else None)
    areas = []
    for area in config.get('areas', []):
        require(area.get('status') == 'verified' and area.get('evidence'), 'Area polygon requires verified source evidence')
        feature = features_by_id.get(area.get('feature'))
        require(feature is not None and feature['kind'] == 'POLYGON' and len(feature['parts']) == 1, 'Area requires a known single-ring source polygon')
        points = feature['parts'][0][:-1]
        require(all(bounds[0] <= p[0] <= bounds[2] and bounds[1] <= p[1] <= bounds[3] for p in points), 'Area crosses crop; explicit polygon clipping is required')
        areas.append(dict(id=feature['id'], name=area['name'], kind=area['kind'], points=[local(p) for p in points], triangles=triangulate(points), evidence=area['evidence'], status='verified'))
    landmarks, ignored_landmarks = [], []
    for landmark in config.get('landmarks', []):
        if landmark.get('status') != 'verified':
            ignored_landmarks.append(landmark)
            continue
        require(bool(landmark.get('evidence', '').strip()), 'Verified landmark requires evidence')
        if landmark.get('feature'):
            feature = features_by_id.get(landmark['feature'])
            require(feature is not None, f"Landmark references missing feature {landmark['feature']}")
            points = feature['parts'][0]
            # Bounds centre is a label anchor, not a surveyed entrance.
            p = ((min(v[0] for v in points) + max(v[0] for v in points)) / 2,
                 (min(v[1] for v in points) + max(v[1] for v in points)) / 2)
        else:
            p = (landmark['sourceX'], landmark['sourceY'])
        require(all(math.isfinite(v) for v in p) and bounds[0] <= p[0] <= bounds[2] and bounds[1] <= p[1] <= bounds[3], f"Landmark {landmark['name']} outside map")
        landmarks.append(dict(name=landmark['name'], **local(p), kind=landmark.get('kind', 'landmark'), status='verified', evidence=landmark['evidence']))
    graph, groups = components(roads)
    signatures = Counter(tuple((p['x'], p['z']) for p in r['points']) for r in roads)
    report = {'status': 'passed', 'map': config.get('id', 'unnamed'), 'sourceFeatureCount': len(all_features), 'sourcePackageCount': len(selected),
              'roadCount': len(roads), 'buildingCount': len(buildings), 'stopCount': len(stops), 'areaCount': len(areas), 'bounds': bounds, 'coordinateStatus': coordinate_status,
              'skippedBuildings': skipped, 'skippedRoads': skipped_roads, 'ignoredOverrides': ignored, 'ignoredLandmarks': ignored_landmarks, 'appliedOverrides': list(overrides.values()),
              'roadGraph': {'componentCount': len(groups), 'componentNodeCounts': [len(g) for g in groups], 'nodeCount': len(graph), 'joinToleranceMetres': .001},
              'duplicateRoadGeometries': sum(n - 1 for n in signatures.values() if n > 1),
              'warnings': ['Planar historical source geometry; current traffic restrictions require review.', 'Gameplay stops are synthetic; not verified legal pickup locations.', 'Building height assumes 3m per floor; holes are skipped.', 'Sheet seams are preserved; no snapping, conflation, or guessed turn connectivity.']}
    if coordinate_status == 'provisional':
        report['warnings'].append('Single-sheet local translation only. Projection and metre interpretation remain provisional; do not merge or georeference.')
    if len(groups) > 1:
        report['warnings'].append('Road graph has disconnected components. Review seams and access before playtesting.')
    report['warnings'].extend(p['metadataWarning'] for p in selected if p.get('metadataWarning'))
    if skipped_roads:
        report['warnings'].append('Road selection excludes unresolved one-way directions or narrow source paths. Omitted roads are not fabricated as bidirectional links.')
    result = {'schemaVersion': 2, 'mapId': config.get('id', 'unnamed'), 'source': 'NGII ' + ', '.join(f"{p['tile']} / {p['metadata'].get('제작년도', '?')} / {p['metadata'].get('축척코드', '?')}" for p in selected),
              'sources': selected, 'coordinateStatus': coordinate_status, 'crsReview': [r['crs'] for r in requests],
              'width': width, 'depth': depth, 'originX': bounds[0], 'originY': bounds[1], 'roads': roads, 'buildings': buildings,
              'stops': stops, 'targets': targets, 'areas': areas, 'landmarks': landmarks, 'skippedBuildings': [s['id'] + ': ' + s['reason'] for s in skipped]}
    return result, report


def write_json(path, value):
    """Stage then replace, so validation failures cannot corrupt an existing map."""
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False) + '\n', encoding='utf8')
    temporary.replace(path)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    inspect = sub.add_parser('inventory', help='List available packages, metadata, hashes, and missing companions')
    inspect.add_argument('--raw', type=Path, default=ROOT / 'data/raw/ngii')
    inspect.add_argument('--report', type=Path)
    build = sub.add_parser('build', help='Validate all required sheets then export Unity MapData JSON')
    build.add_argument('--config', type=Path, required=True)
    build.add_argument('--raw', type=Path, help='Override configured raw directory')
    build.add_argument('--output', type=Path, help='Override configured map output')
    build.add_argument('--report', type=Path, required=True)
    args = parser.parse_args(argv)
    try:
        if args.command == 'inventory':
            packages = inventory(args.raw)
            report = {'status': 'passed' if packages and not any(p['errors'] for p in packages) else 'incomplete', 'packages': packages}
            if args.report:
                write_json(args.report, report)
            print(json.dumps(report, ensure_ascii=False, indent=2))
            return 0 if report['status'] == 'passed' else 1
        cfg = json.loads(args.config.read_text(encoding='utf8'))
        raw = args.raw or ROOT / cfg.get('rawDirectory', 'data/raw/ngii')
        output = args.output or ROOT / cfg['output']
        require(output.resolve() != args.report.resolve(), 'Output and report paths must differ')
        result, report = build_map(cfg, raw)
        report['output'] = str(output)
        write_json(output, result)
        write_json(args.report, report)
        print(f"Exported {len(result['roads'])} roads, {len(result['buildings'])} buildings from {len(result['sources'])} package(s). Report: {args.report}")
        return 0
    except (ImportFailure, OSError, UnicodeError, KeyError, TypeError, json.JSONDecodeError) as exc:
        if args.command == 'build':
            write_json(args.report, {'status': 'failed', 'error': str(exc), 'config': str(args.config), 'outputUpdated': False})
        print(f'Import failed: {exc}', file=sys.stderr)
        return 2


if __name__ == '__main__':
    sys.exit(main())
