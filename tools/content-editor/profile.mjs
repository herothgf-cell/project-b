import {DRUNK_SETTINGS} from './drunk-settings.mjs';
import {createHash} from 'node:crypto';
import {defaultChoiceValue} from './choice-catalog.mjs';
export function hashProfile(p) {
 let data=`content-profile-v${p.schemaVersion}\n${p.id}\n${p.revision}\n`;
 for (const item of [...p.values].sort((a,b)=>a.id<b.id?-1:a.id>b.id?1:0)) data+=`${item.id}\t${Buffer.byteLength(item.value,'utf8')}:${item.value}\n`;
 return createHash('sha256').update(data,'utf8').digest('hex');
}
// JSON.parse normally hides duplicate keys. Scan the grammar first so malformed imports cannot do so.
export function parseJson(text) {
 let i=0; const ws=()=>{while (/\s/.test(text[i]||'') && i<text.length)i++;};
 function str(){const start=i++; while(i<text.length){if(text[i]==='\\'){i+=2;continue;} if(text[i++]==='"') return JSON.parse(text.slice(start,i));} throw Error('잘못된 JSON 문자열');}
 function value(){ws(); if(text[i]==='"'){str();return;} if(text[i]==='{'){i++;ws();const keys=new Set();if(text[i]==='}'){i++;return;} while(true){ws();if(text[i]!=='"')throw Error('잘못된 JSON');const k=str();if(keys.has(k))throw Error(`중복 필드: ${k}`);keys.add(k);ws();if(text[i++]!==':')throw Error('잘못된 JSON');value();ws();if(text[i]==='}'){i++;return;}if(text[i++]!==',')throw Error('잘못된 JSON');}} if(text[i]==='['){i++;ws();if(text[i]===']'){i++;return;}while(true){value();ws();if(text[i]===']'){i++;return;}if(text[i++]!==',')throw Error('잘못된 JSON');}} const m=text.slice(i).match(/^(?:true|false|null|-?(?:0|[1-9]\d*)(?:\.\d+)?(?:[eE][+-]?\d+)?)/);if(!m)throw Error('잘못된 JSON');i+=m[0].length; }
 value();ws();if(i!==text.length)throw Error('잘못된 JSON');return JSON.parse(text);
}
export function validateProfile(p,catalog,{checkHash=true}={}) {
 const errors=[]; const err=x=>errors.push(x); const exact=(o,keys)=>o&&typeof o==='object'&&!Array.isArray(o)&&Object.keys(o).length===keys.length&&keys.every(k=>Object.hasOwn(o,k));
 if(!exact(p,['schemaVersion','id','revision','contentHash','values']))return ['프로필 필드가 올바르지 않습니다.'];
 if(![1,2].includes(p.schemaVersion)||typeof p.id!=='string'||!/^[A-Za-z0-9_-]{1,64}$/.test(p.id)||!Number.isSafeInteger(p.revision)||p.revision<1||p.revision>2147483647||!Array.isArray(p.values))return ['프로필 형식이 올바르지 않습니다.'];
 const entries=new Map(catalog.entries.filter(e=>e.editable&&(p.schemaVersion===2||!e.id.startsWith('choice.'))).map(e=>[e.id,e])); const seen=new Set();
 for(const v of p.values){if(!exact(v,['id','value'])||typeof v.id!=='string'||typeof v.value!=='string'){err('값 형식 오류');continue;}const e=entries.get(v.id);if(!e||seen.has(v.id)){err(`알 수 없거나 중복된 ID: ${v.id}`);continue;}seen.add(v.id);
 if(!v.value.isWellFormed())err(`${v.id}: 올바르지 않은 유니코드입니다.`);
 if(e.type==='text'&&v.value.length>4000)err(`${v.id}: 텍스트가 너무 깁니다.`);
 if(e.type==='boolean'&&!['true','false'].includes(v.value))err(`${v.id}: 참/거짓 값 오류`);
 if(e.type==='number'||e.type==='integer'){const n=Number(v.value);if(!/^[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?$/.test(v.value)||!Number.isFinite(n)||(e.type==='integer'&&!Number.isSafeInteger(n))||(e.min!=null&&n<e.min)||(e.max!=null&&n>e.max))err(`${v.id}: 허용 범위 오류`);}}
 const oldDrunk=DRUNK_SETTINGS.every(e=>!seen.has(e.id));
 for(const id of entries.keys())if(!seen.has(id)&&!(oldDrunk&&DRUNK_SETTINGS.some(e=>e.id===id)))err(`누락된 ID: ${id}`);
 const vals=new Map(p.values.filter(v=>v&&typeof v.id==='string'&&typeof v.value==='string').map(v=>[v.id,v.value]));
 if(['schedule.count0','schedule.count1','schedule.count2'].every(k=>vals.has(k))&&['schedule.count0','schedule.count1','schedule.count2'].reduce((a,k)=>a+Number(vals.get(k)),0)!==100)err('요청 개수 비중의 합은 100이어야 합니다.');
 if(['schedule.progressMin','schedule.progressMax','schedule.cutoff'].every(k=>vals.has(k))&&(Number(vals.get('schedule.progressMin'))>Number(vals.get('schedule.progressMax'))||Number(vals.get('schedule.progressMax'))>=Number(vals.get('schedule.cutoff'))))err('요청 발생 진행률 범위가 올바르지 않습니다.');
 if(['common.required.easy','common.required.normal','common.required.hard'].every(k=>vals.has(k))&&(Number(vals.get('common.required.easy'))>Number(vals.get('common.required.normal'))||Number(vals.get('common.required.normal'))>Number(vals.get('common.required.hard'))))err('요구 만족도는 쉬움 ≤ 보통 ≤ 어려움 순서여야 합니다.');
 if(Buffer.byteLength(JSON.stringify(p,null,2)+'\n','utf8')>240*1024)err('프로필이 게임의 240 KiB 제한을 초과합니다.');
 for(const prefix of ['request.','type.']){const weights=[...vals.keys()].filter(k=>k.startsWith(prefix)&&k.endsWith('.weight'));if(weights.length&&weights.reduce((sum,k)=>sum+(vals.get(k.slice(0,-6)+'enabled')==='false'?0:Number(vals.get(k))),0)<=0)err(prefix+' 사용 가능한 가중치 합계는 양수여야 합니다.');}
 if(checkHash&&(!/^[0-9a-f]{64}$/.test(p.contentHash)||p.contentHash!==hashProfile(p)))err('프로필 해시가 일치하지 않습니다.'); return errors;
}

export function migrateProfile(profile,catalog){
 const errors=validateProfile(profile,catalog);if(errors.length)throw Error(errors.join('\n'));
 const copy=structuredClone(profile);if(copy.schemaVersion===1&&catalog.entries.some(e=>e.id==='choice.effects.version'&&e.editable)){
 const legacy=new Map(copy.values.map(v=>[v.id,v.value]));for(const entry of catalog.entries.filter(e=>e.editable&&e.id.startsWith('choice.')))copy.values.push({id:entry.id,value:defaultChoiceValue(entry.id,legacy)});
 copy.schemaVersion=2;copy.contentHash=hashProfile(copy);const errors=validateProfile(copy,catalog);if(errors.length)throw Error(errors.join('\n'));}if(DRUNK_SETTINGS.every(e=>!copy.values.some(v=>v.id===e.id))){for(const e of DRUNK_SETTINGS)if(catalog.entries.some(c=>c.id===e.id))copy.values.push({id:e.id,value:e.defaultValue});copy.contentHash=hashProfile(copy);}return copy;
}
