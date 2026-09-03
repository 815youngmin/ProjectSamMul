using System.Collections.Generic;
using DG.Tweening;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.StageEvents
{
    public class BossSpawnStageEvent : StageEventBase
    {
        private static readonly string BossSpawnPositionEffectPath = "Stages/ETCEffects/boss warning.prefab";

        private IStageTimerControllable _stageTimerController;
        private CharacterType _bossMonsterType;
        private StageFormType _stageFormType;
        private int _dropExp;
        private IReadOnlyList<DropItemType> _dropItems;
        private Monster _bossMonster;
        private float _hpWeight;
        private float _attackPowerWeight;

        private Vector2 _bossSpawnPosition;
        private Vector2 _fenceSpawnPosition;

        // Unity 시계 기준으로 언제 보스 스폰할 것인지.
        private float _spawningBossAt;
        private bool _isLastBoss;

        //
        private SpriteRenderer _bossSpawnPositionEffect;
        private Sequence _bossSpawnPositionEffectSequence;

        public BossSpawnStageEvent(IStageTimerControllable stageTimerController, StageEventStaticData stageEventStaticData, StageStaticData stageStaticData) : base(stageEventStaticData)
        {
            _stageTimerController = stageTimerController;
            _bossMonsterType = stageEventStaticData.MonsterType;
            _stageFormType = stageStaticData.StageFormType;
            _dropExp = stageEventStaticData.TotalExp;
            _dropItems = stageEventStaticData.DropItems;
            _bossMonster = null;
            _hpWeight = stageEventStaticData.MonsterHPWeight;
            _attackPowerWeight = stageEventStaticData.MonsterAttackPowerWeight;
            _spawningBossAt = float.MaxValue;
            _isLastBoss = false;
        }

        public override void Begin(Stage stage)
        {
            base.Begin(stage);

            _stageTimerController.PauseStageTimer(pauser: this);
            stage.RemoveAllMonstersOnBossSpawn();

            (_fenceSpawnPosition, _bossSpawnPosition) = this.CalculateFenceAndBossPosition(stage);

            this.SpawnFence(stage);
            _spawningBossAt = Time.time + 1.2f;

            var pc = stage.PC;
            Debug.Assert(pc != null);
            pc.transform.position = this.CalculatePlayerCharacterPosition(stage);  
            
            var bossStaticData = StaticDataRepository.Instance.Monsters.Get(_bossMonsterType);
            float lastBossSpawnAt = stage.GetLastBossSpawnTime();
            _isLastBoss = (stage.StageRunningTime >= lastBossSpawnAt);
            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            stageSceneUI.OnBossStageEventBegin(stage, bossStaticData, _isLastBoss);
            stageSceneUI.PauseAccelerationButon();

            stage.PC.OnEnterredIntoBossStageEvent(stage, bossStaticData);

            //보스 생성될 위치 표시 연출
            {
                _bossSpawnPositionEffect = ResourcePool.Instance.InstantiateFromResource(BossSpawnPositionEffectPath).GetComponent<SpriteRenderer>();
                _bossSpawnPositionEffect.gameObject.transform.localPosition = _bossSpawnPosition;
                float _bossSpawnPositionEffectScale = 1 / 2.395f * bossStaticData.ColliderRadius * 1.5f;
                _bossSpawnPositionEffect.gameObject.transform.localScale = Vector3.one * _bossSpawnPositionEffectScale;    //적당히 사이즈 크게 표시

                _bossSpawnPositionEffectSequence = DOTween.Sequence();
                _bossSpawnPositionEffectSequence.Append(_bossSpawnPositionEffect.DOFade(0.5f, 0.3f).From(1f));
                _bossSpawnPositionEffectSequence.Join(_bossSpawnPositionEffect.transform.DOScale(_bossSpawnPositionEffectScale * 0.8f, 0.3f).From(_bossSpawnPositionEffectScale));
                _bossSpawnPositionEffectSequence.Append(_bossSpawnPositionEffect.DOFade(1f, 0.3f).From(0.5f));
                _bossSpawnPositionEffectSequence.Join(_bossSpawnPositionEffect.transform.DOScale(_bossSpawnPositionEffectScale, 0.3f).From(_bossSpawnPositionEffectScale * 0.8f));
                _bossSpawnPositionEffectSequence.Append(_bossSpawnPositionEffect.DOFade(0.5f, 0.3f).From(1f));
                _bossSpawnPositionEffectSequence.Join(_bossSpawnPositionEffect.transform.DOScale(_bossSpawnPositionEffectScale * 0.8f, 0.3f).From(_bossSpawnPositionEffectScale));
                _bossSpawnPositionEffectSequence.Append(_bossSpawnPositionEffect.DOFade(1f, 0.3f).From(0.5f));
                _bossSpawnPositionEffectSequence.Join(_bossSpawnPositionEffect.transform.DOScale(_bossSpawnPositionEffectScale, 0.3f).From(_bossSpawnPositionEffectScale * 0.8f));
                _bossSpawnPositionEffectSequence.Append(_bossSpawnPositionEffect.DOFade(0f, 0.5f).From(1f));
                _bossSpawnPositionEffectSequence.Join(_bossSpawnPositionEffect.transform.DOScale(_bossSpawnPositionEffectScale * 3, 0.5f).From(_bossSpawnPositionEffectScale));
                _bossSpawnPositionEffectSequence.Restart();
            }

            Debug.Log("Call BossSpawnStageEvent.Begin" + BeginAt);
        }

        private (Vector2 fenceSpawnPosition, Vector2 bossSpawnPosition) CalculateFenceAndBossPosition(Stage stage)
        {
            Vector2 bossSpawnPosition;
            Vector2 fenceSpawnPosition;

            if (_stageFormType == StageFormType.Rectangle)
            {
                switch (_bossMonsterType)
                {
                    case CharacterType.Viking_BossGiantOctopus:
                        {
                            //해당 보스는 스테이지에 두마리 등장하는 보스로 스테이지 좌측, 우측에 하나씩 생성된다.
                            //보스 스폰 이벤트에서는 좌측에 해당하는 몬스터를 소환시키고 소환된 보스 몬스터가 반대편 
                            //문어를 추가로 소환 요청을 하도록 작업되어있다. 
                            bossSpawnPosition = new Vector2(-10.5f, 0);
                            fenceSpawnPosition = Vector2.zero;
                        }
                        break;
                    default:
                        bossSpawnPosition = Vector2.zero;
                        //사각형 맵은 스테이지 정 중앙에 보스가 생성되고, 펜스를 치지 않는다.
                        fenceSpawnPosition = Vector2.zero;
                        break;
                }

            }
            else if (_stageFormType == StageFormType.Vertical)
            {
                switch (_bossMonsterType)
                {
                    case CharacterType.IceAge_BossIceGolem:
                    case CharacterType.Egypt_BossSphinx:
                        bossSpawnPosition = new Vector2(0, stage.PC.CenterPos.y) + Vector2.up * 18.0f;
                        fenceSpawnPosition = new Vector2(0, stage.PC.CenterPos.y) + Vector2.up * 5.0f;
                        break;
                    case CharacterType.Viking_BossKraken:
                        bossSpawnPosition = new Vector2(0, stage.PC.CenterPos.y) + Vector2.right * 7.8f;//8 이상 넘어가면 타겟 탐색범위에서 넘어감
                        fenceSpawnPosition = new Vector2(0, stage.PC.CenterPos.y);
                        break;
                    case CharacterType.Crusades_EasternEmpireShip:
                        bossSpawnPosition = new Vector2(0, stage.PC.CenterPos.y) + Vector2.right * 5f;//8 이상 넘어가면 타겟 탐색범위에서 넘어감
                        fenceSpawnPosition = new Vector2(0, stage.PC.CenterPos.y);
                        break;
                    default:
                        bossSpawnPosition = new Vector2(0, stage.PC.CenterPos.y) + Vector2.up * 5.0f;
                        fenceSpawnPosition = new Vector2(0, stage.PC.CenterPos.y) + Vector2.up * 5.0f;
                        break;
                }
            }
            else if (_stageFormType == StageFormType.Infinite)
            {
                switch (_bossMonsterType)
                {
                    case CharacterType.IceAge_BossIceGolem:
                    case CharacterType.Egypt_BossSphinx:
                        bossSpawnPosition = stage.PC.CenterPos + Vector2.up * 18.0f;
                        fenceSpawnPosition = stage.PC.CenterPos + Vector2.up * 5.0f;
                        break;
                    case CharacterType.Viking_BossKraken:
                        bossSpawnPosition = new Vector2(0, stage.PC.CenterPos.y) + Vector2.right * 7.8f;
                        fenceSpawnPosition = new Vector2(0, stage.PC.CenterPos.y);
                        break;
                    default:
                        bossSpawnPosition = stage.PC.CenterPos + Vector2.up * 5.0f;
                        fenceSpawnPosition = stage.PC.CenterPos + Vector2.up * 5.0f;
                        break;
                }
            }
            else
            {
                Debug.LogError($"{_stageFormType} 작업 안 됨 구현해주세요");
                bossSpawnPosition = Vector2.zero;
                fenceSpawnPosition = Vector2.zero;
            }

            return (fenceSpawnPosition, bossSpawnPosition);
        }

        //보스가 제갈량 일때만 플레이어 위치를 조정한다
        private Vector2 CalculatePlayerCharacterPosition(Stage stage)
        {
            switch (_bossMonsterType)
            {
                case CharacterType.Threekingdoms_BossZhugeliang:
                    {
                        return new Vector2(0, stage.StaticData.Height * -0.45f);
                    }
                case CharacterType.EraMix_Boss_Zhugeliang:
                    {
                        return new Vector2(0, stage.StaticData.Height * -0.45f);
                    }
                default:
                    {
                        return stage.PC.Pos;
                    }

            }
        }

        private void SpawnFence(Stage stage)
        {
            if (_stageFormType == StageFormType.Rectangle)
            {
                //사각형 맵은 스테이지 정 중앙에 보스가 생성되고, 펜스를 치지 않는다.
            }
            else if (_stageFormType == StageFormType.Vertical)
            {
                //스테이지 가로 범위보다 조금 넉넉히 울타리 가로 범위를 지정한다.
                stage.CreateFenceObjectSquareAreaBorder(_fenceSpawnPosition, this.GetVerticalFenceSize(stage, _bossMonsterType));
            }
            else if (_stageFormType == StageFormType.Infinite)
            {
                switch (_bossMonsterType)
                {
                    case CharacterType.Inca_BossMancoCapac:
                        {
                            stage.CreateFenceObjectCircularAreaBorder(_fenceSpawnPosition, this.GetInfiniteStageFenceSize(_bossMonsterType));
                        }
                        break;
                    default:
                        {
                            stage.CreateFenceObjectSquareAreaBorder(_fenceSpawnPosition, this.GetInfiniteStageFenceSize(_bossMonsterType));
                        }
                        break;
                }

            }
            else
            {
                Debug.LogError($"{_stageFormType} 작업 안 됨 구현해주세요");
            }
        }

        private Vector2 GetInfiniteStageFenceSize(CharacterType bossType)
        {
            Vector2 fenceSize;

            switch (_bossMonsterType)
            {
                case CharacterType.IceAge_BossDireWolfChief:
                    {
                        fenceSize = new Vector2(35, 40);
                    }
                    break;
                default:
                    {
                        fenceSize = new Vector2(30f, 30);
                    }
                    break;
            }
            return fenceSize;
        }

        private Vector2 GetVerticalFenceSize(Stage stage, CharacterType bossType)
        {
           Vector2 fenceSize;
            switch (_bossMonsterType)
            {
                case CharacterType.Viking_BossKraken:
                    {
                        fenceSize = new Vector2(stage.StaticData.Width + 4f, 36f);
                    }
                    break;
                case CharacterType.Crusades_EasternEmpireShip:
                    {
                        fenceSize = new Vector2(stage.StaticData.Width + 4f, 36f);
                    }
                    break;
                case CharacterType.Western_Boss_MiningKing:
                    {
                        fenceSize = new Vector2(stage.StaticData.Width + 4f, 36f);
                    }
                    break;
                case CharacterType.Western_Boss_WyattEarp:
                    {
                        fenceSize = new Vector2(stage.StaticData.Width + 4f, 36f);
                    }
                    break;
                case CharacterType.WorldWar_Boss_JosephStalinHard:
                    {
                        fenceSize = new Vector2(stage.StaticData.Width + 4f, 36f);
                    }
                break;
                case CharacterType.WorldWar_Boss_AdolfHitler:
                    {
                        fenceSize = new Vector2(stage.StaticData.Width + 4f, 36f);
                    }
                    break;
                case CharacterType.EarthGuardian_Boss_TrampHard2:
                    {
                        fenceSize = new Vector2(stage.StaticData.Width + 4f, 36f);
                    }
                    break;
                default:
                    {
                        fenceSize = new Vector2(stage.StaticData.Width + 4f, 30f);
                    }
                    break;
            }
            return fenceSize;
        }

        private void SpawnBossMonster(Stage stage)
        {
            var bossInitialData = MonsterInstanceInitialData.CreateForStageMonster(
                _bossSpawnPosition, _hpWeight, _attackPowerWeight, _dropExp,
                dropGolds: 0, _dropItems);

            _bossMonster = stage.CreateMonster(AllianceType.Monsters, _bossMonsterType, bossInitialData, isBoss: true, isElite: false);
        }

        public override void End(Stage stage)
        {
            stage.RemoveAllFence();
            _stageTimerController.ResumeStageTimer(pauser: this);

            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            stageSceneUI.OnBossStageEventEnd();
            stageSceneUI.ResumeAccelerationButton();

            //마지막 보스가 죽을때 스테이지에 획득하지 못한 랜덤 강화석 아이템들을 획득해준다.
            if(_isLastBoss)
            {
                foreach (var acquirableItem in stage.TakeAllRandomElementObjects())
                {
                    acquirableItem.OnAcquired(stage.PC, stage);
                }
            }
            if(_bossSpawnPositionEffect != null)
            {
                ResourcePool.Instance.PutBackInstance(BossSpawnPositionEffectPath, _bossSpawnPositionEffect.gameObject);
                _bossSpawnPositionEffect = null;
            }

            if(_bossSpawnPositionEffectSequence != null)
            {
                DOTween.Kill(_bossSpawnPositionEffectSequence);
                _bossSpawnPositionEffectSequence = null;
            }

            Debug.Log("Call BossSpawnStageEvent.End " + EndAt);
        }

        public override void Update(Stage stage, int stageEventTickNumber)
        {
            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            if (_bossMonster == null)
            {
                if (Time.time < _spawningBossAt)
                {
                    // 아직 보스 스폰되기 전임
                    stageSceneUI.UpdateBossHPBar(100f, 100f);
                    return;
                }
                else
                {
                    this.SpawnBossMonster(stage);
                    Debug.Assert(_bossMonster != null);
                }
            }

            if (_bossMonster.Action.IsDead)
            {
                this.End(stage);
                return;
            }

            stageSceneUI.UpdateBossHPBar(_bossMonster.CurrentHP, _bossMonster.MaxHP);
        }
    }
}