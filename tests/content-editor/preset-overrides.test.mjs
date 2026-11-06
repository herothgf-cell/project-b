import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
import {PassengerPresets as P} from '../../tools/content-editor/passenger-presets.mjs';
import {validateProfile,hashProfile} from '../../tools/content-editor/profile.mjs';
const catalog=JSON.parse(fs.readFileSync('config/content/catalog.json'));
const defaults=JSON.parse(fs.readFileSync('config/content/default-profile.json'));
const rider=()=>({id:'different_drunk',name:'개별 취객',enabled:true,weight:1,pool:'Drunk',traits:['body.drunk','item.luggage'],intro:'',overrides:[]});
const data=p=>({version:2,presets:[p]});
test('shared override contract fixtures match Node validation',()=>{for(const row of JSON.parse(fs.readFileSync('tests/content-editor/fixtures/passenger-overrides.json')).cases)assert.equal(P.validate(row.data,catalog).length===0,row.valid,row.name);});
function profile(p){const result=structuredClone(defaults);result.values.find(v=>v.id==='passenger.presets').value=JSON.stringify(data(p));result.contentHash=hashProfile(result);return result;}
test('v2 validates scoped probability dialogue effects and explicit zero',()=>{
 const p=rider();p.overrides=[{id:'type.Drunk.vomitPercent',value:'0'},{id:'type.Luggage.intro.0',value:'제 가방은 조심히 부탁해요.'},{id:'effect.luggage.prepareSeconds',value:'17'},{id:'choice.type.Luggage.0.immediate.satisfaction',value:'7'},{id:'request.Comfort.reply.Accept',value:'천천히 갈게요.'}];
 assert.deepEqual(validateProfile(profile(p),catalog),[]);
 assert.equal(P.effective(p,defaults,'type.Drunk.vomitPercent'),'0');
 assert.equal(P.effective(p,defaults,'type.Drunk.sleepPercent'),'30');
});
test('override keys reject global policy, wrong module, obsolete effect and bad values',()=>{
 for(const row of [{id:'type.Drunk.dailyMax',value:'4'},{id:'type.Drunk.spawnPercent',value:'40'},{id:'effect.food.spillPercent',value:'25'},{id:'type.Drunk.intro.0',value:'not primary'},{id:'request.Comfort.success',value:'9'},{id:'common.requestCap',value:'20'},{id:'type.Drunk.vomitPercent',value:'101'},{id:'type.Drunk.vomitPercent',value:'0.5'},{id:'effect.luggage.prepareSeconds',value:'NaN'},{id:'effect.luggage.prepareSeconds',value:3},{id:'request.Comfort.prompt',value:''}]){const p=rider();p.overrides=[row];assert(validateProfile(profile(p),catalog).length,JSON.stringify(row));}
 const p=rider();p.overrides=[{id:'type.Drunk.vomitPercent',value:'10'},{id:'type.Drunk.vomitPercent',value:'20'}];assert(validateProfile(profile(p),catalog).length);
});
test('upgrade is explicit and reset restores inheritance without changing another rider',()=>{
 const old=structuredClone(P.defaults),raw=JSON.stringify(old);const upgraded=P.upgrade(old);assert.equal(JSON.stringify(old),raw);assert.equal(upgraded.version,2);assert(upgraded.presets.every(p=>Array.isArray(p.overrides)));
 const p=upgraded.presets.find(p=>p.pool==='Drunk');P.setOverride(upgraded,p,'type.Drunk.vomitPercent','0');const copy=P.create(upgraded,p);P.setOverride(upgraded,copy,'type.Drunk.vomitPercent','100');assert.equal(P.effective(p,defaults,'type.Drunk.vomitPercent'),'0');
 P.resetOverride(p,'type.Drunk.vomitPercent');assert.equal(P.effective(p,defaults,'type.Drunk.vomitPercent'),'25');assert.equal(P.effective(copy,defaults,'type.Drunk.vomitPercent'),'100');
 const changed=structuredClone(defaults);changed.values.find(v=>v.id==='type.Drunk.vomitPercent').value='48';assert.equal(P.effective(p,changed,'type.Drunk.vomitPercent'),'48');assert.equal(P.effective(copy,changed,'type.Drunk.vomitPercent'),'100');
});
test('trait removal preserves invalid override for visible correction and v1 rejects v2 fields',()=>{
 const p=rider();p.overrides=[{id:'effect.luggage.prepareSeconds',value:'4'}];p.traits=['body.drunk'];assert(P.validate(data(p),catalog).length);assert.equal(p.overrides.length,1);
 assert(P.validate({version:1,presets:[p]},catalog).length);
});
test('editor authors local probability, shows final value and resets it to shared without edits elsewhere',()=>{
 class E{constructor(){this.children=[];this.value='';this.textContent=''}append(...v){this.children.push(...v)}replaceChildren(...v){this.children=v}addEventListener(){}setAttribute(){}}
 const nodes=new Map(),el=id=>{if(!nodes.has(id))nodes.set(id,new E);return nodes.get(id)};for(const id of ['filter','mode','impl','enabled','effect'])el(id).value='all';
 const c=vm.createContext({document:{getElementById:el,createElement:()=>new E},location:{hash:'',pathname:'/'},history:{replaceState(){}},sessionStorage:{getItem(){}},window:{addEventListener(){}},structuredClone,JSON,Blob,URL,setTimeout,fixtureCatalog:catalog,fixtureProfile:defaults});
 for(const file of ['passenger-presets.js','content-model.js','app.js'])vm.runInContext(fs.readFileSync('tools/content-editor/public/'+file,'utf8'),c);
 vm.runInContext("catalog=fixtureCatalog;profile=structuredClone(fixtureProfile);defaults=structuredClone(profile);saved=structuredClone(profile);group='승객 프리셋';selected='drunk';render()",c);
 const all=n=>[n,...n.children.flatMap(all)],find=label=>all(el('detail')).find(n=>n.ariaLabel===label);
 const picker=find('개별 설정 항목');assert(picker);picker.value='type.Drunk.vomitPercent';all(el('detail')).find(n=>n.textContent==='개별 설정 추가').onclick();
 const input=find('개별값 type.Drunk.vomitPercent');assert(input);input.value='0';input.oninput();
 const read=()=>JSON.parse(vm.runInContext("val(profile,'passenger.presets')",c));assert.equal(read().version,2);assert.equal(read().presets.find(p=>p.id==='drunk').overrides[0].value,'0');assert.equal(vm.runInContext("val(profile,'type.Drunk.vomitPercent')",c),'25');
 assert(all(el('detail')).some(n=>n.textContent==='최종 적용: 0 %'));
 find('공통값으로 복원 type.Drunk.vomitPercent').onclick();assert.deepEqual(read().presets.find(p=>p.id==='drunk').overrides,[]);
 find('개별 설정 항목').value='request.Quiet.unanswered';all(el('detail')).find(n=>n.textContent==='개별 설정 추가').onclick();const penalty=find('개별값 request.Quiet.unanswered');assert.equal(penalty.value,'-2');penalty.value='-6';penalty.oninput();assert.equal(read().presets.find(p=>p.id==='drunk').overrides[0].value,'6');assert(all(el('detail')).some(n=>n.textContent==='최종 적용: -6 점'));
});
