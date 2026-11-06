import {test} from 'node:test';
import assert from 'node:assert/strict';
import * as fs from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import {spawnSync} from 'node:child_process';
import {createHash} from 'node:crypto';
import {hashProfile} from '../tools/content-editor/profile.mjs';
const root=path.resolve(import.meta.dirname,'..');
const digest=b=>createHash('sha256').update(b).digest('hex');
async function fixture(){
 const dir=await fs.mkdtemp(path.join(os.tmpdir(),'taxi-manifest-'));
 const put=async(name,data)=>{const to=path.join(dir,name);await fs.mkdir(path.dirname(to),{recursive:true});await fs.writeFile(to,data);};
 const catalog={schemaVersion:1,entries:[{id:'example.number',type:'integer',defaultValue:'5',min:0,max:100,editable:true}]};
 const profile={schemaVersion:1,id:'example',revision:1,contentHash:'',values:[{id:'example.number',value:'5'}]};profile.contentHash=hashProfile(profile);
 await put('config/content/catalog.json',JSON.stringify(catalog));
 await put('builds/EungyeTaxi/app.dll','original-runtime');
 await put('builds/EungyeTaxi/content/active-profile.json',JSON.stringify(profile));
 await put('tools/content-editor/choice-catalog.mjs',await fs.readFile(path.join(root,'tools/content-editor/choice-catalog.mjs')));
 await put('tools/content-editor/profile.mjs',await fs.readFile(path.join(root,'tools/content-editor/profile.mjs')));
 try{await put('tools/content-editor/verify-profile.mjs',await fs.readFile(path.join(root,'tools/content-editor/verify-profile.mjs')));}catch{}
 const files=[];for(const name of ['builds/EungyeTaxi/app.dll','builds/EungyeTaxi/content/active-profile.json']){const b=await fs.readFile(path.join(dir,name));files.push({path:name,bytes:b.length,sha256:digest(b)});}
 const manifest={totalBytes:files.reduce((n,f)=>n+f.bytes,0),files};await put('distribution/EungyeTaxi-build-manifest.json',JSON.stringify(manifest));
 return {dir,put,profile,manifest};
}
function verify(dir){return spawnSync('pwsh',['-NoProfile','-File',path.join(root,'tools/update_distribution_manifest.ps1'),'-Root',dir,'-VerifyOnly'],{encoding:'utf8',timeout:30000});}
test('valid edited profile is reported while shipped runtime hash remains strict',async()=>{
 const f=await fixture();try{
  f.profile.values[0].value='8';f.profile.contentHash=hashProfile(f.profile);await f.put('builds/EungyeTaxi/content/active-profile.json',JSON.stringify(f.profile));
  const good=verify(f.dir);assert.equal(good.status,0,good.stdout+good.stderr);assert.match(good.stdout,/CONTENT_PROFILE_USER_OVERRIDE/);assert.match(good.stdout,/DISTRIBUTION_OK/);
  await f.put('builds/EungyeTaxi/app.dll','changed-runtime');const bad=verify(f.dir);assert.notEqual(bad.status,0);assert.match(bad.stdout+bad.stderr,/hash mismatch/);
 }finally{await fs.rm(f.dir,{recursive:true,force:true});}
});
test('malformed override, unknown file and duplicate manifest entries fail',async()=>{
 const f=await fixture();try{
  await f.put('builds/EungyeTaxi/content/active-profile.json','{}');assert.notEqual(verify(f.dir).status,0);
  await f.put('builds/EungyeTaxi/content/active-profile.json',JSON.stringify(f.profile));await f.put('builds/EungyeTaxi/unexpected.dll','x');assert.notEqual(verify(f.dir).status,0);
  await fs.rm(path.join(f.dir,'builds/EungyeTaxi/unexpected.dll'));f.manifest.files.push(f.manifest.files[0]);await f.put('distribution/EungyeTaxi-build-manifest.json',JSON.stringify(f.manifest));assert.notEqual(verify(f.dir).status,0);
 }finally{await fs.rm(f.dir,{recursive:true,force:true});}
});

test('manifest generation excludes optional user profile and preserves its bytes',async()=>{
 const f=await fixture();try{
  const git=(...args)=>{const result=spawnSync('git',['-C',f.dir,...args],{encoding:'utf8'});assert.equal(result.status,0,result.stdout+result.stderr);return result.stdout.trim();};git('init');git('-c','user.name=Manifest Fixture','-c','user.email=manifest-fixture@example.invalid','commit','--allow-empty','-m','fixture');const commit=git('rev-parse','HEAD');
  const active=path.join(f.dir,'builds/EungyeTaxi/content/active-profile.json'),before=await fs.readFile(active);
  const generated=spawnSync('pwsh',['-NoProfile','-File',path.join(root,'tools/update_distribution_manifest.ps1'),'-Root',f.dir,'-SourceCommit',commit],{encoding:'utf8',timeout:30000});assert.equal(generated.status,0,generated.stdout+generated.stderr);
  const manifest=JSON.parse((await fs.readFile(path.join(f.dir,'distribution/EungyeTaxi-build-manifest.json'),'utf8')).replace(/^\uFEFF/,''));assert.deepEqual(manifest.files.map(f=>f.path),['builds/EungyeTaxi/app.dll']);assert.equal(manifest.totalBytes,Buffer.byteLength('original-runtime'));assert.deepEqual(await fs.readFile(active),before);
  const valid=verify(f.dir);assert.equal(valid.status,0,valid.stdout+valid.stderr);assert.match(valid.stdout,/CONTENT_PROFILE_USER_OVERRIDE/);assert.match(valid.stdout,/DISTRIBUTION_OK/);
 }finally{await fs.rm(f.dir,{recursive:true,force:true});}
});
