using System;

namespace DSTU.VTVL.PhysicsCore
{
    /// <summary>
    /// Результат расчета динамического контакта посадочного шасси с поверхностью баржи (ИСС-4, Регламент 1.0).
    /// </summary>
    public struct LandingContactResult
    {
        /// <summary>Суммарная нормальная сила реакции опор (Н)</summary>
        public double NormalForce;

        /// <summary>Текущее сжатие штока амортизаторов (м, [0 .. 1.2])</summary>
        public double Stroke;

        /// <summary>Ощущаемая перегрузка при амортизации (G)</summary>
        public double GForce;

        /// <summary>Признак успешного завершения посадки</summary>
        public bool IsLanded;

        /// <summary>Признак аварии (пробой амортизаторов или опрокидывание)</summary>
        public bool IsCrashed;

        /// <summary>Категория исхода посадки ("Normal", "Easy", "Hard", "Rough", "Crash", "Overturned")</summary>
        public string OutcomeCategory;

        /// <summary>Описание результата</summary>
        public string Message;
    }

    /// <summary>
    /// Механическая упруго-диссипативная модель контакта 4-опорного шасси ракеты (Кельвин-Фойгт).
    /// Разработана по утвержденным спецификациям ИСС-4 и КР-1.
    /// </summary>
    public static class LandingGearContact
    {
        /// <summary>
        /// Расчет сил контакта и оценка безопасности посадки по критериям ИСС-4 ("Doghouse criteria").
        /// </summary>
        /// <param name="posY">Высота донного среза над поверхностью палубы (м)</param>
        /// <param name="velY">Вертикальная скорость ступени (м/с, отрицательная при снижении)</param>
        /// <param name="velHoriz">Модуль горизонтальной скорости сноса (м/с)</param>
        /// <param name="tiltDeg">Угол отклонения продольной оси от вертикали (градусы)</param>
        /// <param name="mass">Текущая масса ракеты (кг)</param>
        public static LandingContactResult Evaluate(
            double posY, double velY, double velHoriz, double tiltDeg, double mass)
        {
            LandingContactResult result = new LandingContactResult
            {
                NormalForce = 0.0,
                Stroke = 0.0,
                GForce = 1.0,
                IsLanded = false,
                IsCrashed = false,
                OutcomeCategory = "InFlight",
                Message = string.Empty
            };

            // Если ракета выше касания опорами (длина развернутых опор 2.0 м)
            double contactAltitude = 0.0;
            if (posY > contactAltitude)
            {
                return result;
            }

            // Глубина внедрения штока амортизатора s = -posY
            double stroke = Math.Max(0.0, -posY);
            result.Stroke = stroke;

            // Суммарная жесткость 4 опор (4 * 80 кН/м = 320 кН/м)
            double totalK = RocketParameters.LegCount * RocketParameters.LegSpringStiffness;

            // Суммарное демпфирование 4 опор (4 * 30 кН*с/м = 120 кН*с/м)
            double totalC = RocketParameters.LegCount * RocketParameters.LegDampingCoefficient;

            // Скорость сжатия штока v_s = -velY
            double compRate = -velY;

            // Упруго-вязкая сила Кельвина-Фойгта: F = K * s + C * v_s
            double fSpring = totalK * stroke;
            double fDamp = totalC * Math.Max(0.0, compRate);
            double fContact = Math.Max(0.0, fSpring + fDamp);

            result.NormalForce = fContact;
            result.GForce = fContact / (Math.Max(RocketParameters.DryMass, mass) * RocketParameters.G0);

            // 1. Проверка опрокидывания
            if (tiltDeg > 15.0 || (velHoriz > 1.0 && Math.Abs(velY) < 1.0))
            {
                result.IsCrashed = true;
                result.OutcomeCategory = "Overturned";
                result.Message = $"Авария: Опрокидывание ступени на бок (наклон {tiltDeg:F1}° > 15° или снос {velHoriz:F2} м/с)";
                return result;
            }

            // 2. Проверка пробоя штока амортизатора до упора (Bottoming Out)
            if (stroke >= RocketParameters.LegMaxStroke || Math.Abs(velY) >= RocketParameters.LegBottomingOutVelocity)
            {
                result.IsCrashed = true;
                result.OutcomeCategory = "Crash";
                result.Message = $"Авария: Смыкание штока амортизаторов (ход {stroke:F2} м >= 1.20 м, Vy = {velY:F2} м/с)";
                return result;
            }

            // 3. Анализ качества штатной посадки
            double absVy = Math.Abs(velY);
            if (absVy <= 5.0 && stroke < RocketParameters.LegMaxStroke)
            {
                result.IsLanded = true;

                if (absVy <= 1.5 && velHoriz <= 0.3 && tiltDeg <= 3.0)
                {
                    result.OutcomeCategory = "Hard (Precision)";
                    result.Message = $"Ювелирная посадка Hard (Vy={absVy:F2} м/с, сжатие штока {stroke * 100.0:F0} см)";
                }
                else if (absVy <= 2.0 && velHoriz <= 0.5 && tiltDeg <= 5.0)
                {
                    result.OutcomeCategory = "Normal";
                    result.Message = $"Штатная посадка Normal (Vy={absVy:F2} м/с, сжатие штока {stroke * 100.0:F0} см)";
                }
                else if (absVy <= 3.0 && velHoriz <= 1.0 && tiltDeg <= 10.0)
                {
                    result.OutcomeCategory = "Easy";
                    result.Message = $"Безопасная посадка Easy (Vy={absVy:F2} м/с, сжатие штока {stroke * 100.0:F0} см)";
                }
                else
                {
                    result.OutcomeCategory = "Rough";
                    result.Message = $"Грубая посадка Rough (Vy={absVy:F2} м/с, сжатие штока {stroke * 100.0:F0} см)";
                }
            }

            return result;
        }
    }
}
