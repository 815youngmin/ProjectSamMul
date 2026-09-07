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

- 지금 보스 10종은 모두 `MoveAndStopAI`이며 이동·정지 시간은 `Param1`, `Param2`, 속도는 `MoveSpeed`입니다.
- 전용 패턴은 위 "AI 전략 추가"와 같습니다. 보스 전용 이전 코드(`BossAIStrategies/`, `Actions/Boss/`)는 테마가 달라 참고용으로만 남아 있습니다.
- 보스 스폰 위치·울타리 크기는 `BossSpawnStageEvent`의 `switch`에서 보스 타입별로 조정합니다. 맵 형태가 `Rectangle`이면 중앙 스폰, 울타리 없음입니다.
- 테스트는 스테이지에서 숫자 키 1~4로 해당 보스전 직전으로 이동해 확인합니다.

## 스킬 추가

1. `SkillId`에 값을 추가하고 `Skills.json`에 레벨 1~5(초월이 있으면 6) 행을 넣습니다. 파라미터 의미는 스킬 클래스 생성자 주석에 적습니다.
2. `PCs/Skills/XxxSkill.cs`에서 `SkillBase`를 상속합니다. `Activate`에서 스탯 모디파이어를 더하고 `Deactivate`에서 빼며, `Update`에서 쿨타임 뒤 오브젝트를 만듭니다.
3. 스킬 팩토리(`SkillSet.CreateSkill`)의 `switch`에 생성 코드를 추가합니다.
4. 오브젝트가 필요하면 `AreaEffectType`에 값을 추가하고 `AreaEffectObjectPool`의 `switch`와 `Stage.AreaEffect.cs`의 `CreateXxx`를 추가합니다. 몸체는 전용 리소스가 없으면 `PlayerAttackVisual.Attach(transform, 판정지름)`을 쓰고, 판정은 `CombatSystem.HitOnTargetArea`를 거치면 범위 표시가 자동으로 붙습니다.
5. 아이콘을 `Commons/SkillIcons/<SkillId>.png`에 넣고 `GameConstants.SKILLDECK_DEFAULT_SEASON_SKILLDECK`에 추가합니다.

## 팝업 추가

1. `BasePopup`을 상속한 클래스를 만들고 `[SerializeField]`로 필요한 UI 참조를 둡니다.
2. `Assets/Resources/Stage/UIs/<이름>/` 아래에 프리팹을 만들고 컴포넌트를 붙인 뒤 필드를 연결합니다. 프리팹 루트는 `RectTransform`이어야 하며 `CreateAndAddPopup`이 부모 크기에 맞춥니다.
3. `StageSceneUIRoot`(또는 `BaseSceneUIRoot`)에 `AddXxxPopup / CloseXxxPopup`을 추가합니다. 열 때 `PauseResumeOnPopup`을 호출하면 게임이 멈춥니다.

## 리소스 교체

- 경로 상수는 각 클래스 상단(`PREFAB_PATH`, `*_PATH`)이나 `Stage.AreaEffect.cs`, `ItemObjectBase.GetItemObjectBodyPrefabPath`에 모여 있습니다. 새 프리팹을 만든 뒤 경로만 바꾸면 됩니다.
- 스프라이트 몸체는 크기를 판정에 맞추기 위해 `renderer.sprite.bounds`를 읽어 스케일을 계산합니다(`PlayerAttackVisual.SetDiameter`, `PCSpriteBody`). PPU를 바꿔도 코드 쪽 계산이 따라갑니다.

## 디버깅 요령

- **Editor.log 확인**: 마지막 `Reloading assemblies for play mode` 이후 줄에서 `exception`, `[Placeholder]`를 찾으면 어떤 리소스가 대체됐고 어디서 예외가 났는지 바로 보입니다.
- **플레이스홀더 함정**: 렌더러 개수를 가정하는 코드(`renderers[1]`), `GetComponent<Animator>()` 뒤 바로 접근, `SpriteAnimationHandler` 이벤트로 수명을 끝내는 코드는 플레이스홀더에서 깨집니다. 상수 시간(0.5초)으로 바꾸거나 공용 비주얼로 교체하세요.
- **SpriteRenderer에 MaterialPropertyBlock 쓰지 않기**: 스프라이트 텍스처 바인딩이 사라져 하얗게 남습니다. 플레이어 몸체는 `SpriteRenderer.color` 곱으로 피격을 표현합니다.
- **사운드**: 없는 사운드 경로는 예외를 던집니다. 새 사운드 경로를 코드에 넣었다면 **Demo → Build Sound Prefabs**를 다시 실행하세요.
- **빠른 컴파일 확인**: 스크립트 변경 후 Unity 재컴파일 전에 오류만 보고 싶으면 `python _migration/fastbuild/gen.py`를 쓸 수 있습니다(개인 작업 도구, 저장소 미포함).
- **테스트 키**: 숫자 1~4 보스전 이동. 부활 규칙(한 게임 1회)은 `Stage.Update`의 `isAbleToResurrect` 조건에 있습니다.
