using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace DSTU.VTVL.UnityAdapters
{
    using DSTU.VTVL.PhysicsCore;

    /// <summary>
    /// Запись параметров телеметрии за одну секунду полета.
    /// </summary>
    [Serializable]
    public struct TelemetryRecord
    {
        public float Time;
        public float PosX;
        public float PosY;
        public float VelX;
        public float VelY;
        public float TotalSpeed;
        public float PitchDeg;
        public float GimbalDeg;
        public float Mass;
        public float Fuel;
        public float ThrottlePct;
        public float GForce;
        public float Mach;
        public float DynamicPressureKPa;
        public string Phase;
    }

    /// <summary>
    /// Модуль посекундной телеметрии, построения графиков и генерации итогового отчета полёта (CSV + HTML).
    /// </summary>
    public class TelemetryLogger : MonoBehaviour
    {
        public static TelemetryLogger Instance { get; private set; }

        public RocketPhysicsBridge PhysicsBridge;

        [Header("Хранилище телеметрии")]
        public List<TelemetryRecord> SecondBySecondLog = new List<TelemetryRecord>();
        public List<Vector2> TrajectoryCurvePoints = new List<Vector2>(); // X (км) -> Y (км)
        public List<Vector2> AltitudeTimePoints = new List<Vector2>();    // t (с) -> Y (км)
        public List<Vector2> SpeedTimePoints = new List<Vector2>();       // t (с) -> V (м/с)
        public List<Vector2> GForceTimePoints = new List<Vector2>();      // t (с) -> G

        [Header("Статистика миссии")]
        public float MaxAltitudeKm = 0f;
        public float MaxSpeedMs = 0f;
        public float MaxGForce = 0f;
        public float MaxMach = 0f;
        public float TouchdownVy = 0f;
        public float TouchdownVx = 0f;
        public float BargeDistanceError = 0f;
        public float FlightDuration = 0f;
        public float FuelBurned = 0f;
        public string MissionResult = "В ПОЛЁТЕ";
        public bool IsFlightCompleted = false;

        private float _lastSampleSecond = -1f;
        private string _csvPath;
        private string _htmlPath;

        public string GetCsvPath()
        {
            if (string.IsNullOrEmpty(_csvPath))
            {
                _csvPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../FlightReport_VTVL30.csv"));
            }
            return _csvPath;
        }

        public string GetHtmlPath()
        {
            if (string.IsNullOrEmpty(_htmlPath))
            {
                _htmlPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../../FlightReport_VTVL30.html"));
            }
            return _htmlPath;
        }

        private void Awake()
        {
            Instance = this;
            if (PhysicsBridge == null)
            {
                PhysicsBridge = GetComponent<RocketPhysicsBridge>();
                if (PhysicsBridge == null)
                    PhysicsBridge = FindAnyObjectByType<RocketPhysicsBridge>();
            }
            GetCsvPath();
            GetHtmlPath();
        }

        private void Start()
        {
            ResetLogger();
        }

        public void ResetLogger()
        {
            SecondBySecondLog.Clear();
            TrajectoryCurvePoints.Clear();
            AltitudeTimePoints.Clear();
            SpeedTimePoints.Clear();
            GForceTimePoints.Clear();

            MaxAltitudeKm = 0f;
            MaxSpeedMs = 0f;
            MaxGForce = 0f;
            MaxMach = 0f;
            TouchdownVy = 0f;
            TouchdownVx = 0f;
            BargeDistanceError = 0f;
            FlightDuration = 0f;
            FuelBurned = 0f;
            MissionResult = "ОЖИДАНИЕ СТАРТА";
            IsFlightCompleted = false;
            _lastSampleSecond = -1f;
        }

        private void LateUpdate()
        {
            if (PhysicsBridge == null || PhysicsBridge.State == null) return;

            var s = PhysicsBridge.State;

            // Обновление пиковых метрик
            float curAltKm = (float)(s.PosY / 1000.0);
            float curSpeed = (float)s.TotalVelocity;
            float curG = (float)s.GForce;
            float curMach = (float)s.MachNumber;

            if (curAltKm > MaxAltitudeKm) MaxAltitudeKm = curAltKm;
            if (curSpeed > MaxSpeedMs) MaxSpeedMs = curSpeed;
            if (curG > MaxGForce) MaxGForce = curG;
            if (curMach > MaxMach) MaxMach = curMach;

            // Посекундная фиксация (каждую целую секунду)
            float t = (float)s.Time;
            if (t >= _lastSampleSecond + 1.0f || (s.HasLiftoff && _lastSampleSecond < 0f))
            {
                _lastSampleSecond = Mathf.Floor(t);
                RecordSample(s);
            }

            // Обработка посадки или аварии
            if ((s.IsLanded || s.IsCrashed) && !IsFlightCompleted && s.HasLiftoff)
            {
                ProcessFlightCompletion(s);
            }
        }

        public void ProcessFlightCompletion(RocketState s)
        {
            IsFlightCompleted = true;
            FlightDuration = (float)s.Time;
            TouchdownVy = (float)s.VelY;
            TouchdownVx = (float)s.VelX;
            BargeDistanceError = (float)Math.Abs(s.PosX - RocketParameters.PadB_X);
            FuelBurned = (float)(RocketParameters.PropellantMassInitial - s.Fuel);

            if (s.IsLanded)
            {
                MissionResult = Math.Abs(TouchdownVy) <= 2.0f ? "🏆 МЯГКАЯ ПОСАДКА (PASS)" : "⚠️ ГРУБАЯ ПОСАДКА";
            }
            else
            {
                MissionResult = $"💥 КРУШЕНИЕ: {s.CrashReason}";
            }

            // Записываем финальную точку
            RecordSample(s);

            // Автоматический экспорт CSV и красивого HTML отчёта
            ExportCsvReport();
            ExportHtmlReport();
        }

        public void RecordSample(RocketState s)
        {
            float curAltKm = (float)(s.PosY / 1000.0);
            float curSpeed = (float)s.TotalVelocity;
            float curG = (float)s.GForce;
            float curMach = (float)s.MachNumber;

            if (curAltKm > MaxAltitudeKm) MaxAltitudeKm = curAltKm;
            if (curSpeed > MaxSpeedMs) MaxSpeedMs = curSpeed;
            if (curG > MaxGForce) MaxGForce = curG;
            if (curMach > MaxMach) MaxMach = curMach;

            TelemetryRecord rec = new TelemetryRecord
            {
                Time = (float)s.Time,
                PosX = (float)s.PosX,
                PosY = (float)s.PosY,
                VelX = (float)s.VelX,
                VelY = (float)s.VelY,
                TotalSpeed = (float)s.TotalVelocity,
                PitchDeg = (float)s.PitchDegrees,
                GimbalDeg = (float)s.GimbalAngleDegrees,
                Mass = (float)s.Mass,
                Fuel = (float)s.Fuel,
                ThrottlePct = (float)(s.Throttle * 100.0),
                GForce = (float)s.GForce,
                Mach = (float)s.MachNumber,
                DynamicPressureKPa = (float)(s.DynamicPressure / 1000.0),
                Phase = s.FlightPhase
            };

            SecondBySecondLog.Add(rec);

            // Точки для графиков
            TrajectoryCurvePoints.Add(new Vector2((float)(s.PosX / 1000.0), (float)(s.PosY / 1000.0)));
            AltitudeTimePoints.Add(new Vector2((float)s.Time, (float)(s.PosY / 1000.0)));
            SpeedTimePoints.Add(new Vector2((float)s.Time, (float)s.TotalVelocity));
            GForceTimePoints.Add(new Vector2((float)s.Time, (float)s.GForce));
        }

        /// <summary>
        /// Экспорт посекундной телеметрии в стандартный CSV файл в корне проекта.
        /// </summary>
        public void ExportCsvReport()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Time_s,Altitude_m,Downrange_m,VelY_ms,VelX_ms,TotalSpeed_ms,Pitch_deg,TVC_Gimbal_deg,Mass_kg,Fuel_kg,Throttle_pct,GForce_g,Mach,DynamicPressure_kPa,Phase");

                foreach (var r in SecondBySecondLog)
                {
                    sb.AppendLine(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0:F1},{1:F1},{2:F1},{3:F2},{4:F2},{5:F2},{6:F1},{7:F1},{8:F0},{9:F0},{10:F1},{11:F2},{12:F2},{13:F2},\"{14}\"",
                        r.Time, r.PosY, r.PosX, r.VelY, r.VelX, r.TotalSpeed, r.PitchDeg, r.GimbalDeg, r.Mass, r.Fuel, r.ThrottlePct, r.GForce, r.Mach, r.DynamicPressureKPa, r.Phase
                    ));
                }

                string csvFile = GetCsvPath();
                File.WriteAllText(csvFile, sb.ToString(), Encoding.UTF8);
                Debug.Log($"<color=cyan>[ОТЧЁТ CSV] Посекундный лог ({SecondBySecondLog.Count} сек) сохранён: {csvFile}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ОТЧЁТ CSV] Ошибка сохранения: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт интерактивного HTML отчёта с графиками Chart.js для жюри и комиссии ДГТУ.
        /// </summary>
        public void ExportHtmlReport()
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html lang='ru'>");
                sb.AppendLine("<head>");
                sb.AppendLine("<meta charset='UTF-8'>");
                sb.AppendLine("<title>Полный отчёт полёта VTVL-30 | Кейс 8.1 ДГТУ</title>");
                sb.AppendLine("<script src='https://cdn.jsdelivr.net/npm/chart.js'></script>");
                sb.AppendLine("<style>");
                sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; background: #0c1017; color: #e6edf3; margin: 0; padding: 25px; }");
                sb.AppendLine(".card { background: #161b22; border: 1px solid #30363d; border-radius: 8px; padding: 20px; margin-bottom: 25px; }");
                sb.AppendLine(".title { font-size: 24px; font-weight: bold; color: #58a6ff; margin-bottom: 10px; }");
                sb.AppendLine(".grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin-top: 15px; }");
                sb.AppendLine(".metric { background: #21262d; padding: 15px; border-radius: 6px; border-left: 4px solid #58a6ff; }");
                sb.AppendLine(".metric-val { font-size: 20px; font-weight: bold; color: #3fb950; margin-top: 5px; }");
                sb.AppendLine(".charts-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-top: 20px; }");
                sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 13px; }");
                sb.AppendLine("th, td { padding: 8px 12px; text-align: right; border-bottom: 1px solid #30363d; }");
                sb.AppendLine("th { background: #21262d; color: #8b949e; }");
                sb.AppendLine("tr:hover { background: #1f242c; }");
                sb.AppendLine(".badge { padding: 4px 8px; border-radius: 4px; font-weight: bold; }");
                sb.AppendLine(".badge-pass { background: #238636; color: #fff; }");
                sb.AppendLine(".badge-fail { background: #da3633; color: #fff; }");
                sb.AppendLine("</style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body>");

                // Шапка
                sb.AppendLine("<div class='card'>");
                sb.AppendLine("<div class='title'>🚀 ОФИЦИАЛЬНЫЙ ТЕЛЕМЕТРИЧЕСКИЙ ОТЧЁТ ПОЛЁТА VTVL-30</div>");
                sb.AppendLine("<div>Кейс 8.1 «Симулятор вертикального взлёта и посадки» | ДГТУ Группа 03 | Численное ядро: RK4 / Эйлер</div>");

                string badgeClass = MissionResult.Contains("PASS") ? "badge badge-pass" : "badge badge-fail";
                sb.AppendLine($"<div style='margin-top:15px;'><span class='{badgeClass}'>{MissionResult}</span> &nbsp; Время полёта: <b>{FlightDuration:F1} с</b> | Дистанция до баржи: <b>{BargeDistanceError:F1} м</b></div>");

                // Сетка метрик
                sb.AppendLine("<div class='grid'>");
                sb.AppendLine($"<div class='metric'><div>Апогей (макс. высота)</div><div class='metric-val'>{MaxAltitudeKm:F2} км</div></div>");
                sb.AppendLine($"<div class='metric'><div>Макс. скорость</div><div class='metric-val'>{MaxSpeedMs:F1} м/с (M {MaxMach:F2})</div></div>");
                sb.AppendLine($"<div class='metric'><div>Макс. перегрузка</div><div class='metric-val'>{MaxGForce:F2} G</div></div>");
                sb.AppendLine($"<div class='metric'><div>Касание Vy / Vx</div><div class='metric-val'>{TouchdownVy:F2} / {TouchdownVx:F2} м/с</div></div>");
                sb.AppendLine($"<div class='metric'><div>Сожжено топлива</div><div class='metric-val'>{FuelBurned:F0} кг</div></div>");
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");

                // Графики
                sb.AppendLine("<div class='card'>");
                sb.AppendLine("<div class='title'>📈 ГРАФИКИ ПОЛЁТА</div>");
                sb.AppendLine("<div class='charts-grid'>");
                sb.AppendLine("<div><canvas id='chartTrajectory'></canvas></div>");
                sb.AppendLine("<div><canvas id='chartAltitude'></canvas></div>");
                sb.AppendLine("<div><canvas id='chartSpeed'></canvas></div>");
                sb.AppendLine("<div><canvas id='chartGForce'></canvas></div>");
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");

                // Посекундная таблица
                sb.AppendLine("<div class='card'>");
                sb.AppendLine($"<div class='title'>📋 ПОСЕКУНДНЫЙ ЖУРНАЛ ТЕЛЕМЕТРИИ ({SecondBySecondLog.Count} СЕК)</div>");
                sb.AppendLine("<div style='overflow-x:auto; max-height:450px;'>");
                sb.AppendLine("<table>");
                sb.AppendLine("<thead><tr><th>t, с</th><th>Высота, м</th><th>Дальность, м</th><th>Vy, м/с</th><th>Vx, м/с</th><th>V, м/с</th><th>Тангаж, °</th><th>Сопло TVC, °</th><th>Тяга, %</th><th>G-Force</th><th>Масса, кг</th><th>Топливо, кг</th><th>Фаза</th></tr></thead>");
                sb.AppendLine("<tbody>");

                foreach (var r in SecondBySecondLog)
                {
                    sb.AppendLine(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "<tr><td>{0:F0}</td><td>{1:F0}</td><td>{2:F0}</td><td>{3:F1}</td><td>{4:F1}</td><td>{5:F1}</td><td>{6:F1}°</td><td>{7:F1}°</td><td>{8:F0}%</td><td>{9:F2}g</td><td>{10:F0}</td><td>{11:F0}</td><td style='text-align:left;'>{12}</td></tr>",
                        r.Time, r.PosY, r.PosX, r.VelY, r.VelX, r.TotalSpeed, r.PitchDeg, r.GimbalDeg, r.ThrottlePct, r.GForce, r.Mass, r.Fuel, r.Phase
                    ));
                }

                sb.AppendLine("</tbody></table></div></div>");

                // Скрипт графиков
                sb.AppendLine("<script>");
                // 1. Траектория Y(X)
                sb.AppendLine("new Chart(document.getElementById('chartTrajectory'), { type: 'line', data: { datasets: [{ label: 'Траектория Y(X) [км]', data: [");
                foreach (var p in TrajectoryCurvePoints) sb.Append($"{{x:{p.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},y:{p.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},");
                sb.AppendLine("], borderColor: '#58a6ff', borderWidth: 2, fill: false, tension: 0.1 }] }, options: { scales: { x: { title: { display: true, text: 'Дальность X, км' } }, y: { title: { display: true, text: 'Высота Y, км' } } } } });");

                // 2. Высота H(t)
                sb.AppendLine("new Chart(document.getElementById('chartAltitude'), { type: 'line', data: { datasets: [{ label: 'Высота H(t) [км]', data: [");
                foreach (var p in AltitudeTimePoints) sb.Append($"{{x:{p.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},y:{p.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},");
                sb.AppendLine("], borderColor: '#3fb950', borderWidth: 2, fill: false }] }, options: { scales: { x: { title: { display: true, text: 'Время t, с' } }, y: { title: { display: true, text: 'Высота, км' } } } } });");

                // 3. Скорость V(t)
                sb.AppendLine("new Chart(document.getElementById('chartSpeed'), { type: 'line', data: { datasets: [{ label: 'Скорость V(t) [м/с]', data: [");
                foreach (var p in SpeedTimePoints) sb.Append($"{{x:{p.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},y:{p.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},");
                sb.AppendLine("], borderColor: '#d29922', borderWidth: 2, fill: false }] }, options: { scales: { x: { title: { display: true, text: 'Время t, с' } }, y: { title: { display: true, text: 'Скорость, м/с' } } } } });");

                // 4. Перегрузка G(t)
                sb.AppendLine("new Chart(document.getElementById('chartGForce'), { type: 'line', data: { datasets: [{ label: 'Перегрузка G(t) [g]', data: [");
                foreach (var p in GForceTimePoints) sb.Append($"{{x:{p.x.ToString(System.Globalization.CultureInfo.InvariantCulture)},y:{p.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}}},");
                sb.AppendLine("], borderColor: '#f85149', borderWidth: 2, fill: false }] }, options: { scales: { x: { title: { display: true, text: 'Время t, с' } }, y: { title: { display: true, text: 'G-Force' } } } } });");

                sb.AppendLine("</script></body></html>");

                string htmlFile = GetHtmlPath();
                File.WriteAllText(htmlFile, sb.ToString(), Encoding.UTF8);
                Debug.Log($"<color=lime>[ОТЧЁТ HTML] Интерактивный веб-отчёт с графиками сохранён: {htmlFile}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ОТЧЁТ HTML] Ошибка сохранения: {ex.Message}");
            }
        }
    }
}
