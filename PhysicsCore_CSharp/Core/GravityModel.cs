using System;

namespace DSTU.VTVL.PhysicsCore
{
    /// <summary>
    /// Закон всемирного тяготения Ньютона в центральном гравитационном поле Земли.
    /// </summary>
    public static class GravityModel
    {
        /// <summary>
        /// Возвращает локальное ускорение свободного падения g(h) на высоте h.
        /// g(h) = g0 * (Re / (Re + h))²
        /// </summary>
        /// <param name="altitude">Геометрическая высота над уровнем моря (м)</param>
        public static double GetGravity(double altitude)
        {
            double h = Math.Max(0.0, altitude);
            double ratio = RocketParameters.EarthRadius / (RocketParameters.EarthRadius + h);
            return RocketParameters.G0 * ratio * ratio;
        }

        /// <summary>
        /// Сила тяжести F_gravity, действующая на массу m. Направлена вниз к центру Земли.
        /// </summary>
        public static double GetGravityForce(double mass, double altitude)
        {
            return mass * GetGravity(altitude);
        }

        /// <summary>Вторая зональная гармоника геоида J2 (паспорт поля КР-2)</summary>
        public const double J2 = 1.08263e-3;

        /// <summary>Гравитационный параметр Земли mu = G * M (м³/с²)</summary>
        public const double EarthMu = 3.986004418e14;

        /// <summary>
        /// Расчет компонент гравитационного ускорения с учетом гармоники J2 (нерадиусного распределения массы геоида).
        /// </summary>
        /// <param name="altitude">Геометрическая высота (м)</param>
        /// <param name="latitudeRad">Геоцентрическая широта (радианы)</param>
        /// <param name="aRadial">Радиальное ускорение, направленное к центру (м/с²)</param>
        /// <param name="aPolar">Полярное (меридиональное) ускорение (м/с²)</param>
        public static void GetGravityJ2(double altitude, double latitudeRad, out double aRadial, out double aPolar)
        {
            double r = RocketParameters.EarthRadius + Math.Max(0.0, altitude);
            double sinLat = Math.Sin(latitudeRad);
            double cosLat = Math.Cos(latitudeRad);
            double reOverR = RocketParameters.EarthRadius / r;
            double factor = 1.5 * J2 * reOverR * reOverR;

            // Радиальная компонента: a_r = -(mu/r²) * [1 - 1.5*J2*(Re/r)² * (3*sin²phi - 1)]
            aRadial = -(EarthMu / (r * r)) * (1.0 - factor * (3.0 * sinLat * sinLat - 1.0));

            // Меридиональная компонента: a_phi = -(3*mu*J2*Re² / r⁴) * sin(phi) * cos(phi)
            aPolar = -(3.0 * EarthMu * J2 * RocketParameters.EarthRadius * RocketParameters.EarthRadius / (r * r * r * r)) * sinLat * cosLat;
        }

        /// <summary>
        /// Скорость вековой прецессии долготы восходящего узла орбиты под действием J2 (рад/с) по КР-2.
        /// dOmega/dt = -1.5 * J2 * (Re / p)² * n * cos(i)
        /// </summary>
        public static double GetNodalPrecessionRate(double semiMajorAxis, double eccentricity, double inclinationRad)
        {
            double p = semiMajorAxis * (1.0 - eccentricity * eccentricity);
            double meanMotion = Math.Sqrt(EarthMu / (semiMajorAxis * semiMajorAxis * semiMajorAxis));
            double ratio = RocketParameters.EarthRadius / p;
            return -1.5 * J2 * ratio * ratio * meanMotion * Math.Cos(inclinationRad);
        }

        // -------------------------------------------------------------
        // Силы инерции во вращающейся системе координат Земли (КР-1 / ИТ-1)
        // -------------------------------------------------------------
        /// <summary>Угловая скорость суточного вращения Земли omega_earth (рад/с)</summary>
        public const double EarthAngularVelocity = 7.292115e-5;

        /// <summary>
        /// Расчет кориолисова ускорения a_cor = -2 * (omega_earth x v) в топоцентрической СК (X-восток, Y-вверх, Z-север).
        /// Вектор суточного вращения Земли: omega = (0, omega*sin(phi), omega*cos(phi)).
        /// a_cor,x = 2 * omega * (sin(phi) * vz - cos(phi) * vy)
        /// a_cor,y = 2 * omega * cos(phi) * vx  (эффект Этвеша)
        /// a_cor,z = -2 * omega * sin(phi) * vx (отклонение вправо в Сев. полушарии)
        /// </summary>
        /// <param name="vx">Восточная скорость (м/с)</param>
        /// <param name="vy">Вертикальная скорость (м/с)</param>
        /// <param name="vz">Северная скорость (м/с)</param>
        /// <param name="latitudeRad">Географическая широта точки старта/полета (радианы)</param>
        /// <param name="aCorX">Кориолисово ускорение на восток (м/с²)</param>
        /// <param name="aCorY">Кориолисово ускорение по вертикали (м/с²)</param>
        /// <param name="aCorZ">Кориолисово ускорение на север (м/с²)</param>
        public static void ComputeCoriolisAcceleration(
            double vx, double vy, double vz, double latitudeRad,
            out double aCorX, out double aCorY, out double aCorZ)
        {
            double sinLat = Math.Sin(latitudeRad);
            double cosLat = Math.Cos(latitudeRad);

            aCorX = 2.0 * EarthAngularVelocity * (sinLat * vz - cosLat * vy);
            aCorY = 2.0 * EarthAngularVelocity * cosLat * vx;
            aCorZ = -2.0 * EarthAngularVelocity * sinLat * vx;
        }

        /// <summary>
        /// Расчет центробежного ускорения переносного вращения Земли a_cf = - omega x (omega x r).
        /// </summary>
        /// <param name="altitude">Высота над уровнем моря (м)</param>
        /// <param name="latitudeRad">Географическая широта (радианы)</param>
        /// <param name="aCfX">Ускорение по X (0)</param>
        /// <param name="aCfY">Ускорение по вертикали вверх (м/с²)</param>
        /// <param name="aCfZ">Ускорение к экватору (м/с²)</param>
        public static void ComputeCentrifugalAcceleration(
            double altitude, double latitudeRad,
            out double aCfX, out double aCfY, out double aCfZ)
        {
            double r = RocketParameters.EarthRadius + Math.Max(0.0, altitude);
            double sinLat = Math.Sin(latitudeRad);
            double cosLat = Math.Cos(latitudeRad);
            double omega2R = EarthAngularVelocity * EarthAngularVelocity * r;

            aCfX = 0.0;
            aCfY = omega2R * cosLat * cosLat;
            aCfZ = -omega2R * sinLat * cosLat;
        }
    }
}
