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
