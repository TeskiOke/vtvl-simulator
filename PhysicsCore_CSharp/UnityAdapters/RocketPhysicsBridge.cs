using System;
using System.Collections.Generic;
using UnityEngine;

namespace DSTU.VTVL.UnityAdapters
{
    using DSTU.VTVL.PhysicsCore;
    using DSTU.VTVL.Guidance;

    /// <summary>
    /// Выбор численного метода интегрирования.
    /// </summary>
    public enum IntegratorType
    {
        RungeKutta4,
        EulerExplicit
    }

    /// <summary>
    /// Мост между физико-математическим ядром (ОДУ Коши) и графическим движком Unity.
    /// Вычисляет движение ракеты VTVL-30 в FixedUpdate без PhysX / Rigidbody.
    /// Поддерживает ручное управление, отрисовку 3D-траектории и реалистичные визуальные эффекты.
    /// </summary>
    public class RocketPhysicsBridge : MonoBehaviour
    {
        [Header("Настройки численного решателя")]
        [Tooltip("Выбор численного интегратора: RK4 (точность O(dt^4)) или Euler (1-й порядок)")]
        public IntegratorType Integrator = IntegratorType.RungeKutta4;

        [Tooltip("Фиксированный шаг физического времени (с)")]
        [Range(0.001f, 0.05f)]
        public float SimulationFixedDt = 0.01f;

        [Tooltip("Множитель ускорения времени (Time Warp)")]
        [Range(1, 10)]
        public int TimeWarp = 1;

        [Header("Режим управления")]
        public bool IsAutopilotEnabled = true;

        [Range(0f, 1f)]
        public float ManualThrottle = 0f;

        [Range(0f, 180f)]
        public float ManualPitchDegrees = 90f;

        [Header("Визуализация 3D Траектории")]
        public bool ShowTrajectory = true;
        public LineRenderer TrajectoryLine;
        private List<Vector3> _trajectoryPoints = new List<Vector3>();
        private float _lastTrajectoryRecordTime = 0f;

        [Header("Визуальные эффекты (VFX)")]
        public Transform EngineNozzle;
        public ParticleSystem MainEnginePlume;
        public ParticleSystem LandingSteamPlume;
        public ParticleSystem ReentryPlasmaGlow;
        public ParticleSystem LeftRcsPlume;
        public ParticleSystem RightRcsPlume;
        public GameObject GridFinsModel;
        public AudioSource EngineAudio;

        // Внутреннее математическое состояние
        public RocketState State { get; private set; }
        public AutopilotGNC Autopilot { get; private set; }

        private bool _isSimulationActive = false;

        public void EnsureInitialized()
        {
            if (State == null) State = new RocketState();
            if (Autopilot == null) Autopilot = new AutopilotGNC();
        }

        private void Awake()
        {
            EnsureInitialized();
            Time.fixedDeltaTime = SimulationFixedDt;
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            EnsureInitialized();
            ResetSimulation();
        }

        private void Update()
        {
            // Горячие клавиши управления
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                IsAutopilotEnabled = !IsAutopilotEnabled;
                Debug.Log($"[GNC] Режим переключен: {(IsAutopilotEnabled ? "АВТОПИЛОТ" : "РУЧНОЕ УПРАВЛЕНИЕ")}");
            }

            if (!IsAutopilotEnabled)
            {
                // Управление тягой W / S
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                {
                    ManualThrottle = Mathf.Clamp01(ManualThrottle + 0.4f * Time.unscaledDeltaTime);
                }
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                {
                    ManualThrottle = Mathf.Clamp01(ManualThrottle - 0.4f * Time.unscaledDeltaTime);
                }

                // Управление тангажом A / D
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                {
                    ManualPitchDegrees = Mathf.Clamp(ManualPitchDegrees + 20f * Time.unscaledDeltaTime, 0f, 180f);
                }
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                {
                    ManualPitchDegrees = Mathf.Clamp(ManualPitchDegrees - 20f * Time.unscaledDeltaTime, 0f, 180f);
                }

                // Экстренная тяга Space / Сброс тяги X
                if (Input.GetKeyDown(KeyCode.Space)) ManualThrottle = 1.0f;
                if (Input.GetKeyDown(KeyCode.X)) ManualThrottle = 0.0f;
            }

            // Динамический расчёт отклонения сопла маршевого двигателя (TVC Gimbal)
            float targetGimbal = 0f;
            if (!IsAutopilotEnabled)
            {
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                    targetGimbal = (float)RocketParameters.TvcMaxGimbalAngleDeg;
                else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                    targetGimbal = -(float)RocketParameters.TvcMaxGimbalAngleDeg;
            }
            else
            {
                float dPitchDeg = (float)(ManualPitchDegrees - State.PitchDegrees);
                targetGimbal = Mathf.Clamp(dPitchDeg * 0.45f, -(float)RocketParameters.TvcMaxGimbalAngleDeg, (float)RocketParameters.TvcMaxGimbalAngleDeg);
            }
            State.GimbalAngleDegrees = Mathf.MoveTowards((float)State.GimbalAngleDegrees, targetGimbal, (float)RocketParameters.TvcMaxSlewRateDegPerSec * Time.unscaledDeltaTime);

            // Сброс сцены R
            if (Input.GetKeyDown(KeyCode.R)) ResetSimulation();

            // Переключение траектории T
            if (Input.GetKeyDown(KeyCode.T))
            {
                ShowTrajectory = !ShowTrajectory;
                if (TrajectoryLine != null) TrajectoryLine.enabled = ShowTrajectory;
            }

            // Ускорение времени [ и ]
            if (Input.GetKeyDown(KeyCode.LeftBracket)) TimeWarp = Mathf.Max(1, TimeWarp / 2);
            if (Input.GetKeyDown(KeyCode.RightBracket)) TimeWarp = Mathf.Min(10, TimeWarp == 1 ? 2 : (TimeWarp == 2 ? 5 : 10));
        }

        /// <summary>
        /// Запуск или возобновление симуляции.
        /// </summary>
        public void StartSimulation()
        {
            _isSimulationActive = true;
        }

        /// <summary>
        /// Приостановка симуляции.
        /// </summary>
        public void PauseSimulation()
        {
            _isSimulationActive = false;
        }

        public void ToggleSimulation()
        {
            _isSimulationActive = !_isSimulationActive;
        }

        /// <summary>
        /// Сброс ракеты на стартовый стол A.
        /// </summary>
        public void ResetSimulation()
        {
            EnsureInitialized();
            _isSimulationActive = false;
            State.Reset();
            ManualThrottle = 0f;
            ManualPitchDegrees = 90f;
            SyncTransformWithState();
            UpdateVisuals(0.0, 90.0, false, false);

            _trajectoryPoints.Clear();
            if (TrajectoryLine != null)
            {
                TrajectoryLine.positionCount = 0;
            }
            if (TelemetryLogger.Instance != null)
            {
                TelemetryLogger.Instance.ResetLogger();
            }
        }

        private void FixedUpdate()
        {
            EnsureInitialized();
            if (!_isSimulationActive || State.IsLanded || State.IsCrashed)
            {
                return;
            }

            // Выполнение шагов с учётом TimeWarp
            for (int step = 0; step < TimeWarp; step++)
            {
                if (State.IsLanded || State.IsCrashed) break;
                StepSimulationPhysics(SimulationFixedDt);
            }

            // 5. Синхронизация визуального представления в Unity
            SyncTransformWithState();
            UpdateVisuals(State.Throttle, State.PitchDegrees, State.IsRcsActive, State.IsGridFinsActive);

            // 6. Запись 3D-траектории
            if (Time.time - _lastTrajectoryRecordTime > 0.15f)
            {
                _lastTrajectoryRecordTime = Time.time;
                RecordTrajectoryPoint(transform.position);
            }
        }

        /// <summary>
        /// Выполняет один численный шаг физики и систем наведения.
        /// </summary>
        public void StepSimulationPhysics(double dt)
        {
            EnsureInitialized();
            if (State.IsLanded || State.IsCrashed) return;

            // 1. Управление (Автопилот или Ручное)
            double throttleCmd;
            double pitchCmdRad;
            bool isReentry = (State.VelY < 0.0 && State.PosY < 70000.0);

            if (IsAutopilotEnabled)
            {
                var cmd = Autopilot.Update(State, dt);
                throttleCmd = cmd.Throttle;
                pitchCmdRad = State.Pitch;
                ManualThrottle = (float)cmd.Throttle;
                ManualPitchDegrees = (float)(cmd.PitchAngle * 180.0 / Math.PI);
            }
            else
            {
                throttleCmd = ManualThrottle;
                pitchCmdRad = (ManualPitchDegrees * Math.PI) / 180.0;
                State.Pitch = pitchCmdRad;
                State.FlightPhase = "РУЧНОЕ УПРАВЛЕНИЕ";
            }

            // Динамика сопла TVC (Gimbal)
            float targetGimbal = 0f;
            if (!IsAutopilotEnabled)
            {
                targetGimbal = Mathf.Clamp(90f - ManualPitchDegrees, -(float)RocketParameters.TvcMaxGimbalAngleDeg, (float)RocketParameters.TvcMaxGimbalAngleDeg);
            }
            else
            {
                targetGimbal = Mathf.Clamp((float)(-State.AngularVelPitch * 180.0 / Math.PI * 1.5), -(float)RocketParameters.TvcMaxGimbalAngleDeg, (float)RocketParameters.TvcMaxGimbalAngleDeg);
            }
            State.GimbalAngleDegrees = Mathf.MoveTowards((float)State.GimbalAngleDegrees, targetGimbal, (float)RocketParameters.TvcMaxSlewRateDegPerSec * (float)dt);

            // 2. Численное интегрирование ОДУ
            if (Integrator == IntegratorType.RungeKutta4)
            {
                RK4Integrator.Step(State, dt, throttleCmd, pitchCmdRad, isReentry);
            }
            else
            {
                EulerIntegrator.Step(State, dt, throttleCmd, pitchCmdRad, isReentry);
            }

            // 3. Обработка касания поверхности (Земля y <= 0)
            if (State.PosY <= 0.0)
            {
                State.PosY = 0.0;
                if (!State.HasLiftoff)
                {
                    State.VelY = 0.0;
                    State.VelX = 0.0;
                }
                else
                {
                    EvaluateTouchdown();
                }
            }

            // 4. Проверка разрушения перегрузкой
            if (State.GForce > RocketParameters.MaxStructuralGForce && State.HasLiftoff)
            {
                State.IsCrashed = true;
                State.CrashReason = $"Разрушение от перегрузки: {State.GForce:F1}G > {RocketParameters.MaxStructuralGForce:F1}G";
                _isSimulationActive = false;
            }
        }

        private void SyncTransformWithState()
        {
            // Позиция в пространстве Unity (1 единица = 1 метр)
            transform.position = new Vector3((float)State.PosX, (float)State.PosY, (float)State.PosZ);

            // Ориентация: при Pitch = 90° ракета смотрит носом вверх (0 по оси Z)
            float angleDeg = (float)(State.PitchDegrees - 90.0);
            transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
        }

        private void UpdateVisuals(double throttle, double pitchDeg, bool rcsActive, bool gridFinsActive)
        {
            // 0. Качание сопла маршевого двигателя (TVC Gimbal)
            if (EngineNozzle != null)
            {
                EngineNozzle.localRotation = Quaternion.Euler(180f, 0f, (float)State.GimbalAngleDegrees);
            }

            // 1. Пламя основного двигателя
            if (MainEnginePlume != null)
            {
                if (throttle > 0.0 && State.Fuel > 0.0)
                {
                    if (!MainEnginePlume.isPlaying) MainEnginePlume.Play();
                    var main = MainEnginePlume.main;
                    main.startSize = (float)(1.0 + throttle * 3.5);
                    main.startSpeed = (float)(15.0 + throttle * 35.0);
                }
                else
                {
                    if (MainEnginePlume.isPlaying) MainEnginePlume.Stop();
                }
            }

            // 2. Пар и пыль от выхлопа у земли при посадке / старте (h < 35м)
            if (LandingSteamPlume != null)
            {
                bool nearDeck = (State.PosY < 35.0 && throttle > 0.1);
                if (nearDeck && !LandingSteamPlume.isPlaying) LandingSteamPlume.Play();
                else if (!nearDeck && LandingSteamPlume.isPlaying) LandingSteamPlume.Stop();
            }

            // 3. Плазменный ударный след при спуске в атмосфере (h < 65 км, V > 450 м/с)
            if (ReentryPlasmaGlow != null)
            {
                double speed = Math.Sqrt(State.VelX * State.VelX + State.VelY * State.VelY);
                bool isReentryGlow = (State.VelY < -100.0 && State.PosY < 65000.0 && State.PosY > 15000.0 && speed > 450.0);
                if (isReentryGlow && !ReentryPlasmaGlow.isPlaying) ReentryPlasmaGlow.Play();
                else if (!isReentryGlow && ReentryPlasmaGlow.isPlaying) ReentryPlasmaGlow.Stop();
            }

            // 4. Маневровые сопла RCS
            if (LeftRcsPlume != null)
            {
                if (rcsActive && !LeftRcsPlume.isPlaying) LeftRcsPlume.Play();
                else if (!rcsActive && LeftRcsPlume.isPlaying) LeftRcsPlume.Stop();
            }
            if (RightRcsPlume != null)
            {
                if (rcsActive && !RightRcsPlume.isPlaying) RightRcsPlume.Play();
                else if (!rcsActive && RightRcsPlume.isPlaying) RightRcsPlume.Stop();
            }

            // 5. Решётчатые рули
            if (GridFinsModel != null)
            {
                GridFinsModel.SetActive(gridFinsActive);
            }

            // 6. Звук двигателя
            if (EngineAudio != null)
            {
                if (throttle > 0.0 && State.Fuel > 0.0)
                {
                    if (!EngineAudio.isPlaying) EngineAudio.Play();
                    EngineAudio.volume = (float)(0.3 + throttle * 0.7);
                    EngineAudio.pitch = (float)(0.8 + throttle * 0.4);
                }
                else
                {
                    if (EngineAudio.isPlaying) EngineAudio.Stop();
                }
            }
        }

        private void RecordTrajectoryPoint(Vector3 pt)
        {
            if (TrajectoryLine == null) return;

            if (_trajectoryPoints.Count == 0 || Vector3.Distance(_trajectoryPoints[_trajectoryPoints.Count - 1], pt) > 15f)
            {
                _trajectoryPoints.Add(pt);
                TrajectoryLine.positionCount = _trajectoryPoints.Count;
                TrajectoryLine.SetPosition(_trajectoryPoints.Count - 1, pt);
            }
        }

        private void EvaluateTouchdown()
        {
            _isSimulationActive = false;

            double vy = Math.Abs(State.VelY);
            double vx = Math.Abs(State.VelX);
            double pitchError = Math.Abs(State.PitchDegrees - 90.0);
            double distToBarge = Math.Abs(State.PosX - RocketParameters.PadB_X);

            if (vy <= RocketParameters.TargetTouchdownVy && vx <= 0.8 && pitchError <= 7.0 && distToBarge <= 150.0)
            {
                State.IsLanded = true;
                Debug.Log($"<color=lime>[ПОСАДКА УСПЕШНА] Мягкая посадка на баржу B (Touchdown: PASS)! Vy={vy:F2} м/с, Vx={vx:F2} м/с, Тангаж={pitchError:F1}°, Отклонение={distToBarge:F1} м, Остаток топлива={State.Fuel:F0} кг.</color>");
            }
            else if (vy <= RocketParameters.MaxTouchdownVy && pitchError <= 12.0)
            {
                State.IsLanded = true;
                Debug.LogWarning($"<color=yellow>[ПОСАДКА С ДОПУСКАМИ] Ступень уцелела на барже. Vy={vy:F2} м/с, Vx={vx:F2} м/с, Тангаж={pitchError:F1}°, Отклонение={distToBarge:F1} м, Топливо={State.Fuel:F0} кг.</color>");
            }
            else
            {
                State.IsCrashed = true;
                State.CrashReason = $"Крушение при ударе о палубу: Vy={vy:F2} м/с (лимит {RocketParameters.MaxTouchdownVy:F1} м/с), Vx={vx:F2} м/с, Наклон={pitchError:F1}°.";
                Debug.LogError($"<color=red>[КРУШЕНИЕ] {State.CrashReason}</color>");
            }
        }
    }
}
