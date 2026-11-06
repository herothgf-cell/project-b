import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
const root=new URL('../../',import.meta.url);
const catalog=JSON.parse(fs.readFileSync(new URL('config/content/catalog.json',root)));
const context=vm.createContext({});
vm.runInContext(fs.readFileSync(new URL('tools/content-editor/public/content-model.js',root),'utf8'),context);
const model=context.ContentModel;

test('request evaluation, no answer and shared settings are separate and score editors have one home',()=>{
 for(const e of model.build(catalog).filter(e=>e.kind==='request')){
  assert.ok(e.sections.some(s=>s.title==='운송 종료 평가'),e.id);
  assert.ok(e.sections.some(s=>s.title==='답하지 않았을 때'),e.id);
  const editorIds=e.sections.flatMap(s=>[...(s.fieldIds||[]),...(s.cards||[]).flatMap(c=>c.fieldIds||[])]);
  for(const suffix of ['success','failure','unanswered'])assert.equal(editorIds.filter(id=>id===`${e.id}.${suffix}`).length,1,`${e.id}.${suffix}`);
  const cards=e.sections.find(s=>s.kind==='choices').cards;
  for(const card of cards)assert.ok(!card.fieldIds.some(id=>/\.(success|failure|unanswered)$/.test(id)),e.id);
 }
});

test('every setting explains when and who it affects without exposing method names',()=>{
 assert.equal(typeof model.fieldInfo,'function');
 for(const e of catalog.entries){const info=model.fieldInfo(e.id,catalog);assert.ok(info.when?.length,e.id);assert.ok(info.affects?.length,e.id);assert.ok(!/PassengerRequestUI|ContentTuning\./.test(info.when+' '+info.affects),e.id);}
 const quiet=model.fieldInfo('request.Quiet.unanswered',catalog);
 assert.match(quiet.when,/하차|운송 종료/);assert.match(quiet.affects,/조용한 운행/);assert.match(quiet.detail,/M/);
 const cap=model.fieldInfo('common.requestCap',catalog);
 assert.match(cap.when,/합산|하차/);assert.match(cap.affects,/모든|전체/);assert.match(cap.detail,/8.*5.*10/);
 assert.match(model.fieldInfo('effect.food.spill',catalog).affects,/음식/);
 assert.match(model.fieldInfo('effect.resecure.seconds',catalog).affects,/짐/);
});

test('followup restriction describes blocked dialogue and preserved requests instead of a probability',()=>{
 const quiet=model.build(catalog).find(e=>e.id==='request.Quiet');
 assert.match(quiet.sections.flatMap(s=>(s.cards||[]).flatMap(c=>[c.description,...(c.notes||[])])).join(' '),/추가 상황/);
 assert.match(quiet.notes.join(' '),/확률/);
 assert.match(quiet.notes.join(' '),/별도.*요청/);
 assert.match(quiet.notes.join(' '),/필수.*안내/);
});

test('ETA fixed zero and empathy dormant failure are labelled as unsupported values',()=>{
 const eta=model.build(catalog).find(e=>e.id==='request.Eta');
 assert.match(eta.sections.find(s=>s.kind==='outcomes').description,/읽기 전용 0점/);
 for(const suffix of ['success','failure','unanswered'])assert.equal(catalog.entries.find(e=>e.id===`request.Eta.${suffix}`).editable,false);
 assert.equal(model.fieldInfo('request.Empathy.failure',catalog).unused,true);
 assert.match(model.fieldInfo('request.Empathy.failure',catalog).detail,/실패 판정 경로가 없/);
});
