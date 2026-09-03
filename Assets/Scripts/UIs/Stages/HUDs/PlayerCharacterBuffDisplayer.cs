using DG.Tweening;
using TMPro;
using UnityEngine;
using Z.GameClients;
using Z.GameClients.Stages.Characters.PCs;
using Z.ResourcePools;
using Z.Scenes;

public class PlayerCharacterBuffDisplayer : MonoBehaviour
{
    public static readonly string PREFAB_PATH = "Stages/UIs/HUDs/PlayerCharacterBuffDisplayer/PlayerCharacterBuffDisplayer.prefab";

    private static readonly float FADE_IN_TIME = 0.5f;
    private static readonly float WAITING_TIME = 1.2f;
    private static readonly float FADE_OUT_TIME = 0.3f;
    private static readonly Vector2 OFFSET = new Vector2(0.7f, 1f);

    [SerializeField] private RectTransform _statGroup;
    [SerializeField] private TextMeshProUGUI _buffText;

    public void DisplayPlayerCharacterBuff(PlayerCharacter pc, StageSceneUIRoot uiRoot, string buffInfoText)
    {
        _statGroup.gameObject.SetActive(false);
        _buffText.gameObject.SetActive(false);

        _buffText.text = buffInfoText;
        var rectTransfrom = this.GetComponent<RectTransform>();
        var displaySequence = DOTween.Sequence();

        //페이드인 연출
        var fadeInSequence = DOTween.Sequence();
        fadeInSequence.Join(_buffText.DOFade(1f, FADE_IN_TIME).From(0f).SetEase(Ease.Linear));
        displaySequence.Append(fadeInSequence);

        displaySequence.AppendInterval(WAITING_TIME);

        var fadeOutSequence = DOTween.Sequence();
        fadeOutSequence.Join(_buffText.DOFade(0f, FADE_OUT_TIME).From(1f).SetEase(Ease.Linear));
        displaySequence.Append(fadeOutSequence);


        displaySequence
        .OnStart(()=>
        {
            _statGroup.gameObject.SetActive(true);
            _statGroup.localPosition = Vector2.zero;
            _buffText.gameObject.SetActive(true);
        })
        .OnUpdate(() =>
        {
            var screenPoint = GameClient.CameraController.MainCamera.WorldToScreenPoint(pc.Pos + OFFSET);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot.RectTransform, screenPoint, GameClient.CameraController.MainCamera, out var resultPoint);
            rectTransfrom.localPosition = resultPoint;
        })
        .OnComplete(() =>
        {
            ResourcePool.Instance.PutBackInstance(PREFAB_PATH, gameObject);
        })
        .Play();

    }

}
