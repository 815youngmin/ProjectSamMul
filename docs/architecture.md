# 아키텍처 개요

이 문서는 코드를 처음 보는 사람이 전체 구조를 잡을 수 있도록 실행 흐름과 핵심 객체를 설명합니다.
개별 시스템의 세부 동작은 [gameplay-systems.md](gameplay-systems.md), 데이터와 리소스 규칙은 [data-and-resources.md](data-and-resources.md)를 보세요.

## 한 장 요약

```
Loading 씬 ──> Lobby 씬 ──(챕터·캐릭터·장비 선택)──> Stage 씬 ──(결과 팝업)──> Lobby 씬
                                                        │
                    GameClient ─ OfflineSession(저장)    │
                        └─ Stage ─┬─ StageEventController (시간표대로 몬스터/보스 스폰)
                                  ├─ PlayerCharacter ─ SkillSet(스킬) ─ Stats
                                  ├─ Monster[] ─ MonsterAIController(전략) ─ Action
                                  ├─ AreaEffect / Projectile / Item 오브젝트 풀
                                  └─ CombatSystem(판정) ─ AttackAreaFlashManager(범위 표시)
```

- 게임 로직은 `MonoBehaviour`가 아닌 일반 C# 객체(`GameClient`, `Stage`, 각 컨트롤러)가 들고 있고, 씬의 `BaseScene.Update()`가 매 프레임 `GameClient.Update()`를 호출해 돌립니다.
- 씬에 놓인 오브젝트(캐릭터, 장판, 투사체, 아이템)는 `MonoBehaviour`지만 스스로 `Update()`하지 않고 `Stage`가 순서대로 `UpdateLogic()`을 불러 줍니다. 실행 순서를 코드에서 통제하기 위한 규칙입니다.

## 씬 흐름과 초기화

| 씬 | 클래스 | 역할 |
|---|---|---|
| Loading | `LoadingScene`, `LoadingSceneUIRoot` | 첫 진입 씬. 로딩 바를 보여 주고 Lobby로 넘어감 |
| Lobby | `LobbyScene`, `LobbySceneUIRoot`, `MainLobbyPage` | 챕터·캐릭터·장비 라디오 선택, 시작 버튼 |
| Stage | `StageScene`, `StageSceneUIRoot` | 스테이지 생성·루프, HUD, 팝업 |

- 씬 전환은 `UnityGlobal.Scenes.ChangeTo(SceneType, ISceneInitialData, reason)` 한 곳으로만 합니다. `SceneChanger`가 새 씬의 `BaseScene.Initialize(initialData)`를 호출하고, 떠나는 씬에는 `ClearBeforeChangingScene`으로 풀링된 오브젝트를 정리하게 합니다.
- 전역 서비스는 `UnityGlobal`(씬 전환, 사운드, 스프라이트 애니메이션)과 `GameClient`(세션, 스테이지, 카메라, 플레이어 컨트롤러)에 모여 있습니다. 둘 다 정적 접근자를 제공하지만, 스테이지 안의 코드는 가능한 한 인자로 받은 `Stage`를 씁니다.
- 정적 데이터(`StaticDataRepository.Instance`)와 `ResourcePool.Instance`는 첫 씬에서 한 번 만들어지고 앱이 끝날 때까지 유지됩니다.

`StageScene.Initialize()`는 다음 순서로 스테이지를 준비합니다.

1. `StageSceneInitialData`(챕터, 스킬 덱, 영웅 데이터, 장비)를 받는다. 에디터에서 Stage 씬을 직접 Play하면 개발용 기본값을 만든다.
2. 바닥·경계 콜라이더를 만들고(`InitializeFloor`), 몬스터 리소스를 미리 캐시한다.
3. `GameClient.CreateMainChapterStage()`가 `Stage`를 만들고 `Stage.Initialize()`가 플레이어를 생성한 뒤 스테이지 타이머를 시작한다.
4. `StageSceneUIRoot.Initialize()`가 HUD를 연결하고, 조이스틱으로 `PlayerCharacterController`를 만든다.

## 프레임 루프

`Stage.Update()`는 `Time.timeScale`이 0이면(팝업이 떠 있으면) 아무것도 하지 않습니다. 그 외에는 아래 순서로 갱신합니다.

1. `ZoneManager`가 모든 캐릭터를 5x5 유닛 격자에 분류한다. 범위 탐색(`FindAliveCharactersInArea` 등)은 이 격자를 사용한다.
2. 캐릭터 갱신 (`UpdateCharacters`) — 플레이어와 몬스터의 `UpdateLogic`: 액션, 상태이상, 애니메이션, AI.
3. 투사체 → 장판(AreaEffect) → 인디케이터 → 공격 범위 표시 → 획득 아이템 → 파괴 아이템 → 아이템 상자 스폰 → 파티클 → 사망 이펙트 순으로 갱신.
4. 스테이지 결과가 확정되었으면(`_stagePlayResult`) 1.85초 뒤 부활 팝업 또는 결과 팝업을 띄우고 종료.
5. 아니면 `StageEventController.Update()`가 스테이지 시각을 흘리고 시각이 된 이벤트를 실행한 뒤, `CheckStagePlayResult()`로 실패(플레이어 사망)·클리어(마지막 이벤트 시각 경과)를 판정한다.

## 폴더 맵

`Assets/Scripts` 아래 653개 파일이 있습니다. 데모에서 실제로 실행되는 부분을 굵게 표시했습니다.

| 폴더 | 내용 |
|---|---|
| `Scenes/` | **씬 루트와 UI 루트** (`BaseScene`, `BaseSceneUIRoot`와 Loading/Lobby/Stage 구현) |
| `GameClients/` | **`GameClient`, `OfflineSession`(로컬 세션·저장), 카메라, `Heroes/SkillDeck`** |
| `GameClients/Stages/` | **`Stage` partial 클래스들**(캐릭터·장판·투사체·아이템·인디케이터 생성/관리) |
| `GameClients/Stages/StageEvents/` | **스테이지 이벤트**(몬스터 스폰, 엘리트, 대량 스폰, 자동 리스폰, 보스 경고/스폰, 러시 경고) |
| `GameClients/Stages/Characters/` | **`Character` 기반 클래스, `PCs/PlayerCharacter`, `Monsters/Monster`** |
| `.../Characters/Actions/` | 캐릭터 행동 단위(대기, 이동, 근접 공격, 원거리 공격, 돌진, 소환, 사망 등). `Boss/` 하위는 이전 테마 보스용으로 데모에서 미사용 |
| `.../Characters/Monsters/MonsterAIs/` | **`MonsterAIController`와 `AIStrategies/`** (전략 패턴). `BossAIStrategies/`는 미사용 |
| `.../Characters/PCs/Skills/` | **`SkillBase`, `SkillSet`, 스킬 구현** (덱에 없는 스킬 파일도 남아 있음) |
| `.../Characters/Stats/`, `StatusEffects/`, `ConditionalEffects/` | **스탯 계산기, 상태이상, 등급 효과(조건부 효과)** |
| `GameClients/Stages/AreaEffectObjects/` | **장판·이동 오브젝트 127종**. 데모 스킬과 몬스터가 쓰는 것은 일부 |
| `GameClients/Stages/ProjectileObjects/` | **투사체와 몸체(스프라이트/스켈레톤)** |
| `GameClients/Stages/ItemObjects/` | **경험치, 골드, 체력, 자석, 폭탄, 스킬 상자, 아이템 상자, 울타리** |
| `GameClients/Stages/CombatSystems/` | **판정 영역 구조체(원·사각·부채꼴), 범위 타격, 데미지·넉백 계산** |
| `GameClients/Stages/AreaIndicators/`, `IndicatorObjects/` | **공격 범위 표시**(`AttackAreaFlashManager`), 인디케이터 |
| `Shared/` | **정적 데이터 타입·로더, 유저 데이터, 프로토콜 요청/응답 타입, 로컬라이저, 게임 상수** |
| `Animations/Placeholder/` | **Spine 런타임 대체.** `SkeletonAnimation`, `AnimationState`, 트랙/이벤트 API를 같은 이름으로 제공 |
| `ResourcePools/` | **`ResourcePool`, `PlaceholderFactory`** |
| `UIs/` | **로비 UI, 스테이지 HUD, 팝업, 공용 버튼(`ZButton`), 조이스틱** |

## 핵심 객체와 소유 관계

- `GameClient` → `OfflineSession`(유저 데이터·저장), `Stage`(현재 스테이지, 없으면 null), `CameraController`, `PlayerCharacterController`.
- `Stage` → `StageEventController`, `ZoneManager`, 캐릭터 풀·투사체 풀·장판 풀·아이템 풀·인디케이터 풀, `DamagePopupManager`, `DeadEffectManager`, `ParticleManager`, `AreaIndicatorManager`, `AttackAreaFlashManager`.
- `Character`(추상) → `CharacterStatCalculators`(스탯), `StatusEffectSet`(상태이상), `ActionBase`(현재 행동), `CharacterAnimationController`(애니메이션).
  - `PlayerCharacter` → `SkillDeck`(이번 판에 배울 수 있는 스킬), `SkillSet`(배운 스킬), `ConditionalEffectManager`(등급·장비 효과), `LevelCalculator`.
  - `Monster` → `MonsterAIController`(전략), `MonsterStaticData`.
- 스테이지의 모든 동적 오브젝트는 `IPoolible<TKey>`를 구현하고 `ObjectPools/`의 풀로 재사용됩니다. 재사용 시 초기화는 `Initialize...()`, 반환 시 정리는 `PuttingBackToPool()`에 적습니다.

## 자주 쓰는 진입점

| 알고 싶은 것 | 시작 파일 |
|---|---|
| 스테이지 한 판의 시작과 끝 | `Scenes/StageScene.cs`, `GameClients/Stages/Stage.cs` |
| 몬스터가 언제 나오는가 | `StageEvents/StageEventController.cs`, `Resources/StaticData/StageEvents.json` |
| 몬스터가 어떻게 움직이는가 | `Monsters/MonsterAIs/MonsterAIController.cs`, `AIStrategies/*` |
| 스킬이 어떻게 발동하는가 | `PCs/Skills/SkillBase.cs`, `SkillSet.cs`, 각 `*Skill.cs` |
| 데미지가 어떻게 들어가는가 | `CombatSystems/CombatSystem.Area.cs`, `Character.Hitted()` |
| 리소스가 없을 때 무슨 일이 생기는가 | `ResourcePools/ResourcePool.cs`, `PlaceholderFactory.cs` |
| 팝업을 띄우는 법 | `Scenes/BaseSceneUIRoot.cs`의 `CreateAndAddPopup`, `StageSceneUIRoot.cs`의 `Add*Popup` |
