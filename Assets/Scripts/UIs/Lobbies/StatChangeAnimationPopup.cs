using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;

public class StatChangeAnimationPopup : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private Image _statIcon;
    [SerializeField] private TextMeshProUGUI _changeAmountStatText;

    private Color _saveBackgroundImageColor;
    private Color _saveStatIconColor;
    private Color _saveTextColor;

    /// <summary>
    /// 스탯 변화 연출을 사용하기 위한 인터페이스 입니다.
    /// 스탯 아이콘 값, 현재 스탯, 변경된 스탯 값 등을 넣으면 증가, 감소에 따른 애니메이션 연출이 재생됩니다.
    /// </summary>
    /// <param name="iconPath">스탯 아이콘</param>
    /// <param name="currentStat">변경되기전 스탯 값</param>
    /// <param name="changeStat">변견된 후 스탯 값</param>
    /// <param name="parentTransform">연출을 진행할 부모 트랜스폼</param>
    /// <param name="offsetStartPosition">애니메이션 연출 시작 위치 보정 값</param>
    /// <param name="playTime">진행 시간</param>
    public void InitializeAndPlay(string iconPath, int currentStat, int changeStat, RectTransform parentTransform, Vector3 offsetStartPosition, float playTime)
    {
        this.transform.SetParent(parentTransform, false);
        this.transform.localPosition = offsetStartPosition;
        this.gameObject.transform.localScale = Vector3.one;

        _statIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(iconPath);
        _saveBackgroundImageColor = _background.color;
        _saveStatIconColor = _statIcon.color;
        _saveTextColor = _changeAmountStatText.color;

        if (currentStat < changeStat)
        {
            _changeAmountStatText.text = $"▲ (+{changeStat - currentStat})";
            _changeAmountStatText.color = Color.green;
        }
        else if(currentStat > changeStat)
        {
            _changeAmountStatText.text = $"▼ (-{currentStat - changeStat})";
            _changeAmountStatText.color = Color.red;
        }

        Sequence sequence;
        sequence = DOTween.Sequence();
        sequence.Append(_background.DOFade(0.0f, playTime).SetEase(Ease.InQuint));
        sequence.Join(_statIcon.DOFade(0.0f, playTime).SetEase(Ease.InQuint));
        sequence.Join(_changeAmountStatText.DOFade(0.0f, playTime).SetEase(Ease.InQuint));

        if (currentStat < changeStat)
        {
            sequence.Join(this.GetComponent<RectTransform>().DOLocalMoveY(100f + offsetStartPosition.y, playTime));
        }
        else if (currentStat > changeStat)
        {
            sequence.Join(this.GetComponent<RectTransform>().DOLocalMoveY(-100f + offsetStartPosition.y, playTime));
        }
        sequence.OnComplete(() =>
        {
            Putback();
        });
        sequence.Play();
    }

    private void Putback()
    {
        _background.color = _saveBackgroundImageColor;
        _statIcon.color = _saveStatIconColor;
        _changeAmountStatText.color = _saveTextColor;
        this.gameObject.transform.localScale = Vector3.one;
        this.gameObject.SetActive(false);
        ResourcePool.Instance.PutBackInstance("Lobbys/UIs/StatChangeAnimation.prefab", this.gameObject);
    }

}
