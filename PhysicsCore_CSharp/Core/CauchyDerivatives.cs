using System;

namespace DSTU.VTVL.PhysicsCore
{
    /// <summary>
    /// Структура производных задачи Коши (dY/dt).
    /// </summary>
    public struct StateDerivatives
    {
        public double dPosX; // dx/dt = vx
        public double dPosY; // dy/dt = vy
        public double dPosZ; // dz/dt = vz

        public double dVelX; // dvx/dt = ax
        public double dVelY; // dvy/dt = ay
        public double dVelZ; // dvz/dt = az

        public double dPitch; // dTheta/dt = Omega_z
        public double dYaw;   // dPsi/dt
        public double dRoll;  // dGamma/dt

        public double dMass; // dm/dt (секундный расход топлива)
        public double dFuel; // dfuel/dt

        // Вспомогательные данные для обновления состояния
        public double ThrustForce;
        public double DragForce;
        public double GForce;
        public double KinematicAccel;
        public double Mach;
        public double DynamicPressure;
    }

    /// <summary>
    /// Вычислитель правых частей системы 14 обыкновенных дифференциальных уравнений (ОДУ).
    /// dY/dt = f(Y, U, t)
    /// </summary>
    public static class CauchyDerivatives
    {
        /// <summary>
        /// Вычисляет вектор производных состояния ракеты на основе текущих сил и управляющих воздействий.
        /// </summary>
        /// <param name="s">Текущее состояние ракеты</param>
        /// <param name="throttleCmd">Команда дросселя [0..1]</param>
        /// <param name="pitchAngle">Угол тангажа ракеты (радианы)</param>
        /// <param name="isReentryMode">Флаг аэродинамического торможения</param>
        public static StateDerivatives Evaluate(RocketState s, double throttleCmd, double pitchAngle, bool isReentryMode)
        {
            StateDerivatives d = new StateDerivatives();

            double altitude = Math.Max(0.0, s.PosY);

            // 1. Кинематические производные координат
            d.dPosX = s.VelX;
            d.dPosY = s.VelY;
            d.dPosZ = s.VelZ;

            // 2. Дросселирование и расчёт тяги
            double actualThrottle = 0.0;
            if (s.Fuel > 0.0 && throttleCmd > 0.0)
            {
                actualThrottle = Math.Max(RocketParameters.ThrottleMin, Math.Min(1.0, throttleCmd));
            }
            double thrust = actualThrottle * RocketParameters.ThrustMax;
            d.ThrustForce = thrust;

            // Проекции вектора тяги
            double fTx = thrust * Math.Cos(pitchAngle);
            double fTy = thrust * Math.Sin(pitchAngle);
            double fTz = 0.0;

            // 3. Аэродинамические силы
            double mach, q, fDx, fDy;
            AerodynamicsModel.ComputeDragForce(s.VelX, s.VelY, altitude, isReentryMode, out mach, out q, out fDx, out fDy);
            d.Mach = mach;
            d.DynamicPressure = q;
            d.DragForce = Math.Sqrt(fDx * fDx + fDy * fDy);

            // 4. Сила тяжести Ньютона
            double gh = GravityModel.GetGravity(altitude);
            double fGy = -s.Mass * gh;

            // 5. Суммарные ускорения в полёте
            double massSafe = Math.Max(RocketParameters.DryMass, s.Mass);
            double ax = (fTx + fDx) / massSafe;
            double ay = (fTy + fDy + fGy) / massSafe;
            double az = fTz / massSafe;

            // 6. Реакция опоры стартового стола (N = mg при ay <= 0)
            double feltG = 1.0;
            if (s.PosY <= 0.0 && ay <= 0.0 && !s.HasLiftoff)
            {
                ax = 0.0;
                ay = 0.0;
                az = 0.0;
                feltG = 1.0; // Состояние покоя на Земле (1 G)
            }
            else
            {
                // Ощущаемая перегрузка (акселерометр измеряет негравитационные силы)
                double fNetNonGrav = Math.Sqrt((fTx + fDx) * (fTx + fDx) + (fTy + fDy) * (fTy + fDy));
                feltG = fNetNonGrav / (massSafe * RocketParameters.G0);
            }

            d.dVelX = ax;
            d.dVelY = ay;
            d.dVelZ = az;
            d.GForce = feltG;
            d.KinematicAccel = Math.Sqrt(ax * ax + ay * ay + az * az);

            // 7. Расход топлива dm/dt = - T / (Isp * g0)
            double dFuel = 0.0;
            if (s.Fuel > 0.0 && actualThrottle > 0.0)
            {
                dFuel = -(thrust / (RocketParameters.SpecificImpulse * RocketParameters.G0));
            }
            d.dFuel = dFuel;
            d.dMass = dFuel;

            // 8. Угловые производные
            d.dPitch = s.AngularVelPitch;
            d.dYaw = s.AngularVelYaw;
            d.dRoll = s.AngularVelRoll;

            return d;
        }
    }
}
