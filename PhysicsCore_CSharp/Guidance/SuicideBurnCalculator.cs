using System;

namespace DSTU.VTVL.Guidance
{
    using DSTU.VTVL.PhysicsCore;

    /// <summary>
    /// Вычислитель параметров посадочного тормозного импульса Hoverslam (Suicide Burn).
    /// Определяет критическую высоту включения маршевого двигателя и замкнутый контур
    /// дросселирования для гашения скорости строго у поверхности посадочной площадки.
    /// </summary>
    public static class SuicideBurnCalculator
    {
        /// <summary>
        /// Расчёт теоретической высоты включения тормозного импульса h_burn (м)
        /// с учётом модели задержки зажигания и набора тяги КР-3 (tau_d = 0.5 с, tau_r = 1.0 с).
        /// </summary>
        /// <param name="mass">Текущая масса ракеты (кг)</param>
        /// <param name="altitude">Текущая высота (м)</param>
        /// <param name="verticalVelocity">Вертикальная скорость Vy (м/с, < 0 при падении)</param>
        /// <param name="safetyMargin">Коэффициент запаса по высоте (обычно 1.15..1.35)</param>
        public static double CalculateIgnitionAltitude(double mass, double altitude, double verticalVelocity, double safetyMargin = 1.25)
        {
            if (verticalVelocity >= 0.0) return 0.0;

            double gh = GravityModel.GetGravity(altitude);
            double maxAccel = (RocketParameters.ThrustMax / mass) - gh;

            if (maxAccel <= 0.1) return double.MaxValue;

            // Тормозная дистанция на установившемся режиме
            double stoppingDist = (verticalVelocity * verticalVelocity) / (2.0 * maxAccel);

            // Потеря высоты за время задержки зажигания (КР-3: tau_d = 0.5 с чистый простой + tau_r = 1.0 с набор)
            double absVy = Math.Abs(verticalVelocity);
            double hIgnitionLoss = absVy * (RocketParameters.EngineIgnitionDelay + 0.5 * RocketParameters.EngineRampUpTime) 
                                 + 0.5 * gh * Math.Pow(RocketParameters.EngineIgnitionDelay, 2);

            return (stoppingDist * safetyMargin) + hIgnitionLoss;
        }

        /// <summary>
        /// Вычисляет оптимальный уровень тяги двигателя для мягкого касания со скоростью ~1.2 м/с.
        /// </summary>
        /// <param name="mass">Текущая масса ступени (кг)</param>
        /// <param name="altitude">Текущая высота над площадкой (м)</param>
        /// <param name="verticalVelocity">Текущая вертикальная скорость (м/с)</param>
        public static double ComputeLandingThrottle(double mass, double altitude, double verticalVelocity)
        {
            double gh = GravityModel.GetGravity(altitude);

            // Целевая вертикальная скорость при касании (1.2 м/с)
            const double targetV = 1.2;
            double absVy = Math.Abs(verticalVelocity);
            
            // Если скорость уже ниже целевой на малой высоте, удерживаем минимальное зависание
            if (altitude < 3.0 && absVy <= targetV + 0.3)
            {
                double hoverThrust = mass * (gh - 0.3); // Мягкое опускание с -0.3 м/с²
                return Math.Max(RocketParameters.ThrottleMin, Math.Min(1.0, hoverThrust / RocketParameters.ThrustMax));
            }

            double vSqDelta = Math.Max(0.0, (absVy * absVy) - (targetV * targetV));
            double requiredDecel = vSqDelta / (2.0 * Math.Max(0.8, altitude));

            double requiredThrust = mass * (requiredDecel + gh);
            double throttle = requiredThrust / RocketParameters.ThrustMax;

            return Math.Max(RocketParameters.ThrottleMin, Math.Min(1.0, throttle));
        }

        /// <summary>
        /// Вычисляет корректирующий угол наклона вектора тяги для наведения на центр посадочной баржи B.
        /// </summary>
        /// <param name="currentX">Текущая координата X (м)</param>
        /// <param name="targetPadX">Координата посадочной баржи B (м)</param>
        /// <param name="currentVx">Текущая горизонтальная скорость Vx (м/с)</param>
        public static double ComputeSteeringTilt(double currentX, double targetPadX, double currentVx)
        {
            double dx = targetPadX - currentX;
            double targetVx = Math.Max(-8.0, Math.Min(8.0, dx * 0.04));
            double errVx = targetVx - currentVx;

            // Ограничение отклонения вектора тяги не более ±3 градусов (±0.05 радиан)
            return Math.Max(-0.05, Math.Min(0.05, -errVx * 0.015));
        }
    }
}
