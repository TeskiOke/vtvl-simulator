using System;

namespace DSTU.VTVL.PhysicsCore
{
    /// <summary>
    /// Численный интегратор 4-го порядка точности Рунге-Кутты (RK4).
    /// Обеспечивает погрешность порядка O(dt⁴) и численную устойчивость
    /// при интегрировании нелинейной баллистической траектории.
    /// </summary>
    public static class RK4Integrator
    {
        /// <summary>
        /// Выполняет один шаг интегрирования методом Рунге-Кутты 4-го порядка.
        /// </summary>
        /// <param name="s">Состояние ракеты (модифицируется на месте)</param>
        /// <param name="dt">Шаг по времени (с)</param>
        /// <param name="throttle">Команда тяги [0..1]</param>
        /// <param name="pitchAngle">Угол тангажа (радианы)</param>
        /// <param name="isReentryMode">Режим спуска в атмосфере</param>
        public static void Step(RocketState s, double dt, double throttle, double pitchAngle, bool isReentryMode)
        {
            // k1
            StateDerivatives k1 = CauchyDerivatives.Evaluate(s, throttle, pitchAngle, isReentryMode);

            // Состояние для k2 (шаг на dt/2 по k1)
            RocketState s2 = s.Clone();
            s2.PosX += k1.dPosX * dt * 0.5;
            s2.PosY += k1.dPosY * dt * 0.5;
            s2.PosZ += k1.dPosZ * dt * 0.5;
            s2.VelX += k1.dVelX * dt * 0.5;
            s2.VelY += k1.dVelY * dt * 0.5;
            s2.VelZ += k1.dVelZ * dt * 0.5;
            s2.Mass = Math.Max(RocketParameters.DryMass, s2.Mass + k1.dMass * dt * 0.5);
            s2.Fuel = Math.Max(0.0, s2.Fuel + k1.dFuel * dt * 0.5);
            StateDerivatives k2 = CauchyDerivatives.Evaluate(s2, throttle, pitchAngle, isReentryMode);

            // Состояние для k3 (шаг на dt/2 по k2)
            RocketState s3 = s.Clone();
            s3.PosX += k2.dPosX * dt * 0.5;
            s3.PosY += k2.dPosY * dt * 0.5;
            s3.PosZ += k2.dPosZ * dt * 0.5;
            s3.VelX += k2.dVelX * dt * 0.5;
            s3.VelY += k2.dVelY * dt * 0.5;
            s3.VelZ += k2.dVelZ * dt * 0.5;
            s3.Mass = Math.Max(RocketParameters.DryMass, s3.Mass + k2.dMass * dt * 0.5);
            s3.Fuel = Math.Max(0.0, s3.Fuel + k2.dFuel * dt * 0.5);
            StateDerivatives k3 = CauchyDerivatives.Evaluate(s3, throttle, pitchAngle, isReentryMode);

            // Состояние для k4 (полный шаг на dt по k3)
            RocketState s4 = s.Clone();
            s4.PosX += k3.dPosX * dt;
            s4.PosY += k3.dPosY * dt;
            s4.PosZ += k3.dPosZ * dt;
            s4.VelX += k3.dVelX * dt;
            s4.VelY += k3.dVelY * dt;
            s4.VelZ += k3.dVelZ * dt;
            s4.Mass = Math.Max(RocketParameters.DryMass, s4.Mass + k3.dMass * dt);
            s4.Fuel = Math.Max(0.0, s4.Fuel + k3.dFuel * dt);
            StateDerivatives k4 = CauchyDerivatives.Evaluate(s4, throttle, pitchAngle, isReentryMode);

            // Взвешенное суммирование RK4: Y_{n+1} = Y_n + (dt / 6) * (k1 + 2*k2 + 2*k3 + k4)
            double dt6 = dt / 6.0;

            s.PosX += dt6 * (k1.dPosX + 2.0 * k2.dPosX + 2.0 * k3.dPosX + k4.dPosX);
            s.PosY += dt6 * (k1.dPosY + 2.0 * k2.dPosY + 2.0 * k3.dPosY + k4.dPosY);
            s.PosZ += dt6 * (k1.dPosZ + 2.0 * k2.dPosZ + 2.0 * k3.dPosZ + k4.dPosZ);

            s.VelX += dt6 * (k1.dVelX + 2.0 * k2.dVelX + 2.0 * k3.dVelX + k4.dVelX);
            s.VelY += dt6 * (k1.dVelY + 2.0 * k2.dVelY + 2.0 * k3.dVelY + k4.dVelY);
            s.VelZ += dt6 * (k1.dVelZ + 2.0 * k2.dVelZ + 2.0 * k3.dVelZ + k4.dVelZ);

            s.Mass = Math.Max(RocketParameters.DryMass, s.Mass + dt6 * (k1.dMass + 2.0 * k2.dMass + 2.0 * k3.dMass + k4.dMass));
            s.Fuel = Math.Max(0.0, s.Fuel + dt6 * (k1.dFuel + 2.0 * k2.dFuel + 2.0 * k3.dFuel + k4.dFuel));

            s.Time += dt;

            // Телеметрия с шага k1
            s.GForce = k1.GForce;
            s.MachNumber = k1.Mach;
            s.DynamicPressure = k1.DynamicPressure;
            s.ThrustForce = k1.ThrustForce;
            s.DragForce = k1.DragForce;
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
