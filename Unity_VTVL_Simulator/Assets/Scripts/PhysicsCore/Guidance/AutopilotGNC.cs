using System;

namespace DSTU.VTVL.Guidance
{
    using DSTU.VTVL.PhysicsCore;

    /// <summary>
    /// Выходная управляющая команда автопилота GNC.
    /// </summary>
    public struct AutopilotCommand
    {
        public double Throttle;
        public double PitchAngle;
        public string PhaseName;
        public string PhaseDescription;
        public bool RcsActive;
        public bool GridFinsActive;
        public bool HoverslamActive;
    }

    /// <summary>
    /// Система наведения, навигации и управления (GNC) для суборбитального профиля полёта:
    /// Стартовый стол A -> Линия Кармана C (>100 км) -> Посадочная баржа B.
    /// </summary>
    public class AutopilotGNC
    {
        /// <summary>Максимальная скорость переориентации корпуса по тангажу (рад/с, ~4.5 град/с)</summary>
        public double MaxPitchRate = 0.08;

        /// <summary>
        /// Вычисление команд автопилота для текущего состояния ракеты.
        /// </summary>
        public AutopilotCommand Update(RocketState s, double dt)
        {
            AutopilotCommand cmd = new AutopilotCommand
            {
                Throttle = 0.0,
                PitchAngle = Math.PI / 2.0,
                PhaseName = "СТАРТОВЫЙ СТОЛ A",
                PhaseDescription = "Ожидание команды на запуск двигателей",
                RcsActive = false,
                GridFinsActive = false,
                HoverslamActive = s.IsHoverslamActive
            };

            double gh = GravityModel.GetGravity(s.PosY);
            double estimatedApogee = s.VelY > 0.0 ? s.PosY + (s.VelY * s.VelY) / (2.0 * gh) : s.PosY;

            // -------------------------------------------------------------
            // Фаза 1. Вертикальный подъём со стола A до 300 м
            // -------------------------------------------------------------
            if (s.Fuel > 2000.0 && s.PosY < 300.0 && s.VelY >= 0.0)
            {
                cmd.Throttle = 1.0;
                cmd.PitchAngle = Math.PI / 2.0;
                cmd.PhaseName = "ВЕРТИКАЛЬНЫЙ ПОДЪЁМ";
                cmd.PhaseDescription = $"Высота {s.PosY:F0} м (прохождение приземного слоя)";
            }
            // -------------------------------------------------------------
            // Фаза 2. Гравитационный разворот (программа КР-4 до theta_k = 85.5°)
            // -------------------------------------------------------------
            else if (s.Fuel > 2000.0 && s.VelY > 0.0 && estimatedApogee < 105000.0 && s.PosY < 70000.0)
            {
                cmd.Throttle = 1.0;
                // Точная формула КР-4: theta(h) = 90° - 4.5° * ((h - 200) / 69800)^1.5
                double targetDeg = 90.0 - 4.5 * Math.Pow(Math.Max(0.0, s.PosY - 200.0) / 69800.0, 1.5);
                cmd.PitchAngle = (targetDeg * Math.PI) / 180.0;
                cmd.PhaseName = "ГРАВИТАЦИОННЫЙ РАЗВОРОТ";
                cmd.PhaseDescription = $"Тангаж {targetDeg:F1}° ➔ расчетный апогей {estimatedApogee / 1000.0:F1} км";
            }
            // -------------------------------------------------------------
            // Фаза 3. Отсечка тяги (MECO) и выход на суборбитальный апогей C (> 100 км)
            // -------------------------------------------------------------
            else if (s.VelY > 5.0)
            {
                cmd.Throttle = 0.0; // Двигатель выключен, экономим топливо на посадку!
                cmd.PitchAngle = (85.5 * Math.PI) / 180.0; // Суборбитальный тангаж 85.5° по КР-4
                cmd.PhaseName = s.PosY >= RocketParameters.KarmanLine ? "ОТКРЫТЫЙ КОСМОС (>100 км)" : "ВЫХОД НА АПОГЕЙ C";
                cmd.PhaseDescription = "Инерционный полёт к апогею C (вакуум, невесомость)";
            }
            // -------------------------------------------------------------
            // Фаза 4. Прохождение апогея: разворот RCS кормой вниз (Flip Maneuver)
            // -------------------------------------------------------------
            else if (s.PosY > 2500.0)
            {
                cmd.Throttle = 0.0;
                cmd.RcsActive = (s.PosY > 40000.0);
                cmd.GridFinsActive = (s.PosY <= 60000.0);
                cmd.PitchAngle = Math.PI / 2.0; // Нос вверх, двигатель вниз для посадки
                cmd.PhaseName = s.PosY >= RocketParameters.KarmanLine ? "АПОГЕЙ C: РАЗВОРОТ RCS" : "АЭРОДИНАМИЧЕСКИЙ СПУСК";
                cmd.PhaseDescription = s.PosY >= RocketParameters.KarmanLine
                    ? "Маневровые сопла RCS разворачивают ступень кормой вниз"
                    : "Решётчатые рули (Grid Fins) стабилизируют и тормозят ступень";
            }
            // -------------------------------------------------------------
            // Фаза 5. Посадка: Тормозной импульс Hoverslam (Suicide Burn)
            // -------------------------------------------------------------
            else
            {
                cmd.GridFinsActive = true;
                double hBurn = SuicideBurnCalculator.CalculateIgnitionAltitude(s.Mass, s.PosY, s.VelY, 1.55);

                if ((s.PosY <= hBurn || s.IsHoverslamActive) && s.Fuel > 0.0 && s.PosY < 2000.0)
                {
                    cmd.HoverslamActive = true;
                    s.IsHoverslamActive = true;
                    cmd.PhaseName = "ПОСАДКА: HOVERSLAM (SUICIDE BURN)";

                    cmd.Throttle = SuicideBurnCalculator.ComputeLandingThrottle(s.Mass, s.PosY, s.VelY);
                    double tilt = SuicideBurnCalculator.ComputeSteeringTilt(s.PosX, RocketParameters.PadB_X, s.VelX);
                    cmd.PitchAngle = Math.PI / 2.0 + tilt;

                    cmd.PhaseDescription = $"🔥 Торможение двигателем! h = {s.PosY:F1} м, Vy = {s.VelY:F1} м/с";
                }
                else
                {
                    cmd.Throttle = 0.0;
                    cmd.PhaseName = "УПРАВЛЯЕМЫЙ СПУСК К БАРЖЕ B";
                    double dx = RocketParameters.PadB_X - s.PosX;
                    cmd.PitchAngle = Math.PI / 2.0 - s.VelX * 0.02 + Math.Max(-0.06, Math.Min(0.06, dx * 0.0005));
                    cmd.PhaseDescription = $"Аэродинамическое торможение решётчатыми рулями (dx = {dx / 1000.0:F1} км)";
                }
            }

            // Плавное изменение угла тангажа с учётом максимальной угловой скорости (инерция ступени)
            double dPitch = cmd.PitchAngle - s.Pitch;
            double pitchStep = Math.Max(-MaxPitchRate * dt, Math.Min(MaxPitchRate * dt, dPitch));
            s.Pitch += pitchStep;

            s.IsRcsActive = cmd.RcsActive;
            s.IsGridFinsActive = cmd.GridFinsActive;
            s.FlightPhase = cmd.PhaseName;

            return cmd;
        }
    }
}
