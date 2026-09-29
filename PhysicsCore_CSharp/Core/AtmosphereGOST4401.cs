using System;

namespace DSTU.VTVL.PhysicsCore
{
    /// <summary>
    /// Данные параметров атмосферы на заданной геометрической высоте.
    /// </summary>
    public struct AtmosphereData
    {
        /// <summary>Абсолютная температура воздуха T (Кельвины)</summary>
        public double Temperature;

        /// <summary>Статическое давление воздуха P (Па)</summary>
        public double Pressure;

        /// <summary>Плотность воздуха rho (кг/м³)</summary>
        public double Density;

        /// <summary>Локальная скорость звука a = sqrt(gamma * R * T) (м/с)</summary>
        public double SoundSpeed;
    }

    /// <summary>
    /// Стандартная атмосфера ГОСТ 4401-81 (высоты 0 .. 120 000 м).
    /// Реализует барометрическую экспоненциальную и полиномиальную интерполяцию
    /// параметров состояния газа между табличными реперными узлами.
    /// </summary>
    public static class AtmosphereGOST4401
    {
        private struct IsaPoint
        {
            public double Altitude;
            public double Temperature;
            public double Pressure;
            public double Density;
            public double SoundSpeed;

            public IsaPoint(double h, double t, double p, double rho, double a)
            {
                Altitude = h;
                Temperature = t;
                Pressure = p;
                Density = rho;
                SoundSpeed = a;
            }
        }

        // Опорная таблица параметров стандартной атмосферы ГОСТ 4401-81
        private static readonly IsaPoint[] Table = new IsaPoint[]
        {
            new IsaPoint(0.0,      288.15, 101325.0, 1.225000, 340.29),
            new IsaPoint(5000.0,   255.68, 54048.3,  0.736400, 320.55),
            new IsaPoint(10000.0,  223.25, 26499.9,  0.413500, 299.53),
            new IsaPoint(15000.0,  216.65, 12111.8,  0.194800, 295.07),
            new IsaPoint(20000.0,  216.65, 5529.3,   0.088900, 295.07),
            new IsaPoint(25000.0,  221.55, 2549.2,   0.040100, 298.39),
            new IsaPoint(30000.0,  226.51, 1197.0,   0.018400, 301.71),
            new IsaPoint(40000.0,  250.35, 287.14,   0.004000, 317.19),
            new IsaPoint(50000.0,  270.65, 79.78,    0.001030, 329.80),
            new IsaPoint(60000.0,  247.02, 21.96,    0.000310, 315.07),
            new IsaPoint(70000.0,  219.59, 5.22,     8.28e-05, 297.06),
            new IsaPoint(80000.0,  198.64, 1.05,     1.85e-05, 282.54),
            new IsaPoint(90000.0,  186.65, 0.183,    3.42e-06, 273.88),
            new IsaPoint(100000.0, 196.60, 0.0319,   5.55e-07, 281.09),
            new IsaPoint(120000.0, 334.42, 0.00267,  2.44e-08, 366.60)
        };

        /// <summary>
        /// Возвращает параметры стандартной атмосферы на геометрической высоте h над уровнем моря.
        /// </summary>
        /// <param name="altitude">Геометрическая высота (м)</param>
        public static AtmosphereData GetData(double altitude)
        {
            // Ниже уровня моря
            if (altitude <= 0.0)
            {
                var basePt = Table[0];
                return new AtmosphereData
                {
                    Temperature = basePt.Temperature,
                    Pressure = basePt.Pressure,
                    Density = basePt.Density,
                    SoundSpeed = basePt.SoundSpeed
                };
            }

            // Выше 120 км — глубокий космический вакуум
            if (altitude >= 120000.0)
            {
                return new AtmosphereData
                {
                    Temperature = 350.0,
                    Pressure = 0.0,
                    Density = 0.0,
                    SoundSpeed = 370.0
                };
            }

            // Поиск интервала
            for (int i = 0; i < Table.Length - 1; i++)
            {
                var p1 = Table[i];
                var p2 = Table[i + 1];

                if (altitude >= p1.Altitude && altitude <= p2.Altitude)
                {
                    double fraction = (altitude - p1.Altitude) / (p2.Altitude - p1.Altitude);

                    // Линейная интерполяция температуры и скорости звука
                    double T = p1.Temperature + (p2.Temperature - p1.Temperature) * fraction;
                    double a = p1.SoundSpeed + (p2.SoundSpeed - p1.SoundSpeed) * fraction;

                    // Барометрическая логарифмическая интерполяция давления и плотности
                    double P = p1.Pressure * Math.Pow(p2.Pressure / p1.Pressure, fraction);
                    double rho = p1.Density * Math.Pow(Math.Max(p2.Density, 1e-12) / Math.Max(p1.Density, 1e-12), fraction);

                    return new AtmosphereData
                    {
                        Temperature = T,
                        Pressure = P,
                        Density = rho,
                        SoundSpeed = a
                    };
                }
            }

            // Запасной возврат (вакуум)
            return new AtmosphereData { Temperature = 288.15, Pressure = 0.0, Density = 0.0, SoundSpeed = 340.0 };
        }
    }
}
