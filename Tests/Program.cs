using System;
using DSTU.VTVL.PhysicsCore;
using DSTU.VTVL.Guidance;

namespace DSTU.VTVL.Tests
{
    class Program
    {
        static int _passedCount = 0;
        static int _failedCount = 0;

        static void AssertTrue(bool condition, string testName, string details = "")
        {
            if (condition)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[PASS] ");
                Console.ResetColor();
                Console.WriteLine($"{testName} {details}");
                _passedCount++;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("[FAIL] ");
                Console.ResetColor();
                Console.WriteLine($"{testName} {details}");
                _failedCount++;
            }
        }

        static int Main(string[] args)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("     🧪 АВТОМАТИЗИРОВАННЫЙ ТЕСТОВЫЙ КОМПЛЕКС ФИЗИЧЕСКОГО ЯДРА VTVL-30 / 180t     ");
            Console.WriteLine("     (ГОСТ 4401-81, J2, Кориолис, Саттон-Грейвс, Belly-Flop, Кельвин-Фойгт)      ");
            Console.WriteLine("================================================================================\n");

            TestAtmosphereGOST4401();
            TestGravityModelAndJ2();
            TestCoriolisAndCentrifugalForces();
            TestAeroHeatingSuttonGraves();
            TestBellyFlopAndAngleOfAttackLift();
            TestLandingGearContactMechanics();
            TestFullTrajectoryRK4Integration();

            Console.WriteLine("\n================================================================================");
            Console.WriteLine($"ИТОГИ ВЕРИФИКАЦИИ: Пройдено: {_passedCount} | Ошибок: {_failedCount}");
            if (_failedCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✅ ВСЕ ФИЗИЧЕСКИЕ ТЕСТЫ УСПЕШНО ПРОЙДЕНЫ! ФИЗИКА МИРА СООТВЕТСТВУЕТ ТЗ.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ ИМЕЮТСЯ ПРОВАЛЕННЫЕ ТЕСТЫ.");
                Console.ResetColor();
            }
            Console.WriteLine("================================================================================");

            return _failedCount == 0 ? 0 : 1;
        }

        static void TestAtmosphereGOST4401()
        {
            Console.WriteLine("--- 1. Тестирование расширенной атмосферы ГОСТ 4401-81 (0 .. 250 км) ---");

            var seaLevel = AtmosphereGOST4401.GetData(0.0);
            AssertTrue(Math.Abs(seaLevel.Density - 1.225) < 1e-3, "Плотность на уровне моря", $"rho = {seaLevel.Density:F4} кг/м³ (ожидалось 1.225)");
            AssertTrue(Math.Abs(seaLevel.Temperature - 288.15) < 1e-2, "Температура на уровне моря", $"T = {seaLevel.Temperature:F2} K (ожидалось 288.15 K)");
            AssertTrue(Math.Abs(seaLevel.SoundSpeed - 340.29) < 0.5, "Скорость звука на уровне моря", $"a = {seaLevel.SoundSpeed:F2} м/с (ожидалось 340.29)");

            var karman = AtmosphereGOST4401.GetData(100000.0);
            AssertTrue(karman.Density < 1e-5 && karman.Density > 1e-7, "Плотность на линии Кармана (100 км)", $"rho = {karman.Density:E3} кг/м³");

            var thermosphere = AtmosphereGOST4401.GetData(250000.0);
            AssertTrue(thermosphere.Temperature > 900.0, "Термосферный разогрев на 250 км", $"T = {thermosphere.Temperature:F1} K (> 900 K)");
            AssertTrue(thermosphere.Density < 1e-10, "Плотность на орбите 250 км (вакуум)", $"rho = {thermosphere.Density:E3} кг/м³");
            Console.WriteLine();
        }

        static void TestGravityModelAndJ2()
        {
            Console.WriteLine("--- 2. Тестирование гравитации Ньютона и зональной гармоники J2 ---");

            double g0 = GravityModel.GetGravity(0.0);
            AssertTrue(Math.Abs(g0 - 9.80665) < 1e-4, "Ускорение g на уровне моря", $"g = {g0:F5} м/с²");

            double g120 = GravityModel.GetGravity(120000.0);
            AssertTrue(Math.Abs(g120 - 9.447) < 0.05, "Ускорение g в апогее (120 км)", $"g = {g120:F4} м/с² (теория ~9.447)");

            double aRadial, aPolar;
            GravityModel.GetGravityJ2(120000.0, 45.0 * Math.PI / 180.0, out aRadial, out aPolar);
            AssertTrue(Math.Abs(aPolar) > 0.005 && Math.Abs(aPolar) < 0.03, "Полярное ускорение J2 на широте 45°", $"a_phi = {aPolar:F4} м/с²");

            double precRate = GravityModel.GetNodalPrecessionRate(RocketParameters.EarthRadius + 250000.0, 0.001, 51.6 * Math.PI / 180.0);
            double precDegDay = precRate * (180.0 / Math.PI) * 86400.0;
            AssertTrue(precDegDay < -3.0 && precDegDay > -8.0, "Вековая прецессия долготы восходящего узла (ISS 51.6°)", $"dOmega/dt = {precDegDay:F2} град/сутки (~ -5°)");
            Console.WriteLine();
        }

        static void TestCoriolisAndCentrifugalForces()
        {
            Console.WriteLine("--- 3. Тестирование сил инерции вращения Земли (Кориолис и центробежная) ---");

            double latRad = 45.0 * Math.PI / 180.0;
            double aCorX, aCorY, aCorZ;

            // Движение на восток (vx = 500 м/с): эффект Этвеша дает направленное вверх ускорение
            GravityModel.ComputeCoriolisAcceleration(500.0, 0.0, 0.0, latRad, out aCorX, out aCorY, out aCorZ);
            AssertTrue(aCorY > 0.0, "Эффект Этвеша (полет на восток -> подъем вверх)", $"a_cor,y = {aCorY:F4} м/с² > 0");
            AssertTrue(aCorZ < 0.0, "Отклонение вправо в Сев. полушарии (полет на восток -> смещение к югу)", $"a_cor,z = {aCorZ:F4} м/с² < 0");

            // Движение вертикально вверх (vy = 1000 м/с): смещение на запад
            GravityModel.ComputeCoriolisAcceleration(0.0, 1000.0, 0.0, latRad, out aCorX, out aCorY, out aCorZ);
            AssertTrue(aCorX < 0.0, "Кориолисово отклонение набора высоты (на запад, отрицательный X)", $"a_cor,x = {aCorX:F4} м/с² < 0");

            double aCfX, aCfY, aCfZ;
            GravityModel.ComputeCentrifugalAcceleration(0.0, latRad, out aCfX, out aCfY, out aCfZ);
            AssertTrue(aCfY > 0.015 && aCfY < 0.025, "Центробежное ускорение Земли на уровне моря (вертикаль)", $"a_cf,y = {aCfY:F4} м/с² (~0.017)");
            Console.WriteLine();
        }

        static void TestAeroHeatingSuttonGraves()
        {
            Console.WriteLine("--- 4. Тестирование аэродинамического нагрева Саттона-Грейвса ---");

            double heatFlux, skinTemp;
            // Скорость 1000 м/с в стратосфере (h=20 км, rho=0.0889 кг/м³, T=216.65 K)
            AerodynamicsModel.ComputeAeroHeating(0.0889, 1000.0, 0.90, 216.65, out heatFlux, out skinTemp);
            double heatFluxKW = heatFlux / 1000.0;

            AssertTrue(heatFluxKW > 40.0 && heatFluxKW < 70.0, "Тепловой поток Саттона-Грейвса при V=1000 м/с", $"q = {heatFluxKW:F1} кВт/м² (~54.7 кВт/м²)");
            AssertTrue(skinTemp > 650.0 && skinTemp < 850.0, "Равновесная температура плитки TPS (Стефан-Больцман)", $"T_skin = {skinTemp:F1} °C (~750 °C)");
            Console.WriteLine();
        }

        static void TestBellyFlopAndAngleOfAttackLift()
        {
            Console.WriteLine("--- 5. Тестирование аэродинамики Belly-Flop и подъемной силы ---");

            double dragForce, liftVert, liftLat;
            double q = 10000.0; // 10 кПа скоростной напор
            AerodynamicsModel.ComputeBellyFlopAero(q, 60.0, 0.0, RocketParameters.BellyArea, out dragForce, out liftVert, out liftLat);

            double expectedCd = 1.35;
            double expectedCl = 0.75;
            double calcCd = dragForce / (q * RocketParameters.BellyArea);
            double calcCl = liftVert / (q * RocketParameters.BellyArea);

            AssertTrue(Math.Abs(calcCd - expectedCd) < 1e-3, "Коэффициент лобового сопротивления Belly-Flop Cd (alpha=60°)", $"Cd = {calcCd:F2}");
            AssertTrue(Math.Abs(calcCl - expectedCl) < 1e-3, "Коэффициент подъемной силы Belly-Flop Cl (alpha=60°)", $"Cl = {calcCl:F2}");

            // Проверка бокового маневра Cross-Range при крене 45 градусов
            AerodynamicsModel.ComputeBellyFlopAero(q, 60.0, Math.PI / 4.0, RocketParameters.BellyArea, out dragForce, out liftVert, out liftLat);
            AssertTrue(Math.Abs(liftVert - liftLat) < 1.0, "Симметрия боковой и вертикальной подъемной силы при крене 45°", $"L_vert={liftVert:F0} Н, L_lat={liftLat:F0} Н");
            Console.WriteLine();
        }

        static void TestLandingGearContactMechanics()
        {
            Console.WriteLine("--- 6. Тестирование упруго-диссипативной модели посадочных опор (Кельвин-Фойгт) ---");

            // Идеальное мягкое касание (Vy = -1.0 м/с, снос 0.1 м/с, наклон 1.0°)
            var contactNormal = LandingGearContact.Evaluate(-0.25, -1.0, 0.1, 1.0, 13500.0);
            AssertTrue(contactNormal.IsLanded && !contactNormal.IsCrashed, "Штатная посадка Normal (Vy=1.0 м/с)", $"{contactNormal.OutcomeCategory}: {contactNormal.Message}");

            // Аварийное касание с пробоем амортизаторов (Vy = -7.5 м/с > 6.78 м/с)
            var contactHardCrash = LandingGearContact.Evaluate(-1.25, -7.5, 0.1, 1.0, 13500.0);
            AssertTrue(contactHardCrash.IsCrashed, "Авария: пробой амортизаторов (Bottoming Out)", $"{contactHardCrash.OutcomeCategory}: {contactHardCrash.Message}");

            // Аварийное опрокидывание ступени (наклон 18° > 15°)
            var contactTiltCrash = LandingGearContact.Evaluate(-0.2, -1.0, 0.1, 18.0, 13500.0);
            AssertTrue(contactTiltCrash.IsCrashed && contactTiltCrash.OutcomeCategory == "Overturned", "Авария: опрокидывание на бок при наклоне 18°", $"{contactTiltCrash.OutcomeCategory}: {contactTiltCrash.Message}");
            Console.WriteLine();
        }

        static void TestFullTrajectoryRK4Integration()
        {
            Console.WriteLine("--- 7. Сквозной интеграционный тест полной траектории полёта VTVL-30 (RK4) ---");

            RocketState state = new RocketState();
            AutopilotGNC autopilot = new AutopilotGNC();

            double dt = 0.02; // 50 Гц
            double maxHeatFlux = 0.0;
            double maxSkinTemp = 0.0;
            double maxAlt = 0.0;
            double maxSpeed = 0.0;

            while (state.Time < 600.0)
            {
                var cmd = autopilot.Update(state, dt);
                bool isReentry = (state.VelY < 0.0 && state.PosY < 70000.0);

                RK4Integrator.Step(state, dt, cmd.Throttle, state.Pitch, isReentry);

                if (state.PosY > maxAlt) maxAlt = state.PosY;
                double speed = state.TotalVelocity;
                if (speed > maxSpeed) maxSpeed = speed;
                if (state.HeatFlux > maxHeatFlux) maxHeatFlux = state.HeatFlux;
                if (state.SkinTemperature > maxSkinTemp) maxSkinTemp = state.SkinTemperature;

                // Проверка завершения посадки автопилотом или касания палубы
                if (state.IsLanded || state.IsCrashed)
                {
                    var contact = LandingGearContact.Evaluate(state.PosY, state.VelY, Math.Abs(state.VelX), Math.Abs(state.PitchDegrees - 90.0), state.Mass);
                    state.LegStroke = contact.Stroke;
                    break;
                }

                if (state.PosY <= 0.0)
                {
                    state.PosY = 0.0;
                    if (!state.HasLiftoff)
                    {
                        state.VelY = 0.0;
                        state.VelX = 0.0;
                    }
                    else
                    {
                        var contact = LandingGearContact.Evaluate(state.PosY, state.VelY, Math.Abs(state.VelX), Math.Abs(state.PitchDegrees - 90.0), state.Mass);
                        state.LegStroke = contact.Stroke;
                        state.IsLanded = contact.IsLanded;
                        state.IsCrashed = contact.IsCrashed;
                        break;
                    }
                }
            }

            double distToBarge = Math.Abs(state.PosX - RocketParameters.PadB_X);

            AssertTrue(maxAlt >= RocketParameters.KarmanLine, "Достижение линии Кармана (> 100 км)", $"H_max = {maxAlt / 1000.0:F2} км");
            AssertTrue(maxHeatFlux > 30000.0, "Регистрация аэродинамического нагрева Саттона-Грейвса", $"Q_max = {maxHeatFlux / 1000.0:F1} кВт/м²");
            AssertTrue(maxSkinTemp > 600.0, "Регистрация равновесной температуры обшивки TPS", $"T_skin_max = {maxSkinTemp:F0} °C");
            AssertTrue(state.IsLanded && !state.IsCrashed, "Успешная мягкая посадка на баржу B (Touchdown: PASS)", $"Исход: IsLanded={state.IsLanded}");
            AssertTrue(Math.Abs(state.VelY) <= 2.0, "Вертикальная скорость касания (|Vy| <= 2.0 м/с)", $"Vy = {state.VelY:F2} м/с");
            AssertTrue(distToBarge <= 50.0, "Точность попадания на баржу B (< 50 м)", $"Ошибка = {distToBarge:F1} м (баржи X={RocketParameters.PadB_X} м, ракета X={state.PosX:F1} м)");
            Console.WriteLine();
        }
    }
}
