using Shared.GameDataTypes;
using Shared.StaticDatas;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.Loggers;

namespace Z.GameClients.Stages.StageEvents
{
	/// <summary>
	/// 정해진 시간동안, 살아있는 몬스터의 수가 일정수량을 유지하는 스테이지 이벤트
	/// 몬스터가 죽으면 일정수량 다시 스폰해준다.
	/// 경험치 수량은 시간에 비례하여 지급하고, 다 지급되지 못한 경험치는 마지막 몬스터에게 몰빵한다.
	/// </summary>
	public class AutoRespawnMonsterStageEvent : StageEventBase
	{
		// 경험치 드롭 규칙 : 
		// 전체 경험치를 k 구간으로 나누어 몬스터에게 배치한다.
		// k = 이벤트 시간길이 / 구간별 시간길이
		// k구간마다 화면에 있는 몬스터를 모두 제거한다는 가정으로 경험치를 배치하는 것. 
		// 몬스터를 지정한만큼 제거하지 못한다면, 경험치를 더 획득하지 못하고 레벨업하지 못하는 것. 난이도가 적절하게 올라가게 된다. 
		// 이를 통해, 몬스터를 더 많이 잡아야 레벨업이 더 많이되는 구조를 가져가도록 한다. (정해진 경험치량을 제한하는 것은 기본이고)

		private readonly CharacterType _spawnMonsterType;
		private readonly float _hpWeight;
		private readonly float _attackPowerWeight;
		// 스테이지에 유지해야할 몬스터 수량
		private readonly int _targetAmountToMaintain;

		private readonly int _totalExpAmount;
		// 이 스테이지 이벤트에서 발급해야할 경험치중 남은 경험치. ForCurrentPhase로 뺀 것은 제외
		private long _remainingExpToDrop;
		// 이번 페이즈에 발급가능한 경험치중 남은 경험치. 0이되면 다음페이즈로 넘어가기 전까지 몬스터의 경험치는 10으로 고정
		private long _remainingExpForCurrentPhase;
		// 이번 페이즈에서 하나의 몬스터마다 발급하기로 한 경험치. 페이즈 변경시마다 결정된다. remainingExpForCurrentPhase가 0보다 작으면, 이 값은 무시하고 10으로 고정된다.
		private long _unitExpForCurrentPhase;

		private readonly float _phasePeriod;
		private readonly int _maxPhases;
		private int _currentPhase;
		private int _leftPhases;

		private bool _isActivated;

		private int _aliveMonsterCount;

		public AutoRespawnMonsterStageEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
		{
			//----------- 튜토리얼용 코드 ---------------
			// 0챕터(0스테이지) 첫번째 도전의 90초 이후부터는
			// 스폰량을 두배로 만든다.
			bool isFirstTutorialStageRushTime =
				stageEventStaticData.EventType != StageEventType.BossSpawn &&
				stageEventStaticData.StageNumber == 0 &&
				(stageEventStaticData.BeginAt >= 90f) &&
				(GameClient.CS.UserGameData.HighestStageTimeInSeconds <= 0);

			int spawnAmount = isFirstTutorialStageRushTime ? (int)(stageEventStaticData.Amount * 1.8f) : stageEventStaticData.Amount;
			float hpWeight = isFirstTutorialStageRushTime ? stageEventStaticData.MonsterHPWeight * 11f : stageEventStaticData.MonsterHPWeight;
			float attackPowerWeight = isFirstTutorialStageRushTime ? stageEventStaticData.MonsterAttackPowerWeight * 2.2f : stageEventStaticData.MonsterAttackPowerWeight;
			//-----------------------------------------

			_spawnMonsterType = stageEventStaticData.MonsterType;
			_hpWeight = hpWeight;
			_attackPowerWeight = attackPowerWeight;
			_targetAmountToMaintain = spawnAmount;
			_remainingExpToDrop = _totalExpAmount = stageEventStaticData.TotalExp;
			_remainingExpForCurrentPhase = 0;

			_phasePeriod = stageEventStaticData.RespawnPhasePeriod;
			_maxPhases = (int)((stageEventStaticData.EndAt - stageEventStaticData.BeginAt) / stageEventStaticData.RespawnPhasePeriod);
			_currentPhase = -1;
			_unitExpForCurrentPhase = 10;
			_leftPhases = _maxPhases - _currentPhase;
			_isActivated = false;
			_aliveMonsterCount = 0;

			if (stageEventStaticData.DropItems.Count > 0)
			{
				Log.I.Error($"AutoRespawnMonsterStageEvent는 DropItems를 지원하지 않습니다. Stage[{stageEventStaticData.StageNumber}] [{stageEventStaticData.BeginAt}]");
			}
		}

		public override void Begin(Stage stage)
		{
			base.Begin(stage);

			_isActivated = true;
			_aliveMonsterCount = 0;

			this.ChangeToNextPhase(stage);
		}

		private void ChangeToNextPhase(Stage stage)
		{
			++_currentPhase;

			_leftPhases = _maxPhases - _currentPhase;

			if (_leftPhases <= 0)
			{
				_currentPhase = _maxPhases - 1;
				_leftPhases = 1;
			}

			// 경험치 드롭 규칙 : 
			// 전체 경험치를 k 구간으로 나누어 몬스터에게 배치한다.
			// k = 이벤트 시간길이 / 구간별 시간길이
			// k구간마다 화면에 있는 몬스터를 모두 제거한다는 가정으로 경험치를 배치하는 것. 
			// 몬스터를 지정한만큼 제거하지 못한다면, 경험치를 더 획득하지 못하고 레벨업하지 못하는 것. 난이도가 적절하게 올라가게 된다. 
			// 이를 통해, 몬스터를 더 많이 잡아야 레벨업이 더 많이되는 구조를 가져가도록 한다. (정해진 경험치량을 제한하는 것은 기본이고)

			// AutoRespawnMonsterStageEvent는 RespawnPhasePeriod(호출주기)를 구간별 시간길이로 쓴다.
			// 
			// TotalExp / (K+1) 만큼으로 첫 웨이브 스폰
			// 다음 구간 동안 생성된 몬스터는 : (TotalExp / (K+1) / 화면내몬스터 총량) 만큼의 경험치를 가지고 스폰
			// 그 다음 구간 동안 생성된 몬스터는 : (남은 Exp /

			long incrementForCurrentPhase = (_remainingExpToDrop <= 0) ?
				0 : (_remainingExpToDrop / _leftPhases);

			_remainingExpForCurrentPhase += incrementForCurrentPhase;

			_remainingExpToDrop -= incrementForCurrentPhase;

			_unitExpForCurrentPhase = _remainingExpForCurrentPhase / _targetAmountToMaintain;
		}

		public override void End(Stage stage)
		{
			_isActivated = false;

			if (_remainingExpToDrop > 0 ||
				_remainingExpForCurrentPhase > 0)
			{
				Debug.Log($"AutoRespawnMonsterStageEvent left exp : TotalExp[{_totalExpAmount}]  RemainingExpToDrop[{_remainingExpToDrop}] RemainingForCurrentPhase[{_remainingExpForCurrentPhase}]");
			}

			if (_remainingExpForCurrentPhase < 0)
			{
				Debug.Log($"AutoRespawnMonsterStageEvent Exp Drop Exceeded : TotalExp[{_totalExpAmount}] RemainingExpToDrop[{_remainingExpToDrop}] RemainingForCurrentPhase[{_remainingExpForCurrentPhase}]");
			}
		}

		public override void Update(Stage stage, int stageEventTickNumber)
		{
			if (!_isActivated)
			{
				return;
			}

			if ((this.BeginAt + (_currentPhase + 1) * _phasePeriod) <= stage.StageRunningTime)
			{
				this.ChangeToNextPhase(stage);
			}

			if (_aliveMonsterCount >= _targetAmountToMaintain)
			{
				return;
			}

			// 한 틱에 너무 많은 몬스터를 스폰하면 안된다. 스폰량은 한틱에 39마리로 제한한다.
			// StageEvent의 한개 틱 주기는 이벤트타입마다 다르고, AutoRespawnEvent는 1초에 6번 업데이트 한다.
			int spawnAmount = math.min(39, (_targetAmountToMaintain - _aliveMonsterCount));
			this.SpawnMonsters(stage, spawnAmount);
		}

		protected virtual void SpawnMonsters(Stage stage, int amount)
		{
			var (innerRadius, outerRadius) = MonsterSpawnTools.CalculateSpawnRadius(this.TargetCameraOrthographicSize, stage.StaticData.StageFormType);
			var spawnBoundary = MonsterSpawnTools.CalculateSpawnBoundary(stage, outerRadius, outerRadius);
			var pcPosition = stage.PC?.Pos ?? spawnBoundary.center;

			var spawnLogic = stage.StaticData.SpawnLogicType;
			for (int i = 0; i < amount; ++i)
			{
				var spawnPoint = spawnLogic == SpawnLogicType.FromVerticalBoundary ?
					MonsterSpawnTools.PickRandomPointFromVertical(spawnBoundary, innerRadius, outerRadius) :
					MonsterSpawnTools.PickRandomPointFromAnnulusWithinBoundary(spawnBoundary, innerRadius, outerRadius, pcPosition);
				this.SpawnMonster(stage, spawnPoint);
			}
		}

		private IReadOnlyList<DropItemType> _emptyDropItemList = new List<DropItemType>();
		/// <param name="hpWeight">스폰할 몬스터의 HP 가중치. 몬스터 테이블의 기본값에 이만큼을 곱해서 밸런싱한다.</param>
		/// <param name="attackPowerWeight">스폰할 몬스터의 공격력 가중치. 몬스터 테이블의 기본값에 이만큼을 곱해서 밸런싱한다.</param>
		protected Monster SpawnMonster(Stage stage, Vector2 spawnPoint)
		{
			long dropGoldAmount = 0;
			long dropExp = this.TakeExp();

			var monster = stage.CreateMonster(AllianceType.Monsters, _spawnMonsterType,
				MonsterInstanceInitialData.CreateForStageMonster(
					spawnPoint,
					_hpWeight,
					_attackPowerWeight,
					dropExp,
					dropGoldAmount,
					_emptyDropItemList
				), isBoss: false, isElite: false);

			// 스테이지 이벤트가 몬스터의 DeadHandler를 제거하지는 않고, 몬스터가 죽어서 Pool에 반납될 때 본인의 핸들러를 제거하는 것에 의존한다.
			_ = monster.EventHandlers.AddOnDeadHandler(OnSpawnedMonsterDead);
			++_aliveMonsterCount;

			return monster;
		}

		private long TakeExp()
		{
			if (_remainingExpForCurrentPhase <= 0)
			{
				// 이번페이즈에서 발급할 것 다썻다면
				// 전체량에서 차감하고 3으로 고정해서 발급
				_remainingExpToDrop -= 3;
				_remainingExpToDrop = math.min(3, _remainingExpToDrop);
				// 전체량에서도 부족하면, 그냥 경험치를 초과발급한다
				return 3;
			}

			long exp = math.min(_unitExpForCurrentPhase, _remainingExpForCurrentPhase);
			_remainingExpForCurrentPhase -= exp;

			return exp;
		}

		private void OnSpawnedMonsterDead(Stage stage, Character monster, Vector2 hitVector)
		{
			--_aliveMonsterCount;

			if (!_isActivated)
			{
				return;
			}
			// 아직 스테이지이벤트가 유효하다면 재스폰시켜주고
			// 스테이지이벤트가 이미 종료되었다면 재스폰 시키지 않는다,

			// 여기서 바로 스폰시키지 않고, Update로직에서 모아서 스폰한다.
		}
	}
}