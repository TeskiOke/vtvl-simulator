using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using DSTU.VTVL.PhysicsCore;
using DSTU.VTVL.UnityAdapters;

namespace DSTU.VTVL.EditorTools
{
    /// <summary>
    /// Автоматизированный тестер физики и посекундного отчёта полёта.
    /// Позволяет промоделировать весь полёт за доли секунды и проверить генерацию отчётов.
    /// </summary>
    public static class FlightTestRunner
    {
        [MenuItem("VTVL Simulator/🧪 Запустить проверочный тест полёта (Fast Sim)", false, 2)]
        public static void RunFastFlightSimulation()
        {
            Debug.Log("<color=cyan>--- НАЧАЛО ТЕСТОВОГО МОДЕЛИРОВАНИЯ ПОЛЁТА VTVL ---</color>");

            GameObject runnerObj = new GameObject("TestRocketRunner");
            RocketPhysicsBridge bridge = runnerObj.AddComponent<RocketPhysicsBridge>();
            TelemetryLogger logger = runnerObj.AddComponent<TelemetryLogger>();
            logger.PhysicsBridge = bridge;

            bridge.EnsureInitialized();
            bridge.ResetSimulation();
            bridge.IsAutopilotEnabled = true;
            bridge.StartSimulation();

            RocketState state = bridge.State;

            double dt = 0.02; // 50 Гц физический шаг
            float nextLogTime = 0f;

            int lastLoggedSec = -1;
            while (state.Time < 700.0)
            {
                bridge.StepSimulationPhysics(dt);

                // Посекундная фиксация
                float curTime = (float)state.Time;
                if (curTime >= nextLogTime)
                {
                    logger.RecordSample(state);
                    nextLogTime = Mathf.Floor(curTime) + 1.0f;

                    int curSec = (int)curTime;
                    if (curSec % 30 == 0 && curSec != lastLoggedSec)
                    {
                        lastLoggedSec = curSec;
                        Debug.Log($"[T+{curSec:D3}s] Фаза={state.FlightPhase,-20} | H={state.PosY / 1000.0,6:F2} км | X={state.PosX / 1000.0,5:F2} км | Vy={state.VelY,7:F1} м/с | Vx={state.VelX,6:F1} м/с | Сопло={state.GimbalAngleDegrees,5:F1}° | Топливо={state.Fuel,5:F0} кг");
                    }
                }

                if (state.IsLanded || state.IsCrashed)
                {
                    logger.ProcessFlightCompletion(state);
                    break;
                }
            }

            Debug.Log($"<color=lime>✅ ТЕСТ ЗАВЕРШЕН: {logger.MissionResult}</color>");
            Debug.Log($"📊 Итоги полёта: Время = {logger.FlightDuration:F1} с | H_max = {logger.MaxAltitudeKm:F2} км | V_max = {logger.MaxSpeedMs:F1} м/с | Vy_touchdown = {logger.TouchdownVy:F2} м/с | Ошибка баржи = {logger.BargeDistanceError:F1} м | Топлива сожжено = {logger.FuelBurned:F0} кг");
            Debug.Log($"📈 Записано посекундных срезов: {logger.SecondBySecondLog.Count}");

            GameObject.DestroyImmediate(runnerObj);
        }
    }
}
