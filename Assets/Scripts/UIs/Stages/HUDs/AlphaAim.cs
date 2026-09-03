using System.Collections.Generic;
using UnityEngine;

public class AlphaAim : MonoBehaviour
{
    [SerializeField] private List<GameObject> _aimLineObjects;
    [SerializeField] private GameObject _arrow;

    private float _arrowDistance = 2.5f;

    public void Initialize()
    {
        this.SetArrow(Vector2.up);
    }

    public void SetArrow(Vector2 fireDirection)
    {
        float angle = this.GetDegreeAngle(fireDirection);
        _arrow.transform.localPosition = fireDirection * _arrowDistance;
        _arrow.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if (angle < 0)
        {
            angle += 360f;
        }

        var lineCount = _aimLineObjects.Count;
        if (lineCount <= 0)
        {
            return;
        }

        float divAngle = 360f / lineCount;
        int aimActiveIndex = ((int)(angle / divAngle)) % lineCount;
        
        for (int i = 0; i < lineCount; i++)
        {
            _aimLineObjects[i].gameObject.SetActive(false);
        }

        _aimLineObjects[aimActiveIndex].gameObject.SetActive(true);
    }

    private float GetDegreeAngle(Vector2 fireDirection)
    {
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        return angle;
    }

}
