using _3DSpritesheetConverter.Scenes;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace _3DSpritesheetConverter
{
    class InputHandler
    {
        private readonly GameWindow _window;
        private readonly Camera _camera;

        private bool _firstMove = true;
        private Vector2 _lastPosition;
        private const float CameraSpeed = 1.5f;
        private const float Sensitivity = 0.2f;
        private const float ZoomSensitivity = 1.0f;

        private const float PanSensitivity = 0.005f;
        private const float DollySensitivity = 0.01f;

        public InputHandler(GameWindow window, Camera camera)
        {
            _window = window;
            _camera = camera;
        }

        public void HandleInput(FrameEventArgs args)
        {
            if (!_window.IsFocused)
            {
                return;
            }

            KeyboardState keyboard = _window.KeyboardState;
            MouseState mouse = _window.MouseState;

            if (mouse.IsButtonDown(MouseButton.Middle))
            {
                HandleMiddleMouse(mouse, keyboard);
            }
            else
            {
                _window.CursorState = CursorState.Normal;
                _firstMove = true;
            }
        }

        private void HandleMiddleMouse(MouseState mouse, KeyboardState keyboard)
        {
            if (_camera == null)
            {
                return;
            }

            _window.CursorState = CursorState.Grabbed;

            if (_firstMove)
            {
                _lastPosition = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
                return;
            }

            float deltaX = mouse.X - _lastPosition.X;
            float deltaY = mouse.Y - _lastPosition.Y;
            _lastPosition = new Vector2(mouse.X, mouse.Y);

            if (keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift))
            {
                Vector3 rightMove = _camera.Right * (deltaX * PanSensitivity * CameraSpeed);
                Vector3 upMove = _camera.Up * (deltaY * PanSensitivity * CameraSpeed);

                _camera.Position -= rightMove;
                _camera.Position += upMove;

                _camera.Target -= rightMove;
                _camera.Target += upMove;
                return;
            }

            if (keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl))
            {
                _camera.Position += _camera.Front * (-deltaY) * DollySensitivity * CameraSpeed;
                return;
            }

            float yawDelta = deltaX * Sensitivity;
            float pitchDelta = -deltaY * Sensitivity;

            _camera.Orbit(yawDelta, pitchDelta);
        }

        public void HandleMouseWheel(MouseWheelEventArgs e)
        {
            if (!_window.IsFocused || _camera == null)
            {
                return;
            }

            float zoomSensitivity = ZoomSensitivity;
            _camera.Zoom(e.OffsetY * zoomSensitivity);
        }
    }
}