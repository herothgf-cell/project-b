import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp, mkdir, writeFile, readFile, rm} from 'node:fs/promises';
import {tmpdir} from 'node:os'; import path from 'node:path';
import {startServer} from '../../tools/content-editor/server.mjs';
import {hashProfile} from '../../tools/content-editor/profile.mjs';
test('loopback API enforces token and origin and saves with conflict detection',async t=>{
 const root=await mkdtemp(path.join(tmpdir(),'editor-'));t.after(()=>rm(root,{recursive:true,force:true}));
 await mkdir(path.join(root,'config/content'),{recursive:true}); const catalog={schemaVersion:1,entries:[{id:'n',group:'effects',type:'number',min:0,max:10,editable:true}]};
 const profile={schemaVersion:1,id:'default',revision:1,contentHash:'',values:[{id:'n',value:'1'}]};profile.contentHash=hashProfile(profile);
 await writeFile(path.join(root,'config/content/catalog.json'),JSON.stringify(catalog));await writeFile(path.join(root,'config/content/default-profile.json'),JSON.stringify(profile));
 const app=await startServer({root,port:0});t.after(()=>app.close()); const base=app.url.split('#')[0].replace(/\/$/,'');
 assert.equal((await fetch(base+'/api/bootstrap')).status,403);
 const headers={'X-Content-Token':app.token,Origin:base,'Content-Type':'application/json'};
 assert.equal((await fetch(base+'/api/bootstrap',{headers})).status,200);
 assert.equal((await fetch(base+'/api/bootstrap',{headers:{...headers,Origin:'http://evil.test'}})).status,403);
 const model=await fetch(base+'/content-model.js');assert.equal(model.status,200);assert.match(model.headers.get('content-type'),/javascript/);assert.match(await model.text(),/ContentModel/);
 const page=await (await fetch(base+'/')).text();assert.ok(page.indexOf('src="/content-model.js"')<page.indexOf('src="/app.js"'));
 assert.equal((await fetch(base+'/profile.mjs')).status,404);
 let r=await fetch(base+'/api/save',{method:'POST',headers,body:JSON.stringify({profile,expectedRevision:0})}); assert.equal(r.status,200);
 r=await fetch(base+'/api/save',{method:'POST',headers,body:JSON.stringify({profile,expectedRevision:0})}); assert.equal(r.status,409);
});

test('apply writes pending profile and rejects path, malformed import, and missing game',async t=>{
 const root=await mkdtemp(path.join(tmpdir(),'editor-'));t.after(()=>rm(root,{recursive:true,force:true}));await mkdir(path.join(root,'config/content'),{recursive:true});
 const catalog={schemaVersion:1,entries:[{id:'n',type:'number',min:0,max:10,editable:true}]};const profile={schemaVersion:1,id:'default',revision:1,contentHash:'',values:[{id:'n',value:'1'}]};profile.contentHash=hashProfile(profile);
 await writeFile(path.join(root,'config/content/catalog.json'),JSON.stringify(catalog));await writeFile(path.join(root,'config/content/default-profile.json'),JSON.stringify(profile));const app=await startServer({root,port:0});t.after(()=>app.close());const base=app.url.split('#')[0].replace(/\/$/,'');const headers={'X-Content-Token':app.token,Origin:base,'Content-Type':'application/json'};
 const post=(name,body)=>fetch(base+'/api/'+name,{method:'POST',headers,body:JSON.stringify(body)});
 let r=await post('apply',{profile});assert.equal(r.status,200);assert.match((await r.json()).status,/다음 새/);
 r=await post('import',{raw:'{"schemaVersion":1,"schemaVersion":1}'});assert.equal(r.status,400);
 r=await post('test',{profile});assert.equal(r.status,404);
 r=await fetch(base+'/api/profiles/%2e%2e%2fsecret',{headers});assert.equal(r.status,400);
 r=await post('validate',{profile,huge:'x'.repeat(2*1024*1024)});assert.equal(r.status,413);
});

test('two servers reject concurrent saves against the same revision',async t=>{
 const root=await mkdtemp(path.join(tmpdir(),'editor-concurrent-'));t.after(()=>rm(root,{recursive:true,force:true}));await mkdir(path.join(root,'config/content/profiles'),{recursive:true});
 const catalog={schemaVersion:1,entries:[{id:'n',type:'number',min:0,max:10,editable:true}]};const profile={schemaVersion:1,id:'default',revision:1,contentHash:'',values:[{id:'n',value:'1'}]};profile.contentHash=hashProfile(profile);
 await writeFile(path.join(root,'config/content/catalog.json'),JSON.stringify(catalog));await writeFile(path.join(root,'config/content/default-profile.json'),JSON.stringify(profile));await writeFile(path.join(root,'config/content/profiles/default.json'),JSON.stringify(profile));
 const a=await startServer({root,port:0}),b=await startServer({root,port:0});t.after(()=>a.close());t.after(()=>b.close());
 const send=(server,value)=>{const base=server.url.split('#')[0].replace(/\/$/,'');const changed=structuredClone(profile);changed.values[0].value=value;return fetch(base+'/api/save',{method:'POST',headers:{'X-Content-Token':server.token,Origin:base,'Content-Type':'application/json'},body:JSON.stringify({profile:changed,expectedRevision:1,expectedHash:profile.contentHash})})};
 const results=await Promise.all([send(a,'2'),send(b,'3')]);assert.deepEqual(results.map(r=>r.status).sort(),[200,409]);const disk=JSON.parse(await readFile(path.join(root,'config/content/profiles/default.json'),'utf8'));assert.equal(disk.revision,2);assert.ok(['2','3'].includes(disk.values[0].value));assert.equal(disk.contentHash,hashProfile(disk));
});
test('legacy imports migrate without overwriting and saves compare migrated meaning hash',async t=>{
 const {migrateProfile}=await import('../../tools/content-editor/profile.mjs');const root=await mkdtemp(path.join(tmpdir(),'editor-migrate-'));t.after(()=>rm(root,{recursive:true,force:true}));await mkdir(path.join(root,'config/content/profiles'),{recursive:true});
 const catalog=JSON.parse(await readFile(new URL('../../config/content/catalog.json',import.meta.url),'utf8')),defaults=JSON.parse(await readFile(new URL('../../config/content/default-profile.json',import.meta.url),'utf8'));
 const old={...defaults,id:'custom',schemaVersion:1,values:defaults.values.filter(v=>!v.id.startsWith('choice.'))};old.values.find(v=>v.id==='request.Empathy.success').value='11';old.contentHash=hashProfile(old);const original=JSON.stringify(old);await writeFile(path.join(root,'config/content/catalog.json'),JSON.stringify(catalog));await writeFile(path.join(root,'config/content/default-profile.json'),JSON.stringify(defaults));await writeFile(path.join(root,'config/content/profiles/custom.json'),original);
 const app=await startServer({root,port:0});t.after(()=>app.close());const base=app.url.split('#')[0].replace(/\/$/,''),headers={'content-type':'application/json','x-content-token':app.token,origin:base};
 const loaded=await (await fetch(base+'/api/profiles/custom',{headers})).json();assert.equal(loaded.profile.schemaVersion,2);assert.equal(await readFile(path.join(root,'config/content/profiles/custom.json'),'utf8'),original);
 const imported=await (await fetch(base+'/api/import',{method:'POST',headers,body:JSON.stringify({raw:original})})).json();assert.equal(imported.profile.contentHash,migrateProfile(old,catalog).contentHash);
 const saved=await fetch(base+'/api/save',{method:'POST',headers,body:JSON.stringify({profile:loaded.profile,expectedRevision:old.revision,expectedHash:loaded.profile.contentHash})});assert.equal(saved.status,200);const result=await saved.json();assert.equal(result.profile.revision,old.revision+1);const {readdir}=await import('node:fs/promises');const backups=await readdir(path.join(root,'config/content/backups'));assert.equal(await readFile(path.join(root,'config/content/backups',backups[0]),'utf8'),original);
});

test('new passenger preset saves reloads exports imports and rejects invalid combinations',async t=>{
 const root=await mkdtemp(path.join(tmpdir(),'editor-presets-'));t.after(()=>rm(root,{recursive:true,force:true}));await mkdir(path.join(root,'config/content'),{recursive:true});for(const file of ['catalog.json','default-profile.json'])await writeFile(path.join(root,'config/content',file),await readFile(new URL('../../config/content/'+file,import.meta.url)));
 const app=await startServer({root,port:0});t.after(()=>app.close());const base=app.url.split('#')[0].replace(/\/$/,''),headers={'X-Content-Token':app.token,Origin:base,'Content-Type':'application/json'};const post=async(action,body)=>fetch(base+'/api/'+action,{method:'POST',headers,body:JSON.stringify(body)});
 assert.equal((await fetch(base+'/passenger-presets.js')).status,200);
 const profile=JSON.parse(await readFile(path.join(root,'config/content/default-profile.json'),'utf8'));profile.id='created-passenger';const row=profile.values.find(v=>v.id==='passenger.presets'),data=JSON.parse(row.value);data.presets.push({id:'quiet_drunk_bag',name:'새 조합',enabled:true,weight:10,pool:'Drunk',traits:['body.drunk','speech.quiet','item.luggage','purpose.errand'],intro:'용무 후 내려 주세요.'});row.value=JSON.stringify(data);
 const save=await post('save',{profile,expectedRevision:0});assert.equal(save.status,200);const saved=(await save.json()).profile;assert.equal(saved.contentHash,hashProfile(saved));const loaded=(await (await fetch(base+'/api/profiles/'+profile.id,{headers})).json()).profile;assert.deepEqual(loaded,saved);
 data.presets.at(-1).traits.push('item.food');row.value=JSON.stringify(data);assert.equal((await post('validate',{profile})).status,400);
 const exported=await post('export',{profile:saved});assert.equal(exported.status,200);const exportedProfile=(await exported.json()).profile;const imported=await post('import',{raw:JSON.stringify(exportedProfile)});assert.equal(imported.status,200);assert.deepEqual((await imported.json()).profile,saved);
});
