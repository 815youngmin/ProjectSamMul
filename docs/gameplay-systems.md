# 게임플레이 시스템

각 시스템이 어떤 클래스로 구성되고 데이터가 어디서 오는지 정리했습니다. 전체 흐름은 [architecture.md](architecture.md)를 먼저 보세요.

## 1. 스테이지 이벤트 (몬스터 스폰 시간표)

- 데이터: `StageEvents.json`. 행 하나가 이벤트 하나이며 `StageNumber`, `EventType`, `BeginAt`, `EndAt`, `TickPeriod`, `MonsterType`, `Amount`, `MonsterHPWeight`, `MonsterAttackPowerWeight`, `DropItems` 등을 가집니다.
- `StageEventController`는 생성 시 모든 이벤트를 **실행 정보 목록**으로 펼칩니다. 이벤트마다 `Begin`(BeginAt), `Update`(BeginAt부터 TickPeriod 간격으로 EndAt까지, 틱 번호 증가), `End`(EndAt) 호출이 시각순으로 정렬됩니다.
- 매 프레임 스테이지 시각을 `Time.deltaTime`만큼 흘리고, 시각이 지난 실행 정보를 순서대로 실행합니다. 마지막 실행 정보의 시각이 `MaxStageTime`이며 이를 1초 넘기면 클리어입니다.
- 보스 스폰 이벤트(`BossSpawnStageEvent`)는 시작할 때 타이머를 **일시정지**(`PauseStageTimer`)하고 몬스터를 모두 제거한 뒤 보스를 만듭니다. 보스가 죽으면 `End`에서 타이머를 재개합니다. 일시정지 중에는 이 이벤트의 `Update`만 호출됩니다.
- 이벤트 종류: `StageEnterInit`(입장 초기화), `MonsterSpawn`, `EliteSpawn`, `MonsterMassSpawn`, `AutoRespawnMonster`(주기 리스폰), `RushWarning`, `BossWarning`, `BossSpawn`. 나머지 타입은 이전 챕터용으로 데모 데이터에 없습니다.
- 테스트: `Stage.TEST_JumpToBossBattle(n)`이 n번째 보스 스폰 5초 전으로 시각을 옮깁니다. `ForceMoveExecutionIndex`가 이동 지점 10초 전부터의 이벤트를 다시 실행합니다.

## 2. 캐릭터

`Character`(추상 `MonoBehaviour`)가 플레이어와 몬스터의 공통 부분을 맡습니다.

- 스탯: `CharacterStatCalculators`. 기본값에 `StatModifier`(Flat / PercentAdd / PercentMult)를 쌓아 최종값을 계산합니다. 스킬·장비·등급 효과는 모두 모디파이어를 더하고 빼는 방식입니다.
- 피격: `Hitted(stage, attacker, damage, hitVector, ...)`가 치명타·넉백저항·상태이상을 반영해 HP를 깎고, 데미지 팝업과 피격 애니메이션(`BeginHittedBodyEffect`)을 실행합니다. HP가 0이면 `Dead`.
- 행동: 한 번에 하나의 `ActionBase`만 실행합니다(`_action.ChangeTo`). 액션은 `Begin / Update / End`를 가지며 `End`가 다음 액션을 돌려주면 자동으로 이어집니다. 애니메이션 재생도 액션이 담당합니다.
- 상태이상: `StatusEffectSet`에 `StatusEffect`를 추가·갱신합니다(`Burn`, `SpreadBurn`, `Stun`, `SlowMove`, `SuperArmor`, `Freeze`, `PushedBack`, `Invisible` 등). 같은 종류가 이미 있으면 `updateThresholdTime` 규칙으로 갱신합니다.
- 애니메이션: `CharacterAnimationController`가 추상 인터페이스이고, 플레이어는 `PCAnimationController`(+ 스프라이트 몸체 `PCSpriteBody`), 스프라이트 몬스터는 `SpriteMonsterAnimationController`, 스켈레톤 몬스터는 `MonsterAnimationController`입니다. 몸체 리소스 경로의 확장자가 `.asset`이면 스켈레톤, 그 외(`.controller`)면 Animator 기반 스프라이트로 만듭니다.

### 플레이어 (`PlayerCharacter`)

- 생성 시 `BaseStats.FromHeroData`로 기본 스탯(HP·공격력은 `HeroStats` 등급표, 이동속도는 `Heroes`)을 넣고, 장비 스탯과 등급 효과를 `ConditionalEffectManager`로 적용합니다.
- `PlayerCharacterController`가 조이스틱(에디터에서는 WASD)을 읽어 `Move`를 호출합니다.
- 경험치는 `ReserveToGainExp`로 모아 두었다가 `UpdateLevelExp`에서 레벨업을 처리하고, 레벨업마다 스킬 선택 팝업을 띄웁니다.
- 죽으면 `Stage.CheckStagePlayResult`가 실패를 반환하고, 부활 팝업(한 게임에 한 번) 또는 결과 팝업으로 이어집니다. 부활은 `Stage.OnResurrected`가 처리합니다.

### 몬스터 (`Monster`)

- `Monsters.json` 한 행이 몬스터 한 종입니다. 스폰 이벤트의 HP·공격력 가중치가 곱해져 `BaseStats.FromMonsterStaticData`로 들어갑니다.
- 행동은 `Monster.DoAction.cs`의 `Do*` 메서드(근접·원거리·돌진·소환·반사탄 등)로 시작하며, AI 전략이 이 메서드를 호출합니다.
- 죽으면 경험치·아이템을 떨어뜨리고(`DropExp`, `DropItems`) 사망 이펙트를 남긴 뒤 풀로 돌아갑니다.

## 3. 몬스터 AI (전략 패턴)

- `MonsterAIController`가 `Monsters.json`의 `AIType`을 보고 **첫 전략**을 만듭니다(`case MonsterAIType.X:`). 대부분의 AI는 `XxxIdleAIStrategy`(대기·탐색)와 `XxxCombatAIStrategy`(추적·공격) 두 클래스로 구성됩니다.
- 전략은 `MonsterAIStrategyBase`를 상속하고 `Begin / Update / End / OnHitted / OnDead`를 구현합니다. `Update`가 다른 전략 인스턴스를 반환하면 컨트롤러가 `End → Begin`으로 교체합니다.
- 데모에서 쓰는 AI: `MeleeAttackAI`, `RangeAttackAI`, `DashAttackAI`, `PassByAI`, `PoisonousAreaAI`, `EliteSummonAI`, `EliteReflectionRangeAttackAI`, `AreaEffectOnDieAI`, 그리고 보스 전용 `DemoFirstBossAI` ~ `DemoSixthBossAI`.
- 탐색 범위 상수: `GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE`(48). 탐색은 `Stage.FindClosestCharacter` 계열을 사용합니다.

### 보스 AI와 액션

- 보스는 `BossAIStrategies/Demo/Demo<N>BossAIStrategy.cs`에 `BossIdleAIStrategy`와 `BossCombatAIStrategy`를 상속한 클래스 한 쌍을 가집니다. Idle은 등장 후 `WaitingTime`만큼 기다렸다가 Combat으로 넘어갑니다.
- Combat 전략은 `Actions` 목록(`Monster.DoSpriteMonster<액션>`)에서 하나를 무작위로 골라 실행하고, 액션이 끝난 뒤 `ActionCoolTime` 동안은 `MovementType`에 따라 이동만 합니다. 목록이 비어 있으면 타겟을 따라 이동만 합니다.
- 액션은 `Actions/Boss/Demo/`에 있으며 `SmartAction<SpriteMonsterAnimationController>`를 상속합니다. `Begin`에서 `AddOneOffSubAction(시각, ...)`과 `AddDurationalSubAction(시각, 길이, ...)`으로 예고 → 공격 → 후딜의 시간표를 등록하고, 길이는 상단의 상수로 조절합니다. 진행 시간이 정해지지 않는 패턴(`DemoSixthBossDashReflectionObjetAction`)은 `Update`에서 상태 머신으로 처리합니다.
- 애니메이션 규칙: 예고 중에는 `PlayIdleAttackActionInfinitely()`, 공격 순간에는 `PlaySpecialAttack(길이)`, 방향은 `UpdateBodyDirectionByMoveDirection`으로 맞춥니다.
- 범위 예고는 `stage.CreateDashAttackRangeIndicator`, `CreateCircularAttackRangeIndicator`, `CreateDirectionalSquareRangeIndicator`를 씁니다(프리팹은 `Stage/Indicator/`).

| 보스 | 등장 | 액션 |
|---|---|---|
| 1 티라노 | 1챕터 150초 | 돌진, 부채꼴 투사체 |
| 2 매머드 족장 | 1챕터 300초 | 유도 오브젝트, 돌진 |
| 3 벌레 여왕 | 2챕터 150초 | 나선 이동 장판, 반사 오브젝트 |
| 4 거대 얼음 골렘 | 2챕터 300초 | 돌진 중 원형 탄막, 엘리트 소환 |
| 5 뱀 주술사 | 3챕터 90초 | 분열 오브젝트, 몬스터 소환, 돌진 |
| 6 라 | 3챕터 300초 | 맵 밖 돌진 + 반사 오브젝트, 반사 오브젝트, 다중 장판, 몬스터 소환 |

- 보스의 HP와 공격력은 `Monsters.json`의 기본값에 `StageEvents.json` `BossSpawn` 행의 가중치를 곱한 값입니다. 스킬·투사체 피해는 여기에 액션의 `DAMAGE_COEFFICIENT`를 곱합니다.

## 4. 스킬

- 데이터: `Skills.json`. 스킬 ID와 레벨(1~5, 초월은 6) 조합이 한 행이며 `Parameter1~6`의 의미는 스킬 클래스 생성자 주석에 적혀 있습니다. `TranscendCondition`은 초월에 필요한 패시브 스킬입니다.
- `SkillDeck`(`GameClients/Heroes`): 이번 판에서 배울 수 있는 스킬 목록. `GameConstants.SKILLDECK_DEFAULT_SEASON_SKILLDECK` 전체가 사용 가능하며, 캐릭터 기본 스킬과 초월 조건 패시브를 보정해 넣습니다. 레벨업 후보 3개 선택(`Select3SkillsToLearn`)과 스킬 상자 선택(`SelectSkillsToLearnBySkillBox`)도 여기 있습니다.
- `SkillSet`: 실제로 배운 스킬 인스턴스. 액티브 5 / 패시브 5 슬롯. `AcquireOrUpgradeSkill`이 새 스킬을 만들거나 레벨을 올리며, 이때 `Activate`가 불리고 매 프레임 `Update`가 돌아갑니다.
- 스킬 클래스는 `SkillBase`를 상속합니다. 대부분의 액티브 스킬은 `Update`에서 쿨타임을 재고 `stage.CreateXxxAreaEffectObject(...)` 또는 `stage.CreateProjectile(...)`로 오브젝트를 만듭니다. 캐릭터 기본 스킬(`TentiSweep`, `IgnitionWave`, `SpaceCoin`)은 `Parameter2`를 플레이어 공격속도에 더해 주며, 이 값이 0이면 기본 공격이 나가지 않습니다.
- 데미지 계산은 `CombatSystem.CalculateSkillAttackDamage(stats, rate)`, 넉백은 `CalculateSkillAttackKnockBackPower`로 통일합니다.

## 5. 장판 · 투사체 · 아이템

- **장판/이동 오브젝트** (`AreaEffectObjectBase`): `IsAlive`가 false가 되면 스테이지가 풀로 돌려보냅니다. `UpdateLogic(stage, deltaTime)`에서 이동과 판정을 함께 합니다. 생성은 `Stage.AreaEffect.cs`의 `CreateXxx` 메서드, 타입별 생성은 `AreaEffectObjectPool`의 `switch`.
- **투사체** (`ProjectileObject`): 직선 이동, 콜라이더 충돌, 관통 횟수(`hitChances`), 분열(`splitCount`). 몸체는 프리팹의 `SpriteRenderer`(자식 포함) 또는 `SkeletonAnimation`으로 자동 판별합니다.
- **아이템** (`ItemObjectBase`): 획득형(`AcquirableItemObject`: 경험치, 골드, 체력, 자석, 스킬 상자)과 파괴형(`BreakableItemObject`: 아이템 상자, 울타리). 몸체 프리팹 경로는 `ItemObjectBase.GetItemObjectBodyPrefabPath`에 있습니다.
- 데모 공용 몸체: 전용 리소스가 없는 플레이어 스킬 오브젝트는 `PlayerAttackVisual.Attach(parent, diameter)`로 `Stage/Common/PlayerAttackVisual.prefab`을 판정 지름에 맞춰 붙입니다.

## 6. 전투 판정과 범위 표시

- 판정 영역은 `CombatSystems/TargetArea.cs`의 구조체 `CircularTargetArea`, `SquareTargetArea`(회전 가능), `CircularSectorTargetArea`입니다.
- `CombatSystem.HitOnTargetArea(stage, area, attacker, damage, knockBackType, pivot, power, hittedCollector, excepted, hitSfx)`가 영역 안의 적을 찾아 `Hitted`를 호출합니다. `hittedCollector`에 담긴 캐릭터는 같은 공격 주기 안에서 다시 맞지 않게 하는 용도입니다.
- 이 함수는 **판정 영역을 `AttackAreaFlashManager`로 그립니다.** 플레이어 진영은 파란색, 적 진영은 빨간색이며 0.15초 동안 사라집니다. 매 프레임 판정하는 오브젝트는 결과적으로 범위가 계속 보입니다. 직접 `Hitted`를 부르는 오브젝트(투사체, 바운싱 클로 등)는 필요하면 `stage.AttackAreaFlashes.Show(area, color)`를 직접 호출합니다.
- 독장판과 원형 깜빡임 인디케이터도 같은 매니저로 붉게 표시합니다.

## 7. 등급 효과와 장비

- 캐릭터 등급 효과(`HeroGradeEffects`)와 장비 등급 효과(`EquipmentGradeEffects`)는 `GradeEffectType` + 파라미터 두 개로 정의되고, `ConditionalEffectManager.AddConditionalEffectByGradeEffect`가 해당 `ConditionalEffect` 클래스를 만듭니다. 장비는 **아이템 등급 이하의 모든 행이 누적** 적용됩니다.
- 장비의 기본 스탯은 `EquipmentStats`(슬롯·희귀도·등급)에서 오고, `PlayerCharacter.InitializeEquipmentEffects`가 적용합니다. 갑옷은 최대 HP, 장갑은 공격력을 줍니다.
- 데모 로비의 장비 3종은 세트(`SetType`)입니다. 로비에는 세트의 대표인 갑옷만 버튼으로 보이고, `UserGameData.CreateSelectedEquipments`가 고른 갑옷과 같은 세트의 장비(갑옷 + 장갑)를 모두 스테이지로 들고 갑니다. 등급은 `Equipments.json`의 `Grade` 컬럼(D / B / S)으로 정해집니다.

## 8. UI

- `BaseSceneUIRoot`가 캔버스, 팝업 스택, UI 블로커를 관리합니다. 팝업은 `CreateAndAddPopup<T>(prefabPath, ...)`로 만들고 `CloseAndDestroyPopup`으로 닫으며, 팝업이 하나라도 있으면 `PauseResumeOnPopup`이 `Time.timeScale`을 0으로 둡니다.
- 스테이지 HUD(`StageSceneUIRoot`): 조이스틱, 경험치 바, 체력·공격력 표시, 타이머와 보스 이름, 보스 HP 바, 킬 카운트, 경고 팝업, 일시정지·가속 버튼. 게임 로직은 `stageSceneUI.UpdateXxx(...)` 형태로 값을 밀어 넣습니다.
- 팝업: 스킬 선택, 스킬 상자, 결과, 부활, 일시정지, 보스 VS 연출, 공용 확인창. 프리팹은 `Assets/Resources/Stage/UIs/` 아래에 있고 경로는 각 팝업 클래스 또는 `StageSceneUIRoot`의 상수에 적혀 있습니다.
- 가속 버튼(`AccelerationButton`)은 x1.0으로 시작해 누를 때마다 x1.5 → x2.0 → x1.0으로 바뀝니다. 이미 클리어한 챕터에서만 보입니다.
- 로비: `MainLobbyPage`가 `LobbyRadioGroup` 세 개(챕터·캐릭터·장비)를 정적 데이터 순서대로 초기화하고, 선택값을 `UserGameData`에 저장한 뒤 `MainChapterStartButton`이 스테이지로 진입합니다.
- 옵션 팝업(`OptionPopup`, 로비): 언어·음소거·볼륨을 바꾸며 값은 즉시 적용·저장됩니다. 언어는 `Localizer.SetLanguage` 후 화면의 `LocalizedText`를 모두 다시 채우고, 코드로 넣은 텍스트는 `onLanguageChanged` 콜백(`MainLobbyPage.UpdateTexts`)으로 갱신합니다.

## 9. 사운드

- `SoundManager`가 사운드 프리팹(`SoundObject` + `AudioSource`)을 풀링해 재생합니다. `SoundObject`의 `Audio Clips` 목록에서 하나를 골라 재생하며, 목록이 비어 있으면 소리 없이 넘어갑니다.
- 음량은 `전역 볼륨(배경음 또는 효과음) × SoundObject의 Volume Scale`입니다. 특정 효과음만 줄이려면 그 프리팹의 `Volume Scale`을 낮춥니다.
- 음소거(`SoundManager.SetMuted`)는 볼륨 설정을 건드리지 않고 `AudioListener.volume`만 0으로 둡니다. 옵션 팝업과 스테이지 일시정지 팝업의 사운드 버튼이 같은 값을 씁니다.
- 같은 효과음이 18프레임 안에 다시 요청되면 무시합니다(동시에 여러 번 겹쳐 커지는 것을 막기 위함).
