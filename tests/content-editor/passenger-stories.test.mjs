import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import {hashProfile, migrateProfile, validateProfile} from '../../tools/content-editor/profile.mjs';

const read = path => fs.readFileSync(new URL('../../' + path, import.meta.url), 'utf8');
const catalog = JSON.parse(read('config/content/catalog.json'));
const profile = JSON.parse(read('config/content/default-profile.json'));
const stories = ['first-interview', 'feverish-child', 'moving-day', 'night-shift', 'last-train',
  'wedding-guest', 'plant-gift', 'after-dinner', 'trip-departure', 'rough-day'];
const row = (data, id) => data.values.find(entry => entry.id === id);
const seal = data => ({...data, contentHash: hashProfile(data)});

test('ten single-player stories have shipped presets, supported requests and three endings', () => {
  assert.equal(read('config/content/catalog.json'), read('unity/EungyeTaxi/Assets/Resources/Content/catalog.json'));
  assert.equal(read('config/content/default-profile.json'), read('unity/EungyeTaxi/Assets/Resources/Content/default-profile.json'));
  const roster = new Set(JSON.parse(row(profile, 'passenger.presets').value).presets.filter(preset => preset.enabled).map(preset => preset.id));
  assert.deepEqual(catalog.entries.filter(entry => /^story\.[^.]+\.title$/.test(entry.id)).map(entry => entry.id.split('.')[1]), stories);
  for (const id of stories) {
    assert.ok(roster.has(row(profile, `story.${id}.presetId`).value), id);
    const lines = ['good', 'ordinary', 'disappointing'].map(ending => row(profile, `story.${id}.ending.${ending}`).value);
    assert.equal(new Set(lines).size, 3, id);
    assert.ok(lines.every(line => line.trim().length > 0), id);
  }
  assert.ok(catalog.entries.filter(entry => entry.id.startsWith('story.')).every(entry => entry.modes.join() === 'single'));
  assert.deepEqual(validateProfile(profile, catalog), []);
});

test('old profile hashes remain valid and migration fills story defaults', () => {
  const old = seal({...profile, values: profile.values.filter(entry => !entry.id.startsWith('story.'))});
  assert.deepEqual(validateProfile(old, catalog), []);
  const moved = migrateProfile(old, catalog);
  assert.equal(row(moved, 'story.daily.minimum').value, '2');
  assert.equal(row(moved, 'story.daily.maximum').value, '3');
  assert.equal(row(moved, 'story.last-train.secondaryRequest').value, 'Eta');
  assert.equal(row(moved, 'story.logbook.longRideMeters').value, '3000');
  assert.deepEqual(validateProfile(moved, catalog), []);
});

test('story configuration rejects impossible quota and unsupported editable symbols', () => {
  for (const [id, invalid] of [
    ['story.daily.minimum', '4'],
    ['story.first-interview.request', 'LostItem'],
    ['story.first-interview.secondaryRequest', 'OnTime'],
    ['story.first-interview.criteria', 'OnTime,OnTime'],
    ['story.first-interview.criteria', 'Unexpected'],
    ['story.first-interview.presetId', 'Not a preset'],
    ['story.first-interview.ending.good', '  '],
  ]) {
    const edited = structuredClone(profile);
    row(edited, id).value = invalid;
    assert.ok(validateProfile(seal(edited), catalog).length > 0, id);
  }
});

test('content editor groups story text and criteria by story', () => {
  const sandbox = {};
  vm.createContext(sandbox);
  vm.runInContext(read('tools/content-editor/public/content-model.js'), sandbox);
  const entities = sandbox.ContentModel.build(catalog);
  for (const id of stories) {
    const story = entities.find(entity => entity.id === 'story.' + id);
    assert.ok(story, id);
    for (const suffix of ['intro', 'criteria', 'ending.good', 'ending.ordinary', 'ending.disappointing']) {
      assert.ok(story.fieldIds.includes(`story.${id}.${suffix}`), id + '.' + suffix);
    }
  }
});
