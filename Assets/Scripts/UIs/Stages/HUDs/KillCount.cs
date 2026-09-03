using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillCount : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _killCountText;

    public void UpdateKillCount(long killCount)
    {
        this._killCountText.text = killCount.ToString();
    }
}
