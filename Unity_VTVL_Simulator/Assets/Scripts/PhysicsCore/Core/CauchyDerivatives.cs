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
        public double LiftForce;
        public double LiftForceX;
        public double LiftForceY;
        public double LiftForceZ;
        public double GForce;
        public double KinematicAccel;
        public double Mach;
        public double DynamicPressure;
        public double AngleOfAttackDeg;
        public double HeatFlux;
        public double SkinTemperature;
        public double CoriolisAccelX;
        public double CoriolisAccelY;
        public double CoriolisAccelZ;
        public double GravityJ2Radial;
        public double GravityJ2Polar;
    }

    /// <summary>
    /// Вычислитель правых частей системы 14 обыкновенных дифференциальных уравнений (ОДУ).
    /// dY/dt = f(Y, U, t) с учетом полной физики внешней среды:
    /// - Стандартная атмосфера ГОСТ 4401-81 / USSA-76 до 250 км
    /// - Аэродинамическое сопротивление и подъемная сила угла атаки / Belly-Flop
    /// - Аэродинамический нагрев Саттона-Грейвса и тепловое излучение Стефана-Больцмана
    /// - Гравитационное поле Земли с зональной гармоникой геоида J2
    /// - Кориолисово ускорение суточного вращения Земли (в топоцентрической СК ENU)
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

            // Проекции вектора тяги в стартовой СК
            double fTx = thrust * Math.Cos(pitchAngle);
            double fTy = thrust * Math.Sin(pitchAngle);
            double fTz = 0.0;

            // 3. Параметры атмосферы ГОСТ 4401-81 (0 .. 250 км)
            AtmosphereData atm = AtmosphereGOST4401.GetData(altitude);

            // 4. Аэродинамические силы (сопротивление и подъемная сила)
            double mach, q, fDx, fDy;
            AerodynamicsModel.ComputeDragForce(s.VelX, s.VelY, altitude, isReentryMode, out mach, out q, out fDx, out fDy);
            d.Mach = mach;
            d.DynamicPressure = q;
            d.DragForce = Math.Sqrt(fDx * fDx + fDy * fDy);

            // Подъемная сила от угла атаки alpha = theta - gamma
            double vTotal = Math.Sqrt(s.VelX * s.VelX + s.VelY * s.VelY + s.VelZ * s.VelZ);
            double fLx = 0.0;
            double fLy = 0.0;
            double fLz = 0.0;
            double alphaDeg = 0.0;

            if (vTotal > 1.0 && q > 0.01)
            {
                double flightPathAngle = Math.Atan2(s.VelY, s.VelX);
                double alpha = pitchAngle - flightPathAngle;

                // Нормализация угла атаки к [-pi .. +pi]
                while (alpha > Math.PI) alpha -= 2.0 * Math.PI;
                while (alpha < -Math.PI) alpha += 2.0 * Math.PI;

                alphaDeg = alpha * (180.0 / Math.PI);

                // Коэффициент подъемной силы C_L(alpha)
                double cL = Math.Max(-0.85, Math.Min(0.85, 1.8 * Math.Sin(2.0 * alpha)));
                double liftMagnitude = q * RocketParameters.CrossSectionArea * cL;

                // Вектор подъемной силы перпендикулярен вектору скорости в плоскости тангажа
                fLx = -liftMagnitude * Math.Sin(flightPathAngle);
                fLy = liftMagnitude * Math.Cos(flightPathAngle);
            }

            d.AngleOfAttackDeg = alphaDeg;
            d.LiftForceX = fLx;
            d.LiftForceY = fLy;
            d.LiftForceZ = fLz;
            d.LiftForce = Math.Sqrt(fLx * fLx + fLy * fLy + fLz * fLz);

            // 5. Аэродинамический нагрев обшивки (Саттон-Грейвс + Стефан-Больцман)
            double heatFluxWatts, skinTempCelsius;
            AerodynamicsModel.ComputeAeroHeating(
                atm.Density, vTotal, RocketParameters.NoseRadius, atm.Temperature,
                out heatFluxWatts, out skinTempCelsius);
            d.HeatFlux = heatFluxWatts;
            d.SkinTemperature = skinTempCelsius;

            // 6. Сила тяжести Ньютона с зональной гармоникой геоида J2
            double gh = GravityModel.GetGravity(altitude);
            double fGy = -s.Mass * gh;

            double aJ2Radial, aJ2Polar;
            GravityModel.GetGravityJ2(altitude, RocketParameters.LaunchLatitudeRad, out aJ2Radial, out aJ2Polar);
            // Возмущающая радиальная добавка J2 (разница с чистым монополем GM/r²)
            double r = RocketParameters.EarthRadius + altitude;
            double monopole = -(GravityModel.EarthMu / (r * r));
            double deltaJ2Radial = aJ2Radial - monopole;
            fGy += s.Mass * deltaJ2Radial;
            double fGz = s.Mass * aJ2Polar;

            d.GravityJ2Radial = deltaJ2Radial;
            d.GravityJ2Polar = aJ2Polar;

            // 7. Силы инерции во вращающейся системе координат Земли (Кориолис и центробежное ускорение)
            double aCorX, aCorY, aCorZ;
            GravityModel.ComputeCoriolisAcceleration(
                s.VelX, s.VelY, s.VelZ, RocketParameters.LaunchLatitudeRad,
                out aCorX, out aCorY, out aCorZ);
            d.CoriolisAccelX = aCorX;
            d.CoriolisAccelY = aCorY;
            d.CoriolisAccelZ = aCorZ;

            // 8. Суммарные ускорения в полёте
            double massSafe = Math.Max(RocketParameters.DryMass, s.Mass);
            double ax = (fTx + fDx + fLx) / massSafe + aCorX;
            double ay = (fTy + fDy + fLy + fGy) / massSafe + aCorY;
            double az = (fTz + fLz + fGz) / massSafe + aCorZ;

            // 9. Реакция опоры стартового стола (N = mg при ay <= 0 до отрыва)
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
                double fNetNonGravX = fTx + fDx + fLx;
                double fNetNonGravY = fTy + fDy + fLy;
                double fNetNonGravZ = fTz + fLz;
                double fNetNonGrav = Math.Sqrt(fNetNonGravX * fNetNonGravX + fNetNonGravY * fNetNonGravY + fNetNonGravZ * fNetNonGravZ);
                feltG = fNetNonGrav / (massSafe * RocketParameters.G0);
            }

            d.dVelX = ax;
            d.dVelY = ay;
            d.dVelZ = az;
            d.GForce = feltG;
            d.KinematicAccel = Math.Sqrt(ax * ax + ay * ay + az * az);

            // 10. Расход топлива dm/dt = - T / (Isp * g0)
            double dFuel = 0.0;
            if (s.Fuel > 0.0 && actualThrottle > 0.0)
            {
                dFuel = -(thrust / (RocketParameters.SpecificImpulse * RocketParameters.G0));
            }
            d.dFuel = dFuel;
            d.dMass = dFuel;

            // 11. Угловые производные
            d.dPitch = s.AngularVelPitch;
            d.dYaw = s.AngularVelYaw;
            d.dRoll = s.AngularVelRoll;

            return d;
        }
    }
}
