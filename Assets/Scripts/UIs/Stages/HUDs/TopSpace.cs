using DG.Tweening;
using Shared.GameDataTypes;
using UnityEngine;
using UnityEngine.UI;

public class TopSpace : MonoBehaviour
{
    [SerializeField] private Image _bossHud;
    Sequence _bossHudSequence;
    public void Initialize(HeroType heroType )
    {
        Color original = new Color(38.0f/255.0f,38.0f / 255.0f, 44.0f / 255.0f);
        Color warningColor = new Color(133.0f / 255.0f, 0.0f, 0.0f);
        _bossHud.color = original;
        Sequence seq = DOTween.Sequence();
        seq.Append(_bossHud.DOColor(warningColor, 0.2f));
        seq.Append(_bossHud.DOColor(original, 0.2f));
        seq.SetLoops(-1);
        seq.SetRecyclable(true);
        seq.SetAutoKill(false);
        seq.Pause();
        _bossHudSequence = seq;
        _bossHud.gameObject.SetActive(false);       
    }

    public void OnBossWarning()
    {
        _bossHud.gameObject.SetActive(true);
        _bossHudSequence.Restart();
    }

    public void OffBossWarning()
    {
        _bossHud.gameObject.SetActive(false);
        _bossHudSequence.Pause();
    }
}
