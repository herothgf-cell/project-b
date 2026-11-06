(function(){
const traits=[
 ['speech.quiet','대화','조용함','일반 후속 수다를 제한합니다. 작업·안전 안내는 유지합니다.',null],
 ['speech.talkative','대화','대화 선호','기존 수다 대화와 대표 요청을 사용합니다.','type.Talkative'],
 ['body.drunk','신체','음주','기존 잠듦·구토 확률과 경고·깨우기·청결 처리를 사용합니다.','type.Drunk'],
 ['body.sleepy','신체','졸림','수면·흔들림 깨기·진정·도착 기상을 사용합니다.','type.Sleeping'],
 ['body.sensitive','신체','승차감 민감','편안한 운행 요청과 관찰을 사용합니다.','request.Comfort'],
 ['purpose.hurried','목적','급함','정시 도착 요청과 종료 판정을 사용합니다.','request.OnTime'],
 ['purpose.route','목적','경로 확인','경로 안내 요청을 사용합니다.','type.RouteChecking'],
 ['purpose.errand','목적','경유 용무','경유 요청·안전 정차 용무를 사용합니다.','type.Errand'],
 ['purpose.precise','목적','안전 하차 위치','안전 정차 요청과 도착 판정을 사용합니다.','request.SafeStop'],
 ['item.luggage','소지품','짐','적재·재고정·하역을 안전 정차 중 진행합니다.','type.Luggage'],
 ['item.muddy','소지품','흙 묻은 짐','보호 준비·재고정·청결 처리를 사용합니다.','type.MuddyLuggage'],
 ['item.food','소지품','음식','허용·섭취·흘림·정리와 청결 처리를 사용합니다.','type.Food']
].map(([id,group,name,summary,link])=>({id,group,name,summary,link}));
const pools=['General','Drunk','Sleeping','Comfort','Hurried','RouteChecking','Errand','Luggage','MuddyLuggage','Food','PrecisionStop','Talkative'];
const names=['일반','취객','수면','편안한 운행','급한 승객','경로 확인','경유 용무','짐','흙 묻은 짐','음식','정확한 정차','수다'];
const defaults={version:1,presets:pools.map((pool,i)=>({id:pool.toLowerCase(),name:names[i],enabled:true,weight:1,pool,traits:i?[['body.drunk'],['body.sleepy'],['body.sensitive'],['purpose.hurried'],['purpose.route'],['purpose.errand'],['item.luggage'],['item.muddy'],['item.food'],['purpose.precise'],['speech.talkative']][i-1]:[],intro:''}))};
function validate(data,catalog){const errors=[],exact=(o,keys)=>o&&typeof o==='object'&&!Array.isArray(o)&&Object.keys(o).length===keys.length&&keys.every(k=>Object.hasOwn(o,k));if(!exact(data,['version','presets'])||![1,2].includes(data.version)||!Array.isArray(data.presets))return ['승객 프리셋 형식이 올바르지 않습니다.'];if(data.presets.length>32)errors.push('프리셋은 최대 32개입니다.');const ids=new Set();for(const p of data.presets){if(!exact(p,['id','name','enabled','weight','pool','traits','intro',...(data.version===2?['overrides']:[])])){errors.push('프리셋 필드가 올바르지 않습니다.');continue;}const err=s=>errors.push((p.id||'프리셋')+': '+s);if(typeof p.id!=='string'||!/^[a-z0-9_-]{1,48}$/.test(p.id)||ids.has(p.id))err('ID는 중복 없는 영문 소문자/숫자/_/- 1~48자여야 합니다.');ids.add(p.id);if(typeof p.name!=='string'||!p.name.trim()||p.name.length>60)err('이름은 1~60자입니다.');if(typeof p.intro!=='string'||p.intro.length>240)err('소개는 240자 이하입니다.');if(typeof p.enabled!=='boolean'||!Number.isInteger(p.weight)||p.weight<0||p.weight>100)err('사용 여부 또는 가중치(정수 0~100)가 올바르지 않습니다.');if(!pools.includes(p.pool))err('알 수 없는 배정 그룹입니다.');if(!Array.isArray(p.traits)||p.traits.some(t=>!traits.some(x=>x.id===t))||new Set(p.traits).size!==p.traits.length){err('알 수 없거나 중복된 특성입니다.');continue;}if(data.version===2)errors.push(...validateOverrides(p,catalog));const has=t=>p.traits.includes(t);for(const group of ['speech.','purpose.','item.'])if(p.traits.filter(t=>t.startsWith(group)).length>1)err(group+' 특성은 최대 하나입니다.');if(has('body.drunk')&&has('body.sleepy'))err('음주와 졸림은 함께 사용할 수 없습니다.');if((p.pool==='Drunk')!==has('body.drunk'))err('음주 특성은 Drunk 그룹에서 필수이며 다른 그룹에는 사용할 수 없습니다.');if(p.traits.filter(t=>t==='body.sensitive'||t.startsWith('purpose.')).length>1)err('대표 요청(승차감 민감·목적)은 최대 하나입니다.');if(p.traits.filter(t=>t==='body.sleepy'||t==='speech.talkative'||t.startsWith('item.')).length>1)err('주 행동(졸림·수다·소지품)은 최대 하나입니다.');if(p.pool==='General'&&p.traits.some(t=>t!=='speech.quiet'))err('일반 배정 그룹은 조용함 외의 행동을 사용할 수 없습니다.');}return errors;}
function create(data,source){if(data.presets.length>=32)throw Error('프리셋은 최대 32개입니다.');let i=1;while(data.presets.some(p=>p.id==='passenger_'+i))i++;const p=source?JSON.parse(JSON.stringify(source)):{name:'새 승객',enabled:true,weight:1,pool:'General',traits:[],intro:''};p.id='passenger_'+i;if(data.version===2&&!p.overrides)p.overrides=[];if(source)p.name=p.name.slice(0,56)+' 복제';data.presets.push(p);return p;}
function effectRows(selected,profile){const mapping={
 'body.drunk':['type.Drunk.spawnPercent','type.Drunk.dailyMax','type.Drunk.sleepPercent','type.Drunk.vomitPercent'],
 'item.luggage':['effect.luggage.prepareSeconds','effect.luggage.unloadSeconds','effect.resecure.seconds'],
 'item.muddy':['effect.muddy.prepareSeconds','effect.muddy.dirt','effect.resecure.seconds'],
 'item.food':['choice.type.Food.1.immediate.satisfaction','effect.food.crumbs','effect.food.spill','effect.food.spillPercent','effect.food.cleanupSeconds'],
 'body.sleepy':['effect.sleep.calmSeconds'],
 'body.sensitive':['choice.request.Comfort.Accept.end.success.satisfaction','choice.request.Comfort.Accept.end.failure.satisfaction','choice.request.Comfort.Accept.during.motionPenaltyDelta'],
 'purpose.hurried':['choice.request.OnTime.Accept.end.success.satisfaction','choice.request.OnTime.Accept.end.failure.satisfaction'],
 'purpose.errand':['effect.errand.seconds','choice.request.ViaStop.Accept.end.success.satisfaction'],
 'purpose.precise':['choice.request.SafeStop.Accept.end.success.satisfaction','choice.request.SafeStop.Accept.end.failure.satisfaction'],
 'speech.quiet':[]};return [...new Set(selected.flatMap(t=>mapping[t]||[]))].flatMap(id=>{const row=profile.values.find(v=>v.id===id);const requestLabels={Comfort:'편안한 운행',OnTime:'정시 도착',ViaStop:'경유 약속',SafeStop:'안전 하차'};let label;if(id==='choice.type.Food.1.immediate.satisfaction')label='음식 허용 · 즉시 만족도 S';else if(id.startsWith('choice.request.')){const parts=id.split('.');label=requestLabels[parts[2]]+(parts.includes('success')?' 이행 성공 · 종료 만족도 S':parts.includes('failure')?' 이행 실패 · 종료 만족도 S':' · 급가감속 M 추가 변화');}return row?[{id,value:row.value,...(label?{label}:{})}]:[]});}
// Explicit per-passenger scope: shared scheduling, economy and migration-only values stay global.
function actionModule(p){
 const mapping={'body.drunk':'Drunk','body.sleepy':'Sleeping','body.sensitive':'Comfort','speech.talkative':'Talkative','purpose.hurried':'Hurried','purpose.route':'RouteChecking','purpose.errand':'Errand','purpose.precise':'PrecisionStop','item.luggage':'Luggage','item.muddy':'MuddyLuggage','item.food':'Food'};
 const ts=p.traits||[], primary=ts.find(t=>t==='body.sleepy'||t==='speech.talkative'||t.startsWith('item.'));
 const representative=ts.find(t=>t==='body.sensitive'||t.startsWith('purpose.'));
 return mapping[primary||representative||(ts.includes('body.drunk')?'body.drunk':'')]||'General';
}
function allowsOverride(p,id){
 const module=actionModule(p),has=t=>(p.traits||[]).includes(t);
 if(new RegExp('^type\\.'+module+'\\.(intro|choice|followup)\\.\\d+$').test(id))return true;
 if(id.startsWith('choice.type.'+module+'.')||module==='Sleeping'&&id.startsWith('choice.type.SleepingFollowup.'))return true;
 if(/^request\.[^.]+\.(prompt|unanswered)$/.test(id)||/^request\.[^.]+\.reply\.[^.]+$/.test(id)||id.startsWith('choice.request.'))return true;
 if(/^situation\.[^.]+\.reply\.[01]$/.test(id)||id.startsWith('choice.situation.'))return true;
 if(has('body.drunk')&&['type.Drunk.sleepPercent','type.Drunk.vomitPercent'].includes(id))return true;
 for(const [trait,prefix]of [['item.food','effect.food.'],['item.luggage','effect.luggage.'],['item.muddy','effect.muddy.']])if(has(trait)&&id.startsWith(prefix)&&id!=='effect.food.satisfaction')return true;
 return has('body.sleepy')&&id==='effect.sleep.calmSeconds'||(has('item.luggage')||has('item.muddy'))&&id==='effect.resecure.seconds'||has('purpose.errand')&&id==='effect.errand.seconds';
}
function overrideEntries(p,catalog){return (catalog?.entries||[]).filter(e=>e.editable&&allowsOverride(p,e.id));}
function validateOverrides(p,catalog){
 const errors=[],err=s=>errors.push(`${p.id||'프리셋'}: ${s}`);
 if(!Array.isArray(p.overrides)){err('개별 설정 목록이 필요합니다.');return errors;}
 if(p.overrides.length>128)err('개별 설정은 승객당 최대 128개입니다.');
 const entries=new Map(overrideEntries(p,catalog).map(e=>[e.id,e])),seen=new Set();
 for(const row of p.overrides){
  if(!row||typeof row!=='object'||Array.isArray(row)||Object.keys(row).length!==2||typeof row.id!=='string'||typeof row.value!=='string'){err('개별 설정은 ID와 문자열 값이 필요합니다.');continue;}
  const e=entries.get(row.id);if(!e||seen.has(row.id)){err(`사용할 수 없거나 중복된 개별 설정: ${row.id}`);continue;}seen.add(row.id);
  const v=row.value,n=Number(v);
  if(v.isWellFormed&&!v.isWellFormed())err(`${row.id}: 올바르지 않은 유니코드입니다.`);
  if(e.type==='text'&&(!v.trim()||v.length>4000))err(`${row.id}: 대사는 1~4000자입니다. 비우려면 공통값으로 복원하세요.`);
  if(e.type==='boolean'&&!['true','false'].includes(v))err(`${row.id}: 참/거짓 값 오류`);
  if(['number','integer'].includes(e.type)&&(!/^[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?$/.test(v)||!Number.isFinite(n)||e.type==='integer'&&!Number.isSafeInteger(n)||n<e.min||n>e.max))err(`${row.id}: 허용 범위 ${e.min}~${e.max}${e.unit||''}를 확인하세요.`);
 }
 return errors;
}
function upgrade(data){const copy=JSON.parse(JSON.stringify(data));if(copy.version===1){copy.version=2;for(const p of copy.presets)p.overrides=[];}return copy;}
function setOverride(data,p,id,value){if(data.version===1){data.version=2;for(const row of data.presets)row.overrides=[];}const row=p.overrides.find(o=>o.id===id);if(row)row.value=String(value);else p.overrides.push({id,value:String(value)});}
function resetOverride(p,id){if(p.overrides)p.overrides=p.overrides.filter(o=>o.id!==id);}
function effective(p,profile,id){const local=p.overrides?.find(o=>o.id===id);return local?local.value:profile.values.find(v=>v.id===id)?.value;}
const poolLabels=Object.fromEntries(pools.map((pool,i)=>[pool,names[i]]));
globalThis.PassengerPresets={traits,pools,poolLabels,defaults,validate,create,effectRows,actionModule,allowsOverride,overrideEntries,validateOverrides,upgrade,setOverride,resetOverride,effective};
})();
