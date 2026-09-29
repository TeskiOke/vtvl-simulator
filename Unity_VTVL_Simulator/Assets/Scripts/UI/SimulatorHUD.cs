using System;
using UnityEngine;

namespace DSTU.VTVL.UnityAdapters
{
    using DSTU.VTVL.PhysicsCore;

    /// <summary>
    /// Интерактивный внутриигровой HUD телеметрии для Кейса 8.1.
    /// Поддерживает сворачивание панелей, скрытие по клавише H, отображение ручного управления,
    /// раздельный вывод перегрузки акселерометра (G-force) и кинематического ускорения (|a|),
    /// а также переключатели 3D-траектории и камер.
    /// </summary>
    public class SimulatorHUD : MonoBehaviour
    {
        public RocketPhysicsBridge PhysicsBridge;

        [Header("Настройки отображения HUD")]
        public bool ShowHUD = true;
        public bool FoldTelemetry = false;
        public bool FoldControls = false;
        public bool FoldKeybinds = false;

        private GUIStyle _headerStyle;
        private GUIStyle _telemetryStyle;
        private GUIStyle _phaseStyle;
        private GUIStyle _alertStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _keybindStyle;

        private void Update()
        {
            // Горячая клавиша H для полного скрытия / показа интерфейса
            if (Input.GetKeyDown(KeyCode.H))
            {
                ShowHUD = !ShowHUD;
            }
        }

        private void OnGUI()
        {
            if (!ShowHUD)
            {
                // Маленькая плашка подсказки для возврата HUD
                if (GUI.Button(new Rect(10, 10, 140, 26), "👁 Показать HUD (H)"))
                {
                    ShowHUD = true;
                }
                return;
            }

            if (PhysicsBridge == null)
            {
#if UNITY_2023_1_OR_NEWER
                PhysicsBridge = FindFirstObjectByType<RocketPhysicsBridge>();
#else
                PhysicsBridge = FindObjectOfType<RocketPhysicsBridge>();
#endif
                if (PhysicsBridge == null) return;
            }

            InitStyles();

            if (_boxStyle == null) _boxStyle = GUI.skin.box;
            if (_btnStyle == null) _btnStyle = GUI.skin.button;
            if (_headerStyle == null) _headerStyle = GUI.skin.label;
            if (_telemetryStyle == null) _telemetryStyle = GUI.skin.label;
            if (_phaseStyle == null) _phaseStyle = GUI.skin.label;
            if (_alertStyle == null) _alertStyle = GUI.skin.label;
            if (_keybindStyle == null) _keybindStyle = GUI.skin.label;

            RocketState s = PhysicsBridge.State;
            if (s == null) return;

            // =========================================================
            // 1. ВЕРХНЯЯ ПАНЕЛЬ: ФАЗА ПОЛЁТА И СТАТУС
            // =========================================================
            GUI.Box(new Rect(10, 10, 520, 75), "", _boxStyle);
            
            // Кнопка быстрого скрытия
            if (GUI.Button(new Rect(480, 14, 40, 22), "—", _btnStyle))
            {
                ShowHUD = false;
            }

            GUI.Label(new Rect(20, 14, 450, 28), $"ФАЗА: {s.FlightPhase.ToUpper()}", _phaseStyle);

            string statusSub = s.IsLanded
                ? "🏆 УСПЕШНАЯ ПОСАДКА НА БАРЖУ B! (Touchdown: PASS)"
                : (s.IsCrashed ? $"💥 АВАРИЯ: {s.CrashReason}" : $"Дальность до баржи B: {Math.Abs(RocketParameters.PadB_X - s.PosX) / 1000.0:F2} км");
            GUI.Label(new Rect(20, 46, 480, 25), statusSub, s.IsCrashed ? _alertStyle : _telemetryStyle);

            // =========================================================
            // 2. ЛЕВАЯ ПАНЕЛЬ: ТЕЛЕМЕТРИЯ (СВОРАЧИВАЕМАЯ)
            // =========================================================
            int telemHeight = FoldTelemetry ? 35 : 345;
            GUI.Box(new Rect(10, 95, 360, telemHeight), "", _boxStyle);

            // Заголовок и кнопка свернуть
            GUI.Label(new Rect(20, 100, 280, 22), "📊 ТЕЛЕМЕТРИЯ (КЕЙС 8.1)", _headerStyle);
            if (GUI.Button(new Rect(325, 100, 35, 20), FoldTelemetry ? "▼" : "▲", _btnStyle))
            {
                FoldTelemetry = !FoldTelemetry;
            }

            if (!FoldTelemetry)
            {
                double altitudeKm = s.PosY / 1000.0;
                double speed = Math.Sqrt(s.VelX * s.VelX + s.VelY * s.VelY);
                double fuelPct = (s.Fuel / RocketParameters.PropellantMassInitial) * 100.0;

                int yOffset = 130;
                int step = 22;

                DrawMetric("Высота (Altitude):", $"{s.PosY:F0} м ({altitudeKm:F2} км)", ref yOffset, step);
                DrawMetric("Вертикальная скор. Vy:", $"{s.VelY:F1} м/с", ref yOffset, step);
                DrawMetric("Горизонтальная скор. Vx:", $"{s.VelX:F1} м/с", ref yOffset, step);
                DrawMetric("Полная скорость V:", $"{speed:F1} м/с", ref yOffset, step);
                DrawMetric("Число Маха (Mach):", $"{s.MachNumber:F2} M", ref yOffset, step);
                DrawMetric("Динамич. напор Q:", $"{s.DynamicPressure / 1000.0:F2} кПа", ref yOffset, step);
                DrawMetric("Угол тангажа (Pitch):", $"{(s.Pitch * 180.0 / Math.PI):F1}°", ref yOffset, step);
                DrawMetric("Масса ракеты (Mass):", $"{s.Mass:F0} кг", ref yOffset, step);
                DrawMetric("Остаток топлива:", $"{s.Fuel:F0} кг ({fuelPct:F1}%)", ref yOffset, step);

                // Раздельное отображение перегрузки и ускорения
                string gDesc = s.GForce < 0.05 ? "(0g невесомость)" : (s.GForce > 3.0 ? "🔥 ТОРМОЖЕНИЕ" : "");
                DrawMetric("Перегрузка (G-Force):", $"{s.GForce:F2} g  {gDesc}", ref yOffset, step);
                DrawMetric("Ускорение свободн. |a|:", $"{s.KinematicAccel:F1} м/с²", ref yOffset, step);

                string thrText = s.Throttle > 0 ? $"ТЯГА {(s.Throttle * 100.0):F0}%" : "ВЫКЛЮЧЕН (0%)";
                DrawMetric("Режим маршевой ДУ:", thrText, ref yOffset, step);
            }

            // =========================================================
            // 3. ПРАВАЯ ПАНЕЛЬ: УПРАВЛЕНИЕ И TIMEWARP
            // =========================================================
            int ctrlWidth = 260;
            int ctrlX = Screen.width - ctrlWidth - 10;
            int ctrlHeight = FoldControls ? 35 : 270;
            GUI.Box(new Rect(ctrlX, 10, ctrlWidth, ctrlHeight), "", _boxStyle);

            GUI.Label(new Rect(ctrlX + 10, 15, 190, 22), "⚙️ УПРАВЛЕНИЕ", _headerStyle);
            if (GUI.Button(new Rect(ctrlX + 215, 15, 35, 20), FoldControls ? "▼" : "▲", _btnStyle))
            {
                FoldControls = !FoldControls;
            }

            if (!FoldControls)
            {
                int btnY = 45;
                if (GUI.Button(new Rect(ctrlX + 10, btnY, 240, 28), "▶ СТАРТ / ПАУЗА (SPACE)"))
                {
                    PhysicsBridge.ToggleSimulation();
                }
                btnY += 34;

                if (GUI.Button(new Rect(ctrlX + 10, btnY, 240, 28), "🔄 СБРОС НА СТАРТ A (R)"))
                {
                    PhysicsBridge.ResetSimulation();
                }
                btnY += 34;

                string modeLabel = PhysicsBridge.IsAutopilotEnabled ? "РЕЖИМ: АВТОПИЛОТ GNC" : "РЕЖИМ: РУЧНОЙ (MANUAL)";
                if (GUI.Button(new Rect(ctrlX + 10, btnY, 240, 28), modeLabel))
                {
                    PhysicsBridge.IsAutopilotEnabled = !PhysicsBridge.IsAutopilotEnabled;
                }
                btnY += 34;

                string trajText = PhysicsBridge.ShowTrajectory ? "3D-ТРАЕКТОРИЯ: [ВКЛ] (T)" : "3D-ТРАЕКТОРИЯ: [ВЫКЛ] (T)";
                if (GUI.Button(new Rect(ctrlX + 10, btnY, 240, 28), trajText))
                {
                    PhysicsBridge.ShowTrajectory = !PhysicsBridge.ShowTrajectory;
                    if (PhysicsBridge.TrajectoryLine != null) PhysicsBridge.TrajectoryLine.enabled = PhysicsBridge.ShowTrajectory;
                }
                btnY += 36;

                // Кнопки TimeWarp
                GUI.Label(new Rect(ctrlX + 10, btnY, 240, 20), $"Ускорение времени: x{PhysicsBridge.TimeWarp}", _telemetryStyle);
                btnY += 22;

                int twW = 55;
                if (GUI.Button(new Rect(ctrlX + 10, btnY, twW, 25), "x1")) PhysicsBridge.TimeWarp = 1;
                if (GUI.Button(new Rect(ctrlX + 70, btnY, twW, 25), "x2")) PhysicsBridge.TimeWarp = 2;
                if (GUI.Button(new Rect(ctrlX + 130, btnY, twW, 25), "x5")) PhysicsBridge.TimeWarp = 5;
                if (GUI.Button(new Rect(ctrlX + 190, btnY, twW, 25), "x10")) PhysicsBridge.TimeWarp = 10;
            }

            // =========================================================
            // 4. ПАНЕЛЬ ПОДСКАЗОК ПО КЛАВИШАМ (СВОРАЧИВАЕМАЯ СНИЗУ СПРАВА)
            // =========================================================
            int keyHeight = FoldKeybinds ? 35 : 220;
            int keyY = Screen.height - keyHeight - 10;
            GUI.Box(new Rect(ctrlX, keyY, ctrlWidth, keyHeight), "", _boxStyle);

            GUI.Label(new Rect(ctrlX + 10, keyY + 6, 190, 22), "⌨️ ГОРЯЧИЕ КЛАВИШИ", _headerStyle);
            if (GUI.Button(new Rect(ctrlX + 215, keyY + 6, 35, 20), FoldKeybinds ? "▲" : "▼", _btnStyle))
            {
                FoldKeybinds = !FoldKeybinds;
            }

            if (!FoldKeybinds)
            {
                int ky = keyY + 32;
                int kstep = 18;
                DrawKeyRow("Tab", "Автопилот / Ручной", ref ky, kstep, ctrlX);
                DrawKeyRow("W / S", "Тяга (газ больше / меньше)", ref ky, kstep, ctrlX);
                DrawKeyRow("A / D", "Тангаж (наклон влево / вправо)", ref ky, kstep, ctrlX);
                DrawKeyRow("Space / X", "Полный газ 100% / Отсечка 0%", ref ky, kstep, ctrlX);
                DrawKeyRow("ПКМ + мышь", "Свободный обзор камеры 360°", ref ky, kstep, ctrlX);
                DrawKeyRow("Колёсико", "Приблизить / отдалить зум", ref ky, kstep, ctrlX);
                DrawKeyRow("1 / 2 / 3 / 4", "Камеры (хвост/бок/стол A/баржа)", ref ky, kstep, ctrlX);
                DrawKeyRow("T / H", "Траектория / Скрыть весь HUD", ref ky, kstep, ctrlX);
                DrawKeyRow("R", "Сброс на стартовый стол A", ref ky, kstep, ctrlX);
            }
        }

        private void DrawMetric(string label, string value, ref int y, int step)
        {
            GUI.Label(new Rect(20, y, 175, 20), label, _telemetryStyle);
            GUI.Label(new Rect(195, y, 160, 20), value, _telemetryStyle);
            y += step;
        }

        private void DrawKeyRow(string key, string desc, ref int y, int step, int xBase)
        {
            GUI.Label(new Rect(xBase + 10, y, 90, 18), $"[{key}]", _keybindStyle);
            GUI.Label(new Rect(xBase + 95, y, 155, 18), desc, _telemetryStyle);
            y += step;
        }

        private void InitStyles()
        {
            if (GUI.skin == null) return;
            if (_headerStyle != null && _btnStyle != null && _boxStyle != null && _telemetryStyle != null) return;

            try
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.background = MakeTex(2, 2, new Color(0.04f, 0.07f, 0.12f, 0.88f));

                _headerStyle = new GUIStyle(GUI.skin.label);
                _headerStyle.fontSize = 12;
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.normal.textColor = new Color(0.38f, 0.74f, 1.0f);

                _telemetryStyle = new GUIStyle(GUI.skin.label);
                _telemetryStyle.fontSize = 11;
                _telemetryStyle.normal.textColor = new Color(0.85f, 0.92f, 1.0f);

                _keybindStyle = new GUIStyle(GUI.skin.label);
                _keybindStyle.fontSize = 11;
                _keybindStyle.fontStyle = FontStyle.Bold;
                _keybindStyle.normal.textColor = new Color(0.0f, 0.95f, 0.75f);

                _phaseStyle = new GUIStyle(GUI.skin.label);
                _phaseStyle.fontSize = 14;
                _phaseStyle.fontStyle = FontStyle.Bold;
                _phaseStyle.normal.textColor = new Color(0.0f, 0.95f, 1.0f);

                _alertStyle = new GUIStyle(GUI.skin.label);
                _alertStyle.fontSize = 11;
                _alertStyle.fontStyle = FontStyle.Bold;
                _alertStyle.normal.textColor = new Color(1.0f, 0.35f, 0.35f);

                _btnStyle = new GUIStyle(GUI.skin.button);
                _btnStyle.fontSize = 11;
                _btnStyle.fontStyle = FontStyle.Bold;
            }
            catch
            {
                // Использовать стандартные стили GUI.skin при сбоях
            }
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
