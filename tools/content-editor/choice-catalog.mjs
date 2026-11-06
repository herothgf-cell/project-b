import {PRESET_ENTRY} from './passenger-presets.mjs';
import {DRUNK_SETTINGS} from './drunk-settings.mjs';
import fs from 'node:fs/promises';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {hashProfile,validateProfile} from './profile.mjs';
export const IMMEDIATE_FIELDS=['satisfaction','fatigue','satisfactionPercent','fatiguePercent'];
export const DURING_FIELDS=['satisfactionPerMinute','fatiguePerMinute','satisfactionPercentPerMinute','fatiguePercentPerMinute','fatigueGrowthPercent','motionPenaltyDelta'];
export const END_FIELDS=IMMEDIATE_FIELDS;
export const FAILURE_KINDS=['Comfort','OnTime','SafeStop','DestinationChange','ViaStop'];
export const NEUTRAL_KINDS=['LargeRoad','ShortRoad','OnTime','RouteAdvice','RejectIllegal','SafeStop','DestinationChange','ViaStop'];
const alternatives=['Empathy','RouteAdvice','Interview','FareHelp','LocalChat'];
const kinds=['Quiet','Eta','Comfort','OnTime','LargeRoad','ShortRoad','SafeStop','Empathy','RouteAdvice','Interview','Music','FareHelp','LocalChat','DestinationChange','ViaStop','RejectIllegal'];
export const CHOICE_DEFINITIONS=[...kinds.flatMap(kind=>[...(kind==='Eta'?['EtaFaster','EtaNormal','EtaRelaxed']:['Accept',...(alternatives.includes(kind)?['Alternative']:[])]),'Defer'].map(reply=>({id:`request.${kind}.${reply}`,kind,reply,group:'request',outcomes:reply==='Defer'?['neutral']:['success',...(FAILURE_KINDS.includes(kind)?['failure']:[]),...(NEUTRAL_KINDS.includes(kind)?['neutral']:[])]}))),...['Drunk','Sleeping','Comfort','Hurried','RouteChecking','Errand','Luggage','MuddyLuggage','Food','PrecisionStop','Talkative','SleepingFollowup'].flatMap(kind=>Array.from({length:['MuddyLuggage','Food'].includes(kind)?3:2},(_,i)=>({id:`type.${kind}.${i}`,kind,index:i,group:'type',outcomes:['success']}))),...['SignalWait','Jolt','NecessaryBrake'].flatMap(kind=>[0,1].map(i=>({id:`situation.${kind}.${i}`,kind,index:i,group:'situation',outcomes:['success']})))];
export const choiceDefinitions=CHOICE_DEFINITIONS;
export function defaultChoiceValue(id,legacy){
 if(id==='choice.effects.version')return '1';
 const get=k=>String(legacy.get(k)??'0');
 if(id==='choice.type.Food.1.immediate.satisfaction')return get('effect.food.satisfaction');
 if(id==='choice.request.Comfort.Accept.during.motionPenaltyDelta')return String(Number(get('manner.comfortMotion.penalty'))-Number(get('manner.motion.penalty')));
 const m=id.match(/^choice\.request\.([^.]+)\.([^.]+)\.end\.(success|failure)\.satisfaction$/);
 return m&&m[2]!=='Defer'&&!['Eta','RejectIllegal'].includes(m[1])?get(`request.${m[1]}.${m[3]}`):'0';
}
export function generateChoiceEntries(legacyEntries){
 const legacy=new Map(legacyEntries.map(e=>[e.id,e.defaultValue]));
 const row=(id,title,unit,min=-100,max=100)=>({id,group:'선택지 단계 효과',section:id.split('.').slice(1,-2).join('.'),title,type:id.endsWith('.during.motionPenaltyDelta')?'integer':'number',defaultValue:defaultChoiceValue(id,legacy),min,max,unit,description:'해당 선택지의 독립 효과. 0은 추가 효과 없음.',affects:['승객 만족도','기사 피로'],modes:['single','pvp'],status:'implemented',consumer:'ChoiceEffectDefinition.Read',editable:true});
 const entries=[{...row('choice.effects.version','선택 효과 버전','버전',1,1),type:'integer'}];
 for(const choice of CHOICE_DEFINITIONS)for(const [phase,fields] of [['immediate',IMMEDIATE_FIELDS],['during',DURING_FIELDS],...choice.outcomes.map(o=>['end.'+o,END_FIELDS])])for(const field of fields)entries.push(row(`choice.${choice.id}.${phase}.${field}`,`${choice.id} · ${phase} · ${field}`,field.includes('Percent')?(phase==='during'&&field.endsWith('PerMinute')?'%/분':'%'):field.endsWith('PerMinute')?'점/분':'점',field==='fatigueGrowthPercent'?-100:-100,field==='fatigueGrowthPercent'?1000:100));
 return entries;
}
export async function generate(root,{verify=false}={}){
 const file=path.join(root,'config/content/catalog.json');const catalog=JSON.parse(await fs.readFile(file,'utf8'));const legacy=[...catalog.entries.filter(e=>e.id!==PRESET_ENTRY.id&&!e.id.startsWith('choice.')&&!DRUNK_SETTINGS.some(d=>d.id===e.id)),...DRUNK_SETTINGS,PRESET_ENTRY];for(const e of legacy)if(/^request\.[^.]+\.(success|failure)$/.test(e.id)||['effect.food.satisfaction','manner.comfortMotion.penalty'].includes(e.id))e.description='이관 원본/호환 참조. 새 선택 효과는 이 값과 독립적으로 편집합니다.';catalog.entries=[...legacy,...generateChoiceEntries(legacy)];
 // Approved release defaults are separate from schema1 migration semantics.
 const overrideFile=path.join(root,'config/content/default-overrides.json');let overrides;try{overrides=JSON.parse(await fs.readFile(overrideFile,'utf8'));}catch(e){if(e.code!=='ENOENT')throw e;}
 if(overrides){if(overrides.schemaVersion!==1||!Array.isArray(overrides.values)||Object.keys(overrides).length!==2)throw Error('Invalid release defaults');const seen=new Set();for(const row of overrides.values){const entry=catalog.entries.find(e=>e.editable&&e.id===row.id);if(!entry||seen.has(row.id)||typeof row.value!=='string'||Object.keys(row).length!==2)throw Error('Invalid release default '+row.id);seen.add(row.id);entry.defaultValue=row.value;}}
 catalog.literalReplyIds=(overrides?.values||[]).filter(row=>/^request\.[^.]+\.reply\.[^.]+$/.test(row.id)&&row.value.length).map(row=>row.id);
 const profile={schemaVersion:2,id:'default',revision:1,contentHash:'',values:catalog.entries.filter(e=>e.editable).map(e=>({id:e.id,value:e.defaultValue}))};profile.contentHash=hashProfile(profile);const errors=validateProfile(profile,catalog);if(errors.length)throw Error(errors.join('\n'));
 for(const dir of ['config/content','unity/EungyeTaxi/Assets/Resources/Content'])for(const [name,data] of [['catalog.json',catalog],['default-profile.json',profile]]){const target=path.join(root,dir,name),text=JSON.stringify(data,null,2)+'\n';if(verify){if(await fs.readFile(target,'utf8')!==text)throw Error('Generated content differs: '+target);}else await fs.writeFile(target,text);}
 return {choices:CHOICE_DEFINITIONS.length,entries:catalog.entries.length,bytes:Buffer.byteLength(JSON.stringify(profile,null,2)+'\n')};
}
if(process.argv[1]&&path.resolve(process.argv[1])===fileURLToPath(import.meta.url))console.log(await generate(path.resolve(process.argv[2]||'.'),{verify:process.argv.includes('--verify')}));
