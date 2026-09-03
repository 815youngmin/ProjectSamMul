using DG.Tweening;
using Shared.Localizers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Stages.HUDs
{
    public class StagePCStatGroup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _attackPowerText;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private TextMeshProUGUI _resurrectCountText;
        [SerializeField] private RectTransform _resurrectCountIcon;
        [SerializeField] private RectTransform _resurrectSequenceIcon;

        private Vector2 _savePrevPosition;
        private Vector3 _savePrevScale;
        public void Initialize(int attackPower, int currentHp, int maxHp, int currentResurrectCount)
        {
            this.UpdateAttackPower(attackPower);
            this.UpdateHP(currentHp, maxHp);
            this.UpdateResurrectCount(currentResurrectCount);

            //부활 연출에 사용될 _resurrectSequenceIcon은 계산의 편의성을 위해 StageSceneUI 바로 아래 단계로 하이어라키를 옮긴다.
            RectTransform canvasRectTransform = UnityGlobal.Scenes.GetCurrentSceneUI().RectTransform;
            _resurrectSequenceIcon.SetParent(canvasRectTransform);

            _savePrevPosition = _resurrectSequenceIcon.localPosition;
            _savePrevScale = _resurrectSequenceIcon.localScale;

            _resurrectSequenceIcon.gameObject.SetActive(false);
        }

        public void UpdateAttackPower(int attackPower)
        {
            _attackPowerText.text = attackPower.ToShortNumericText();
        }

        public void UpdateHP(int currentHp, int maxHp)
        {
            _hpText.text = string.Format("{0}/{1}", currentHp.ToShortNumericText(), maxHp.ToShortNumericText());
        }

        public void UpdateResurrectCount(int currentResurrectCount)
        {
            _resurrectCountText.text = currentResurrectCount.ToShortNumericText();

            if (currentResurrectCount <= 0)
            {
                _resurrectCountText.gameObject.SetActive(false);
                _resurrectCountIcon.gameObject.SetActive(false);
                _resurrectSequenceIcon.gameObject.SetActive(false);
            }
            else
            {
                _resurrectCountText.gameObject.SetActive(true);
                _resurrectCountIcon.gameObject.SetActive(true);
                _resurrectSequenceIcon.gameObject.SetActive(false);
            }
        }

        public void PlayResurrectSequence(PlayerCharacter pc)
        {
            var sequence = DOTween.Sequence(); 
            Vector2 screenPoint = GameClient.CameraController.MainCamera.WorldToScreenPoint(pc.CenterPos);
            RectTransform canvasRectTransform = UnityGlobal.Scenes.GetCurrentSceneUI().RectTransform;

            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPoint,
                GameClient.CameraController.MainCamera,
                out localPos);

            Vector2 pivotOffset = new Vector2(
                (0.5f - _resurrectSequenceIcon.pivot.x) * _resurrectSequenceIcon.rect.width,
                (0.5f - _resurrectSequenceIcon.pivot.y) * _resurrectSequenceIcon.rect.height
            );

            Vector2 finalPos = localPos - pivotOffset;
            _resurrectSequenceIcon.localPosition = _savePrevPosition;

            var pathPoints = new Vector3[]
            {
            Vector2.Lerp(_savePrevPosition,finalPos, 0.3f) + new Vector2(-500,0),//WPO
            _savePrevPosition, //A
            Vector2.Lerp(_savePrevPosition,finalPos, 0.3f) + new Vector2(-200,300),//B
            finalPos,//WP1
            Vector2.Lerp(_savePrevPosition,finalPos, 0.3f) + new Vector2(-800,-300),//C
            finalPos//D
            };

            sequence.Append(_resurrectSequenceIcon.DOLocalPath(pathPoints, 1.5f,PathType.CubicBezier, PathMode.TopDown2D).SetEase(Ease.OutQuad));
            sequence.Join(_resurrectSequenceIcon.DOScale(4, 1f).From(Vector3.one));
            sequence.Insert(1f,_resurrectSequenceIcon.DOScale(0, 0.5f));
            sequence.Append(_resurrectSequenceIcon.DOAnchorPos(finalPos + Vector2.up * 200f, 0.5f));
            sequence.Join(_resurrectSequenceIcon.DOScale(10f, 0.5f));
            sequence.Join(_resurrectSequenceIcon.GetComponent<Image>().DOFade(0f, 0.4f));

            sequence.OnComplete(() =>
            {
                _resurrectSequenceIcon.localPosition = _savePrevPosition;
                _resurrectSequenceIcon.localScale = _savePrevScale;
                _resurrectSequenceIcon.GetComponent<Image>().color = Color.white;
                _resurrectSequenceIcon.gameObject.SetActive(false);
            });

            _resurrectSequenceIcon.gameObject.SetActive(true);
        }

    }
}
