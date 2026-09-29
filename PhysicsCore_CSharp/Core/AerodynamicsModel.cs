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

            // Вектор силы направлен строго противоположно вектору скорости
            fDragX = -totalDragForce * (vx / v);
            fDragY = -totalDragForce * (vy / v);
        }
    }
}
