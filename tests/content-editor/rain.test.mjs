import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {validateProfile,hashProfile,migrateProfile} from '../../tools/content-editor/profile.mjs';
const catalog=JSON.parse(fs.readFileSync('config/content/catalog.json','utf8'));
const defaults=JSON.parse(fs.readFileSync('config/content/default-profile.json','utf8'));
test('rain controls exist, default dry, old saves migrate dry and partial profiles fail',()=>{
 assert.equal(catalog.entries.filter(e=>e.id.startsWith('weather.')).length,6);
 assert.deepEqual(validateProfile(defaults,catalog),[]);
 const old=structuredClone(defaults);old.values=old.values.filter(v=>!v.id.startsWith('weather.'));old.contentHash=hashProfile(old);
 assert.deepEqual(validateProfile(old,catalog),[]);
 const migrated=migrateProfile(old,catalog);assert.equal(migrated.values.find(v=>v.id==='weather.rain.enabled').value,'false');
 assert.deepEqual(validateProfile(migrated,catalog),[]);
 const partial=structuredClone(migrated);partial.values=partial.values.filter(v=>v.id!=='weather.rain.boardingDirt');partial.contentHash=hashProfile(partial);assert.ok(validateProfile(partial,catalog).length);
});
test('rain brake range rejects invalid physical tuning',()=>{
 for(const value of ['0','1.1','NaN']){const p=structuredClone(defaults);const item=p.values.find(v=>v.id==='weather.rain.brakeMultiplier');assert.ok(item);item.value=value;p.contentHash=hashProfile(p);assert.ok(validateProfile(p,catalog).length);}
});
