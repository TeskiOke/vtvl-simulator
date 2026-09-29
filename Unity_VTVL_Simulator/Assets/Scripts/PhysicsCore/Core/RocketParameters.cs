using System;

namespace DSTU.VTVL.PhysicsCore
{
    /// <summary>
    /// Физические константы и паспортные характеристики ракеты-носителя VTVL-30 (Кейс 8.1 ДГТУ).
    /// Данные верифицированы по регламенту КР-1, КР-3 и ГОСТ 4401-81.
    /// </summary>
    public static class RocketParameters
    {
        // -------------------------------------------------------------
        // Планетарные константы (Земля)
        // -------------------------------------------------------------
        /// <summary>Стандартное ускорение свободного падения на уровне моря (м/с²)</summary>
        public const double G0 = 9.80665;

        /// <summary>Экваториальный радиус Земли по WGS-84 (м)</summary>
        public const double EarthRadius = 6371000.0;

        /// <summary>Высота границы космоса (линия Кармана, м)</summary>
        public const double KarmanLine = 100000.0;

        // -------------------------------------------------------------
        // Массовые и геометрические характеристики ступени (VTVL-30)
        // -------------------------------------------------------------
        /// <summary>Полная стартовая масса ступени m0 (кг)</summary>
        public const double TotalMassInitial = 30000.0;

        /// <summary>Сухая масса конструкции ступени m_dry (кг)</summary>
        public const double DryMass = 10000.0;

        /// <summary>Начальная масса заправленного топлива m_prop (кг)</summary>
        public const double PropellantMassInitial = 20000.0;
        public const double FuelMass0 = PropellantMassInitial;

        /// <summary>Внешний диаметр цилиндрической части корпуса (м)</summary>
        public const double Diameter = 1.80;

        /// <summary>Площадь поперечного сечения (мидель) КР-3 (м²)</summary>
        public const double CrossSectionArea = 2.55;

        /// <summary>Эффективная площадь торможения при спуске кормой вперед с решётчатыми рулями (м²)</summary>
        public const double BroadsideReentryArea = 16.0;

        /// <summary>Длина ступени (м)</summary>
        public const double Length = 22.0;

        // -------------------------------------------------------------
        // Двигательная установка (ДУ)
        // -------------------------------------------------------------
        /// <summary>Максимальная тяга двигателя T_max (Н)</summary>
        public const double ThrustMax = 450000.0; // 450 кН

        /// <summary>Минимальный уровень дросселирования двигателя (40%)</summary>
        public const double ThrottleMin = 0.40;

        /// <summary>Удельный импульс тяги в пустоте / на уровне моря Isp (с)</summary>
        public const double SpecificImpulse = 300.0;

        /// <summary>Секундный массовый расход топлива при 100% тяге q_m = T / (Isp * g0) (кг/с)</summary>
        public static readonly double MassFlowRateMax = ThrustMax / (SpecificImpulse * G0); // ~152.957432 кг/с

        // -------------------------------------------------------------
        // Навигационные координаты миссии (A -> C -> B)
        // -------------------------------------------------------------
        /// <summary>Координата X стартового стола Pad A (м)</summary>
        public const double PadA_X = 0.0;

        /// <summary>Координата X целевой посадочной баржи ASDS Pad B (м)</summary>
        public const double PadB_X = 25270.0; // 25.27 км по дальности

        /// <summary>Максимально допустимая вертикальная скорость касания (м/с) по регламенту Normal</summary>
        public const double MaxTouchdownVy = 3.5;

        /// <summary>Идеальная скорость мягкой посадки (м/с) по регламенту Normal</summary>
        public const double TargetTouchdownVy = 2.0;

        /// <summary>Максимально допустимая перегрузка конструкции ступени (G)</summary>
        public const double MaxStructuralGForce = 6.0;

        // -------------------------------------------------------------
        // Моменты инерции и центр масс (ИСС-1 / КР-3)
        // -------------------------------------------------------------
        /// <summary>
        /// Продольный момент инерции Jx(m) (крен / roll): Jx = 0.5 * m * R² (кг·м²).
        /// </summary>
        public static double GetInertiaRoll(double mass)
        {
            double r = Diameter / 2.0;
            return 0.5 * mass * (r * r);
        }

        /// <summary>
        /// Экваториальный момент инерции Jy(m) = Jz(m) (тангаж / рыскание): Jy = 1/12 * m * (3*R² + L²) (кг·м²).
        /// </summary>
        public static double GetInertiaPitch(double mass)
        {
            double r = Diameter / 2.0;
            return (1.0 / 12.0) * mass * (3.0 * (r * r) + (Length * Length));
        }

        /// <summary>
        /// Положение центра масс x_цм(m) от донного среза двигателя (м).
        /// 11.0 м (старт m0) -> 7.5 м (пустая mdry).
        /// </summary>
        public static double GetCenterOfMass(double mass)
        {
            double massFuel = Math.Max(0.0, mass - DryMass);
            return 7.5 + (massFuel / PropellantMassInitial) * 3.5;
        }

        // -------------------------------------------------------------
        // Ограничения исполнительных органов TVC и аэродинамики (КР-1, Раздел 3.3.1)
        // -------------------------------------------------------------
        /// <summary>Максимальный угол качания сопла δ_max (град)</summary>
        public const double TvcMaxGimbalAngleDeg = 7.0;

        /// <summary>Максимальный угол качания сопла δ_max (рад)</summary>
        public const double TvcMaxGimbalAngleRad = TvcMaxGimbalAngleDeg * Math.PI / 180.0; // ~0.12217 рад

        /// <summary>Предельная скорость перекладки сопла δ_dot_max (град/с)</summary>
        public const double TvcMaxSlewRateDegPerSec = 15.0;

        /// <summary>Предельная скорость перекладки сопла δ_dot_max (рад/с)</summary>
        public const double TvcMaxSlewRateRadPerSec = TvcMaxSlewRateDegPerSec * Math.PI / 180.0; // ~0.2618 рад/с

        /// <summary>Постоянная времени привода рулевой машины T_пр (с)</summary>
        public const double TvcActuatorTimeConstant = 0.05;

        /// <summary>Производная момента демпфирования тангажа m_z^(omega_z) < 0</summary>
        public const double AerodynamicDampingMzWz = -0.20;

        /// <summary>Гарантированный посадочный резерв топлива P_L (кг) по КР-3 v3</summary>
        public const double MinLandingFuelReserve = 3500.0;

        // -------------------------------------------------------------
        // Динамика зажигания и переходных процессов ДУ (КР-3 v3, 29.09.2026)
        // -------------------------------------------------------------
        /// <summary>Чистая задержка зажигания без полезной тяги tau_ign (с)</summary>
        public const double EngineIgnitionDelay = 1.5;

        /// <summary>Время линейного набора тяги от нуля до заданного уровня tau_r (с)</summary>
        public const double EngineRampUpTime = 1.0;

        /// <summary>Полное время выхода двигателя на режим тяги tau_tot = tau_ign + tau_r (с)</summary>
        public const double EngineTotalIgnitionTime = EngineIgnitionDelay + EngineRampUpTime; // 2.5 с

        // -------------------------------------------------------------
        // Механические параметры посадочных опор (ИСС-4, Регламент 1.0)
        // -------------------------------------------------------------
        /// <summary>Количество посадочных опор (шт)</summary>
        public const int LegCount = 4;

        /// <summary>Максимальный ход штока амортизатора Delta_s_leg (м)</summary>
        public const double LegMaxStroke = 1.20;

        /// <summary>Жёсткость одного амортизатора K_leg (Н/м)</summary>
        public const double LegSpringStiffness = 80000.0;

        /// <summary>Коэффициент вязкого затухания одного амортизатора C_leg (Н·с/м)</summary>
        public const double LegDampingCoefficient = 30000.0;

        /// <summary>Радиус базы опорного контура R_stance (м)</summary>
        public const double LegStanceRadius = 5.0;

        /// <summary>Критическая вертикальная скорость смыкания опор до упора (м/с)</summary>
        public const double LegBottomingOutVelocity = 6.78;

        /// <summary>Максимально допустимый безопасный угол наклона корпуса при касании (град)</summary>
        public const double MaxSafeTouchdownTiltDeg = 5.0;

        /// <summary>Максимально допустимая безопасная горизонтальная скорость сноса (м/с)</summary>
        public const double MaxSafeHorizontalVelocity = 0.50;
    }
}
