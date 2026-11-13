import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import {hashProfile,validateProfile,migrateProfile} from '../../tools/content-editor/profile.mjs';
import '../../tools/content-editor/public/content-model.js';
const catalog=JSON.parse(fs.readFileSync('config/content/catalog.json','utf8'));
const defaults=JSON.parse(fs.readFileSync('config/content/default-profile.json','utf8'));
const id='law.centreLine.penalty';
test('centreline has its own editable one-point law deduction',()=>{
 const entry=catalog.entries.find(e=>e.id===id);
 assert.equal(entry?.defaultValue,'1');assert.equal(entry.editable,true);
 assert.equal(defaults.values.find(e=>e.id===id)?.value,'1');
 const model=ContentModel.build(catalog).find(e=>e.id==='law.centreLine');
 assert.ok(model);assert.ok(model.fieldIds.includes(id));
 assert.ok(model.fieldIds.includes('manner.wrongWay.penalty'));
});
test('legacy profiles acquire centreline default while preserving customized wrong-way and manners',()=>{
 const old=structuredClone(defaults);old.values=old.values.filter(e=>e.id!==id);
 old.values.find(e=>e.id==='law.wrongWay.penalty').value='37';
 old.values.find(e=>e.id==='manner.wrongWay.penalty').value='23';
 old.contentHash=hashProfile(old);assert.deepEqual(validateProfile(old,catalog),[]);
 const migrated=migrateProfile(old,catalog);
 assert.equal(migrated.values.find(e=>e.id===id)?.value,'1');
 for(const row of old.values)assert.equal(migrated.values.find(e=>e.id===row.id).value,row.value);
 migrated.values.find(e=>e.id===id).value='7';migrated.contentHash=hashProfile(migrated);
 assert.deepEqual(validateProfile(migrated,catalog),[]);
});
