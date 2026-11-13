import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
const root = new URL('../../', import.meta.url);
const catalog = JSON.parse(fs.readFileSync(new URL('config/content/catalog.json', root)));
const sandbox = {}; vm.createContext(sandbox);
const modelFile = new URL('tools/content-editor/public/content-model.js', root);
if (fs.existsSync(modelFile)) vm.runInContext(fs.readFileSync(modelFile,'utf8'), sandbox);
const model = sandbox.ContentModel;
function entities(){ assert.ok(model, 'semantic ContentModel API must exist'); return model.build(catalog); }
test('all settings and all references are browseable semantic entities',()=>{
 const all=entities(); const ids=new Set(all.flatMap(e=>e.fieldIds));
 for(const field of catalog.entries) assert.ok(ids.has(field.id),field.id);
 assert.equal(all.filter(e=>e.kind==='request').length,17);
 assert.equal(all.filter(e=>e.id.startsWith('type.')).length,12);
 assert.equal(all.filter(e=>e.kind==='reference').length,76);
 const valid=new Set(catalog.entries.map(e=>e.id)); const entityIds=new Set(all.map(e=>e.id));
 assert.equal(entityIds.size,all.length);
 for(const e of all){for(const id of e.links) assert.ok(entityIds.has(id),`${e.id} -> ${id}`); for(const id of e.fieldIds) assert.ok(valid.has(id),id);}
});
test('request cards bind real reply text to independent staged effects and neutral defer',()=>{
 const all=entities(); const quiet=all.find(e=>e.id==='request.Quiet');
 const choices=quiet.sections.find(s=>s.kind==='choices').cards;
 assert.ok(choices[0].fieldIds.includes('request.Quiet.reply.Accept'));
 assert.ok(choices[0].effectGroups.flatMap(g=>g.fieldIds).includes('choice.request.Quiet.Accept.end.success.satisfaction'));assert.equal(choices[0].resultIds.length,0);
 const defer=choices.find(c=>c.fieldIds.includes('request.Quiet.reply.Defer'));
 assert.ok(!defer.fieldIds.includes('request.Quiet.failure'));
 const empathy=all.find(e=>e.id==='request.Empathy');
 const cards=empathy.sections.find(s=>s.kind==='choices').cards;
 for(const reply of ['Accept','Alternative']) assert.ok(cards.find(c=>c.fieldIds.includes(`request.Empathy.reply.${reply}`)).effectGroups.flatMap(g=>g.fieldIds).includes(`choice.request.Empathy.${reply}.end.success.satisfaction`));
 assert.ok(quiet.fieldIds.includes('common.requestCap'));
 assert.ok(all.find(e=>e.id==='request.Comfort').fieldIds.includes('choice.request.Comfort.Accept.during.motionPenaltyDelta'));
});
test('food choices expose actual permission, dirt, consumption timing and cleanup',()=>{
 const food=entities().find(e=>e.id==='type.Food');
 const cards=food.sections.find(s=>s.kind==='choices').cards;
 assert.ok(cards.find(c=>c.fieldIds.includes('type.Food.choice.1')).effectGroups.flatMap(g=>g.fieldIds).includes('choice.type.Food.1.immediate.satisfaction'));
 assert.ok(cards.find(c=>c.fieldIds.includes('type.Food.choice.2')).fieldIds.includes('effect.food.cleanupSeconds'));
 assert.ok(food.fieldIds.includes('effect.food.consumeMovingSeconds'));
 assert.ok(food.links.includes('situation.Jolt'));
});
test('law condition settings show the linked driving satisfaction by name',()=>{
 const all=entities(); const law=all.find(e=>e.id==='law.editorSpeed');
 assert.ok(law.fieldIds.includes('law.editorSpeed.toleranceKmh'));
 assert.ok(law.fieldIds.includes('law.editorSpeed.seconds'));
 assert.ok(law.fieldIds.includes('law.editorSpeed.penalty'));
 assert.match(law.notes.join(' '),/법규.*운전 만족도/);
 const rightTurn=all.find(e=>e.id==='law.redRight');
 const linked=rightTurn.sections.find(s=>s.fieldIds.includes('manner.rightStop.penalty'));
 assert.equal(linked.title,'함께 기록되는 운전 만족도');
 assert.match(linked.description,/운전 만족도 감점/);
});
test('weights 1, 1, 2 show a conditional 25%, 25%, 50% baseline',()=>{
 const all=entities(); const values=catalog.entries.map(e=>({id:e.id,value:e.defaultValue}));
 for(const v of values) if(/^request\..*\.weight$/.test(v.id))v.value='0';
 for(const [kind,weight] of [['Quiet',1],['Eta',1],['Comfort',2]]) values.find(v=>v.id===`request.${kind}.weight`).value=String(weight);
 for(const [kind,percent]of [['Quiet','25%'],['Eta','25%'],['Comfort','50%']]){
 const info=model.weightInfo(all.find(e=>e.id===`request.${kind}`),{values},catalog);
 assert.ok(info.text.includes(percent),info.text); assert.match(info.explanation,/후보|조건/); assert.match(info.explanation,/실제|전체/);
 }
 values.find(v=>v.id==='request.Quiet.enabled').value='false';
 assert.match(model.weightInfo(all.find(e=>e.id==='request.Quiet'),{values},catalog).text,/제외|사용 안함/);
 values.find(v=>v.id==='request.Eta.weight').value='0';
 assert.match(model.weightInfo(all.find(e=>e.id==='request.Eta'),{values},catalog).text,/제외|0/);
 const lost=model.weightInfo(all.find(e=>e.id==='request.LostItem'),{values},catalog);
 assert.match(lost.text,/10%/);assert.match(lost.explanation,/하차/);assert.match(lost.explanation,/가중치/);
});
test('condition explanations preserve food AND gate, stable recovery OR gate and exact law observations',()=>{
 const all=entities();
 assert.match(all.find(e=>e.id==='effect.food').summary,/진행도.*둘 다|모두/);
 assert.match(all.find(e=>e.id==='manner.recovery').summary,/거리 또는 시간/);
 assert.match(all.find(e=>e.id==='law.interference').summary,/대기열.*차로/);
 assert.match(all.find(e=>e.id==='law.stopLine').summary,/넘.*정차/);
 const quiet=all.find(e=>e.id==='request.Quiet');
 assert.match(quiet.sections.find(s=>s.kind==='unanswered').description,/미응답.*운전 만족도/);
 assert.match(all.find(e=>e.id==='request.Eta').sections.find(s=>s.kind==='outcomes').description,/현재 선택지 효과에서 편집/);
});
test('blank override choices still show the actual built-in player reply',()=>{
 const all=entities();const food=all.find(e=>e.id==='type.Food');
 assert.equal(food.sections.find(s=>s.kind==='choices').cards[1].title,'드셔도 괜찮아요');
 assert.equal(all.find(e=>e.id==='type.Luggage').sections.find(s=>s.kind==='choices').cards[0].title,'안전하게 짐을 싣고 내려드릴게요');
});
test('equal eligible weights show exact baseline and disabled candidates leave denominator',()=>{
 const all=entities();const quiet=all.find(e=>e.id==='request.Quiet');
 const profile={values:catalog.entries.map(e=>({id:e.id,value:e.defaultValue}))};
 const legacySchedule=JSON.parse(fs.readFileSync('tests/content-editor/fixtures/passenger-legacy-schedule.json'));for(const v of profile.values)if(Object.hasOwn(legacySchedule,v.id))v.value=legacySchedule[v.id];
 assert.match(model.weightInfo(quiet,profile,catalog).text,/6\.25% \(1 \/ 16\)/);
 profile.values.find(v=>v.id==='request.Eta.enabled').value='false';
 assert.match(model.weightInfo(quiet,profile,catalog).text,/6\.67% \(1 \/ 15\)/);
 for(const v of profile.values)if(/^request\..*\.weight$/.test(v.id))v.value='0';
 profile.values.find(v=>v.id==='request.Quiet.weight').value='1';
 assert.match(model.weightInfo(quiet,profile,catalog).text,/100% \(1 \/ 1\)/);
});
test('shared effect and common influence entities link back to actual consumers',()=>{
 const all=entities();const get=id=>all.find(e=>e.id===id);
 for(const id of ['type.Luggage','type.MuddyLuggage','situation.Jolt','situation.NecessaryBrake'])assert.ok(get('effect.resecure').links.includes(id),id);
 assert.ok(get('effect.sleep').links.includes('situation.Jolt'));
 assert.ok(get('effect.errand').links.includes('request.ViaStop'));
 assert.equal(get('common.schedule').links.filter(id=>id.startsWith('request.')).length,16);
 assert.ok(!get('common.schedule').links.includes('request.LostItem'));
 assert.ok(get('common.schedule').fieldIds.includes('constraint.requestCompatibility'));
 assert.ok(get('common.time').links.includes('common.fare'));
 assert.ok(get('common.comfort').links.includes('manner.recovery'));
 assert.ok(get('common.fare').links.includes('request.FareHelp'));
 for(const id of ['request.OnTime','request.SafeStop','request.DestinationChange','request.ViaStop'])assert.ok(get('common.deadline').links.includes(id),id);
});
test('illegal request refusal preserves fixed zero and separate violation observation',()=>{
 const request=entities().find(e=>e.id==='request.RejectIllegal');
 assert.match(request.sections.find(s=>s.kind==='outcomes').description,/현재 선택지 효과에서 편집/);
 assert.equal(catalog.entries.find(e=>e.id==='request.RejectIllegal.success').editable,false);
 assert.match(request.sections.find(s=>s.kind==='outcomes').description,/위반.*판정 불가/);
});
test('context links are bidirectional and independent postride event has no request schedule settings',()=>{
 const all=entities();const get=id=>all.find(e=>e.id===id);
 assert.ok(get('request.OnTime').links.includes('common.deadline'));
 assert.ok(get('request.FareHelp').links.includes('common.fare'));
 for(const entity of all)for(const link of entity.links)assert.ok(get(link).links.includes(entity.id),`${entity.id}<->${link}`);
 const lost=get('request.LostItem');
 assert.ok(!lost.links.includes('common.schedule'));
 assert.ok(!lost.fieldIds.includes('common.requestCap'));
 const recovery=get('law.recovery');
 assert.match(recovery.sections.find(s=>s.kind==='outcomes').description,/위반.*없는/);
 assert.match(recovery.sections.find(s=>s.kind==='outcomes').description,/0.*위반/);
});
test('request frequency overview gathers schedule and request weights with live per-ride average',()=>{
 const all=entities();const overview=all.find(e=>e.id==='frequency.request');
 assert.ok(overview&&overview.group==='승객 요청');
 assert.equal(all.filter(e=>e.group==='승객 요청')[0].id,'frequency.request');
 for(const id of ['schedule.count0','schedule.count1','schedule.count2','schedule.progressMin','schedule.progressMax','schedule.cutoff','schedule.movingSeconds','request.Quiet.weight','request.Quiet.enabled'])assert.ok(overview.fieldIds.includes(id),id);
 assert.ok(!overview.fieldIds.includes('request.LostItem.weight'));
 const profile={values:catalog.entries.map(e=>({id:e.id,value:e.defaultValue}))};
 assert.match(model.weightInfo(overview,profile,catalog).text,/운송당 평균 요청 1\.5건 · 0건 5% \/ 1건 40% \/ 2건 55%/);
 profile.values.find(v=>v.id==='schedule.count2').value='45';
 assert.match(model.weightInfo(overview,profile,catalog).text,/합계 90/);
});
