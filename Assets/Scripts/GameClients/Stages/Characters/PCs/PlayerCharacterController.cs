#nullable enable
using UnityEngine;

namespace SamMul.GameClients.Stages.Characters.PCs
{
    /// <summary>
    /// UI의 조작 상태에 따라 PlayerCharacter를 조작하는 컨트롤러
    /// 현재 PlayerCharacter를 조작하는 UI로 조이스틱이 있다.
    /// 이후 버튼 등으로 스킬을 조작하게 된다면, 해당 버튼 혹은 버튼의 이벤트를 PlayerController가 수신하여 플레이어캐릭터를 조작하게 구현한다.
    /// </summary>
    public class PlayerCharacterController
    {
        private readonly PlayerCharacter _pc;
        private readonly VariableJoystick _joystick;

        // 플레이어 캐릭터를 조작하고 있는 방향. 조이스틱 방향을 따른다.
        private Vector2 _controlDirection;
        // 플레이어 캐릭터를 조작하고 있는지 여부.
        public bool IsControlling => _controlDirection != Vector2.zero;

        public PlayerCharacterController(PlayerCharacter target, VariableJoystick joystick)
        {
            _pc = target;
            _joystick = joystick;
            _controlDirection = Vector2.zero;
        }

        public void Update(Stage stage)
        {
            if (!_pc.gameObject.activeSelf)
            {
                // 등장 연출중에는 PC의 비활성화중이다.
                return;
            }

            if (Time.timeScale <= 0f)
            {
                // 팝업이 띄워져 있다던지 해서 시간이 멈췄다. 업데이트도 멈춘다.
                return;
            }

            if (_pc.Action.IsAppearing)
            {
                // Appearing 액션중에는 움직이지 않는다.
                return;
            }

            if(_pc.Action.IsDead)
            {
                return;
            }

            this.ReadJoystickAndControlPlayer(stage);
        }

        private void BeginControl(Stage stage, Vector2 joystickDirection)
        {
            _controlDirection = joystickDirection;

            // 이동을 계속 이렇게 구현할 것인가?
            // Controller는 pc의 Velocity or MovePower만 설정하는게 맞지 않겠나?
            _pc.Move(_controlDirection);
        }

        private void UpdateControl(Stage stage, Vector2 joystickDirection)
        {
            // MoveVector 크기 1로 고정해달라고 요청해주셔서, Normalize한다.
            // (0.0~1.0 값으로 움직이는게아니라 항상 1의 크기로 움직이길 원함)
            _controlDirection = joystickDirection.normalized;
            _pc.Move(_controlDirection);
        }

        private void EndControl(Stage stage)
        {
            _controlDirection = Vector2.zero;
            _pc.StopMovement();
        }

        #region Reading Joystick Logic
        private void ReadJoystickAndControlPlayer(Stage stage)
        {
            var joystickDirection = _joystick.Direction;

            // 개발용 빌드에서만 키보드 사용해서 캐릭터 움직인다.
            if (Debug.isDebugBuild &&
                (joystickDirection == Vector2.zero))
            {
                joystickDirection = ReadKeyboardInputAndMakeJoystickDirection();
            }

            if (!IsControlling)
            {
                if (joystickDirection == Vector2.zero)
                {
                    // 조이스틱 조작도 안하면 조기 종료
                    return;
                }

                this.BeginControl(stage, joystickDirection);
            }
            else
            {
                if (joystickDirection != Vector2.zero)
                {
                    this.UpdateControl(stage, joystickDirection);
                }
                else
                {
                    this.EndControl(stage);
                }
            }
        }

        // 개발용 빌드에서 사용할 키보드 조작
        private Vector2 ReadKeyboardInputAndMakeJoystickDirection()
        {
            Vector2 joystickDirection = Vector2.zero;
            if (Input.anyKey)
            {
                if (Input.GetKey(KeyCode.A))
                {
                    joystickDirection.x += -1f;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    joystickDirection.y += -1f;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    joystickDirection.x += 1f;
                }

                if (Input.GetKey(KeyCode.W))
                {
                    joystickDirection.y += 1f;
                }
                joystickDirection.Normalize();
            }

            return joystickDirection;
        }

        #endregion
    }
}
