# Content Tuning Tool Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 사용자가 별도 관리창을 실행해 현재 승객/대화/효과/게임 법규 설정을 바꾸고 실제 통합 실행본에서 시험한다.

**Architecture:** 버전 JSON 카탈로그와 전체 프로필을 Node 관리창·순수 C# 평가 코어가 공유한다. 런타임은 세션당 고정한 프로필을 모든 기존 소비자에 제공하고 저장과 LAN에 포함한다. 로컬 서버와 UI는 별도 파일 범위에서 병행 개발하고 통합 시 C# 실제 평가·배포를 검사한다.

**Tech Stack:** Node24 표준 라이브러리, HTML/CSS/JS, Unity2022.3 C#, PowerShell, 기존 .NET Framework csc 모델 검사.

**Spec:** `docs/superpowers/specs/2026-11-05-content-tuning-tool-design.md` (사용자2026-11-05 구현 승인)

## Global Constraints

- 외부 서비스·로그인·추가 패키지·공개 호스팅 없이 사용한다. 서버는127.0.0.1만 바인딩한다.
- 첫 값은 현행 동작과 같다. 요청17종·특수11유형·법규76개 색인과 구현 상태를 구분한다.
- 새 싱글 캠페인/새 PVP 방부터 적용, 이어하기는 저장 설정 snapshot 유지. 테스트 실행은 기본 사용자 저장과 분리한다.
- LAN 최종 설정은 호스트. 기존 프로토콜과 혼용 거부, 기본 은계 싱글/A2 PVP·회사/제품/지도/저장 위치 유지.
- 기본 실행은 `builds/EungyeTaxi/EungyeTaxi.exe`, 빌드는 `PrototypeBuild.BuildHybrid`다. 배포 manifest 검증을 갱신한다.
- 현재 HUD 작업을 보존한다. 분리 worktree의 소스 완료 후 원래 폴더 최신 상태에 작은 diff로 통합하고 기본 폴더에서 최종 빌드한다.

## Review Focus

- 문자열에 줄바꿈/따옴표/유니코드가 있는 프로필도 Node/C# hash가 같아야 한다 (Task1/2 교차 fixture).
- 변경 프로필로 옛 운송을 이어도 예전 시간 상한/약정이 바뀌면 안 된다 (Task3 복원).
- 대사에 HTML을 넣어도 실행되지 않아야 한다 (Task2 UI textContent·브라우저 확인).
- 게임 실행 중 편집/저장해도 적용 완료를 오인하지 않아야 한다 (Task2/3 다음 세션 안내).
- 실행본 업데이트/사용자 active 설정 변경 뒤에도 정식 파일 hash 오류를 숨기면 안 된다 (Task4 manifest).

## Shared interfaces

Catalog `config/content/catalog.json`: `{schemaVersion:1,entries:[{id,group,section,title,type,defaultValue,min,max,unit,description,affects:[string],modes,status,consumer,editable}]}`. All IDs ASCII; types `number`,`integer`,`text`,`boolean`; all profile values strings. `editable:false` is reference-only and excluded from profile values. Catalog may include `references` array for legal index, kept read-only.

Profile `{schemaVersion:1,id:"default",revision:1,contentHash:"sha256 lowercase",values:[{id:"...",value:"..."}]}`. Unknown/duplicate/missing editable IDs or unknown JSON members reject. Hash UTF8 of `content-profile-v1\n` + profile.id + `\n` + revision invariant integer + `\n`, followed by each value sorted by ordinal ID: id + `\t` + UTF8 byte length(value) + `:` + value + `\n`. No BOM. Full profiles, no partial overrides. `config/content/default-profile.json` is copied byte-for-byte with catalog to Unity Resources.

Pure C# `ContentTuning` API: `Number(string id,double fallback)`, `Int(string id,int fallback)`, `Text(string id,string fallback)`, `Flag(string id,bool fallback)`, `CaptureJson()`, `UseJson(string json)`, `UseDefaults()`, `ValidateJson(string json)`, `ConfigureCatalog(string json)`. Static immutable active profile per process (single native world); baseline fallbacks permit existing model tests without Unity. `UseJson` atomic validate then install. Runtime adapter `ContentTuningRuntime`: `LoadActive()`, `Restore(string json)` (null=>defaults), `Snapshot()`, `Status`, `TestMode`, `Initialize()`; active path is executable sibling `content/active-profile.json`. `-contentTest` and `-contentProfile <path>` select isolated test settings; normal application must not take arbitrary profile paths except this explicit local testing launch.

Numeric IDs follow domain semantic names. Core implementer owns the definitive catalog and informs other implementers of concrete IDs; they must not invent competing core schema. Existing labels are fallback if no text override. Stable text IDs: `request.<RequestKind>.prompt`, `request.<RequestKind>.reply.<PassengerReply>`, `type.<PassengerType>.intro.<variant>`, `type.<PassengerType>.choice.<index>`, `situation.<kind>.reply.<index>`. Extra IDs may be added for followups with consumers.

Preview: game executable invoked with `-batchmode -nographics -contentPreview <input.json> -contentPreviewOutput <output.json>` where input contains full profile and sample calculation inputs. Same C# core computes result then exits. Local server spawns only fixed executable with validated input and generated bounded temp paths; no shell.

### Task1: Catalog, C# core, actual passenger/law consumers

**Files:** create `ContentTuning.cs`, `ContentJson.cs` in Scripts, `config/content/*`, matching Resources JSON, `tests/ContentTuningTests.cs`, `tools/test_content_tuning.ps1`. Modify PassengerRequest/Schedule/Observation/UI, PassengerBehavior/UI, PassengerSituations, existing type scheduling, MannerLedger, PrototypeState/DrivingLaw and necessary supported consumer files; update model test scripts with core sources.

**Consumes:** shared profile/catalog wire contract. **Produces:** pure C# API above, full registry linked to real consumers and law reference76 index.

- [ ] RED tests default equivalence, changed reward/dirt/work time/request draw/penalty, strict schema/duplicate/missing/range/hash rejection and Unicode hash fixture; run and observe missing feature.
- [ ] Implement strict pure JSON/profile codec and registry, actual numeric/text consumers and generated effects, preserving legacy fare policy.
- [ ] GREEN run `pwsh -File tools/test_content_tuning.ps1` and affected existing model suites. Record exact outputs and defaults equivalence; commit scoped source/tests/catalog.

### Task2: Local management server, UI, launchers

**Files:** create `tools/content-editor/{server.mjs,profile.mjs,public/index.html,public/app.js,public/style.css}`, Node tests under `tests/content-editor/`, `tools/start_content_editor.ps1`, root `설정 관리.cmd`.

**Consumes:** shared catalog/profile contract, same C# preview executable flags. **Produces:** `node tools/content-editor/server.mjs --root <repo> --port <number>` server and testable exported start/create app function. Output `CONTENT_EDITOR_URL=http://127.0.0.1:<port>/...`.

- [ ] RED Node tests full profile roundtrip/hash/UTF8, invalid fields/duplicates/ranges, save conflict/atomic persistence, Origin/token/body/path rejection, active apply pending status, fixed test launch/preview and no-profile game error. Run `node --test tests/content-editor/*.test.mjs`.
- [ ] Implement server profile storage/backups outside build, local-only access and session token. UI five groups/search/filter/detail, defaults/current comparison, typed field editing, related effects/consumers, validate/save/apply/reset/import/export, sample preview and isolated test launch. Use textContent for authored text.
- [ ] GREEN Node suite, actual browser at1280 and smaller width; launcher opens browser via hidden background server, no install. Commit only owned files.

### Task3: Unity runtime, checkpoint/LAN snapshots, actual preview/test launch

**Files:** create `ContentTuningRuntime.cs`, `ContentTuningProbe.cs`, Editor `ContentTuningChecks.cs`; modify `BusinessProgress.cs`, `PvpNativeLan.cs` and necessary startup files, `CampaignCheckpoint` field.

**Consumes:** core API and fixed executable flags. **Produces:** immutable active session; `CampaignCheckpoint.contentProfileJson`; host room full profile `contentProfileJson`; bumped LAN protocol; actual pure-core preview JSON result.

- [ ] RED tests new session active load, invalid fallback status, saved snapshot wins over disk, legacy null baseline, failed restore does not mutate active global, LAN full snapshot/schema/hash, isolated test save paths and preview actual C# reward calculation.
- [ ] Implement adapter and small lifecycle hooks. Start each new campaign/room load; resume uses snapshot before validating passenger state. Checkpoint serializes snapshot. Host sends profile before constructing client world/any scoring; reject unsupported before start. Test mode reroutes all campaign save/log artifacts and must not touch user saves.
- [ ] GREEN tests via Unity Editor checks when available and record run method. Avoid starting/building Unity while parent uses it. Commit scoped files.

### Task4: Integration, build/distribution, documentation

**Files:** integration fixes, `PrototypeBuild.cs`/new build guard, distribution script/manifest, `docs/content-tuning-tool-guide.md`, current planning/checklist/history/LATEST.

**Consumes:** Tasks1–3, parent integrates/reviews. **Produces:** actual executable+launch tool, strict shipment verification with separately reported valid user content override.

- [ ] Verify default catalog Resource byte equality and all editable entries have consumer. Compile and run complete existing model suites plus Node tests; C# preview hash fixture cross-check.
- [ ] Integrate original checkout latest HUD work without overwriting it; run Strict workspace; BuildHybrid exit0; actual Player tests changed setting display/result, legacy resume, test save isolation, host/client sync/reconnect and malformed rejection.
- [ ] Manifest only exempts exact validated user profile location; all shipped assets still full hash checked. Capture browser screenshot and actual edited setting/preview; leave default active values after verification and editor available for user.
- [ ] Fresh whole-branch review, resolve important findings with regression verification. Update approval status/spec, guide with double-click executable instructions and application timing. Record actual vs untested validation. Commit source separately from build using actual source hash; final manifest verification and real history commit check.

## Execution ledger

2026-11-05: implementation authorized. Isolated native worktree created from e25fd5d. Original checkout has active HUD/build changes; preserve them. Tasks1/2/3 share fixed wire/API above but own separate file sets; integration is Task4. No user permission needed between reversible steps. Parent coordinates integration and task reviews; report artifacts remain in `artifacts/content-tuning/`.
