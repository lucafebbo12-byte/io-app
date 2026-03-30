// PlayerInput.cs — wraps Unity New Input System for desktop.
// Mobile path uses VirtualJoystick (set via SetMobileInput).
using UnityEngine;
using UnityEngine.InputSystem;

namespace PaintGame
{
    public class PlayerInput : MonoBehaviour
    {
        private PlayerStats _stats;
        private Camera      _cam;

        // Mobile joystick references (assigned by VirtualJoystick components)
        private VirtualJoystick _moveJoystick;
        private VirtualJoystick _aimJoystick;

        private bool _isMobile;

        // Default Input Actions (created in code, no .inputactions asset needed)
        private InputAction _moveAction;
        private InputAction _aimAction;
        private InputAction _shootAction;

        public void Init(PlayerStats stats)
        {
            _stats  = stats;
            _cam    = Camera.main;

            // Detect mobile
            _isMobile = Application.isMobilePlatform;

#if UNITY_EDITOR
            // In editor, always use desktop controls
            _isMobile = false;
#endif

            if (!_isMobile) SetupDesktopActions();
        }

        private void SetupDesktopActions()
        {
            _moveAction = new InputAction("Move",
                InputActionType.Value,
                "<Keyboard>/w,<Keyboard>/s,<Keyboard>/a,<Keyboard>/d");
            _moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");

            // Simple composite WASD
            var moveMap = new InputActionMap("Player");

            _moveAction  = moveMap.AddAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up",    "<Keyboard>/w")
                .With("Down",  "<Keyboard>/s")
                .With("Left",  "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _shootAction = moveMap.AddAction("Shoot", InputActionType.Button);
            _shootAction.AddBinding("<Mouse>/leftButton");

            moveMap.Enable();
        }

        public void SetMobileJoysticks(VirtualJoystick move, VirtualJoystick aim)
        {
            _moveJoystick = move;
            _aimJoystick  = aim;
            _isMobile     = true;
        }

        // ── Called each frame by PlayerController ─────────────────────────────
        public void ReadInput()
        {
            if (_stats == null) return;

            if (_isMobile)
                ReadMobile();
            else
                ReadDesktop();
        }

        private void ReadDesktop()
        {
            // Move
            Vector2 move = Vector2.zero;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) move.y += 1f;
                if (Keyboard.current.sKey.isPressed) move.y -= 1f;
                if (Keyboard.current.aKey.isPressed) move.x -= 1f;
                if (Keyboard.current.dKey.isPressed) move.x += 1f;
            }
            _stats.MoveDir = move.normalized;

            // Aim (mouse world position)
            if (_cam != null && Mouse.current != null)
            {
                Vector3 mouseWorld = _cam.ScreenToWorldPoint(
                    new Vector3(Mouse.current.position.x.ReadValue(),
                                Mouse.current.position.y.ReadValue(), 0f));
                Vector2 dir = (Vector2)mouseWorld - _stats.WorldPos;
                if (dir.sqrMagnitude > 0.01f)
                    _stats.AimAngle = Mathf.Atan2(dir.y, dir.x);
            }

            // Shoot / Spray
            bool lmb = Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool space = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
            bool shooting = lmb || space;
            _stats.IsShooting    = shooting;
            _stats.WantsToShoot  = shooting;
        }

        private void ReadMobile()
        {
            if (_moveJoystick != null)
                _stats.MoveDir = _moveJoystick.Direction.normalized;

            if (_aimJoystick != null)
            {
                Vector2 aimDir = _aimJoystick.Direction;
                if (aimDir.sqrMagnitude > 0.09f)
                {
                    _stats.AimAngle   = Mathf.Atan2(aimDir.y, aimDir.x);
                    _stats.IsShooting = true;
                    _stats.WantsToShoot = true;
                }
                else
                {
                    _stats.IsShooting   = false;
                    _stats.WantsToShoot = false;
                }
            }
        }

        void OnDestroy()
        {
            _moveAction?.Dispose();
            _shootAction?.Dispose();
        }
    }
}
