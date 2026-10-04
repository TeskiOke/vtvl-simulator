using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DSTU.VTVL.EditorTools
{
    using DSTU.VTVL.UnityAdapters;
    using DSTU.VTVL.PhysicsCore;

    /// <summary>
    /// Генератор реалистичной 3D-сцены суборбитального броска VTVL для Unity 2022.3 / Unity 6.
    /// Строит идеально круглый корпус ракеты, носовой обтекатель, системы частиц пламени,
    /// клубов пара при посадке, плазмы входа, 3D-ленту траектории и баржу B.
    /// </summary>
    public static class VTVLSceneBuilder
    {
        [MenuItem("VTVL Simulator/🚀 Собрать готовую 3D-сцену (Auto-Setup)", false, 1)]
        public static void BuildCompleteFlightScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // =========================================================
            // 1. ОСВЕЩЕНИЕ И СОЛНЦЕ
            // =========================================================
            GameObject sunLightObj = new GameObject("Directional Light (Sun)");
            Light sunLight = sunLightObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.97f, 0.92f);
            sunLight.intensity = 1.35f;
            sunLight.shadows = LightShadows.Soft;
            sunLightObj.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            // Добавляем фоновый заполняющий свет
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.32f, 0.42f);

            // =========================================================
            // 2. БЕСКРАЙНИЙ ОКЕАН (МОРЕ УРОВЕНЬ Y = 0)
            // =========================================================
            GameObject ocean = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ocean.name = "Ocean Surface (Y=0)";
            ocean.transform.position = new Vector3(12635f, 0f, 0f);
            ocean.transform.localScale = new Vector3(5000f, 1f, 2000f);

            Material oceanMat = new Material(Shader.Find("Standard"));
            oceanMat.color = new Color(0.04f, 0.16f, 0.28f);
            oceanMat.SetFloat("_Glossiness", 0.92f);
            oceanMat.SetFloat("_Metallic", 0.15f);
            ocean.GetComponent<Renderer>().sharedMaterial = oceanMat;

            // =========================================================
            // 3. СТАРТОВЫЙ СТОЛ A (КООРДИНАТА X = 0)
            // =========================================================
            GameObject padA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padA.name = "Launch Pad A (X=0)";
            padA.transform.position = new Vector3(0f, 0.5f, 0f);
            padA.transform.localScale = new Vector3(24f, 0.5f, 24f);

            Material padMat = new Material(Shader.Find("Standard"));
            padMat.color = new Color(0.35f, 0.37f, 0.4f);
            padMat.SetFloat("_Glossiness", 0.3f);
            padA.GetComponent<Renderer>().sharedMaterial = padMat;

            // Башня обслуживания
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Service Tower";
            tower.transform.position = new Vector3(-8f, 14f, 0f);
            tower.transform.localScale = new Vector3(3f, 28f, 3f);
            tower.GetComponent<Renderer>().sharedMaterial = padMat;

            // =========================================================
            // 4. ПОСАДОЧНАЯ БАРЖА B (КООРДИНАТА X = 25 270 М)
            // =========================================================
            float bargeX = (float)RocketParameters.PadB_X;
            GameObject bargeB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bargeB.name = "Drone Ship Barge B (X=25.27 km)";
            bargeB.transform.position = new Vector3(bargeX, 0.8f, 0f);
            bargeB.transform.localScale = new Vector3(90f, 1.6f, 50f);

            Material bargeDeckMat = new Material(Shader.Find("Standard"));
            bargeDeckMat.color = new Color(0.14f, 0.16f, 0.18f);
            bargeDeckMat.SetFloat("_Glossiness", 0.5f);
            bargeB.GetComponent<Renderer>().sharedMaterial = bargeDeckMat;

            // Внешний круг посадочной мишени
            GameObject targetOuter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetOuter.name = "Target Outer Circle";
            targetOuter.transform.position = new Vector3(bargeX, 1.62f, 0f);
            targetOuter.transform.localScale = new Vector3(22f, 0.04f, 22f);
            Material whiteMat = new Material(Shader.Find("Standard"));
            whiteMat.color = new Color(0.95f, 0.95f, 0.95f);
            targetOuter.GetComponent<Renderer>().sharedMaterial = whiteMat;

            // Внутренний круг («яблочко»)
            GameObject targetInner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetInner.name = "Target Center Circle";
            targetInner.transform.position = new Vector3(bargeX, 1.66f, 0f);
            targetInner.transform.localScale = new Vector3(10f, 0.04f, 10f);
            Material greenMat = new Material(Shader.Find("Standard"));
            greenMat.color = new Color(0.0f, 0.85f, 0.45f);
            greenMat.SetFloat("_Glossiness", 0.8f);
            targetInner.GetComponent<Renderer>().sharedMaterial = greenMat;

            // Жёлтая посадочная отметка 'X'
            GameObject markX1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            markX1.transform.position = new Vector3(bargeX, 1.7f, 0f);
            markX1.transform.localScale = new Vector3(1.2f, 0.05f, 7f);
            markX1.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            Material markMat = new Material(Shader.Find("Standard"));
            markMat.color = new Color(0.98f, 0.82f, 0.1f);
            markX1.GetComponent<Renderer>().sharedMaterial = markMat;

            GameObject markX2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            markX2.transform.position = new Vector3(bargeX, 1.7f, 0f);
            markX2.transform.localScale = new Vector3(1.2f, 0.05f, 7f);
            markX2.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
            markX2.GetComponent<Renderer>().sharedMaterial = markMat;

            // =========================================================
            // 5. РАКЕТА-НОСИТЕЛЬ VTVL-30 (КРУГЛАЯ СГЛАЖЕННАЯ 3D-МОДЕЛЬ)
            // =========================================================
            GameObject rocketRoot = new GameObject("Rocket_VTVL30");
            rocketRoot.transform.position = new Vector3(0f, 11f, 0f); // Центр масс на высоте 11м от стола

            // Материалы ракеты
            Material rocketBodyMat = new Material(Shader.Find("Standard"));
            rocketBodyMat.color = new Color(0.94f, 0.95f, 0.97f);
            rocketBodyMat.SetFloat("_Glossiness", 0.85f);
            rocketBodyMat.SetFloat("_Metallic", 0.2f);

            Material darkCarbonMat = new Material(Shader.Find("Standard"));
            darkCarbonMat.color = new Color(0.12f, 0.13f, 0.15f);
            darkCarbonMat.SetFloat("_Glossiness", 0.6f);

            Material heatShieldMat = new Material(Shader.Find("Standard"));
            heatShieldMat.color = new Color(0.08f, 0.08f, 0.09f);
            heatShieldMat.SetFloat("_Metallic", 0.5f);

            // Создаем круглый сглаженный цилиндр (40 граней для идеальной круглости)
            GameObject body = CreateSmoothCylinder(0.90f, 18.0f, 40, "RocketBody_Smooth");
            body.transform.SetParent(rocketRoot.transform, false);
            body.transform.localPosition = new Vector3(0f, -1.0f, 0f);
            body.GetComponent<Renderer>().sharedMaterial = rocketBodyMat;

            // Нижняя юбка и теплозащитный экран (Heat Shield)
            GameObject heatShield = CreateSmoothCylinder(0.92f, 1.2f, 40, "HeatShield_Skirt");
            heatShield.transform.SetParent(rocketRoot.transform, false);
            heatShield.transform.localPosition = new Vector3(0f, -10.4f, 0f);
            heatShield.GetComponent<Renderer>().sharedMaterial = heatShieldMat;

            // Сопло маршевого двигателя (Gimbal Bell)
            GameObject nozzle = CreateSmoothCone(0.55f, 1.2f, 24, "EngineNozzle");
            nozzle.transform.SetParent(rocketRoot.transform, false);
            nozzle.transform.localPosition = new Vector3(0f, -11.0f, 0f);
            nozzle.transform.localRotation = Quaternion.Euler(180f, 0f, 0f); // Раструбом вниз
            nozzle.GetComponent<Renderer>().sharedMaterial = darkCarbonMat;

            // Носовой обтекатель (Ogive Cone, 40 граней)
            GameObject nose = CreateSmoothCone(0.90f, 3.5f, 40, "NoseCone_Ogive");
            nose.transform.SetParent(rocketRoot.transform, false);
            nose.transform.localPosition = new Vector3(0f, 8.0f, 0f);
            Material noseTipMat = new Material(Shader.Find("Standard"));
            noseTipMat.color = new Color(0.05f, 0.45f, 0.85f);
            noseTipMat.SetFloat("_Glossiness", 0.8f);
            nose.GetComponent<Renderer>().sharedMaterial = noseTipMat;

            // 4 Посадочные опоры шасси (ИСС-4: Kelvin-Voigt, R=5м)
            GameObject legsRoot = new GameObject("LandingLegs");
            legsRoot.transform.SetParent(rocketRoot.transform, false);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.name = $"LandingLeg_{i + 1}";
                leg.transform.SetParent(legsRoot.transform, false);
                leg.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2.2f, -9.5f, Mathf.Sin(angle) * 2.2f);
                leg.transform.localScale = new Vector3(0.22f, 2.5f, 0.22f);
                leg.transform.localRotation = Quaternion.Euler(Mathf.Sin(angle) * 32f, 0f, -Mathf.Cos(angle) * 32f);
                leg.GetComponent<Renderer>().sharedMaterial = darkCarbonMat;

                // Башмак опоры
                GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                foot.name = $"FootPad_{i + 1}";
                foot.transform.SetParent(leg.transform, false);
                foot.transform.localPosition = new Vector3(0f, -1.0f, 0f);
                foot.transform.localScale = new Vector3(2.5f, 0.4f, 2.5f);
                foot.GetComponent<Renderer>().sharedMaterial = heatShieldMat;
            }

            // 4 Решётчатых руля (Grid Fins у вершины ракеты)
            GameObject finsRoot = new GameObject("GridFins");
            finsRoot.transform.SetParent(rocketRoot.transform, false);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fin.name = $"GridFin_{i + 1}";
                fin.transform.SetParent(finsRoot.transform, false);
                fin.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.35f, 7.0f, Mathf.Sin(angle) * 1.35f);
                fin.transform.localScale = new Vector3(0.9f, 0.12f, 0.9f);
                fin.GetComponent<Renderer>().sharedMaterial = darkCarbonMat;
            }

            // =========================================================
            // 6. СИСТЕМЫ ЧАСТИЦ (VFX): ПЛАМЯ, ПАР ПОСАДКИ, ПЛАЗМА ВХОДА
            // =========================================================
            // А. Пламя основного двигателя (прикреплено к соплу и поворачивается вместе с ним)
            GameObject plumeObj = new GameObject("VFX_MainEnginePlume");
            plumeObj.transform.SetParent(nozzle.transform, false);
            plumeObj.transform.localPosition = new Vector3(0f, 0f, 0f);
            plumeObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // Струя по оси сопла

            ParticleSystem plumePs = plumeObj.AddComponent<ParticleSystem>();
            var pMain = plumePs.main;
            pMain.startLifetime = 0.45f;
            pMain.startSpeed = 25f;
            pMain.startSize = 1.5f;
            pMain.startColor = new Color(1.0f, 0.6f, 0.15f, 0.95f);
            pMain.playOnAwake = false;
            pMain.maxParticles = 500;
            pMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var pEmission = plumePs.emission;
            pEmission.rateOverTime = 120f;
            var pShape = plumePs.shape;
            pShape.shapeType = ParticleSystemShapeType.Cone;
            pShape.angle = 6f;
            pShape.radius = 0.4f;

            // Б. Пар и клубы пыли при посадке (Landing Steam Cloud)
            GameObject steamObj = new GameObject("VFX_LandingSteamCloud");
            steamObj.transform.SetParent(rocketRoot.transform, false);
            steamObj.transform.localPosition = new Vector3(0f, -11.2f, 0f);
            steamObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            ParticleSystem steamPs = steamObj.AddComponent<ParticleSystem>();
            var sMain = steamPs.main;
            sMain.startLifetime = 1.8f;
            sMain.startSpeed = 12f;
            sMain.startSize = 4.0f;
            sMain.startColor = new Color(0.92f, 0.95f, 1.0f, 0.65f); // Белый пар
            sMain.playOnAwake = false;
            sMain.maxParticles = 600;
            sMain.simulationSpace = ParticleSystemSimulationSpace.World;

            var sEmission = steamPs.emission;
            sEmission.rateOverTime = 150f;
            var sShape = steamPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Circle;
            sShape.radius = 2.5f;

            // В. Плазма при торможении в атмосфере (Reentry Plasma Glow)
            GameObject plasmaObj = new GameObject("VFX_ReentryPlasmaGlow");
            plasmaObj.transform.SetParent(rocketRoot.transform, false);
            plasmaObj.transform.localPosition = new Vector3(0f, -9.0f, 0f);

            ParticleSystem plasmaPs = plasmaObj.AddComponent<ParticleSystem>();
            var plMain = plasmaPs.main;
            plMain.startLifetime = 0.3f;
            plMain.startSpeed = 8f;
            plMain.startSize = 5.0f;
            plMain.startColor = new Color(1.0f, 0.25f, 0.05f, 0.75f); // Ярко-красный/оранжевый жар
            plMain.playOnAwake = false;
            plMain.maxParticles = 300;
            plMain.simulationSpace = ParticleSystemSimulationSpace.Local;

            // =========================================================
            // 7. 3D-ТРАЕКТОРИЯ ПОЛЁТА (LINE RENDERER)
            // =========================================================
            LineRenderer line = rocketRoot.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = 4.0f;
            line.endWidth = 2.0f;
            Material lineMat = new Material(Shader.Find("Sprites/Default"));
            lineMat.color = new Color(0.0f, 0.95f, 1.0f, 0.85f); // Свечение циан
            line.sharedMaterial = lineMat;
            line.positionCount = 0;

            // =========================================================
            // 8. НАВЕШИВАНИЕ ФИЗИКИ И СИСТЕМЫ СЛЕЖЕНИЯ
            // =========================================================
            RocketPhysicsBridge bridge = rocketRoot.AddComponent<RocketPhysicsBridge>();
            TelemetryLogger logger = rocketRoot.AddComponent<TelemetryLogger>();
            logger.PhysicsBridge = bridge;

            bridge.EngineNozzle = nozzle.transform;
            bridge.MainEnginePlume = plumePs;
            bridge.LandingSteamPlume = steamPs;
            bridge.ReentryPlasmaGlow = plasmaPs;
            bridge.GridFinsModel = finsRoot;
            bridge.TrajectoryLine = line;

            // Камера слежения
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 180000f; // Охват до 180 км
            cameraObj.tag = "MainCamera";

            RocketCameraFollow follow = cameraObj.AddComponent<RocketCameraFollow>();
            follow.TargetRocket = rocketRoot.transform;
            follow.PhysicsBridge = bridge;

            // HUD Телеметрии
            GameObject hudObj = new GameObject("SimulatorHUD");
            SimulatorHUD hud = hudObj.AddComponent<SimulatorHUD>();
            hud.PhysicsBridge = bridge;

            // =========================================================
            // 9. СОХРАНЕНИЕ СЦЕНЫ
            // =========================================================
            string scenePath = "Assets/Scenes/VTVL_MainFlight.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };

            Debug.Log($"<color=lime>✅ 3D-сцена VTVL обновлена с круглой ракетой, паром и свободной камерой: {scenePath}</color>");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("VTVL Симулятор обновлен!", 
                    "3D-сцена суборбитального полёта успешно пересобрана!\n\n" +
                    "✨ Что улучшено:\n" +
                    "• Идеально круглая ракета (40 сегментов) с носовым конусом и поворотным соплом TVC\n" +
                    "• Плавная камера с облётом 360° (зажми ПКМ и двигай мышь, колёсико — зум)\n" +
                    "• Клубы пара и дыма при посадке у палубы\n" +
                    "• Огненный плазменный след при входе в атмосферу\n" +
                    "• 3D-лента траектории полёта (вкл/выкл по кнопке 'T')\n" +
                    "• Графики траектории, высоты и посекундный отчёт (кнопка 'G')!\n\n" +
                    "Нажмите PLAY (Cmd + P) для запуска!", "Полетели!");
            }
        }

        /// <summary>
        /// Создает сглаженный круглый цилиндр с заданным числом сегментов.
        /// </summary>
        private static GameObject CreateSmoothCylinder(float radius, float height, int segments, string name)
        {
            GameObject obj = new GameObject(name);
            MeshFilter filter = obj.AddComponent<MeshFilter>();
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.name = name;

            int vertCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 6];

            float halfHeight = height * 0.5f;

            for (int i = 0; i <= segments; i++)
            {
                float u = (float)i / segments;
                float angle = u * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                Vector3 normal = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

                vertices[i * 2] = new Vector3(x, -halfHeight, z);
                vertices[i * 2 + 1] = new Vector3(x, halfHeight, z);

                normals[i * 2] = normal;
                normals[i * 2 + 1] = normal;

                uvs[i * 2] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);

                if (i < segments)
                {
                    int baseIdx = i * 2;
                    int triIdx = i * 6;

                    triangles[triIdx] = baseIdx;
                    triangles[triIdx + 1] = baseIdx + 1;
                    triangles[triIdx + 2] = baseIdx + 3;

                    triangles[triIdx + 3] = baseIdx;
                    triangles[triIdx + 4] = baseIdx + 3;
                    triangles[triIdx + 5] = baseIdx + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            filter.sharedMesh = mesh;
            return obj;
        }

        /// <summary>
        /// Создает сглаженный круглый конус с заданным числом сегментов.
        /// </summary>
        private static GameObject CreateSmoothCone(float radius, float height, int segments, string name)
        {
            GameObject obj = new GameObject(name);
            MeshFilter filter = obj.AddComponent<MeshFilter>();
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.name = name;

            int vertCount = segments + 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 3];

            // Вершина конуса
            vertices[0] = new Vector3(0f, height, 0f);
            normals[0] = Vector3.up;
            uvs[0] = new Vector2(0.5f, 1f);

            for (int i = 0; i <= segments; i++)
            {
                float u = (float)i / segments;
                float angle = u * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                vertices[i + 1] = new Vector3(x, 0f, z);
                normals[i + 1] = new Vector3(Mathf.Cos(angle), radius / height, Mathf.Sin(angle)).normalized;
                uvs[i + 1] = new Vector2(u, 0f);

                if (i < segments)
                {
                    triangles[i * 3] = 0;
                    triangles[i * 3 + 1] = i + 1;
                    triangles[i * 3 + 2] = i + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            filter.sharedMesh = mesh;
            return obj;
        }
    }
}
