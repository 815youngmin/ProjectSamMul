using UnityEngine;

namespace Z.GameClients.Stages.CombatSystems
{
    public readonly struct CircularTargetArea
    {
        public readonly Vector2 Center;
        public readonly float Radius;

        public CircularTargetArea(Vector2 center, float radius)
        {
            this.Center = center;
            this.Radius = radius;
        }

        public bool Contains(Vector2 point, float colliderRadius)
        {
            float squaredSearchingRadius = (Radius + colliderRadius);
            squaredSearchingRadius *= squaredSearchingRadius;

            return (point - Center).sqrMagnitude <= squaredSearchingRadius;
        }

        public Rect GetMinMaxRect()
        {
            return new Rect(Center - (Vector2.one * Radius), Vector2.one * Radius * 2.0f);
        }
    }

    public readonly struct EllipseTargetArea
    {
        public readonly Vector2 Center;
        public readonly float xRadius;
        public readonly float yRadius;

        public readonly Vector2 dot1, dot2;   //타원의 정의에 해당하는 두 정점F, F'

        public EllipseTargetArea(Vector2 center, float radius)
        {
            this.Center = center;
            this.xRadius = radius;
            this.yRadius = radius * 0.5f; //일반적으로 사용되는 비율

            dot1 = center;
            dot1.x -= Mathf.Sqrt(xRadius * xRadius - yRadius * yRadius);

            dot2 = center;
            dot2.x += Mathf.Sqrt(xRadius * xRadius - yRadius * yRadius);
        }

        public EllipseTargetArea(Vector2 center, float xRadius, float yRadius)
        {
            this.Center = center;
            this.xRadius = xRadius;
            this.yRadius = yRadius;

            dot1 = center;
            dot1.x -= Mathf.Sqrt(xRadius * xRadius - yRadius * yRadius);

            dot2 = center;
            dot2.x += Mathf.Sqrt(xRadius * xRadius - yRadius * yRadius);
        }

    }

    /// <summary>
    /// 사각형 범위의 목표 영역을 지정합니다.
    /// 정사각형, 직사각형 영역 모두 지정 가능합니다.
    /// </summary>
    public readonly struct SquareTargetArea
    {
        public readonly Vector2 Center;
        public readonly Vector2 Size;
        public readonly float Angle;    // right vector로부터 얼마나 회전되었는지 여부 

        public SquareTargetArea(Vector2 center, Vector2 size, float angle)
        {
            this.Center = center;
            this.Size = size;
            this.Angle = angle;
        }

        public SquareTargetArea(Vector2 center, float width, float height, float angle)
        {
            this.Center = center;
            this.Size = new Vector2(width, height);
            this.Angle = angle;
        }

        public bool Contains(Vector2 point, float colliderRadius)
        {
            Rect rect = new Rect(Size * -0.5f, Size);

            // NOTE: 카메라 기준 Z축 forward를 바라보는 관점에서 반시계가 +, 시계 방향이 - 이다.
            // 해당 도형 축으로 맞추는것이기 때문에 도형 Angle을 0으로 만들어야 하기 때문에 -Angle을 회전시킵니다.
            Vector2 newPoint = point - Center;
            newPoint = Quaternion.AngleAxis(-Angle, Vector3.forward) * newPoint;

            Vector2 distance;
            distance.x = Mathf.Abs(newPoint.x);
            distance.y = Mathf.Abs(newPoint.y);

            float halfWidth = rect.width * 0.5f;
            float halfHight = rect.height * 0.5f;

            if (distance.x > (halfWidth + colliderRadius)) 
            {
                return false; 
            }

            if (distance.y > (halfHight + colliderRadius))
            {
                return false;
            }

            if (distance.x <= halfWidth) 
            {
                return true;
            }

            if (distance.y <= halfHight)
            { 
                return true;
            }

            // 사각형 모서리 점에서 원까지의 거리의 SqrMagnitude
            float cDist_sq = Mathf.Pow((distance.x - halfWidth), 2) + Mathf.Pow((distance.y - halfHight), 2);
            return (cDist_sq <= (colliderRadius * colliderRadius));
        }

        public Rect GetMinMaxRect()
        {
            if(Angle == 0.0f)
            {
                return new Rect(Center - Size * 0.5f, Size);
            }

            Vector2 halfSize = Size * 0.5f;
            Vector2 leftTop = new Vector2(-halfSize.x, halfSize.y);
            Vector2 leftBottom = new Vector2(-halfSize.x, -halfSize.y);
            Vector2 rightTop = new Vector2(halfSize.x, halfSize.y);
            Vector2 rightBottom = new Vector2(halfSize.x, -halfSize.y);

            Quaternion rot = Quaternion.AngleAxis(Angle, Vector3.forward);
            Vector2 rotLeftTop     = rot * leftTop;
            Vector2 rotLeftBottom  = rot * leftBottom;
            Vector2 rotRightTop    = rot * rightTop;
            Vector2 rotRightBottom = rot * rightBottom;

            rotLeftTop     += Center;
            rotLeftBottom  += Center;
            rotRightTop    += Center;
            rotRightBottom += Center;

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            min.x = rotLeftTop.x     < min.x ? rotLeftTop.x     : min.x;
            min.x = rotLeftBottom.x  < min.x ? rotLeftBottom.x  : min.x;
            min.x = rotRightTop.x    < min.x ? rotRightTop.x    : min.x;
            min.x = rotRightBottom.x < min.x ? rotRightBottom.x : min.x;

            min.y = rotLeftTop.y     < min.y ? rotLeftTop.y     : min.y;
            min.y = rotLeftBottom.y  < min.y ? rotLeftBottom.y  : min.y;
            min.y = rotRightTop.y    < min.y ? rotRightTop.y    : min.y;
            min.y = rotRightBottom.y < min.y ? rotRightBottom.y : min.y;

            max.x = max.x < rotLeftTop.x      ? rotLeftTop.x    : max.x;
            max.x = max.x < rotLeftBottom.x   ? rotLeftBottom.x : max.x;
            max.x = max.x < rotRightTop.x     ? rotRightTop.x   : max.x;
            max.x = max.x < rotRightBottom.x  ? rotRightBottom.x: max.x;

            max.y = max.y < rotLeftTop.y      ? rotLeftTop.y    : max.y;
            max.y = max.y < rotLeftBottom.y   ? rotLeftBottom.y : max.y;
            max.y = max.y < rotRightTop.y     ? rotRightTop.y   : max.y;
            max.y = max.y < rotRightBottom.y  ? rotRightBottom.y: max.y;

            Rect resultRect = new Rect();
            resultRect.min = min;
            resultRect.max = max;
            return resultRect;
        }
    }


    public readonly struct DonutTargetArea
    {
        public readonly Vector2 Center;
        // 바깥 원의 반경
        public readonly float Radius;
        // 원형 가운데 구멍의 반경 (뚫린 원의 반경)
        public readonly float HoleRadius;

        public DonutTargetArea(Vector2 center, float radius, float holeRadius)
        {
            this.Center = center;
            this.Radius = radius;
            this.HoleRadius = holeRadius;
        }
    }

    public readonly struct CircularSectorTargetArea
    {
        private readonly CircularTargetArea _circular;
        private readonly Vector2 _direction;
        private readonly float _angle;
        // 비교검사에 사용하기위해 미리 구워둔다. direction기준으로 angle을 펼친 radian값
        private readonly float _cosRadian;

        public readonly Vector2 Center=> _circular.Center;
        public readonly float Radius => _circular.Radius;
        public readonly float Angle => _angle;
        public readonly Vector2 Direction => _direction;
        public readonly float CosRadian => _cosRadian;

        /// <param name="angle"><paramref name="direction"/>기준으로 벌어진 각도 좌우 합</param>
        public CircularSectorTargetArea(Vector2 center, Vector2 direction, float radius, float angle)
        {
            if (direction == Vector2.zero)
            {
                Debug.LogWarning($"{nameof(CircularSectorTargetArea)}에 direction이 Vector2.zero로 입력됨. direction을 Vector2.right로 대체합니다.");
                direction = Vector2.right;
            }
            
            _circular = new CircularTargetArea(center, radius);
            _direction = direction;
            _direction.Normalize();
            _angle = angle;
            _cosRadian = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad) ;
        }

        public Rect GetMinMaxRect()
        {
            Rect minMaxRect = new Rect();
            float halfAngle = Angle * 0.5f;
            Vector2 startDir = Quaternion.AngleAxis(-halfAngle, Vector3.forward) * Direction;
            Vector2 endDir = Quaternion.AngleAxis(halfAngle, Vector3.forward) * Direction ;

            float[] xArray = new float[3] {0, startDir.x, endDir.x };
            float[] yArray = new float[3] { 0, startDir.y, endDir.y };

            float right = (IsInAngle(Vector2.right) ? 1.0f : Mathf.Max(xArray));
            float bottom = (IsInAngle(Vector2.down) ? -1.0f : Mathf.Min(yArray));
            float left = (IsInAngle(Vector2.left) ? -1.0f : Mathf.Min(xArray));
            float up = (IsInAngle(Vector2.up) ? 1.0f : Mathf.Max(yArray));

            minMaxRect.width =  Mathf.Abs(right - left) * Radius;
            minMaxRect.height = Mathf.Abs(up - bottom) * Radius;
            minMaxRect.x = left * Radius;
            minMaxRect.y = bottom * Radius;
            minMaxRect.center += Center;
            return minMaxRect;
        }

        private bool IsInAngle(Vector2 targetDir)
        {
            float dotValue = Vector3.Dot(Direction, targetDir);
            return CosRadian <= dotValue;
        }

        public bool Contains(Vector2 point)
        {
            Vector3 targetDir = point - Center;
            targetDir.z = 0;

            bool isInCircle = (targetDir.sqrMagnitude <= Radius * Radius);
            if (!isInCircle)
            {
                return false;
            }

            targetDir.Normalize();
            return IsInAngle(targetDir);
        }

        public bool Contains(Vector2 point, float colliderRadius)
        {
            Vector2 targetDir = point - Center;

            // colliderRadius를 반지름에 추가하여 확장된 반지름으로 충돌 체크
            float effectiveRadius = Radius + colliderRadius;

            // 원 내에 있는지 확인
            bool isInCircle = (targetDir.sqrMagnitude <= effectiveRadius * effectiveRadius);
            if (!isInCircle)
            {
                return false;
            }

            targetDir.Normalize();

            // IsInAngle 메서드는 여전히 targetDir에 대해 작동합니다.
            return IsInAngle(targetDir);
        }

    }
}
