using System;

namespace DSTU.VTVL.PhysicsCore
{
    /// <summary>
    /// Вектор состояния задачи Коши (14-DOF State Vector) для ракеты-носителя.
    /// Полностью описывает кинематику, динамику вращения, массу и топливный остаток.
    /// </summary>
    [Serializable]
    public class RocketState
    {
        // -------------------------------------------------------------
        // Время интегрирования
        // -------------------------------------------------------------
        /// <summary>Текущее время симуляции t от старта (с)</summary>
        public double Time;

        // -------------------------------------------------------------
        // 1-3. Позиция центра масс в стартовой инерциальной СК (м)
        // -------------------------------------------------------------
        /// <summary>Дальность по горизонтали X (м)</summary>
        public double PosX;

        /// <summary>Высота над уровнем моря Y (м)</summary>
        public double PosY;

        /// <summary>Боковое смещение Z (м)</summary>
        public double PosZ;

        // -------------------------------------------------------------
        // 4-6. Линейные скорости центра масс (м/с)
        // -------------------------------------------------------------
        /// <summary>Горизонтальная скорость Vx (м/с)</summary>
        public double VelX;

        /// <summary>Вертикальная скорость Vy (м/с)</summary>
        public double VelY;

        /// <summary>Боковая скорость Vz (м/с)</summary>
        public double VelZ;

        // -------------------------------------------------------------
        // 7-9. Углы ориентации корпуса (радианы)
        // -------------------------------------------------------------
        /// <summary>Угол тангажа Theta (радианы, 90° = pi/2 = вертикально вверх)</summary>
        public double Pitch;

        /// <summary>Угол рыскания Psi (радианы)</summary>
        public double Yaw;

        /// <summary>Угол крена Gamma (радианы)</summary>
        public double Roll;

        // -------------------------------------------------------------
        // 10-12. Угловые скорости вращения вокруг связанных осей (рад/с)
        // -------------------------------------------------------------
        /// <summary>Угловая скорость по тангажу Omega_z (рад/с)</summary>
        public double AngularVelPitch;

        /// <summary>Угловая скорость по рысканию Omega_y (рад/с)</summary>
        public double AngularVelYaw;

        /// <summary>Угловая скорость по крену Omega_x (рад/с)</summary>
        public double AngularVelRoll;

        // -------------------------------------------------------------
        // 13-14. Переменные массы (кг)
        // -------------------------------------------------------------
        /// <summary>Полная текущая масса ракеты m = m_dry + m_fuel (кг)</summary>
        public double Mass;

        /// <summary>Остаток жидкого топлива в баках m_fuel (кг)</summary>
        public double Fuel;

        // -------------------------------------------------------------
        // Вычисляемые параметры телеметрии и исполнительных органов
        // -------------------------------------------------------------
        /// <summary>Текущий уровень дросселя [0.0 .. 1.0]</summary>
        public double Throttle;

        /// <summary>Текущая ощущаемая перегрузка (G)</summary>
        public double GForce;

        /// <summary>Полное кинематическое ускорение ступени |a| (м/с²)</summary>
        public double KinematicAccel;

        /// <summary>Текущее число Маха полета (M)</summary>
        public double MachNumber;
        public double Mach => MachNumber;

        /// <summary>Скоростной напор q = 0.5 * rho * v² (Па)</summary>
        public double DynamicPressure;

        /// <summary>Текущая сила тяги двигателя (Н)</summary>
        public double ThrustForce;

        /// <summary>Угол качания сопла двигателя TVC (градусы, [-7°..+7°])</summary>
        public double GimbalAngleDegrees;

        /// <summary>Текущая сила аэродинамического сопротивления (Н)</summary>
        public double DragForce;

        /// <summary>Флаг: оторвалась ли ракета от стартового стола</summary>
        public bool HasLiftoff;

        /// <summary>Флаг: совершена ли успешная посадка на баржу</summary>
        public bool IsLanded;

        /// <summary>Флаг: потерпела ли ракета крушение</summary>
        public bool IsCrashed;

        /// <summary>Причина крушения при отказе</summary>
        public string CrashReason;

        /// <summary>Максимальная достигнутая высота апогея (м)</summary>
        public double MaxAltitude;

        /// <summary>Флаг активности маневровых сопел RCS (холодный газ)</summary>
        public bool IsRcsActive;

        /// <summary>Флаг раскрытия решётчатых рулей (Grid Fins)</summary>
        public bool IsGridFinsActive;

        /// <summary>Флаг активности тормозного импульса посадки (Hoverslam)</summary>
        public bool IsHoverslamActive;

        /// <summary>Название текущей фазы полёта</summary>
        public string FlightPhase;

        public RocketState()
        {
            Reset();
        }

        /// <summary>
        /// Сброс состояния в начальные стартовые условия на столе Pad A.
        /// </summary>
        public void Reset()
        {
            Time = 0.0;
            PosX = 0.0;
            PosY = 0.0;
            PosZ = 0.0;
            VelX = 0.0;
            VelY = 0.0;
            VelZ = 0.0;

            // Вертикальное положение носом вверх (90 градусов)
            Pitch = Math.PI / 2.0;
            Yaw = 0.0;
            Roll = 0.0;

            AngularVelPitch = 0.0;
            AngularVelYaw = 0.0;
            AngularVelRoll = 0.0;

            Mass = RocketParameters.TotalMassInitial;
            Fuel = RocketParameters.PropellantMassInitial;

            Throttle = 0.0;
            GForce = 1.0;
            MachNumber = 0.0;
            DynamicPressure = 0.0;
            ThrustForce = 0.0;
            DragForce = 0.0;

            HasLiftoff = false;
            IsLanded = false;
            IsCrashed = false;
            CrashReason = string.Empty;
            MaxAltitude = 0.0;

            IsRcsActive = false;
            IsGridFinsActive = false;
            IsHoverslamActive = false;
            FlightPhase = "СТАРТОВЫЙ СТОЛ A";
        }

        /// <summary>
        /// Глубокое клонирование вектора состояния (необходимо для шагов метода Рунге-Кутты RK4).
        /// </summary>
        public RocketState Clone()
        {
            return (RocketState)this.MemberwiseClone();
        }

        /// <summary>Полная скорость центра масс |V| (м/с)</summary>
        public double TotalVelocity => Math.Sqrt(VelX * VelX + VelY * VelY + VelZ * VelZ);
        public double TotalSpeed => TotalVelocity;

        /// <summary>Угловая скорость по тангажу (рад/с)</summary>
        public double AngularVelocity => AngularVelPitch;

        /// <summary>Угол тангажа в градусах [0..180]</summary>
        public double PitchDegrees => (Pitch * 180.0) / Math.PI;

        /// <summary>Остаток топлива в процентах [0..100%]</summary>
        public double FuelPercent => Math.Max(0.0, Math.Min(100.0, (Fuel / RocketParameters.PropellantMassInitial) * 100.0));

        /// <summary>Текущая тяговооруженность TWR = T / (m * g0)</summary>
        public double CurrentTWR => ThrustForce / (Mass * RocketParameters.G0);

        /// <summary>Текущий продольный момент инерции Jx(m) (кг·м²)</summary>
        public double CurrentInertiaRoll => RocketParameters.GetInertiaRoll(Mass);

        /// <summary>Текущий поперечный момент инерции Jy(m) = Jz(m) (кг·м²)</summary>
        public double CurrentInertiaPitch => RocketParameters.GetInertiaPitch(Mass);

        /// <summary>Текущее положение центра масс x_цм(m) от сопла (м)</summary>
        public double CurrentCenterOfMass => RocketParameters.GetCenterOfMass(Mass);
    }
}
