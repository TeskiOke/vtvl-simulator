using System;

namespace DSTU.VTVL.PhysicsCore
{
    /// <summary>
    /// Модель аэродинамических сил и коэффициентов лобового сопротивления Cd(Mach).
    /// Учитывает дозвуковой диапазон, трансзвуковой кризис (волновое сопротивление),
    /// сверхзвуковой спад и раскрытие решётчатых рулей (Grid Fins).
    /// </summary>
    public static class AerodynamicsModel
    {
        /// <summary>
        /// Возвращает коэффициент лобового сопротивления Cd ракеты в зависимости от числа Маха.
        /// Калибровано по исходному файлу 'числа маха.csv' и аэродинамическим расчётам КР-1.
        /// </summary>
        /// <param name="mach">Число Маха (M = V / a)</param>
        // Опорная сетка коэффициентов сопротивления Cd(Mach) от ИСС-3 (27.09.2026)
        private static readonly double[] MachNodes = {
            0.0, 0.2, 0.4, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.2, 1.3, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0
        };

        private static readonly double[] CdNodes = {
            0.42, 0.40, 0.39, 0.39, 0.40, 0.46, 0.62, 0.78, 0.74, 0.62, 0.55, 0.49, 0.45, 0.43, 0.42, 0.415, 0.41, 0.408, 0.405, 0.403, 0.40
        };

        /// <summary>
        /// Возвращает коэффициент лобового сопротивления Cd ракеты в зависимости от числа Маха.
        /// Точная табличная интерполяция по официальному датасету ИСС-3.
        /// </summary>
        /// <param name="mach">Число Маха (M = V / a)</param>
        public static double GetDragCoefficient(double mach)
        {
            if (mach <= MachNodes[0]) return CdNodes[0];
            int n = MachNodes.Length;
            if (mach >= MachNodes[n - 1]) return CdNodes[n - 1];

            // Табличный линейный поиск и интерполяция
            for (int i = 0; i < n - 1; i++)
            {
                if (mach >= MachNodes[i] && mach <= MachNodes[i + 1])
                {
                    double t = (mach - MachNodes[i]) / (MachNodes[i + 1] - MachNodes[i]);
                    return CdNodes[i] + t * (CdNodes[i + 1] - CdNodes[i]);
                }
            }

            return CdNodes[n - 1];
        }

        /// <summary>
        /// Расчёт вектора силы аэродинамического сопротивления F_drag (Н).
        /// </summary>
        /// <param name="vx">Горизонтальная скорость (м/с)</param>
        /// <param name="vy">Вертикальная скорость (м/с)</param>
        /// <param name="altitude">Высота над уровнем моря (м)</param>
        /// <param name="isReentryMode">Флаг спуска кормой вперёд с решётчатыми рулями</param>
        /// <param name="outMach">Выходное число Маха</param>
        /// <param name="outQ">Выходной скоростной напор (Па)</param>
        /// <param name="fDragX">Выходная сила сопротивления по оси X (Н)</param>
        /// <param name="fDragY">Выходная сила сопротивления по оси Y (Н)</param>
        public static void ComputeDragForce(
            double vx, double vy, double altitude, bool isReentryMode,
            out double outMach, out double outQ,
            out double fDragX, out double fDragY)
        {
            var atm = AtmosphereGOST4401.GetData(altitude);
            double v = Math.Sqrt(vx * vx + vy * vy);

            outMach = atm.SoundSpeed > 0.0 ? v / atm.SoundSpeed : 0.0;
            outQ = 0.5 * atm.Density * v * v;

            if (v < 1e-5 || atm.Density < 1e-12)
            {
                fDragX = 0.0;
                fDragY = 0.0;
                return;
            }

            // Выбор площади и Cd:
            // При входе в атмосферу кормой вперёд ракета представляет собой "поршень"
            // с раскрытыми решётчатыми рулями (площадь 16 м² и Cd ~ 1.1)
            double cd = GetDragCoefficient(outMach);
            double effectiveArea = RocketParameters.CrossSectionArea;
            double effectiveCd = cd;

            if (isReentryMode && vy < 0.0 && altitude < 70000.0)
            {
                effectiveArea = RocketParameters.BroadsideReentryArea;
                effectiveCd = 1.10;
            }

            double totalDragForce = outQ * effectiveArea * effectiveCd;

            // Вектор силы сопротивления направлен строго противоположно вектору скорости
            fDragX = -totalDragForce * (vx / v);
            fDragY = -totalDragForce * (vy / v);
        }

        // -------------------------------------------------------------
        // Аэродинамический нагрев обшивки TPS (ИСС-4, Саттон-Грейвс)
        // -------------------------------------------------------------
        /// <summary>Константа Саттона-Грейвса для воздушной среды Земли (Вт/(м²·(кг/м³)^0.5·(м/с)³))</summary>
        public const double SuttonGravesK = 1.7415e-4;

        /// <summary>Постоянная Стефана-Больцмана sigma (Вт/(м²·К⁴))</summary>
        public const double StefanBoltzmannSigma = 5.670374e-8;

        /// <summary>Степень черноты плитки теплозащиты TPS epsilon</summary>
        public const double TpsEmissivity = 0.85;

        /// <summary>
        /// Расчёт конвективного теплового потока Саттона-Грейвса и равновесной температуры обшивки TPS.
        /// q_conv = k_SG * sqrt(rho / Rn) * V³
        /// T_skin = ((q_conv / (eps * sigma)) + T_atm⁴)^0.25 - 273.15
        /// </summary>
        /// <param name="density">Локальная плотность воздуха (кг/м³)</param>
        /// <param name="velocity">Полная скорость полета аппарата (м/с)</param>
        /// <param name="noseRadius">Эффективный радиус затупления носовой части (м)</param>
        /// <param name="ambientTempKelvin">Температура окружающего воздуха (К)</param>
        /// <param name="heatFluxWatts">Выходной тепловой поток (Вт/м²)</param>
        /// <param name="skinTempCelsius">Выходная температура поверхности плитки (°C)</param>
        public static void ComputeAeroHeating(
            double density, double velocity, double noseRadius, double ambientTempKelvin,
            out double heatFluxWatts, out double skinTempCelsius)
        {
            if (velocity < 10.0 || density < 1e-12 || noseRadius <= 0.0)
            {
                heatFluxWatts = 0.0;
                skinTempCelsius = Math.Max(-273.15, ambientTempKelvin - 273.15);
                return;
            }

            // Конвективный тепловой поток Саттона-Грейвса
            heatFluxWatts = SuttonGravesK * Math.Sqrt(density / noseRadius) * (velocity * velocity * velocity);

            // Равновесная температура излучения по закону Стефана-Больцмана
            double radTerm = heatFluxWatts / (TpsEmissivity * StefanBoltzmannSigma);
            double ambientPow4 = Math.Pow(Math.Max(1.0, ambientTempKelvin), 4.0);
            double tEquilibriumKelvin = Math.Pow(radTerm + ambientPow4, 0.25);

            skinTempCelsius = tEquilibriumKelvin - 273.15;
        }

        // -------------------------------------------------------------
        // Аэродинамика режима Belly-Flop и бокового маневра Cross-Range (ИСС-3)
        // -------------------------------------------------------------
        /// <summary>
        /// Расчет полной аэродинамики спуска в режиме Belly-Flop: лобовое сопротивление и боковая подъемная сила.
        /// </summary>
        /// <param name="q">Скоростной напор (Па)</param>
        /// <param name="alphaDeg">Угол атаки (град, 60° для Belly-Flop)</param>
        /// <param name="bankAngleRad">Угол крена для бокового маневра (радианы)</param>
        /// <param name="bellyArea">Площадь проекции фюзеляжа плашмя (м²)</param>
        /// <param name="dragForce">Сила лобового сопротивления (Н)</param>
        /// <param name="liftVertical">Вертикальная составляющая подъемной силы (Н)</param>
        /// <param name="liftLateral">Боковая составляющая подъемной силы для Cross-Range (Н)</param>
        public static void ComputeBellyFlopAero(
            double q, double alphaDeg, double bankAngleRad, double bellyArea,
            out double dragForce, out double liftVertical, out double liftLateral)
        {
            // По датасету ИСС-3: при alpha=60°: Cd ~ 1.35, Cl ~ 0.75
            // При сбалансированном угле планирования alpha=42°: Cd ~ 0.88, Cl ~ 0.75 (L/D = 0.85)
            double cd = 1.35;
            double cl = 0.75;

            if (alphaDeg <= 45.0)
            {
                cd = 0.8824;
                cl = 0.75;
            }

            dragForce = q * bellyArea * cd;
            double totalLift = q * bellyArea * cl;

            // Разложение подъемной силы вектором крена
            liftVertical = totalLift * Math.Cos(bankAngleRad);
            liftLateral = totalLift * Math.Sin(bankAngleRad);
        }
    }
}
