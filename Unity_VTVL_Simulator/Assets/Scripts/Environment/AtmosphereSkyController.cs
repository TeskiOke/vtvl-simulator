using System;
using UnityEngine;

namespace DSTU.VTVL.Visuals
{
    using DSTU.VTVL.UnityAdapters;

    /// <summary>
    /// Динамический контроллер высотной атмосферы, кривизны горизонта Земли и космического пространства.
    /// Плавно переводит освещение, небосвод и фоновые звёзды при наборе высоты от 0 до 250+ км.
    /// </summary>
    public class AtmosphereSkyController : MonoBehaviour
    {
        [Header("Связь с ракетой")]
        public RocketPhysicsBridge PhysicsBridge;

        [Header("Источники света и окружение")]
        public Light SunDirectionalLight;
        public GameObject CurvedEarthGlobe;
        public ParticleSystem StarfieldSystem;

        [Header("Цветовая палитра высоты")]
        public Color GroundSkyColor = new Color(0.48f, 0.68f, 0.94f);      // Уровень моря (0 км)
        public Color StratosphereColor = new Color(0.12f, 0.16f, 0.38f);   // Стратосфера (30 км)
        public Color MesosphereColor = new Color(0.04f, 0.05f, 0.14f);     // Мезосфера (70 км)
        public Color CosmicSpaceColor = new Color(0.01f, 0.01f, 0.02f);    // Космос (>100 км)

        [Header("Параметры Солнца")]
        public float GroundSunIntensity = 1.35f;
        public float SpaceSunIntensity = 1.85f;

        [Header("Параметры фонового тумана (Haze)")]
        public bool EnableAtmosphericHaze = true;
        public float GroundFogDensity = 0.00015f;

        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (RenderSettings.fog)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
            }
        }

        private void Update()
        {
            float altitudeMeters = 0f;
            if (PhysicsBridge != null && PhysicsBridge.State != null)
            {
                altitudeMeters = (float)PhysicsBridge.State.PosY;
            }
            else if (_mainCamera != null)
            {
                altitudeMeters = Mathf.Max(0f, _mainCamera.transform.position.y);
            }

            float altKm = altitudeMeters / 1000f;

            // 1. Вычисляем высотный коэффициент атмосферы t in [0..1]
            // 0 км -> 0, 30 км -> 0.35, 80 км -> 0.85, 120+ км -> 1.0
            float spaceFactor = Mathf.Clamp01(Mathf.InverseLerp(0f, 100f, altKm));

            // 2. Цвет неба и фонового освещения
            Color currentSkyColor;
            if (altKm < 30f)
            {
                float t = Mathf.InverseLerp(0f, 30f, altKm);
                currentSkyColor = Color.Lerp(GroundSkyColor, StratosphereColor, t);
            }
            else if (altKm < 80f)
            {
                float t = Mathf.InverseLerp(30f, 80f, altKm);
                currentSkyColor = Color.Lerp(StratosphereColor, MesosphereColor, t);
            }
            else
            {
                float t = Mathf.InverseLerp(80f, 120f, altKm);
                currentSkyColor = Color.Lerp(MesosphereColor, CosmicSpaceColor, t);
            }

            if (_mainCamera != null)
            {
                _mainCamera.backgroundColor = currentSkyColor;
            }

            // Заполняющий свет (Ambient Light)
            Color ambientColor = Color.Lerp(new Color(0.35f, 0.42f, 0.52f), new Color(0.05f, 0.06f, 0.09f), spaceFactor);
            RenderSettings.ambientLight = ambientColor;

            // 3. Солнце: в космосе нет рассеивания, свет становится ослепительно ярким и резким
            if (SunDirectionalLight != null)
            {
                SunDirectionalLight.intensity = Mathf.Lerp(GroundSunIntensity, SpaceSunIntensity, spaceFactor);
                SunDirectionalLight.color = Color.Lerp(new Color(1f, 0.96f, 0.90f), new Color(1f, 1f, 0.98f), spaceFactor);
            }

            // 4. Плотность дымки атмосферы (Fog)
            if (EnableAtmosphericHaze)
            {
                RenderSettings.fog = (altKm < 60f);
                if (RenderSettings.fog)
                {
                    float fogFactor = Mathf.Clamp01(1f - (altKm / 45f));
                    RenderSettings.fogDensity = GroundFogDensity * fogFactor;
                    RenderSettings.fogColor = currentSkyColor;
                }
            }

            // 5. Звёздное поле: разгорается по мере выхода из плотной атмосферы
            if (StarfieldSystem != null)
            {
                var main = StarfieldSystem.main;
                float starAlpha = Mathf.Clamp01(Mathf.InverseLerp(25f, 80f, altKm));
                main.startColor = new Color(1f, 1f, 1f, starAlpha);

                // Следуем за камерой, чтобы звёзды казались бесконечно далёкими
                if (_mainCamera != null)
                {
                    StarfieldSystem.transform.position = _mainCamera.transform.position;
                }
            }

            // 6. Искривление Земли и атмосферный лимб
            if (CurvedEarthGlobe != null && _mainCamera != null)
            {
                // Земля слегка смещается в зависимости от высоты, усиливая панораму
                float earthYOffset = -6371000f + Mathf.Min(altitudeMeters * 0.15f, 25000f);
                CurvedEarthGlobe.transform.position = new Vector3(_mainCamera.transform.position.x * 0.1f, earthYOffset, _mainCamera.transform.position.z * 0.1f);
            }
        }
    }
}
