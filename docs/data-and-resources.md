# 데이터와 리소스

## 정적 데이터 (`Assets/Resources/StaticData/*.json`)

- 테이블 하나가 JSON 파일 하나이며, 배열의 각 원소가 행입니다. `StaticDataRepository`가 시작 시 전부 읽어 `StaticDataRepository.Instance.<테이블>`로 노출합니다.
- 열거형 값은 이름 문자열로 적습니다(`"AIType": "MeleeAttackAI"`). 없는 테이블은 빈 테이블로 취급하지만, **없는 행을 조회하면 예외**를 던집니다(잘못된 참조를 조용히 넘기지 않기 위해).
- 원본 값이 필요한 테이블은 이전 프로젝트의 구글 시트 익스포트에서 가져왔고, 리소스 경로만 데모 경로로 바꿨습니다.

| 파일 | 클래스 | 내용 |
|---|---|---|
| `Chapters.json` | `ChapterStaticData` | 챕터 번호, 이름 키, 원소, 연결된 스테이지 번호 |
| `Stages.json` | `StageStaticData` | 맵 형태(`Infinite` / `Rectangle`), 크기, 총 시간, 아이템 상자 주기, 부활 가능 여부, 바닥·울타리·BGM 경로 |
| `StageEvents.json` | `StageEventStaticData` | 스테이지별 스폰 시간표 ([gameplay-systems.md](gameplay-systems.md) 1절) |
| `Monsters.json` | `MonsterStaticData` | 몬스터 종류, AI, 몸체 경로, 히트박스, HP·공격력·이동속도, 특수 공격 파라미터, 소환 몬스터, `Param1~3` |
| `MonsterImagePaths.json` | `MonsterImagePathStaticData` | 보스 일러스트 경로 (VS 연출 팝업) |
| `Heroes.json` | `HeroStaticData` | 캐릭터 종류, 희귀도, 원소, 기본 스킬, 몸체 경로, 히트박스, 이동속도. **행 순서가 로비 버튼 순서** |
| `HeroStats.json` | `HeroStatStaticData` | 희귀도·등급별 기본 공격력·HP와 레벨당 증가량 |
| `HeroGradeEffects.json` | `HeroGradeEffectStaticData` | 캐릭터 등급 효과 (데모에서는 비어 있음) |
| `CharacterImagePaths.json` | `CharacterImagePathStaticData` | 캐릭터 아이콘 경로 |
| `Skills.json` | `SkillStaticData` | 스킬·레벨별 파라미터, 초월 조건, 아이콘·사운드 경로 |
| `Equipments.json` | `EquipmentStaticData` | 장비 정의와 데모용 `Grade`. **행 순서가 로비 버튼 순서** |
| `EquipmentStats.json` | `EquipmentStatStaticData` | 슬롯·희귀도·등급별 장비 기본 스탯 |
| `EquipmentGradeEffects.json` | `EquipmentGradeEffectStaticData` | 장비 등급 효과 (등급 이하 누적) |
| `Exps.json` | `ExpTableRow` | 레벨별 필요 경험치 (70레벨, 이후 상한) |
| `Localization.json` | `Localizer` | 언어별 텍스트 사전 (`Korean`, `English`). 없는 키는 키 이름을 그대로 표시 |

메뉴 **Demo → Validate Static Data**로 전체 로드와 참조 검증을 할 수 있습니다.

## 리소스 경로 규칙

- 코드와 데이터의 경로는 `Assets/Resources/` 기준 상대 경로에 **확장자를 포함**해 적습니다(`Stage/Common/fence.png`). `ResourcePool`이 확장자를 떼고 `Resources.Load`합니다.
- 주요 위치

| 경로 | 내용 |
|---|---|
| `Stage/ChapterNN/` | 챕터별 몬스터 스프라이트 시트와 애니메이션(`MonsterAnimation/MonsterMM/`), 보스(`BossAnimation/Boss_0N/`) |
| `Stage/Common/` | 공용 몬스터 애니메이션, 몬스터 투사체 4종, 울타리, 그림자, 원소 아이콘, `PlayerAttackVisual.prefab` |
| `Stage/Floor/` | 챕터 바닥 이미지 |
| `Stage/Exp/`, `Stage/Item/` | 경험치·아이템 몸체 프리팹 |
| `Stage/UIs/` | HUD 그룹과 팝업 프리팹 |
| `Skill/` | 스킬 전용 리소스 (예: 데스터치 드론) |
| `PlaerCharacter/<Hero>/` | 캐릭터 스프라이트와 idle/run/attack/die 클립, Animator 컨트롤러 |
| `Commons/SkillIcons/` | 스킬 아이콘 |
| `Sounds/` | BGM·효과음 프리팹 (데모는 무음 프리팹) |
| `Lobbys/UIs/` | 로비 아이콘과 라디오 그룹 프리팹 |

- 스프라이트 애니메이션 몬스터·캐릭터는 `idle / move(run) / attack / hitted / die / appear / disappear / heal` 상태 이름을 Animator에서 찾습니다. 없는 상태는 건너뜁니다. 공격 클립의 `HitFrame` 애니메이션 이벤트 시각이 타격 시점입니다.
- 몸체 경로 확장자가 `.asset`이면 스켈레톤(플레이스홀더 Spine), `.controller`면 스프라이트 Animator입니다.

## 플레이스홀더

리소스가 없을 때 `ResourcePool`은 `PlaceholderFactory`로 대체물을 만들고 `[Placeholder] ... 플레이스홀더 사용. Path[...]` 로그를 한 번 남깁니다.

| 요청 | 대체물 |
|---|---|
| 스프라이트 | 4x4 흰색 스프라이트 |
| 프리팹 (루트 컴포넌트 지정) | 해당 컴포넌트를 붙인 오브젝트. 직렬화 필드는 타입에 맞는 빈 컴포넌트(Image, TMP 텍스트, SpriteRenderer 등)로 채움 |
| 프리팹 (타입 미지정) | 흰색 SpriteRenderer 오브젝트 (UI 경로면 반투명 Image) |
| 스켈레톤 데이터 | 색 사각형을 그리는 `SkeletonAnimation`. 애니메이션 이름은 요청 시 생성되며 길이 0.5초, 이벤트는 중간 시점 |
| 사운드 프리팹 | **예외**. 사운드는 중요한 리소스로 취급해 없으면 즉시 알립니다 |

플레이스홀더는 NullReference를 막기 위한 것이라, 렌더러 개수나 특정 자식 구조를 가정하는 코드는 그대로 두면 오류가 납니다. 그런 곳은 공용 비주얼이나 `AttackAreaFlashManager` 표시로 바꿔 왔습니다. 주의할 알려진 함정은 [how-to-extend.md](how-to-extend.md)의 디버깅 절을 보세요.

## 저장 데이터

- `OfflineSession`이 `Application.persistentDataPath/offline-save.json`에 `UserGameData`(골드, 보석, 선택한 캐릭터·장비, 최고 챕터·시간, 부활 코인, 스테이지 부활 횟수)를 저장합니다.
- 서버 프로토콜이었던 챕터 입장/종료/포기/부활은 같은 요청·응답 타입(`Shared/CSProtocols`)으로 즉시 처리됩니다. `ResetSave()`로 초기화할 수 있습니다.

## 에디터 메뉴 (`Assets/Editor/Demo`)

| 메뉴 | 역할 |
|---|---|
| Demo → Validate Static Data | 정적 데이터 로드와 참조 검증 |
| Demo → Build Scenes / Rebuild Scenes | Loading / Lobby / Stage 씬 생성 (이미 커밋된 씬을 다시 만들 때만) |
| Demo → Build Korean Font | TextMeshPro 한글 폴백 폰트 에셋 생성 |
| Demo → Build Sound Prefabs | 코드가 참조하는 사운드 경로에 무음 프리팹 생성 |
