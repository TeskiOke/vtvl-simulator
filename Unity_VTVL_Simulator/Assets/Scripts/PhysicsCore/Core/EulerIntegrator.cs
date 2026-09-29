using System;

namespace DSTU.VTVL.PhysicsCore
{
    /// <summary>
    /// Явный численный интегратор 1-го порядка точности Эйлера.
    /// Используется для сравнительного анализа точности и вычисления
    /// относительной погрешности интегрирования относительно метода RK4.
    /// </summary>
    public static class EulerIntegrator
    {
        /// <summary>
        /// Выполняет один шаг интегрирования методом Эйлера: Y_{n+1} = Y_n + dt * f(Y_n, U_n, t_n).
        /// </summary>
        /// <param name="s">Состояние ракеты</param>
        /// <param name="dt">Шаг по времени (с)</param>
        /// <param name="throttle">Команда тяги [0..1]</param>
        /// <param name="pitchAngle">Угол тангажа (радианы)</param>
        /// <param name="isReentryMode">Режим спуска в атмосфере</param>
        public static void Step(RocketState s, double dt, double throttle, double pitchAngle, bool isReentryMode)
        {
            StateDerivatives d = CauchyDerivatives.Evaluate(s, throttle, pitchAngle, isReentryMode);

            s.PosX += d.dPosX * dt;
            s.PosY += d.dPosY * dt;
            s.PosZ += d.dPosZ * dt;

            s.VelX += d.dVelX * dt;
            s.VelY += d.dVelY * dt;
            s.VelZ += d.dVelZ * dt;

            s.Mass = Math.Max(RocketParameters.DryMass, s.Mass + d.dMass * dt);
            s.Fuel = Math.Max(0.0, s.Fuel + d.dFuel * dt);

            s.Time += dt;

            s.GForce = d.GForce;
            s.MachNumber = d.Mach;
            s.DynamicPressure = d.DynamicPressure;
            s.ThrustForce = d.ThrustForce;
            s.DragForce = d.DragForce;
            s.Throttle = throttle;

            if (s.PosY > 2.0)
            {
                s.HasLiftoff = true;
            }
            if (s.PosY > s.MaxAltitude)
            {
                s.MaxAltitude = s.PosY;
            }
        }
    }
}
