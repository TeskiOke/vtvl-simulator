using System;
using System.Collections.Generic;
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
        public bool ShowFlightReport = false;

        private Vector2 _tableScrollPos = Vector2.zero;
        private static Texture2D _lineTex;

        private GUIStyle _headerStyle;
        private GUIStyle _telemetryStyle;
        private GUIStyle _phaseStyle;
        private GUIStyle _alertStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _keybindStyle;
        private GUIStyle _tableHeaderStyle;
        private GUIStyle _tableRowStyle;

        private void Update()
        {
            // Горячая клавиша H для полного скрытия / показа интерфейса
            if (Input.GetKeyDown(KeyCode.H))
            {
                ShowHUD = !ShowHUD;
            }

            // Горячая клавиша G для показа отчета полёта и графиков
            if (Input.GetKeyDown(KeyCode.G))
            {
                ShowFlightReport = !ShowFlightReport;
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
                PhysicsBridge = FindAnyObjectByType<RocketPhysicsBridge>();
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
            int telemHeight = FoldTelemetry ? 35 : 368;
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
                DrawMetric("Сопло TVC (Gimbal):", $"{s.GimbalAngleDegrees:+0.0;-0.0;0.0}°", ref yOffset, step);
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
            int ctrlHeight = FoldControls ? 35 : 305;
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

                string reportBtnText = ShowFlightReport ? "📈 ЗАКРЫТЬ ОТЧЁТ (G)" : "📈 ОТЧЁТ И ГРАФИКИ (G)";
                if (GUI.Button(new Rect(ctrlX + 10, btnY, 240, 28), reportBtnText))
                {
                    ShowFlightReport = !ShowFlightReport;
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
            int keyHeight = FoldKeybinds ? 35 : 240;
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
                DrawKeyRow("G", "Отчёт полёта и графики", ref ky, kstep, ctrlX);
                DrawKeyRow("Tab", "Автопилот / Ручной", ref ky, kstep, ctrlX);
                DrawKeyRow("W / S", "Тяга (газ больше / меньше)", ref ky, kstep, ctrlX);
                DrawKeyRow("A / D", "Тангаж / Сопло TVC влево-вправо", ref ky, kstep, ctrlX);
                DrawKeyRow("Space / X", "Полный газ 100% / Отсечка 0%", ref ky, kstep, ctrlX);
                DrawKeyRow("ПКМ + мышь", "Свободный обзор камеры 360°", ref ky, kstep, ctrlX);
                DrawKeyRow("Колёсико", "Приблизить / отдалить зум", ref ky, kstep, ctrlX);
                DrawKeyRow("1 / 2 / 3 / 4", "Камеры (хвост/бок/стол A/баржа)", ref ky, kstep, ctrlX);
                DrawKeyRow("T / H", "Траектория / Скрыть весь HUD", ref ky, kstep, ctrlX);
                DrawKeyRow("R", "Сброс на стартовый стол A", ref ky, kstep, ctrlX);
            }

            // =========================================================
            // 5. МОДАЛЬНОЕ ОКНО: ОТЧЁТ ПОЛЁТА И АНАЛИТИКА ГРАФИКОВ (G)
            // =========================================================
            if (ShowFlightReport)
            {
                DrawFlightReportWindow();
            }
        }

        private void DrawFlightReportWindow()
        {
            float winW = Screen.width * 0.90f;
            float winH = Screen.height * 0.88f;
            float winX = (Screen.width - winW) * 0.5f;
            float winY = (Screen.height - winH) * 0.5f;

            GUI.Box(new Rect(winX, winY, winW, winH), "", _boxStyle);

            // Шапка отчёта
            GUI.Label(new Rect(winX + 20, winY + 12, winW - 100, 26), "📊 ОФИЦИАЛЬНЫЙ ОТЧЁТ ПОЛЁТА И КРИВЫЕ ТРАЕКТОРИИ (VTVL-30)", _phaseStyle);
            if (GUI.Button(new Rect(winX + winW - 55, winY + 12, 40, 24), "X", _btnStyle))
            {
                ShowFlightReport = false;
            }

            var logger = TelemetryLogger.Instance;
            if (logger == null)
            {
                GUI.Label(new Rect(winX + 20, winY + 50, 400, 20), "TelemetryLogger не найден на сцене.", _alertStyle);
                return;
            }

            // Статистика миссии
            float barY = winY + 45;
            GUI.Box(new Rect(winX + 15, barY, winW - 30, 65), "", _boxStyle);

            string resColor = logger.MissionResult.Contains("PASS") ? "<color=lime>" : (logger.MissionResult.Contains("КРУШЕНИЕ") ? "<color=red>" : "<color=yellow>");
            GUI.Label(new Rect(winX + 25, barY + 8, winW - 50, 22), $"СТАТУС МИССИИ: {resColor}{logger.MissionResult}</color> | Время полёта: {logger.FlightDuration:F1} с", _headerStyle);

            string summary1 = $"Апогей (макс. высота): {logger.MaxAltitudeKm:F2} км  |  Макс. скорость: {logger.MaxSpeedMs:F0} м/с (M {logger.MaxMach:F2})  |  Макс. перегрузка: {logger.MaxGForce:F2} G";
            string summary2 = $"Касание Vy/Vx: {logger.TouchdownVy:F2} / {logger.TouchdownVx:F2} м/с  |  Отклонение от баржи: {logger.BargeDistanceError:F1} м  |  Сожжено топлива: {logger.FuelBurned:F0} кг";
            GUI.Label(new Rect(winX + 25, barY + 28, winW - 50, 18), summary1, _telemetryStyle);
            GUI.Label(new Rect(winX + 25, barY + 46, winW - 50, 18), summary2, _telemetryStyle);

            // Основная часть: Графики слева (55% ширины), Посекундная таблица справа (42% ширины)
            float contentY = barY + 75;
            float leftW = (winW - 45) * 0.52f;
            float rightW = (winW - 45) * 0.48f;
            float contentH = winH - 170;

            // --- ЛЕВАЯ КОЛОНКА: ГРАФИКИ ---
            float graphH = (contentH - 15) * 0.5f;

            // График 1: Траектория полёта Y(X)
            Rect g1Rect = new Rect(winX + 15, contentY, leftW, graphH);
            DrawGraph(g1Rect, logger.TrajectoryCurvePoints, "1. Траектория полёта: Высота Y от Дальности X", "км", "км", new Color(0.0f, 0.95f, 1.0f));

            // График 2: Высота H(t)
            Rect g2Rect = new Rect(winX + 15, contentY + graphH + 10, leftW, graphH);
            DrawGraph(g2Rect, logger.AltitudeTimePoints, "2. Профиль высоты H(t) во времени", "с", "км", new Color(0.25f, 0.95f, 0.45f));

            // --- ПРАВАЯ КОЛОНКА: ПОСЕКУНДНЫЙ ЖУРНАЛ ТЕЛЕМЕТРИИ ---
            float tableX = winX + 15 + leftW + 15;
            Rect tableBox = new Rect(tableX, contentY, rightW, contentH);
            GUI.Box(tableBox, "", _boxStyle);

            GUI.Label(new Rect(tableX + 10, contentY + 6, rightW - 20, 20), $"📋 ПОСЕКУНДНЫЙ ЖУРНАЛ ({logger.SecondBySecondLog.Count} СЕК)", _headerStyle);

            // Заголовки колонок
            float thY = contentY + 28;
            GUI.Label(new Rect(tableX + 10, thY, 35, 18), "t,с", _tableHeaderStyle);
            GUI.Label(new Rect(tableX + 45, thY, 55, 18), "Выс,м", _tableHeaderStyle);
            GUI.Label(new Rect(tableX + 100, thY, 50, 18), "Vy,м/с", _tableHeaderStyle);
            GUI.Label(new Rect(tableX + 150, thY, 50, 18), "Vx,м/с", _tableHeaderStyle);
            GUI.Label(new Rect(tableX + 200, thY, 50, 18), "Танг°", _tableHeaderStyle);
            GUI.Label(new Rect(tableX + 250, thY, 50, 18), "TVC°", _tableHeaderStyle);
            GUI.Label(new Rect(tableX + 300, thY, 45, 18), "G", _tableHeaderStyle);
            GUI.Label(new Rect(tableX + 345, thY, 80, 18), "Фаза", _tableHeaderStyle);

            // Скроллируемая область строк
            Rect scrollViewRect = new Rect(tableX + 5, thY + 22, rightW - 10, contentH - 55);
            Rect scrollContentRect = new Rect(0, 0, rightW - 30, Mathf.Max(scrollViewRect.height, logger.SecondBySecondLog.Count * 20));

            _tableScrollPos = GUI.BeginScrollView(scrollViewRect, _tableScrollPos, scrollContentRect);
            for (int i = 0; i < logger.SecondBySecondLog.Count; i++)
            {
                var r = logger.SecondBySecondLog[i];
                float ry = i * 20;

                GUI.Label(new Rect(5, ry, 35, 18), $"{r.Time:F0}", _tableRowStyle);
                GUI.Label(new Rect(40, ry, 55, 18), $"{r.PosY:F0}", _tableRowStyle);
                GUI.Label(new Rect(95, ry, 50, 18), $"{r.VelY:F1}", _tableRowStyle);
                GUI.Label(new Rect(145, ry, 50, 18), $"{r.VelX:F1}", _tableRowStyle);
                GUI.Label(new Rect(195, ry, 50, 18), $"{r.PitchDeg:F0}°", _tableRowStyle);
                GUI.Label(new Rect(245, ry, 50, 18), $"{r.GimbalDeg:+0.0;-0.0;0.0}°", _tableRowStyle);
                GUI.Label(new Rect(295, ry, 45, 18), $"{r.GForce:F2}", _tableRowStyle);
                GUI.Label(new Rect(340, ry, 120, 18), r.Phase, _tableRowStyle);
            }
            GUI.EndScrollView();

            // Нижняя панель действий (Экспорт)
            float botY = winY + winH - 40;
            if (GUI.Button(new Rect(winX + 20, botY, 220, 28), "💾 Экспорт лога CSV (Excel)"))
            {
                logger.ExportCsvReport();
            }
            if (GUI.Button(new Rect(winX + 250, botY, 250, 28), "🌐 Открыть интерактивный HTML"))
            {
                logger.ExportHtmlReport();
            }
            if (GUI.Button(new Rect(winX + winW - 140, botY, 120, 28), "Закрыть (G)"))
            {
                ShowFlightReport = false;
            }
        }

        private void DrawGraph(Rect rect, List<Vector2> points, string title, string xUnit, string yUnit, Color lineColor)
        {
            GUI.Box(rect, "", _boxStyle);
            GUI.Label(new Rect(rect.x + 10, rect.y + 6, rect.width - 20, 20), title, _headerStyle);

            if (points == null || points.Count < 2)
            {
                GUI.Label(new Rect(rect.x + 25, rect.y + rect.height * 0.45f, rect.width - 50, 20), "Ожидание данных телеметрии (запустите полёт)...", _telemetryStyle);
                return;
            }

            float minX = 0f, maxX = 0.001f;
            float minY = 0f, maxY = 0.001f;

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].x > maxX) maxX = points[i].x;
                if (points[i].y > maxY) maxY = points[i].y;
            }

            Rect plotArea = new Rect(rect.x + 40, rect.y + 28, rect.width - 55, rect.height - 52);

            // Оси и подписи
            GUI.Label(new Rect(rect.x + 2, plotArea.y, 36, 16), $"{maxY:F0}", _telemetryStyle);
            GUI.Label(new Rect(rect.x + 2, plotArea.y + plotArea.height - 14, 36, 16), $"{minY:F0}", _telemetryStyle);
            GUI.Label(new Rect(plotArea.x, plotArea.y + plotArea.height + 2, 50, 16), $"{minX:F0}", _telemetryStyle);
            GUI.Label(new Rect(plotArea.x + plotArea.width - 70, plotArea.y + plotArea.height + 2, 70, 16), $"{maxX:F0} {xUnit}", _telemetryStyle);

            // Отрисовка ломаной кривой
            for (int i = 1; i < points.Count; i++)
            {
                float x1 = plotArea.x + ((points[i - 1].x - minX) / (maxX - minX)) * plotArea.width;
                float y1 = plotArea.y + plotArea.height - ((points[i - 1].y - minY) / (maxY - minY)) * plotArea.height;
                float x2 = plotArea.x + ((points[i].x - minX) / (maxX - minX)) * plotArea.width;
                float y2 = plotArea.y + plotArea.height - ((points[i].y - minY) / (maxY - minY)) * plotArea.height;

                DrawLine(new Vector2(x1, y1), new Vector2(x2, y2), lineColor, 2.0f);
            }
        }

        private static void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
        {
            if (_lineTex == null)
            {
                _lineTex = new Texture2D(1, 1);
                _lineTex.SetPixel(0, 0, Color.white);
                _lineTex.Apply();
            }

            Color savedColor = GUI.color;
            GUI.color = color;
            Matrix4x4 matrix = GUI.matrix;

            Vector2 d = pointB - pointA;
            float angle = Mathf.Rad2Deg * Mathf.Atan2(d.y, d.x);
            GUIUtility.RotateAroundPivot(angle, pointA);
            GUI.DrawTexture(new Rect(pointA.x, pointA.y - width * 0.5f, d.magnitude, width), _lineTex);

            GUI.matrix = matrix;
            GUI.color = savedColor;
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

                _tableHeaderStyle = new GUIStyle(GUI.skin.label);
                _tableHeaderStyle.fontSize = 10;
                _tableHeaderStyle.fontStyle = FontStyle.Bold;
                _tableHeaderStyle.normal.textColor = new Color(0.55f, 0.75f, 0.95f);

                _tableRowStyle = new GUIStyle(GUI.skin.label);
                _tableRowStyle.fontSize = 10;
                _tableRowStyle.normal.textColor = new Color(0.85f, 0.92f, 0.98f);
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
