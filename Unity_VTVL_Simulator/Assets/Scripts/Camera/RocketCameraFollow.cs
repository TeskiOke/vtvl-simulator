using System;
using UnityEngine;

namespace DSTU.VTVL.UnityAdapters
{
    using DSTU.VTVL.PhysicsCore;

    /// <summary>
    /// Режимы работы кинематографической камеры.
    /// </summary>
    public enum CameraViewMode
    {
        ChaseCamera,
        SideTrackCamera,
        PadACamera,
        BargeBCamera
    }

    /// <summary>
    /// Продвинутая кинематографическая камера с поддержкой свободного 360° вращения мышью (Orbit),
    /// плавного зума колёсиком и отсутствием лагов/рывков на любых скоростях TimeWarp (x1, x5, x10).
    /// </summary>
    public class RocketCameraFollow : MonoBehaviour
    {
        [Header("Цель слежения")]
        public Transform TargetRocket;
        public RocketPhysicsBridge PhysicsBridge;

        [Header("Режим обзора")]
        public CameraViewMode ViewMode = CameraViewMode.ChaseCamera;

        [Header("Настройки свободного вращения (Orbit)")]
        [Tooltip("Чувствительность вращения мышью при зажатой правой кнопке (ПКМ)")]
        public float MouseSensitivityX = 4.0f;
        public float MouseSensitivityY = 2.5f;
        public float ZoomSensitivity = 12.0f;
        public float MinDistance = 8.0f;
        public float MaxDistance = 450.0f;

        [Header("Текущие углы и дистанция")]
        public float CurrentDistance = 35.0f;
        public float TargetDistance = 35.0f;
        private float _orbitYaw = 0f;
        private float _orbitPitch = 15f;

        [Header("Фиксированные точки полигона")]
        public Vector3 PadAPosition = new Vector3(0f, 3f, -70f);
        public Vector3 BargeBPosition = new Vector3(25270f, 6f, 60f);

        private Camera _cam;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
        }

        private void Start()
        {
            if (TargetRocket != null)
            {
                _orbitYaw = TargetRocket.eulerAngles.y + 180f;
            }
        }

        private void LateUpdate()
        {
            if (TargetRocket == null)
            {
                if (PhysicsBridge != null) TargetRocket = PhysicsBridge.transform;
                if (TargetRocket == null) return;
            }

            // Переключение режимов горячими клавишами 1..5
            if (Input.GetKeyDown(KeyCode.Alpha1)) { ViewMode = CameraViewMode.ChaseCamera; TargetDistance = 35f; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { ViewMode = CameraViewMode.SideTrackCamera; TargetDistance = 55f; }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { ViewMode = CameraViewMode.PadACamera; }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { ViewMode = CameraViewMode.BargeBCamera; }
            if (Input.GetKeyDown(KeyCode.Alpha5)) { ViewMode = CameraViewMode.ChaseCamera; }

            // Управление зумом (колёсико мыши)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                TargetDistance = Mathf.Clamp(TargetDistance - scroll * ZoomSensitivity * (TargetDistance * 0.25f), MinDistance, MaxDistance);
            }
            CurrentDistance = Mathf.Lerp(CurrentDistance, TargetDistance, Time.unscaledDeltaTime * 12f);

            // Свободное вращение мышью (зажата правая кнопка мыши ПКМ или левая с зажатым Alt)
            if (Input.GetMouseButton(1) || (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))))
            {
                _orbitYaw += Input.GetAxis("Mouse X") * MouseSensitivityX;
                _orbitPitch -= Input.GetAxis("Mouse Y") * MouseSensitivityY;
                _orbitPitch = Mathf.Clamp(_orbitPitch, -85f, 85f);
            }

            Vector3 rocketPos = TargetRocket.position;

            switch (ViewMode)
            {
                case CameraViewMode.ChaseCamera:
                    UpdateOrbitCamera(rocketPos, followRotation: true);
                    break;

                case CameraViewMode.SideTrackCamera:
                    UpdateSideCamera(rocketPos);
                    break;

                case CameraViewMode.PadACamera:
                    transform.position = PadAPosition;
                    transform.LookAt(rocketPos + Vector3.up * 4f);
                    break;

                case CameraViewMode.BargeBCamera:
                    transform.position = BargeBPosition;
                    transform.LookAt(rocketPos + Vector3.up * 4f);
                    break;
            }
        }

        private void UpdateOrbitCamera(Vector3 targetCenter, bool followRotation)
        {
            // Динамическое масштабирование дистанции в космосе (> 60 км) для красивого охвата Земли
            float dynamicDist = CurrentDistance;
            if (targetCenter.y > 60000f)
            {
                dynamicDist = Mathf.Max(dynamicDist, 80f);
            }

            Quaternion rotation = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -dynamicDist);

            // Позиционируем прямо относительно центра ракеты без задержки на больших TimeWarp
            transform.position = targetCenter + offset + Vector3.up * 2.5f;
            transform.LookAt(targetCenter + Vector3.up * 2.5f);
        }

        private void UpdateSideCamera(Vector3 targetCenter)
        {
            Vector3 sidePos = targetCenter + new Vector3(CurrentDistance * 1.5f, 5f, 0f);
            transform.position = sidePos;
            transform.LookAt(targetCenter + Vector3.up * 3f);
        }
    }
}
