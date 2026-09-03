using DG.Tweening;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using UnityEngine;
using SamMul.ResourcePools;

public class LostSkillObject : MonoBehaviour
{
    public static string PREFAB_PATH = "Stages/ETCEffects/LostSkillObject.prefab";
    private static readonly string ActiveSkillBackgroundPath = "Stages/UIs/Popups/SkillBoxPopup/ball_active.png";
    private static readonly string PassiveSkillBackgroundPath = "Stages/UIs/Popups/SkillBoxPopup/ball_passive.png";
    private static readonly string BanIconPath = "Stages/ETCEffects/ban.png";
    private static readonly string ShadowPath = "Stages/Items/ItemShadow.png";

    [SerializeField] private Transform _objectTransform;
    [SerializeField] private SpriteRenderer _backgroundSpriteRenderer;
    [SerializeField] private SpriteRenderer _iconSpriteRenderer;
    [SerializeField] private SpriteRenderer _banSpriteRenderer;

    [SerializeField] private Transform _shadowTransform;
    [SerializeField] private SpriteRenderer _shadowSpriteRenderer;

    public void Initialize(Vector2 startPosition, Vector2 endPosition, SkillId skillId)
    {
        var skillData =  StaticDataRepository.Instance.Skills.Get(new SkillKey(skillId, 1));
        if(skillData.skillType == SkillType.Active)
        {
            _backgroundSpriteRenderer.sprite = ResourcePool.Instance.LoadResource<Sprite>(ActiveSkillBackgroundPath);
        }
        else
        {
            _backgroundSpriteRenderer.sprite = ResourcePool.Instance.LoadResource<Sprite>(PassiveSkillBackgroundPath);
        }
        _iconSpriteRenderer.sprite = ResourcePool.Instance.LoadResource<Sprite>(skillData.IconResourcePath);
        _banSpriteRenderer.sprite = ResourcePool.Instance.LoadResource<Sprite>(BanIconPath);
        _shadowSpriteRenderer.sprite = ResourcePool.Instance.LoadResource<Sprite>(ShadowPath);

        _backgroundSpriteRenderer.enabled = true;
        _backgroundSpriteRenderer.color = Color.white;
        _iconSpriteRenderer.enabled = true;
        _iconSpriteRenderer.color = Color.white;
        _banSpriteRenderer.enabled = false;
        _banSpriteRenderer.color = Color.white;
        _shadowSpriteRenderer.enabled = false;
        _shadowSpriteRenderer.color = Color.white;

        Sequence seq = DOTween.Sequence();
        float jumpHeight = Random.Range(3f, 4f);
        float totalDuration = Random.Range(1.1f, 1.3f);
        float maxHeight = Mathf.Max(startPosition.y, endPosition.y) + jumpHeight;

        // 초반 스케일업 연출
        seq.Append(_objectTransform.DOScale(1f, 0.1f).SetEase(Ease.OutQuad).From(0.2f));
        // Y축 점프 (상승)
        seq.Join(_objectTransform.DOLocalMoveY(maxHeight, totalDuration * 0.35f).SetEase(Ease.OutQuad));
        seq.AppendCallback(() =>
        {
            _shadowSpriteRenderer.enabled = true;
            _shadowSpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            _shadowTransform.localPosition =  new Vector3(0f, endPosition.y - 0.8f, 0f);
        });
        
        // Y축 착지
        seq.Append(_objectTransform.DOLocalMoveY(endPosition.y, totalDuration * 0.25f).SetEase(Ease.InQuad));
        seq.Join(_shadowSpriteRenderer.DOFade(0.6f, totalDuration * 0.25f).SetEase(Ease.InQuad));
        
        // 바운스 효과1
        seq.Append(_objectTransform.DOLocalMoveY(endPosition.y + jumpHeight * 0.6f, totalDuration * 0.15f).SetEase(Ease.OutQuad));
        seq.Join(_shadowSpriteRenderer.DOFade(0.3f, totalDuration * 0.15f).SetEase(Ease.InQuad));
        seq.Append(_objectTransform.DOLocalMoveY(endPosition.y, totalDuration * 0.1f).SetEase(Ease.InQuad));
        seq.Join(_shadowSpriteRenderer.DOFade(0.6f, totalDuration * 0.1f).SetEase(Ease.InQuad));
        
        // 바운스 효과2
        seq.Append(_objectTransform.DOLocalMoveY(endPosition.y + jumpHeight * 0.3f, totalDuration * 0.09f).SetEase(Ease.OutQuad));
        seq.Join(_shadowSpriteRenderer.DOFade(0.4f, totalDuration * 0.09f).SetEase(Ease.InQuad));
        seq.Append(_objectTransform.DOLocalMoveY(endPosition.y, totalDuration * 0.06f).SetEase(Ease.InQuad));
        seq.Join(_shadowSpriteRenderer.DOFade(0.6f, totalDuration * 0.06f).SetEase(Ease.InQuad));

        //대기 후 금지 표시 활성화
        seq.AppendInterval(1.3f - totalDuration);
        seq.AppendCallback(() =>
        {
            _banSpriteRenderer.enabled = true;
            _banSpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        });

        //금지 표시 깜빡이
        seq.Append(_banSpriteRenderer.DOFade(0.8f, 0.2f).SetEase(Ease.InQuad));
        seq.Append(_banSpriteRenderer.DOFade(0f, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(_banSpriteRenderer.DOFade(1f, 0.2f).SetEase(Ease.InQuad));
        seq.Append(_banSpriteRenderer.DOFade(0f, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(_banSpriteRenderer.DOFade(1f, 0.2f).SetEase(Ease.InQuad));
        seq.Append(_banSpriteRenderer.DOFade(0f, 0.1f).SetEase(Ease.OutQuad));
        seq.Append(_banSpriteRenderer.DOFade(1f, 0.2f).SetEase(Ease.InQuad));

        //대기 후 사라짐
        seq.AppendInterval(0.5f);
        seq.Append(_backgroundSpriteRenderer.DOFade(0f, 0.3f).SetEase(Ease.OutQuad));
        seq.Join(_iconSpriteRenderer.DOFade(0f, 0.3f).SetEase(Ease.OutQuad));
        seq.Join(_banSpriteRenderer.DOFade(0f, 0.3f).SetEase(Ease.OutQuad));
        seq.Join(_shadowSpriteRenderer.DOFade(0f, 0.3f).SetEase(Ease.OutQuad));

        //지속적으로 sortingOrder 계산
        seq.OnUpdate(() =>
        {
            _backgroundSpriteRenderer.sortingOrder = (int)(_objectTransform.position.y * -100.0f);
            _iconSpriteRenderer.sortingOrder = (int)(_objectTransform.position.y * -100.0f) + 1;
            _banSpriteRenderer.sortingOrder = (int)(_objectTransform.position.y * -100.0f) + 2;

            _shadowSpriteRenderer.sortingOrder = (int)(_objectTransform.position.y * -100.0f);
        });

        seq.OnComplete(() =>
        {
            ResourcePool.Instance.PutBackInstance(PREFAB_PATH, this.gameObject);
        });

        // 동시에 X축 이동 (전체 시간에 맞춰 자연스럽게)
        this.transform.DOLocalMoveX(endPosition.x, totalDuration).SetEase(Ease.InOutQuad);
       
    }

}
