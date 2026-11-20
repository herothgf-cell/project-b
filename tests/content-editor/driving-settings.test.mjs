import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {hashProfile,validateProfile,migrateProfile} from '../../tools/content-editor/profile.mjs';
import '../../tools/content-editor/public/content-model.js';
const catalog=JSON.parse(fs.readFileSync('config/content/catalog.json','utf8'));
const defaults=JSON.parse(fs.readFileSync('config/content/default-profile.json','utf8'));
const ids=['flashStop','permissiveTurn','blockedExit','personMinor','personModerate','personMajor','vehicleMinor','vehicleModerate','vehicleMajor'].map(x=>'law.'+x+'.penalty');
test('fixed driving deductions are editable and old profiles migrate without changing other settings',()=>{
 for(const id of ids)assert.equal(catalog.entries.find(e=>e.id===id)?.editable,true,id);
 const old=structuredClone(defaults);old.values=old.values.filter(v=>!ids.includes(v.id));old.contentHash=hashProfile(old);
 assert.deepEqual(validateProfile(old,catalog),[]);
 const migrated=migrateProfile(old,catalog);assert.deepEqual(validateProfile(migrated,catalog),[]);
 for(const row of old.values)assert.equal(migrated.values.find(v=>v.id===row.id).value,row.value);
 for(const id of ids)assert.ok(JSON.stringify(ContentModel.build(catalog)).includes(id));
 const partial=structuredClone(defaults);partial.values=partial.values.filter(v=>v.id!==ids[0]);partial.contentHash=hashProfile(partial);assert.ok(validateProfile(partial,catalog).length);
});

test('v22 brake preferences migrate independently of the indicator and keep other sound settings',()=>{
 for(const brake of ['false','true']){
  const old=structuredClone(defaults);
  const sound=old.values.find(v=>v.id==='display.sound.indicator');
  sound.id='display.sound.brake';sound.value=brake;
  old.values.find(v=>v.id==='display.sound.horn').value='false';
  old.contentHash=hashProfile(old);
  assert.deepEqual(validateProfile(old,catalog),[]);
  const next=migrateProfile(old,catalog);
  assert.equal(next.values.find(v=>v.id==='display.sound.indicator').value,'true');
  assert.equal(next.values.find(v=>v.id==='display.sound.horn').value,'false');
  assert.ok(!next.values.some(v=>v.id==='display.sound.brake'));
  old.values.push({id:'display.sound.indicator',value:'true'});old.contentHash=hashProfile(old);
  assert.ok(validateProfile(old,catalog).length);
 }
});
