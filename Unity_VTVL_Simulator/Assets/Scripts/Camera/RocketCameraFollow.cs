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
        ChaseCamera = 0,         // Свободный орбитальный облёт 360° (Chase/Orbit)
        SideTrackCamera = 1,     // Боковая следящая камера (Side Profile)
        PadACamera = 2,          // Камера со стартовой вышки Pad A
        BargeBCamera = 3,        // Камера с палубы посадочной баржи Drone Ship
        OnboardBellyCamera = 4,  // Бортовая камера вдоль теплозащитного экрана (Starship Belly Cam)
        EngineClusterCamera = 5  // Бортовая камера у сопел двигателей (Engine Cam)
    }

    /// <summary>
    /// Продвинутая кинематографическая камера с поддержкой свободного 360° вращения мышью (Orbit),
    /// бортовых камер (Belly Cam / Engine Cam) и плавного зума колёсиком.
    /// </summary>
    public class RocketCameraFollow : MonoBehaviour
    {
        [Header("Цель слежения")]
        public Transform TargetRocket;
        public RocketPhysicsBridge PhysicsBridge;

        [Header("Режим обзора")]
        public CameraViewMode ViewMode = CameraViewMode.ChaseCamera;

        [Header("Настройки свободного вращения (Orbit)")]
        public float MouseSensitivityX = 4.0f;
        public float MouseSensitivityY = 2.5f;
        public float ZoomSensitivity = 14.0f;
        public float MinDistance = 8.0f;
        public float MaxDistance = 600.0f;

        [Header("Текущие углы и дистанция")]
        public float CurrentDistance = 45.0f;
        public float TargetDistance = 45.0f;
        private float _orbitYaw = 180f;
        private float _orbitPitch = 15f;

        [Header("Фиксированные точки полигона")]
        public Vector3 PadAPosition = new Vector3(0f, 6f, -85f);
        public Vector3 BargeBPosition = new Vector3(25270f, 8f, 75f);

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

            // Переключение режимов горячими клавишами 1..6
            if (Input.GetKeyDown(KeyCode.Alpha1)) { ViewMode = CameraViewMode.ChaseCamera; TargetDistance = 45f; }
            if (Input.GetKeyDown(KeyCode.Alpha2)) { ViewMode = CameraViewMode.SideTrackCamera; TargetDistance = 65f; }
            if (Input.GetKeyDown(KeyCode.Alpha3)) { ViewMode = CameraViewMode.PadACamera; }
            if (Input.GetKeyDown(KeyCode.Alpha4)) { ViewMode = CameraViewMode.BargeBCamera; }
            if (Input.GetKeyDown(KeyCode.Alpha5)) { ViewMode = CameraViewMode.OnboardBellyCamera; }
            if (Input.GetKeyDown(KeyCode.Alpha6)) { ViewMode = CameraViewMode.EngineClusterCamera; }

            // Управление зумом (колёсико мыши)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                TargetDistance = Mathf.Clamp(TargetDistance - scroll * ZoomSensitivity * (TargetDistance * 0.22f), MinDistance, MaxDistance);
            }
            CurrentDistance = Mathf.Lerp(CurrentDistance, TargetDistance, Time.unscaledDeltaTime * 12f);

            // Свободное вращение мышью (зажата правая кнопка мыши ПКМ или левая с Alt)
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
                    UpdateOrbitCamera(rocketPos);
                    break;

                case CameraViewMode.SideTrackCamera:
                    UpdateSideCamera(rocketPos);
                    break;

                case CameraViewMode.PadACamera:
                    transform.position = PadAPosition;
                    transform.LookAt(rocketPos + Vector3.up * 8f);
                    break;

                case CameraViewMode.BargeBCamera:
                    transform.position = BargeBPosition;
                    transform.LookAt(rocketPos + Vector3.up * 8f);
                    break;

                case CameraViewMode.OnboardBellyCamera:
                    UpdateOnboardBellyCamera();
                    break;

                case CameraViewMode.EngineClusterCamera:
                    UpdateEngineCamera();
                    break;
            }
        }

        private void UpdateOrbitCamera(Vector3 targetCenter)
        {
            float dynamicDist = CurrentDistance;
            if (targetCenter.y > 60000f)
            {
                dynamicDist = Mathf.Max(dynamicDist, 90f);
            }

            Quaternion rotation = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -dynamicDist);

            transform.position = targetCenter + offset + Vector3.up * 4f;
            transform.LookAt(targetCenter + Vector3.up * 4f);
        }

        private void UpdateSideCamera(Vector3 targetCenter)
        {
            Vector3 sidePos = targetCenter + new Vector3(CurrentDistance * 1.5f, 8f, 0f);
            transform.position = sidePos;
            transform.LookAt(targetCenter + Vector3.up * 6f);
        }

        private void UpdateOnboardBellyCamera()
        {
            // Камера закреплена на носу корабля с видом вдоль корпуса на Землю
            Vector3 camPos = TargetRocket.TransformPoint(new Vector3(0f, 18f, 3.2f));
            Vector3 lookTarget = TargetRocket.TransformPoint(new Vector3(0f, -15f, 2.8f));
            transform.position = camPos;
            transform.LookAt(lookTarget, TargetRocket.up);
        }

        private void UpdateEngineCamera()
        {
            // Камера закреплена на юбке двигателей с видом вниз на сопла и струю
            Vector3 camPos = TargetRocket.TransformPoint(new Vector3(0f, -18f, -4.2f));
            Vector3 lookTarget = TargetRocket.TransformPoint(new Vector3(0f, -32f, 0f));
            transform.position = camPos;
            transform.LookAt(lookTarget, TargetRocket.up);
        }
    }
}
