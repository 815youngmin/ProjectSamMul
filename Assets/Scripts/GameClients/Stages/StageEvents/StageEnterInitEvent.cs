using DG.Tweening;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using Z.Animations.Placeholder;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.Scenes;
using Z.UnityHelpers;
using Random = UnityEngine.Random;

namespace Z.GameClients.Stages.StageEvents
{
    public class StageEnterInitEvent : StageEventBase
    {
        private IStageTimerControllable _stageTimerController;

        private readonly Vector2 _dropDirection;
        private readonly float _moveSpeed;
        private readonly float _arrivalTimeDuration; // 캐릭터 도착시간.
        private readonly int _monsterAmount;
        private readonly int _totalExp;
        private readonly CharacterType _spawnCharacterType;
        private const string _resourceBoom = "Stages/ETCEffects/StageEnterEffect/fx_StageEnterEffectBoom.prefab";
        private const string _resourceStart = "Stages/ETCEffects/StageEnterEffect/fx_StageEnterEffectStart.prefab";

        private GameObject _EnterEffectBody;
        private SkeletonAnimation _startAnimation;
        private SkeletonAnimation _boomAnimation;
        private float _arrivalTimeAt;

        private Dictionary<Monster, Vector2> _monsters = new Dictionary<Monster, Vector2>();


        public StageEnterInitEvent(IStageTimerControllable stageTimerController, StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {
            _stageTimerController = stageTimerController;
            _spawnCharacterType = stageEventStaticData.MonsterType;
            _monsterAmount = stageEventStaticData.Amount;
            _totalExp = stageEventStaticData.TotalExp;

            _dropDirection = Quaternion.AngleAxis(10.0f, Vector3.back) * Vector2.down;
            _dropDirection.Normalize();

            _moveSpeed = 60.0f;
            _arrivalTimeDuration = 1.0f;
        }

        public void StageEnterInitialize(Stage stage)
        {
            int totalDropExp = _totalExp;
            int[] monsterDropExp = new int[_monsterAmount];
            for (int i = 0; i < _monsterAmount; ++i)
            {
                int resultExp = (int)(_totalExp / _monsterAmount);
                monsterDropExp[i] = resultExp;
                totalDropExp -= resultExp;
            }

            monsterDropExp[_monsterAmount - 1] = 0 < totalDropExp ? totalDropExp : 0;

            List<DropItemType> emptyList = new List<DropItemType>();
            for (int i = 0; i < _monsterAmount; ++i)
            {
                Vector2 spawnPostion = Random.insideUnitCircle * 10.0f;

                try
                {
                    Monster monster = stage.CreateMonster(
                        AllianceType.Monsters, _spawnCharacterType,
                        MonsterInstanceInitialData.CreateForStageMonster(
                            spawnPostion,
                            hpWeight: 1f,
                            attackPowerWeight: 1f,
                            monsterDropExp[i],
                            dropGolds: 0,
                            emptyList
                        ), false, false
                    );

                    float x = Random.Range(0, 2) == 1 ? 1.0f : -1.0f;
                    monster.AnimationController.UpdateBodyDirectionByMoveDirection(new Vector2(x, 0.0f));
                    Vector2 randomVector = Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), Vector3.back) * Vector2.down;
                    Vector2 patrolPosition = (randomVector * 5.0f) + spawnPostion;
                    _monsters.Add(monster, patrolPosition);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create monster. [{_spawnCharacterType}]");
                    Debug.LogException(e);
                }
            }

            // 챕터0 -> 스테이지번호0 한정으로 하드코딩한다.
            // 챕터0에서는 등장시 혜성 애니메이션 + 폭발 연출 여기서 안 한다.
            if (StageNumber == 0)
            {
                stage.PC.gameObject.SetActive(true);
                stage.PC.SetImmuneToHit();
            }
            else
            {
                stage.PC.gameObject.SetActive(false);
                stage.PC.SetImmuneToHit();
                InitializeAnimations();
            }


        }

        private void InitializeAnimations()
        {
            _EnterEffectBody = new GameObject("StageEnterEffectObject");
            GameObject startEffect = ResourcePool.Instance.InstantiateFromResource(_resourceStart);
            startEffect.transform.parent = _EnterEffectBody.transform;
            _startAnimation = startEffect.GetComponent<SkeletonAnimation>();
            _startAnimation.Initialize(true);
            _startAnimation.AnimationState.ClearTracks();
            _startAnimation.skeleton.SetToSetupPose();
            _startAnimation.AnimationState.SetAnimation(0, "animation", true);

            GameObject boomEffect = ResourcePool.Instance.InstantiateFromResource(_resourceBoom);
            boomEffect.transform.parent = _EnterEffectBody.transform;
            _boomAnimation = boomEffect.GetComponent<SkeletonAnimation>();
            _boomAnimation.Initialize(true);
            _boomAnimation.AnimationState.ClearTracks();
            _boomAnimation.skeleton.SetToSetupPose();
            _boomAnimation.transform.localScale = Vector3.one * 2.0f;
            _boomAnimation.gameObject.SetActive(false);

            _EnterEffectBody.SetActive(false);
        }

        public override void Begin(Stage stage)
        {
            base.Begin(stage);
            _stageTimerController.PauseStageTimer(pauser: this);

            if (StageNumber == 0)
            {
                // 챕터0 -> 스테이지번호0 한정으로 하드코딩한다.
                // 챕터0에서는 등장시 혜성 애니메이션 + 폭발 연출 여기서 안 한다.
                _arrivalTimeAt = Time.time;

                Debug.Assert(
                    GameClient.Stage.ChapterStaticData != null &&
                    GameClient.Stage.ChapterStaticData.ChapterNumber == 0
                );
            }
            else
            {
                // 스테이지 입장 시점 등 처리가 꼬여있는듯하다. startAnimation이 null인경우가 나오는데 내일이 빌드 마감이라 덮는다.
                if (_startAnimation == null)
                {
                    InitializeAnimations();
                }

                Vector2 dropDirectionInvers = _dropDirection.normalized * -1.0f;
                Vector2 startPos = stage.PC.Pos + _moveSpeed * _arrivalTimeDuration * dropDirectionInvers;
                _startAnimation.transform.position = startPos;
                _startAnimation.transform.right = _dropDirection;

                _boomAnimation.transform.position = stage.PC.Pos;

                _arrivalTimeAt = _arrivalTimeDuration + Time.time;
                _EnterEffectBody.SetActive(true);
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/1-Appear_SFX.prefab", stage.PC.Pos);

                var stageUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                if (stage.StageType == StageType.Chapter)
                {
                    var chapter = stage.ChapterStaticData!;
                    stageUI.ShowChapterInfo(chapter.ChapterName, chapter.ChapterNumber, chapter.ElementType);
                }
                else
                {
                    throw new NotImplementedException($"{stage.StageType} 구현 안 됨. ");
                }
            }
        }

        public override void End(Stage stage)
        {
            if (StageNumber == 0)
            {
                var stageUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                var chapter = stage.ChapterStaticData!;
                stageUI.ShowChapterInfo(chapter.ChapterName, chapter.ChapterNumber, chapter.ElementType);
                GameClient.CameraController.ShakeController.StageEnterEffectShake();
                stage.KillAllMonsterAllianceCharacters();
            }
        }

        public override void Update(Stage stage, int stageEventTickNumber)
        {
            float now = Time.time;
            if (StageNumber == 0)
            {
                if (_arrivalTimeAt < now)
                {
                    _stageTimerController.ResumeStageTimer(pauser: this);
                }
                else
                {
                    foreach (KeyValuePair<Monster, Vector2> iter in _monsters)
                    {
                        Monster monster = iter.Key;
                        Vector2 patrolPosition = iter.Value;
                        Vector2 dir = patrolPosition - monster.Pos;
                        if (!monster.Action.IsDead)
                        {
                            monster.Move(dir.normalized);
                        }
                    }
                }
                return;
            }

            // 스테이지 입장 시점 등 처리가 꼬여있는듯하다. startAnimation이 null인경우가 나오는데 내일이 빌드 마감이라 덮는다.
            if (_startAnimation == null)
            {
                InitializeAnimations();
            }

            float movingDistance = _moveSpeed * Time.deltaTime;
            _startAnimation.transform.position = Vector2.MoveTowards(_startAnimation.transform.position, stage.PC.Pos, movingDistance);

            if (_arrivalTimeAt < now)
            {
                _startAnimation.gameObject.SetActive(false);
                _boomAnimation.gameObject.SetActive(true);
                _boomAnimation.AnimationState.SetAnimation(0, "animation", false).Complete += (TrackEntry track) =>
                {
                    _EnterEffectBody.SetActive(false);
                };
                GameClient.CameraController.ShakeController.StageEnterEffectShake();
                stage.KillAllMonsterAllianceCharacters();

                _stageTimerController.ResumeStageTimer(pauser: this);
                return;
            }

            foreach (KeyValuePair<Monster, Vector2> iter in _monsters)
            {
                Monster monster = iter.Key;
                Vector2 patrolPosition = iter.Value;
                Vector2 dir = patrolPosition - monster.Pos;
                if (!monster.Action.IsDead)
                {
                    monster.Move(dir.normalized);
                }
            }
        }
    }


}
