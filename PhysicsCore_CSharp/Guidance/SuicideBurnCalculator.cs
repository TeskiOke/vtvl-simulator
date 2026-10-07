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
        public static double CalculateIgnitionAltitude(double mass, double altitude, double verticalVelocity, double safetyMargin = 1.085)
        {
            if (verticalVelocity >= 0.0) return 0.0;

            double gh = GravityModel.GetGravity(altitude);
            // Проектируем зажигание на 86% от максимальной тяги:
            // Это оставляет запас +14% тяги для форсирования при просадке и -46% для снижения дросселя до 40% (ThrottleMin).
            double nominalThrust = RocketParameters.ThrustMax * 0.86;
            double netAccel = (nominalThrust / mass) - gh;

            if (netAccel <= 0.1) return double.MaxValue;

            // Тормозная дистанция на установившемся режиме (гашение до целевой скорости 1.2 м/с)
            const double targetV = 1.2;
            double absVy = Math.Abs(verticalVelocity);
            double vDeltaSq = Math.Max(0.0, (absVy * absVy) - (targetV * targetV));
            double stoppingDist = vDeltaSq / (2.0 * netAccel);

            // Потеря высоты за время задержки зажигания и выхода на тягу (КР-3: tau_d = 0.5 с, tau_r = 1.0 с)
            double tRampEff = RocketParameters.EngineIgnitionDelay + 0.5 * RocketParameters.EngineRampUpTime;
            double hIgnitionLoss = absVy * tRampEff + 0.5 * gh * (tRampEff * tRampEff);

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
            const double targetV = 1.2;
            double absVy = Math.Abs(verticalVelocity);

            // Защита от повторного взлета: если ракета перестала падать, глушим двигатель
            if (verticalVelocity >= -0.2)
            {
                return 0.0;
            }

            // При непосредственной близости к палубе (h <= 3.0 м)
            if (altitude <= 3.0)
            {
                if (absVy <= targetV + 0.5)
                {
                    double hoverThrust = mass * (gh - 0.2); // Мягкое опускание со скоростью 1.0..1.2 м/с
                    return Math.Max(RocketParameters.ThrottleMin, Math.Min(1.0, hoverThrust / RocketParameters.ThrustMax));
                }
            }

            double vSqDelta = Math.Max(0.0, (absVy * absVy) - (targetV * targetV));
            double requiredDecel = vSqDelta / (2.0 * Math.Max(0.5, altitude));

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
            // Целевая горизонтальная скорость сближения: пропорционально смещению, макс ±10 м/с
            double targetVx = Math.Max(-10.0, Math.Min(10.0, dx * 0.08));
            double errVx = targetVx - currentVx;

            // Ограничение отклонения вектора тяги не более ±3 градусов (±0.052 радиан)
            return Math.Max(-0.052, Math.Min(0.052, -errVx * 0.02));
        }
    }
}
