using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;

namespace SamMul.UIs.Lobbies.EvolutionResearchPages
{
    public class ResearchLevelLabel : MonoBehaviour
    {
        [SerializeField]
        private Image _image;
        [SerializeField]
        private TextMeshProUGUI _levelText;

        private readonly string _levelOverImagePath = "Lobbys/UIs/EvolutionPages/Evolution_Level_OFF.png";  //초과
        private readonly string _levelBelowImagePath = "Lobbys/UIs/EvolutionPages/Evolution_Level_ON.png";//이하

        private int _level;
        private int _accountLevel;

        public void Initialize(int levelData, int accountLevel)
        {
            _levelText.text = levelData.ToString();
            _level = levelData;

            _accountLevel= accountLevel;

            this.UpdateImage();
        }

        private void UpdateImage()
        {
            if (_level <= _accountLevel)
            {
                _image.sprite = ResourcePool.Instance.LoadResource<Sprite>(_levelBelowImagePath);
            }
            else
            {
                _image.sprite = ResourcePool.Instance.LoadResource<Sprite>(_levelOverImagePath);
            }
        }

    }
}
