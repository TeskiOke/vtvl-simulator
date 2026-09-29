using System;
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
    /// Мост между чистым физико-математическим ядром (ОДУ Коши) и графическим движком Unity.
    /// Выполняет расчёт движения в FixedUpdate без использования PhysX / Rigidbody.
    /// </summary>
    public class RocketPhysicsBridge : MonoBehaviour
    {
        [Header("Настройки численного решателя")]
        [Tooltip("Выбор численного интегратора: RK4 (высокая точность O(dt^4)) или Euler (1-й порядок)")]
        public IntegratorType Integrator = IntegratorType.RungeKutta4;

        [Tooltip("Фиксированный шаг физического времени (с). Рекомендуется 0.01 - 0.02 с.")]
        [Range(0.001f, 0.05f)]
        public float SimulationFixedDt = 0.01f;

        [Tooltip("Множитель ускорения времени (Time Warp)")]
        [Range(1, 20)]
        public int TimeWarp = 1;

        [Header("Режим управления")]
        public bool IsAutopilotEnabled = true;

        [Range(0f, 1f)]
        public float ManualThrottle = 0f;

        [Range(0f, 180f)]
        public float ManualPitchDegrees = 90f;

        [Header("Визуальные компоненты Unity (необязательно)")]
        public ParticleSystem MainEnginePlume;
        public ParticleSystem LeftRcsPlume;
        public ParticleSystem RightRcsPlume;
        public GameObject GridFinsModel;
        public AudioSource EngineAudio;

        // Внутреннее математическое состояние
        public RocketState State { get; private set; }
        public AutopilotGNC Autopilot { get; private set; }

        private bool _isSimulationActive = false;

        private void Awake()
        {
            State = new RocketState();
            Autopilot = new AutopilotGNC();
            Time.fixedDeltaTime = SimulationFixedDt;
        }

        private void Start()
        {
            ResetSimulation();
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

        /// <summary>
        /// Сброс ракеты на стартовый стол A.
        /// </summary>
        public void ResetSimulation()
        {
            _isSimulationActive = false;
            State.Reset();
            SyncTransformWithState();
            UpdateVisuals(0.0, 90.0, false, false);
        }

        private void FixedUpdate()
        {
            if (!_isSimulationActive || State.IsLanded || State.IsCrashed)
            {
                return;
            }

            // Выполнение подшагов Time Warp
            for (int step = 0; step < TimeWarp; step++)
            {
                if (State.IsLanded || State.IsCrashed) break;

                double dt = SimulationFixedDt;

                // 1. Управление (Автопилот или Ручное)
                double throttleCmd;
                double pitchCmdRad;
                bool isReentry = (State.VelY < 0.0 && State.PosY < 70000.0);

                if (IsAutopilotEnabled)
                {
                    var cmd = Autopilot.Update(State, dt);
                    throttleCmd = cmd.Throttle;
                    pitchCmdRad = State.Pitch;
                }
                else
                {
                    throttleCmd = ManualThrottle;
                    pitchCmdRad = (ManualPitchDegrees * Math.PI) / 180.0;
                    State.Pitch = pitchCmdRad;
                    State.FlightPhase = "РУЧНОЕ УПРАВЛЕНИЕ";
                }

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
                        break;
                    }
                }

                // 4. Проверка разрушения перегрузкой
                if (State.GForce > RocketParameters.MaxStructuralGForce && State.HasLiftoff)
                {
                    State.IsCrashed = true;
                    State.CrashReason = $"Разрушение от перегрузки: {State.GForce:F1}G > {RocketParameters.MaxStructuralGForce:F1}G";
                    _isSimulationActive = false;
                    break;
                }
            }

            // 5. Синхронизация визуального представления в Unity
            SyncTransformWithState();
            UpdateVisuals(State.Throttle, State.PitchDegrees, State.IsRcsActive, State.IsGridFinsActive);
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
            // Пламя основного двигателя
            if (MainEnginePlume != null)
            {
                if (throttle > 0.0 && State.Fuel > 0.0)
                {
                    if (!MainEnginePlume.isPlaying) MainEnginePlume.Play();
                    var main = MainEnginePlume.main;
                    main.startSize = (float)(0.8 + throttle * 2.2);
                }
                else
                {
                    if (MainEnginePlume.isPlaying) MainEnginePlume.Stop();
                }
            }

            // Сопла RCS
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

            // Решётчатые рули
            if (GridFinsModel != null)
            {
                GridFinsModel.SetActive(gridFinsActive);
            }

            // Звук двигателя
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

        private void EvaluateTouchdown()
        {
            _isSimulationActive = false;

            double vy = Math.Abs(State.VelY);
            double vx = Math.Abs(State.VelX);
            double pitchError = Math.Abs(State.PitchDegrees - 90.0);
            double distToBarge = Math.Abs(State.PosX - RocketParameters.PadB_X);

            if (vy <= RocketParameters.TargetTouchdownVy && vx <= 0.5 && pitchError <= 5.0 && distToBarge <= 120.0)
            {
                State.IsLanded = true;
                Debug.Log($"[ПОСАДКА УСПЕШНА] Мягкая посадка на баржу B! Vy={vy:F2} м/с, Vx={vx:F2} м/с, Отклонение={distToBarge:F1} м, Остаток топлива={State.Fuel:F0} кг.");
            }
            else if (vy <= RocketParameters.MaxTouchdownVy && pitchError <= 10.0)
            {
                State.IsLanded = true;
                Debug.LogWarning($"[ГРУБАЯ ПОСАДКА] Ступень уцелела, но перегрузила стойки. Vy={vy:F2} м/с.");
            }
            else
            {
                State.IsCrashed = true;
                State.CrashReason = $"Крушение при ударе о поверхность: Vy = {vy:F2} м/с (лимит {RocketParameters.MaxTouchdownVy:F1} м/с).";
                Debug.LogError($"[КРУШЕНИЕ] {State.CrashReason}");
            }
        }
    }
}
