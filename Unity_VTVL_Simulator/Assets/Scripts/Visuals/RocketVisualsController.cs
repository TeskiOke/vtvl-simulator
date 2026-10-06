using System;
using UnityEngine;

namespace DSTU.VTVL.Visuals
{
    using DSTU.VTVL.UnityAdapters;
    using DSTU.VTVL.PhysicsCore;

    /// <summary>
    /// Контроллер кинематографических визуальных эффектов корабля VTVL-Orbital:
    /// - Динамическое отклонение 4 аэродинамических рулей-флэпов (Belly-Flop);
    /// - Вакуумное расширение факела двигателя и диски Маха;
    /// - Огненная плазма гиперзвукового входа;
    /// - Мерцающий динамический свет пламени на корпусе и стартовом столе;
    /// - Газовые сопла ориентации RCS.
    /// </summary>
    public class RocketVisualsController : MonoBehaviour
    {
        [Header("Связь с мостом физики")]
        public RocketPhysicsBridge PhysicsBridge;

        [Header("Аэродинамические закрылки (Body Flaps)")]
        public Transform FlapForwardLeft;
        public Transform FlapForwardRight;
        public Transform FlapAftLeft;
        public Transform FlapAftRight;

        [Header("Системы частиц и пламя")]
        public ParticleSystem MainEnginePlume;
        public ParticleSystem MachDiamondPlume;
        public ParticleSystem ReentryPlasma;
        public ParticleSystem LandingSteam;
        public ParticleSystem RcsThrusterLeft;
        public ParticleSystem RcsThrusterRight;

        [Header("Динамический свет двигателя")]
        public Light EnginePointLight;
        public float MaxLightIntensity = 8.5f;

        [Header("Сопло и подвес кардана TVC")]
        public Transform EngineGimbalCluster;

        private float _currentFlapAngleForward = 0f;
        private float _currentFlapAngleAft = 0f;

        private void Update()
        {
            if (PhysicsBridge == null)
            {
                PhysicsBridge = GetComponent<RocketPhysicsBridge>();
                if (PhysicsBridge == null) return;
            }

            RocketState state = PhysicsBridge.State;
            if (state == null) return;

            UpdateFlapsArticulation(state);
            UpdateEnginePlumeAndLight(state);
            UpdateReentryPlasma(state);
            UpdateRcsThrusters(state);
        }

        /// <summary>
        /// Управление углами отклонения 4 аэродинамических закрылков-флэпов.
        /// </summary>
        private void UpdateFlapsArticulation(RocketState state)
        {
            float targetAngleForward = 0f;
            float targetAngleAft = 0f;

            string phase = state.FlightPhase ?? "";

            if (phase.Contains("АТМОСФЕРНОЕ ТОРМОЖЕНИЕ") || phase.Contains("BELLY") || phase.Contains("ПЛАНИРОВАНИЕ"))
            {
                // Режим Belly-Flop: закрылки раскрыты на ~65° для балансировки лобового сопротивления
                targetAngleForward = 65f;
                targetAngleAft = 55f;

                // Небольшой тримминг по углу тангажа
                float pitchError = (float)(state.PitchDegrees - 60.0);
                targetAngleForward += pitchError * 0.5f;
                targetAngleAft -= pitchError * 0.5f;
            }
            else if (phase.Contains("ПЕРЕВОРОТ") || phase.Contains("FLIP"))
            {
                // Динамический переворот перед посадкой
                targetAngleForward = 85f;
                targetAngleAft = 20f;
            }
            else if (phase.Contains("ПОСАДКА") || phase.Contains("HOVERSLAM"))
            {
                // Заход на посадку
                targetAngleForward = 30f;
                targetAngleAft = 30f;
            }
            else
            {
                // На активном участке выведения закрылки прижаты к корпусу для минимизации Cx
                targetAngleForward = 5f;
                targetAngleAft = 5f;
            }

            _currentFlapAngleForward = Mathf.Lerp(_currentFlapAngleForward, targetAngleForward, Time.deltaTime * 6f);
            _currentFlapAngleAft = Mathf.Lerp(_currentFlapAngleAft, targetAngleAft, Time.deltaTime * 6f);

            // Применяем вращение к трансфоррмам закрылков
            if (FlapForwardLeft != null) FlapForwardLeft.localRotation = Quaternion.Euler(0f, 0f, _currentFlapAngleForward);
            if (FlapForwardRight != null) FlapForwardRight.localRotation = Quaternion.Euler(0f, 0f, -_currentFlapAngleForward);
            if (FlapAftLeft != null) FlapAftLeft.localRotation = Quaternion.Euler(0f, 0f, _currentFlapAngleAft);
            if (FlapAftRight != null) FlapAftRight.localRotation = Quaternion.Euler(0f, 0f, -_currentFlapAngleAft);
        }

        /// <summary>
        /// Динамика факела: вакуумное расширение струи, диски Маха и пульсирующий свет.
        /// </summary>
        private void UpdateEnginePlumeAndLight(RocketState state)
        {
            float throttle = (float)state.Throttle;
            bool engineFiring = throttle > 0.01f && state.Fuel > 0.1f && !state.IsLanded && !state.IsCrashed;

            float altKm = (float)(state.PosY / 1000.0);

            // 1. Вакуумное расширение струи (при падении атмосферного давления струя расширяется)
            // У земли радиус струи ~1.2м, на высоте 60+ км расширяется до ~4.5м
            float vacuumExpansion = Mathf.Lerp(1.0f, 3.8f, Mathf.Clamp01(altKm / 55f));

            if (MainEnginePlume != null)
            {
                var main = MainEnginePlume.main;
                var emission = MainEnginePlume.emission;
                var shape = MainEnginePlume.shape;

                if (engineFiring)
                {
                    if (!MainEnginePlume.isPlaying) MainEnginePlume.Play();
                    emission.rateOverTime = Mathf.Lerp(80f, 300f, throttle);
                    main.startSize = Mathf.Lerp(2.0f, 4.2f, throttle) * vacuumExpansion;
                    main.startSpeed = Mathf.Lerp(30f, 65f, throttle);
                    shape.angle = Mathf.Lerp(5f, 24f, Mathf.Clamp01(altKm / 55f)); // В вакууме струя раскрывается колоколом
                }
                else
                {
                    if (MainEnginePlume.isPlaying) MainEnginePlume.Stop();
                }
            }

            // Диски Маха (Mach Diamonds) видны преимущественно в плотной атмосфере
            if (MachDiamondPlume != null)
            {
                if (engineFiring && altKm < 35f && throttle > 0.6f)
                {
                    if (!MachDiamondPlume.isPlaying) MachDiamondPlume.Play();
                }
                else
                {
                    if (MachDiamondPlume.isPlaying) MachDiamondPlume.Stop();
                }
            }

            // 2. Динамический источник света пламени
            if (EnginePointLight != null)
            {
                if (engineFiring)
                {
                    EnginePointLight.enabled = true;
                    // Реалистичное мерцание горения метана (15–20 Гц)
                    float flicker = Mathf.PerlinNoise(Time.time * 22f, 0f) * 0.35f + 0.85f;
                    EnginePointLight.intensity = MaxLightIntensity * throttle * flicker;
                    EnginePointLight.color = Color.Lerp(new Color(1f, 0.55f, 0.15f), new Color(0.4f, 0.75f, 1.0f), Mathf.Clamp01(altKm / 70f));
                }
                else
                {
                    EnginePointLight.enabled = false;
                }
            }

            // 3. Качание сопел (TVC Gimbal)
            if (EngineGimbalCluster != null)
            {
                float gimbalDeg = (float)state.GimbalAngleDegrees;
                EngineGimbalCluster.localRotation = Quaternion.Euler(0f, 0f, gimbalDeg);
            }
        }

        /// <summary>
        /// Плазменный кокон при гиперзвуковом входе в атмосферу.
        /// </summary>
        private void UpdateReentryPlasma(RocketState state)
        {
            if (ReentryPlasma == null) return;

            float altKm = (float)(state.PosY / 1000.0);
            float totalSpeed = (float)state.TotalSpeed;

            // Плазма зажигается при скорости > 1200 м/с на высотах 20-80 км
            bool isHypersonicReentry = (altKm > 15f && altKm < 85f && totalSpeed > 1100f);

            if (isHypersonicReentry)
            {
                if (!ReentryPlasma.isPlaying) ReentryPlasma.Play();

                var main = ReentryPlasma.main;
                var emission = ReentryPlasma.emission;

                float intensity = Mathf.Clamp01((totalSpeed - 1100f) / 3500f);
                emission.rateOverTime = Mathf.Lerp(50f, 250f, intensity);
                main.startSize = Mathf.Lerp(3f, 8f, intensity);

                // От огненно-оранжевого к фиолетово-белому свечению
                main.startColor = Color.Lerp(new Color(1f, 0.25f, 0.05f, 0.8f), new Color(0.85f, 0.5f, 1f, 0.95f), intensity);
            }
            else
            {
                if (ReentryPlasma.isPlaying) ReentryPlasma.Stop();
            }
        }

        /// <summary>
        /// Эффект струй газодинамической ориентации (RCS) при маневрах в вакууме.
        /// </summary>
        private void UpdateRcsThrusters(RocketState state)
        {
            float altKm = (float)(state.PosY / 1000.0);
            float angularVel = (float)state.AngularVelocity;

            // RCS работают преимущественно на высоте > 40 км при угловых ускорениях
            bool isRcsActive = altKm > 35f && Mathf.Abs(angularVel) > 0.02f;

            if (RcsThrusterLeft != null)
            {
                if (isRcsActive && angularVel > 0.02f)
                {
                    if (!RcsThrusterLeft.isPlaying) RcsThrusterLeft.Play();
                }
                else
                {
                    if (RcsThrusterLeft.isPlaying) RcsThrusterLeft.Stop();
                }
            }

            if (RcsThrusterRight != null)
            {
                if (isRcsActive && angularVel < -0.02f)
                {
                    if (!RcsThrusterRight.isPlaying) RcsThrusterRight.Play();
                }
                else
                {
                    if (RcsThrusterRight.isPlaying) RcsThrusterRight.Stop();
                }
            }
        }
    }
}
