import test from 'node:test';
import assert from 'node:assert/strict';
import { hashProfile, validateProfile } from '../../tools/content-editor/profile.mjs';
const catalog={schemaVersion:1,entries:[{id:'a',type:'text',editable:true},{id:'b',type:'number',min:0,max:2,editable:true}]};
test('UTF-8 profile hashing and validation',()=>{
 const p={schemaVersion:1,id:'default',revision:1,values:[{id:'b',value:'1'},{id:'a',value:'따옴표 "\n'}]}; p.contentHash=hashProfile(p);
 assert.match(p.contentHash,/^[0-9a-f]{64}$/); assert.deepEqual(validateProfile(p,catalog),[]);
 const bad=structuredClone(p); bad.values[0].value='3'; assert.ok(validateProfile(bad,catalog).length);
});


test('rejects unknown, duplicate and missing values and duplicate JSON members', async()=>{
 const {parseJson}=await import('../../tools/content-editor/profile.mjs');
 assert.throws(()=>parseJson('{"id":"a","id":"b"}'),/중복/);
 const p={schemaVersion:1,id:'default',revision:1,contentHash:'',values:[{id:'a',value:'x'},{id:'b',value:'2'}]};p.contentHash=hashProfile(p);
 for(const mutate of [x=>x.values.push({id:'a',value:'x'}),x=>x.values.pop(),x=>x.values.push({id:'z',value:'x'}),x=>x.extra=1,x=>x.id='../bad']){const v=structuredClone(p);mutate(v);assert.ok(validateProfile(v,catalog).length)}
});

test('schedule and enabled candidate relations reject invalid combinations',()=>{
 const entries=['schedule.count0','schedule.count1','schedule.count2','schedule.progressMin','schedule.progressMax','schedule.cutoff','request.A.weight','request.A.enabled','type.A.weight'].map(id=>({id,type:id.endsWith('enabled')?'boolean':'number',min:0,max:100,editable:true}));const c={schemaVersion:1,entries};const values=['20','60','20','25','65','85','1','true','1'];const p={schemaVersion:1,id:'default',revision:1,contentHash:'',values:entries.map((e,i)=>({id:e.id,value:values[i]}))};p.contentHash=hashProfile(p);assert.deepEqual(validateProfile(p,c),[]);
 for(const [id,value] of [['schedule.count0','21'],['schedule.progressMax','90'],['request.A.enabled','false'],['type.A.weight','0']]){const copy=structuredClone(p);copy.values.find(x=>x.id===id).value=value;copy.contentHash=hashProfile(copy);assert.ok(validateProfile(copy,c).length,id)}
});
