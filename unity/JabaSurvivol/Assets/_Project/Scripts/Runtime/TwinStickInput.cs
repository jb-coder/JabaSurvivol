using JabaSurvivol.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Landscape twin-stick touch input with a desktop keyboard and mouse
    /// fallback for faster iteration in the editor and Windows builds.
    /// </summary>
    public sealed class TwinStickInput : MonoBehaviour, IGameInput
    {
        private const float TouchRadiusFactor = 0.14f;
        private const float AnalogDeadZone = 0.08f;

        private Camera _camera;
        private int _moveFinger = -1;
        private int _aimFinger = -1;
        private Vector2 _moveOrigin;
        private Vector2 _moveCurrent;
        private Vector2 _aimOrigin;
        private Vector2 _aimCurrent;

        public void Initialize(Camera camera)
        {
            _camera = camera;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            _moveFinger = -1;
            _aimFinger = -1;
        }

        public InputFrame Read(Vector2 playerWorldPosition)
        {
            UpdateTouches();

            var frame = new InputFrame
            {
                MoveStick = new StickState(_moveFinger >= 0, _moveOrigin, _moveCurrent),
                AimStick = new StickState(_aimFinger >= 0, _aimOrigin, _aimCurrent)
            };

            ReadTouchSticks(ref frame);
            ReadKeyboardAndMouse(ref frame, playerWorldPosition);
            return frame;
        }

        private void ReadTouchSticks(ref InputFrame frame)
        {
            var radius = Mathf.Max(32f, Mathf.Min(Screen.width, Screen.height) * TouchRadiusFactor);

            if (_moveFinger >= 0)
            {
                var delta = _moveCurrent - _moveOrigin;
                if (CombatMath.TryNormalizeStick(delta.x, delta.y, GameBalance.StickDeadZone, out var nx, out var ny))
                {
                    var magnitude = Mathf.Clamp01(delta.magnitude / radius);
                    frame.Move = new Vector2(nx, ny) * magnitude;
                }
            }

            if (_aimFinger >= 0)
            {
                var delta = _aimCurrent - _aimOrigin;
                if (CombatMath.TryNormalizeStick(delta.x, delta.y, GameBalance.AimStickDeadZone, out var nx, out var ny))
                {
                    frame.Aim = new Vector2(nx, ny);
                    frame.FireHeld = true;
                }
            }
        }

        private void ReadKeyboardAndMouse(ref InputFrame frame, Vector2 playerWorldPosition)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
                var y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
                var raw = new Vector2(x, y);
                if (raw.sqrMagnitude > 0f)
                {
                    frame.Move = raw.normalized;
                }

                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    frame.PausePressed = true;
                }
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (_camera != null && frame.Aim.sqrMagnitude < AnalogDeadZone * AnalogDeadZone)
            {
                var screenPosition = mouse.position.ReadValue();
                var world = _camera.ScreenToWorldPoint(
                    new Vector3(screenPosition.x, screenPosition.y, -_camera.transform.position.z));
                var delta = new Vector2(world.x, world.y) - playerWorldPosition;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    frame.Aim = delta.normalized;
                }
            }

            if (mouse.leftButton.isPressed)
            {
                frame.FireHeld = true;
            }
        }

        private void UpdateTouches()
        {
            foreach (var touch in Touch.activeTouches)
            {
                var finger = touch.finger.index;

                if (touch.phase == TouchPhase.Began)
                {
                    if (_moveFinger < 0 && touch.screenPosition.x < Screen.width * 0.5f)
                    {
                        _moveFinger = finger;
                        _moveOrigin = touch.screenPosition;
                        _moveCurrent = touch.screenPosition;
                    }
                    else if (_aimFinger < 0)
                    {
                        _aimFinger = finger;
                        _aimOrigin = touch.screenPosition;
                        _aimCurrent = touch.screenPosition;
                    }
                }
                else if (finger == _moveFinger)
                {
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        _moveFinger = -1;
                    }
                    else
                    {
                        _moveCurrent = touch.screenPosition;
                    }
                }
                else if (finger == _aimFinger)
                {
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        _aimFinger = -1;
                    }
                    else
                    {
                        _aimCurrent = touch.screenPosition;
                    }
                }
            }

            if (_moveFinger >= 0 && !FingerIsActive(_moveFinger))
            {
                _moveFinger = -1;
            }

            if (_aimFinger >= 0 && !FingerIsActive(_aimFinger))
            {
                _aimFinger = -1;
            }
        }

        private static bool FingerIsActive(int finger)
        {
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.finger.index == finger && touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
