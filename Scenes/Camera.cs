using OpenTK.Mathematics;
using System;

namespace _3DSpritesheetConverter.Scenes
{
    class Camera
    {
        public Vector3 Position;
        private Vector3 _front = -Vector3.UnitZ;
        private Vector3 _up = Vector3.UnitY;
        private Vector3 _right = Vector3.UnitX;

        private float _pitch;
        private float _yaw = 0f;
        private float _fov = MathHelper.PiOver2;

        private Quaternion _orientation = Quaternion.Identity;
        public bool IsOrthographic { get; set; } = false;
        
        public float OrthographicSize
        {
            get
            {
                float distance = (Position - Target).Length;
                return distance * (float)Math.Tan(_fov / 2f) * 2f;
            }
        }

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
            Target = Vector3.Zero;
            UpdateOrientationFromAngles();
            UpdateVectors();
        }

        public Vector3 Front => _front;
        public Vector3 Up => _up;
        public Vector3 Right => _right;
        public float AspectRatio { private get; set; }
        public Vector3 Target { get; set; }

        public float Pitch
        {
            get => MathHelper.RadiansToDegrees(_pitch);
            set
            {
                float angle = MathHelper.Clamp(value, -89f, 89f);
                _pitch = MathHelper.DegreesToRadians(angle);
                UpdateOrientationFromAngles();
                UpdateVectors();
            }
        }

        public float Yaw
        {
            get => MathHelper.RadiansToDegrees(_yaw);
            set
            {
                _yaw = MathHelper.DegreesToRadians(value);
                UpdateOrientationFromAngles();
                UpdateVectors();
            }
        }

        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                float angle = MathHelper.Clamp(value, 1f, 90f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + _front, _up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            if (IsOrthographic)
            {
                float width = OrthographicSize * AspectRatio;
                float height = OrthographicSize;
                return Matrix4.CreateOrthographic(width, height, 0.01f, 10000f);
            }

            return GetPerspectiveProjectionMatrix();
        }

        public Matrix4 GetPerspectiveProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.01f, 10000f);
        }

        private void UpdateOrientationFromAngles()
        {
            Quaternion qYaw = Quaternion.FromAxisAngle(Vector3.UnitY, _yaw);

            Vector3 rightAfterYaw = Vector3.Transform(Vector3.UnitX, qYaw);

            Quaternion qPitch = Quaternion.FromAxisAngle(rightAfterYaw, _pitch);

            _orientation = qPitch * qYaw;
        }

        private void UpdateVectors()
        {
            _front = Vector3.Transform(-Vector3.UnitZ, _orientation);
            _front = Vector3.Normalize(_front);

            _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
            _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        }

        public void Orbit(float yawDeltaDegrees, float pitchDeltaDegrees)
        {
            float distance = (Position - Target).Length;

            Yaw += yawDeltaDegrees;
            Pitch += pitchDeltaDegrees;
            Position = Target - _front * distance;
        }

        public void Zoom(float amount)
        {
            Position += _front * amount;
        }
    }
}