namespace SamMul.GameClients.Stages.Characters.Stats
{
    public class PlayerCharacterStatCalculators : CharacterStatCalculators
    {
        // 기본 공격 넉백 파워
        public readonly StatCalculator CharacterAttackKnockBackPower;
        // 스킬 공격 넉백 파워
        public readonly StatCalculator SkillAttackKnockBackPower;
        // 공격시 발사 탄환의 갯수 : 텐티의 경우 기본공격 촉수 갯수로 쓴다.
        public readonly StatCalculator BulletCount;
        // 추가 탄환 최대 관통 횟수
        public readonly StatCalculator AdditionalBulletPenetrationCount;
        // 탄환 타격시 분쇄되는 갯수 (0이 기본값)
        public readonly StatCalculator BulletSplitCount;
        // 아이템 획득 범위.
        public readonly StatCalculator AcquisitionDistance;
        // 경험치 획득 증가율.
        public readonly StatCalculator ExpIncreaseRate;
        // 경험치 추가 드랍량 (보석에 추가되서 드랍됨)
        public readonly StatCalculator AdditionalExpRate;
        // 공격범위 와 거리 비율
        public readonly StatCalculator AttackRangeDistanceRatio;
        // 골드 획득량 증가.
        public readonly StatCalculator GoldIncreaseRate;
        // 최대 소환량 증가.
        public readonly StatCalculator AddMaxSummonCount;
        // 아이템(고기) 회복량 증가비율
        public readonly StatCalculator EatingHPRecoveryRate;
        //회피율 백분율 (0 ~ 1)
        public readonly StatCalculator DodgeRate;
        // 모든 회복량 증가비율
        public readonly StatCalculator HPRecoveryRate;
        // 플레이어가 사용가능한 최대 부활 가능 개수. 스테이지 입장시점에 정해져있다.
        public readonly StatCalculator MaxResurrectCount;
        // 부활시 체력 비율 (0 ~ 1)
        public readonly StatCalculator ResurrectHpRate;



        public PlayerCharacterStatCalculators() : base()
        {
            this.CharacterAttackKnockBackPower = new StatCalculator(1.0f);
            this.SkillAttackKnockBackPower = new StatCalculator(1.0f);
            this.BulletCount = new StatCalculator();
            this.AdditionalBulletPenetrationCount = new StatCalculator();
            this.BulletSplitCount = new StatCalculator();
            this.AcquisitionDistance = new StatCalculator(1.5f);
            this.ExpIncreaseRate = new StatCalculator();
            this.AdditionalExpRate = new StatCalculator();
            // 공격범위 비율이기 때문에 기본값 1이 들어가야 한다. 여기서 스킬에 의한 증가량에 따라 1.2배(20%증가), 1.5배(50%증가) 등이 적용된다.
            this.AttackRangeDistanceRatio = new StatCalculator(1.0f);
            this.GoldIncreaseRate = new StatCalculator();
            this.AddMaxSummonCount = new StatCalculator();
            this.EatingHPRecoveryRate = new StatCalculator(1.0f);
            this.DodgeRate = new StatCalculator();
            this.HPRecoveryRate = new StatCalculator(1.0f);
            this.MaxResurrectCount = new StatCalculator();
            this.ResurrectHpRate = new StatCalculator(0.5f);
        }

        public override void ClearModifiers()
        {
            base.ClearModifiers();

            this.CharacterAttackKnockBackPower.ClearModifiers();
            this.SkillAttackKnockBackPower.ClearModifiers();
            this.BulletCount.ClearModifiers();
            this.AdditionalBulletPenetrationCount.ClearModifiers();
            this.BulletSplitCount.ClearModifiers();
            this.AcquisitionDistance.ClearModifiers();
            this.ExpIncreaseRate.ClearModifiers();
            this.AdditionalExpRate.ClearModifiers();
            // 공격범위 비율이기 때문에 기본값 1이 들어가야 한다. 여기서 스킬에 의한 증가량에 따라 1.2배(20%증가), 1.5배(50%증가) 등이 적용된다.
            this.AttackRangeDistanceRatio.ClearModifiers();
            this.GoldIncreaseRate.ClearModifiers();
            this.AddMaxSummonCount.ClearModifiers();
            this.EatingHPRecoveryRate.ClearModifiers();
            this.DodgeRate.ClearModifiers();
            this.HPRecoveryRate.ClearModifiers();
            this.MaxResurrectCount.ClearModifiers();
            this.ResurrectHpRate.ClearModifiers();
        }

        public override string ToString()
        {
            return base.ToString() +
                $"CharacterAttackKnockBackPower\n{CharacterAttackKnockBackPower.DetailToString()}" +
                $"SkillAttackKnockBackPower\n{SkillAttackKnockBackPower.DetailToString()}" +
                $"BulletCount\n{BulletCount.DetailToString()}" +
                $"AdditionalBulletPenetrationCount\n{AdditionalBulletPenetrationCount.DetailToString()}" +
                $"BulletSplitCount\n{BulletSplitCount.DetailToString()}" +
                $"AcquisitionDistance\n{AcquisitionDistance.DetailToString()}" +
                $"ExpIncreaseRate \n{ExpIncreaseRate.DetailToString()}" +
                $"AdditionalExpRate \n{AdditionalExpRate.DetailToString()}" +
                $"AttackRangeDistanceRatio\n{AttackRangeDistanceRatio.DetailToString()}" +
                $"GoldIncreaseRate\n{GoldIncreaseRate.DetailToString()}" +
                $"AddMaxSummonCount\n{AddMaxSummonCount.DetailToString()}" +
                $"EatingHPRecoveryRate\n{EatingHPRecoveryRate.DetailToString()}" +
                $"DodgeRate\n{DodgeRate.DetailToString()}" +
                $"HPRecoveryRate\n{HPRecoveryRate.DetailToString()}" +
                $"MaxResurrectCount\n{MaxResurrectCount.DetailToString()}" +
                $"ResurrectHpRate\n{ResurrectHpRate.DetailToString()}"
                ;
        }
    }
}
