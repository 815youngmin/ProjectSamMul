# 확장 절차와 디버깅

자주 하게 될 작업을 순서대로 적었습니다. 클래스 위치는 [architecture.md](architecture.md)의 폴더 맵을 참고하세요.

## 몬스터 추가

1. `Shared/GameDataTypes/CharacterType.cs`에 열거값을 추가합니다 (데모 몬스터는 2000번대).
2. `Monsters.json`에 행을 추가합니다. `AIType`은 `MonsterAIController`의 `switch`에 있는 값이어야 합니다.
3. 몸체 리소스를 준비합니다. 스프라이트 시트라면 `Stage/ChapterNN/MonsterAnimation/MonsterMM/` 구조로 클립(`idle`, `move`, `attack`, `die`)과 `.controller`를 만들고 `SkeletonDataPath`에 컨트롤러 경로를 적습니다. 공격 클립에는 `HitFrame` 이벤트를 넣습니다.
4. 원거리형이면 `SpecialAttack1ResourcePath`에 투사체 프리팹(`Stage/Common/Projectiles/*.prefab`)을 지정합니다.
5. `StageEvents.json`에 스폰 이벤트를 추가합니다.

## AI 전략 추가

1. `AIStrategies/<이름>AIs/` 폴더에 `XxxIdleAIStrategy`와 `XxxCombatAIStrategy`를 만들고 `MonsterAIStrategyBase`를 상속합니다. `Update`가 전략 인스턴스를 반환하면 전환됩니다.
2. `MonsterAIType`에 값을 추가하고 `MonsterAIController`의 `switch`에 첫 전략 생성을 추가합니다.
3. 새 행동이 필요하면 `Monster.DoAction.cs`에 `DoXxx` 메서드와 `Actions/`에 `ActionBase` 구현을 추가합니다. 몬스터 파라미터는 `Monsters.json`의 `Param1~3`으로 받습니다.

## 보스 패턴 추가

보스 6종은 각자 전용 전략(`BossAIStrategies/Demo/Demo<N>BossAIStrategy.cs`)과 액션(`Actions/Boss/Demo/`)을 가집니다. 구조는 [gameplay-systems.md](gameplay-systems.md)의 "보스 AI와 액션"을 보세요.

1. `Actions/Boss/Demo/`에 `SmartAction<SpriteMonsterAnimationController>`를 상속한 액션 클래스를 만듭니다. `Initialize`에서 `InitializeBase(ActionType.Skill, 전체 길이, animationController)`를 호출하고, `Begin`에서 서브 액션으로 예고·공격 시점을 등록합니다. 기존 액션(예: `DemoFirstBossDashAction`)을 복사해 시작하면 빠릅니다.
2. 해당 보스의 Combat 전략에 있는 `ACTIONS` 목록에 `Monster.DoSpriteMonster<새 액션>`을 추가합니다.
3. 오브젝트 몸체는 경로를 받는 오버로드(`CreateSpinMoveObject`, `CreateReflectionAreaEffectObject`, `CreateSplitAreaEffectObject`)에 `Stage/Common/Projectiles/MonsterProjectile_*.prefab`을 넘겨 재사용합니다.
4. 수치(길이, 속도, 개수, 반경, 피해 계수)는 클래스 상단의 상수로 조절합니다.

- 새 보스를 추가하려면 `CharacterType`과 `MonsterAIType`에 값을 추가하고, `MonsterAIController`의 `switch`에 Idle 전략 생성을 넣은 뒤 `Monsters.json` 행과 `StageEvents.json`의 `BossWarning` / `BossSpawn` 행을 추가합니다.
- 이전 테마의 보스 코드(`BossAIStrategies/Arabian`, `Steampunk`, `Western`, `Actions/Boss/Alibaba` 등)는 데모에서 실행되지 않지만 패턴 참고용으로 남아 있습니다.
- 보스 HP·공격력은 `StageEvents.json` `BossSpawn` 행의 가중치로 맞춥니다(실제 값 = `Monsters.json` 기본값 × 가중치).
- 보스 스폰 위치·울타리 크기는 `BossSpawnStageEvent`의 `switch`에서 보스 타입별로 조정합니다. 맵 형태가 `Rectangle`이면 중앙 스폰, 울타리 없음입니다.
- 테스트는 스테이지에서 숫자 키 1~2로 해당 보스전 직전으로 이동해 확인합니다. PlayMode 테스트 `BossPatternTests`는 보스의 모든 전용 액션을 하나씩 실행해 주므로, 액션을 추가한 뒤 돌리면 무작위 선택을 기다리지 않고 확인할 수 있습니다.

## 스킬 추가

1. `SkillId`에 값을 추가하고 `Skills.json`에 레벨 1~5(초월이 있으면 6) 행을 넣습니다. 파라미터 의미는 스킬 클래스 생성자 주석에 적습니다.
2. `PCs/Skills/XxxSkill.cs`에서 `SkillBase`를 상속합니다. `Activate`에서 스탯 모디파이어를 더하고 `Deactivate`에서 빼며, `Update`에서 쿨타임 뒤 오브젝트를 만듭니다.
3. 스킬 팩토리(`SkillSet.CreateSkill`)의 `switch`에 생성 코드를 추가합니다.
4. 오브젝트가 필요하면 `AreaEffectType`에 값을 추가하고 `AreaEffectObjectPool`의 `switch`와 `Stage.AreaEffect.cs`의 `CreateXxx`를 추가합니다. 몸체는 전용 리소스가 없으면 `PlayerAttackVisual.Attach(transform, 판정지름)`을 쓰고, 판정은 `CombatSystem.HitOnTargetArea`를 거치면 범위 표시가 자동으로 붙습니다.
5. 아이콘을 `Commons/SkillIcons/<SkillId>.png`에 넣고 `GameConstants.SKILLDECK_DEFAULT_SEASON_SKILLDECK`에 추가합니다.

## 팝업 추가

1. `BasePopup`을 상속한 클래스를 만들고 `[SerializeField]`로 필요한 UI 참조를 둡니다.
2. `Assets/Resources/Stage/UIs/<이름>/`(로비 팝업은 `Lobbys/UIs/<이름>/`) 아래에 프리팹을 만들고 컴포넌트를 붙인 뒤 필드를 연결합니다. 프리팹 루트는 `RectTransform`이어야 하며 `CreateAndAddPopup`이 부모 크기에 맞춥니다.
3. `StageSceneUIRoot`(또는 `BaseSceneUIRoot`)에 `AddXxxPopup / CloseXxxPopup`을 추가합니다. 열 때 `PauseResumeOnPopup`을 호출하면 게임이 멈춥니다. 로비 팝업은 `LobbySceneUIRoot.AddOptionPopup`이 예시입니다.
4. 화면에 고정된 글자는 `LocalizedText` 컴포넌트에 텍스트 키를 넣고 `Localization.json`의 `Korean` / `English`에 같은 키를 추가합니다. 코드로 넣는 글자는 언어가 바뀔 때 다시 채워 줘야 합니다.

## 리소스 교체

- 경로 상수는 각 클래스 상단(`PREFAB_PATH`, `*_PATH`)이나 `Stage.AreaEffect.cs`, `ItemObjectBase.GetItemObjectBodyPrefabPath`에 모여 있습니다. 새 프리팹을 만든 뒤 경로만 바꾸면 됩니다.
- 스프라이트 몸체는 크기를 판정에 맞추기 위해 `renderer.sprite.bounds`를 읽어 스케일을 계산합니다(`PlayerAttackVisual.SetDiameter`, `PCSpriteBody`). PPU를 바꿔도 코드 쪽 계산이 따라갑니다.

## 디버깅 요령

- **Editor.log 확인**: 마지막 `Reloading assemblies for play mode` 이후 줄에서 `exception`, `[Placeholder]`를 찾으면 어떤 리소스가 대체됐고 어디서 예외가 났는지 바로 보입니다.
- **플레이스홀더 함정**: 렌더러 개수를 가정하는 코드(`renderers[1]`), `GetComponent<Animator>()` 뒤 바로 접근, `SpriteAnimationHandler` 이벤트로 수명을 끝내는 코드는 플레이스홀더에서 깨집니다. 상수 시간(0.5초)으로 바꾸거나 공용 비주얼로 교체하세요.
- **SpriteRenderer에 MaterialPropertyBlock 쓰지 않기**: 스프라이트 텍스처 바인딩이 사라져 하얗게 남습니다. 플레이어 몸체는 `SpriteRenderer.color` 곱으로 피격을 표현합니다.
- **사운드**: 없는 사운드 경로는 예외를 던집니다. 새 사운드 경로를 코드에 넣었다면 **Demo → Build Sound Prefabs**를 다시 실행한 뒤, 만들어진 프리팹의 `SoundObject` → `Audio Clips`에 클립을 넣으세요. `AudioSource`의 Clip과 Volume은 재생 시 덮어쓰이므로 쓰이지 않습니다. 소리가 안 나면 `Audio Clips`가 비어 있는지부터 확인합니다(비어 있으면 경고 없이 넘어갑니다).
- **빠른 컴파일 확인**: 스크립트 변경 후 Unity 재컴파일 전에 오류만 보고 싶으면 `python _migration/fastbuild/gen.py`를 쓸 수 있습니다(개인 작업 도구, 저장소 미포함).
- **자동 테스트**: `Window > General > Test Runner`. 데이터·텍스트를 고쳤으면 EditMode, 몬스터·보스·씬 전환을 건드렸으면 PlayMode 를 돌립니다. PlayMode 테스트가 게임을 조작하는 코드는 `Assets/Tests/PlayMode/StageTestDriver.cs` 한 곳에 있고, 비공개 멤버 이름을 상수로 들고 있으므로 `Stage._stageEventController`, `MonsterAIController.aiStrategy`, `BossCombatAIStrategy.Actions`, `SkillSelectorButton._button`의 이름을 바꾸면 그 상수도 고쳐야 합니다.
- **테스트 키**: 숫자 1~2 보스전 이동. 부활 규칙(한 게임 1회)은 `Stage.Update`의 `isAbleToResurrect` 조건에 있습니다.
