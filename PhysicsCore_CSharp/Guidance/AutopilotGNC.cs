using System;

namespace DSTU.VTVL.Guidance
{
    using DSTU.VTVL.PhysicsCore;

    /// <summary>
    /// Фазы полета (строгий конечный автомат состояний).
    /// Исключает ложные переходы назад при торможении у земли.
    /// </summary>
    public enum FlightStage
    {
        PadA_Prelaunch = 0,
        Ascent_Vertical = 1,
        Ascent_GravityTurn = 2,
        Coast_To_Apogee = 3,
        Descent_Aerodynamic = 4,
        Landing_Hoverslam = 5,
        Touchdown_Success = 6
    }

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
        public FlightStage Stage = FlightStage.PadA_Prelaunch;

        /// <summary>Максимальная скорость переориентации корпуса по тангажу (рад/с, ~4.5 град/с)</summary>
        public double MaxPitchRate = 0.08;

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
                HoverslamActive = false
            };

            double gh = GravityModel.GetGravity(s.PosY);
            double estimatedApogee = s.VelY > 0.0 ? s.PosY + (s.VelY * s.VelY) / (2.0 * gh) : s.PosY;

            switch (Stage)
            {
                case FlightStage.PadA_Prelaunch:
                    if (s.HasLiftoff || s.Time > 0.0)
                    {
                        Stage = FlightStage.Ascent_Vertical;
                    }
                    cmd.Throttle = 1.0;
                    cmd.PitchAngle = Math.PI / 2.0;
                    cmd.PhaseName = "ВЕРТИКАЛЬНЫЙ ПОДЪЁМ";
                    cmd.PhaseDescription = "Старт со стола Pad A, набор вертикальной скорости";
                    break;

                case FlightStage.Ascent_Vertical:
                    cmd.Throttle = 1.0;
                    cmd.PitchAngle = Math.PI / 2.0;
                    cmd.PhaseName = "ВЕРТИКАЛЬНЫЙ ПОДЪЁМ";
                    cmd.PhaseDescription = $"Высота {s.PosY:F0} м (прохождение приземного слоя)";

                    if (s.PosY >= 300.0)
                    {
                        Stage = FlightStage.Ascent_GravityTurn;
                    }
                    break;

                case FlightStage.Ascent_GravityTurn:
                    cmd.Throttle = 1.0;

                    // Наклон тангажа к посадочной барже B (X = 25 270 м)
                    // Тангаж плавно снижается от 90.0° до 75.3° (наклон 14.7° к горизонту)
                    double turnProgress = Math.Pow(Math.Max(0.0, s.PosY - 300.0) / 69700.0, 1.25);
                    double targetPitchDeg = 90.0 - 14.7 * Math.Min(1.0, turnProgress);

                    cmd.PitchAngle = (targetPitchDeg * Math.PI) / 180.0;
                    cmd.PhaseName = "ГРАВИТАЦИОННЫЙ РАЗВОРОТ";
                    cmd.PhaseDescription = $"Тангаж {targetPitchDeg:F1}° ➔ расчетный апогей {estimatedApogee / 1000.0:F1} км";

                    // Условие отсечки тяги (MECO): достижение апогея > 105 км ИЛИ достижение посадочного резерва топлива
                    if (estimatedApogee >= 105000.0 || s.Fuel <= RocketParameters.MinLandingFuelReserve)
                    {
                        Stage = FlightStage.Coast_To_Apogee;
                    }
                    break;

                case FlightStage.Coast_To_Apogee:
                    cmd.Throttle = 0.0;
                    cmd.PitchAngle = (75.3 * Math.PI) / 180.0;
                    cmd.PhaseName = s.PosY >= RocketParameters.KarmanLine ? "ОТКРЫТЫЙ КОСМОС (>100 км)" : "ВЫХОД НА АПОГЕЙ C";
                    cmd.PhaseDescription = "Инерционный полёт к апогею C (вакуум, невесомость)";

                    // Прохождение вершины траектории (скорость по Y сменила знак на отрицательный)
                    if (s.VelY <= 0.0)
                    {
                        Stage = FlightStage.Descent_Aerodynamic;
                    }
                    break;

                case FlightStage.Descent_Aerodynamic:
                    cmd.Throttle = 0.0;
                    cmd.RcsActive = (s.PosY > 40000.0);
                    cmd.GridFinsActive = (s.PosY <= 65000.0);
                    
                    // Управление планированием в направлении баржи с помощью решетчатых рулей
                    double dx = RocketParameters.PadB_X - s.PosX;
                    double desiredGlideVx = Math.Max(-25.0, Math.Min(25.0, dx * 0.04));
                    double glideTilt = Math.Max(-0.06, Math.Min(0.06, (desiredGlideVx - s.VelX) * 0.02));
                    cmd.PitchAngle = Math.PI / 2.0 + glideTilt; // Носом вверх, двигатели вниз

                    cmd.PhaseName = s.PosY >= RocketParameters.KarmanLine ? "АПОГЕЙ C: РАЗВОРОТ RCS" : "УПРАВЛЯЕМЫЙ СПУСК К БАРЖЕ B";
                    cmd.PhaseDescription = $"Аэродинамическое торможение решётчатыми рулями (dx = {dx / 1000.0:F1} км)";

                    // Точная точка зажигания Suicide Burn (1.085 запас)
                    double hBurn = SuicideBurnCalculator.CalculateIgnitionAltitude(s.Mass, s.PosY, s.VelY, 1.085);
                    if (s.PosY <= hBurn && s.PosY < 3000.0 && s.VelY < -10.0 && s.Fuel > 0.0)
                    {
                        Stage = FlightStage.Landing_Hoverslam;
                    }
                    break;

                case FlightStage.Landing_Hoverslam:
                    cmd.HoverslamActive = true;
                    s.IsHoverslamActive = true;
                    cmd.GridFinsActive = true;
                    cmd.PhaseName = "ПОСАДКА: HOVERSLAM (SUICIDE BURN)";

                    cmd.Throttle = SuicideBurnCalculator.ComputeLandingThrottle(s.Mass, s.PosY, s.VelY);
                    double steerTilt = SuicideBurnCalculator.ComputeSteeringTilt(s.PosX, RocketParameters.PadB_X, s.VelX);
                    cmd.PitchAngle = Math.PI / 2.0 + steerTilt;

                    cmd.PhaseDescription = $"🔥 Торможение двигателем! h = {s.PosY:F1} м, Vy = {s.VelY:F1} м/с, Vx = {s.VelX:F1} м/с";

                    // Касание палубы баржи
                    if (s.PosY <= 1.0 || (s.PosY <= 2.5 && Math.Abs(s.VelY) <= 2.0))
                    {
                        Stage = FlightStage.Touchdown_Success;
                    }
                    break;

                case FlightStage.Touchdown_Success:
                    cmd.Throttle = 0.0;
                    cmd.PitchAngle = Math.PI / 2.0;
                    cmd.PhaseName = "МЯГКАЯ ПОСАДКА НА БАРЖУ B (УСПЕХ)";
                    cmd.PhaseDescription = $"Касание завершено: Vy = {s.VelY:F2} м/с, Vx = {s.VelX:F2} м/с";
                    s.IsLanded = true;
                    break;
            }

            // Плавное изменение угла тангажа с учётом максимальной угловой скорости
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
