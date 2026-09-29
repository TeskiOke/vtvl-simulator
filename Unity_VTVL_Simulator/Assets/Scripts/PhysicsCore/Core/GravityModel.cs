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

        /// <summary>
        /// Высотный градиент гравитационного ускорения dg/dh (с^-2) по отчету КР-2.
        /// gamma(h) = -2 * g(h) / (Re + h)
        /// </summary>
        public static double GetGravityGradient(double altitude)
        {
            double h = Math.Max(0.0, altitude);
            return -2.0 * GetGravity(h) / (RocketParameters.EarthRadius + h);
        }
    }
}
