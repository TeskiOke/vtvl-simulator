using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace DSTU.VTVL.EditorTools
{
    using DSTU.VTVL.UnityAdapters;
    using DSTU.VTVL.PhysicsCore;
    using DSTU.VTVL.Visuals;

    /// <summary>
    /// Генератор фотореалистичной 3D-сцены орбитального корабля VTVL-Orbital для Unity 6 / 2022.3 LTS.
    /// Создаёт:
    /// - 50-метровую детальную модель корабля Starship-класса со стальной зеркальной обшивкой и черной термозащитой TPS;
    /// - 4 подвижных аэродинамических закрылка-флэпа (Forward & Aft Body Flaps);
    /// - Кластер из 6 двигателей Raptor (3 центральных на кардане TVC + 3 вакуумных);
    /// - 6 гидравлических посадочных опор шасси;
    /// - Фотореалистичный анимированный океан с волнами и солнечными бликами;
    /// - Динамическую атмосферу с переходом в космос, звёздным полем и искривлением горизонта Земли;
    /// - Детализированный стартовый стол с ферменной башней обслуживания, прожекторами и стравливанием криогеники;
    /// - Морскую посадочную баржу-дрононосец (ASDS Drone Ship) с разметкой палубы;
    /// - Кинематографические VFX (диски Маха, вакуумное расширение пламени, гиперзвуковая плазма входа, свет двигателей).
    /// </summary>
    public static class VTVLSceneBuilder
    {
        [MenuItem("VTVL Simulator/🚀 Собрать кинематографическую 3D-сцену (AAA Visuals)", false, 1)]
        public static void BuildCompleteFlightScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // =========================================================
            // 1. ОСВЕЩЕНИЕ И СОЛНЦЕ
            // =========================================================
            GameObject sunLightObj = new GameObject("Directional Light (Sun)");
            Light sunLight = sunLightObj.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1.0f, 0.97f, 0.92f);
            sunLight.intensity = 1.4f;
            sunLight.shadows = LightShadows.Soft;
            sunLight.shadowBias = 0.03f;
            sunLight.shadowNormalBias = 0.3f;
            sunLightObj.transform.rotation = Quaternion.Euler(42f, -38f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.40f, 0.52f);

            // =========================================================
            // 2. ДИНАМИЧЕСКИЙ ОКЕАН (МОРСКАЯ ГЛАДЬ Y = 0)
            // =========================================================
            GameObject oceanObj = new GameObject("Ocean_Dynamic_Surface");
            oceanObj.transform.position = new Vector3(12635f, 0f, 0f);
            OceanWaterController oceanCtrl = oceanObj.AddComponent<OceanWaterController>();
            oceanCtrl.SizeX = 60000f;
            oceanCtrl.SizeZ = 12000f;
            oceanCtrl.GridResolutionX = 70;
            oceanCtrl.GridResolutionZ = 40;

            // =========================================================
            // 3. ИСКРИВЛЕНИЕ ГОРИЗОНТА ЗЕМЛИ И ЗВЁЗДНОЕ ПОЛЕ
            // =========================================================
            // Огромная сфера планеты Земля для космической панорамы
            GameObject earthGlobe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earthGlobe.name = "Curved_Earth_Horizon_Globe";
            earthGlobe.transform.position = new Vector3(12635f, -6371000f, 0f);
            earthGlobe.transform.localScale = new Vector3(12742000f, 12742000f, 12742000f);

            Material earthMat = new Material(Shader.Find("Standard"));
            earthMat.name = "Earth_Atmosphere_Limb_Mat";
            earthMat.color = new Color(0.06f, 0.22f, 0.42f);
            earthMat.SetFloat("_Metallic", 0.05f);
            earthMat.SetFloat("_Glossiness", 0.65f);
            earthGlobe.GetComponent<Renderer>().sharedMaterial = earthMat;
            UnityEngine.Object.DestroyImmediate(earthGlobe.GetComponent<Collider>());

            // Процедурное звёздное поле (3000 звёзд в космосе)
            GameObject starfieldObj = new GameObject("Cosmic_Starfield");
            ParticleSystem starfieldPs = starfieldObj.AddComponent<ParticleSystem>();
            var starMain = starfieldPs.main;
            starMain.startLifetime = 1000f;
            starMain.startSpeed = 0f;
            starMain.startSize = 1.2f;
            starMain.startColor = new Color(1f, 1f, 1f, 0f); // Загораются в космосе
            starMain.maxParticles = 3000;
            starMain.simulationSpace = ParticleSystemSimulationSpace.World;
            var starEmission = starfieldPs.emission;
            starEmission.rateOverTime = 0f;
            starEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 3000) });
            var starShape = starfieldPs.shape;
            starShape.shapeType = ParticleSystemShapeType.Sphere;
            starShape.radius = 1200f;
            var starRenderer = starfieldPs.GetComponent<ParticleSystemRenderer>();
            starRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            // Контроллер высотной атмосферы
            GameObject atmoCtrlObj = new GameObject("Atmosphere_Sky_Controller");
            AtmosphereSkyController atmoCtrl = atmoCtrlObj.AddComponent<AtmosphereSkyController>();
            atmoCtrl.SunDirectionalLight = sunLight;
            atmoCtrl.CurvedEarthGlobe = earthGlobe;
            atmoCtrl.StarfieldSystem = starfieldPs;

            // =========================================================
            // 4. СТАРТОВЫЙ КОМПЛЕКС PAD A (X = 0)
            // =========================================================
            GameObject padGroup = new GameObject("LaunchComplex_PadA");
            padGroup.transform.position = Vector3.zero;

            Material concreteMat = new Material(Shader.Find("Standard"));
            concreteMat.color = new Color(0.38f, 0.40f, 0.43f);
            concreteMat.SetFloat("_Glossiness", 0.25f);

            Material steelTrussMat = new Material(Shader.Find("Standard"));
            steelTrussMat.color = new Color(0.20f, 0.22f, 0.25f);
            steelTrussMat.SetFloat("_Metallic", 0.75f);
            steelTrussMat.SetFloat("_Glossiness", 0.65f);

            Material safetyYellowMat = new Material(Shader.Find("Standard"));
            safetyYellowMat.color = new Color(0.95f, 0.78f, 0.08f);
            safetyYellowMat.SetFloat("_Glossiness", 0.5f);

            // Массивный восьмиугольный стартовый стол
            GameObject padMount = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padMount.name = "Launch_Mount_Octagon";
            padMount.transform.SetParent(padGroup.transform, false);
            padMount.transform.position = new Vector3(0f, 2.5f, 0f);
            padMount.transform.localScale = new Vector3(28f, 2.5f, 28f);
            padMount.GetComponent<Renderer>().sharedMaterial = concreteMat;

            // Газоотводный лоток (Flame Trench) под ракетой
            GameObject flameTrench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flameTrench.name = "Flame_Deflector_Trench";
            flameTrench.transform.SetParent(padGroup.transform, false);
            flameTrench.transform.position = new Vector3(0f, 1.0f, 0f);
            flameTrench.transform.localScale = new Vector3(14f, 2.0f, 32f);
            Material trenchMat = new Material(Shader.Find("Standard"));
            trenchMat.color = new Color(0.12f, 0.12f, 0.13f);
            flameTrench.GetComponent<Renderer>().sharedMaterial = trenchMat;

            // Башня обслуживания (65 м стальная ферменная конструкция)
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Service_Umbilical_Tower";
            tower.transform.SetParent(padGroup.transform, false);
            tower.transform.position = new Vector3(-14f, 32.5f, 0f);
            tower.transform.localScale = new Vector3(5f, 65f, 5f);
            tower.GetComponent<Renderer>().sharedMaterial = steelTrussMat;

            // Откидная рука заправки и коммуникаций (Quick-Disconnect Arm)
            GameObject qdArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            qdArm.name = "Quick_Disconnect_Arm";
            qdArm.transform.SetParent(tower.transform, false);
            qdArm.transform.localPosition = new Vector3(1.4f, 0.05f, 0f);
            qdArm.transform.localScale = new Vector3(1.8f, 0.04f, 0.35f);
            qdArm.GetComponent<Renderer>().sharedMaterial = safetyYellowMat;

            // Прожектор подсветки ракеты на башне
            GameObject towerLightObj = new GameObject("Tower_Floodlight");
            towerLightObj.transform.SetParent(tower.transform, false);
            towerLightObj.transform.localPosition = new Vector3(0.6f, 0.25f, 0f);
            Light towerLight = towerLightObj.AddComponent<Light>();
            towerLight.type = LightType.Spot;
            towerLight.range = 70f;
            towerLight.spotAngle = 65f;
            towerLight.intensity = 4.5f;
            towerLight.color = new Color(1.0f, 0.95f, 0.88f);
            towerLightObj.transform.localRotation = Quaternion.Euler(30f, 90f, 0f);

            // Криогенный сброс паров (Cryo Boil-off Venting)
            GameObject ventObj = new GameObject("VFX_CryoBoilOffVent");
            ventObj.transform.SetParent(padGroup.transform, false);
            ventObj.transform.position = new Vector3(-3f, 32f, 0f);
            ParticleSystem ventPs = ventObj.AddComponent<ParticleSystem>();
            var vMain = ventPs.main;
            vMain.startLifetime = 3.5f;
            vMain.startSpeed = 3.5f;
            vMain.startSize = 2.5f;
            vMain.startColor = new Color(0.95f, 0.98f, 1.0f, 0.45f);
            var vEmission = ventPs.emission;
            vEmission.rateOverTime = 25f;
            var vShape = ventPs.shape;
            vShape.shapeType = ParticleSystemShapeType.Cone;
            vShape.angle = 12f;
            ventObj.transform.rotation = Quaternion.Euler(-25f, -90f, 0f);

            // =========================================================
            // 5. МОРСКАЯ ПОСАДОЧНАЯ БАРЖА B (ASDS DRONE SHIP, X = 25 270 М)
            // =========================================================
            float bargeX = (float)RocketParameters.PadB_X;
            GameObject bargeGroup = new GameObject("DroneShip_ASDS_BargeB");
            bargeGroup.transform.position = new Vector3(bargeX, 0f, 0f);

            // Корпус баржи (95м длина x 52м ширина)
            GameObject bargeHull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bargeHull.name = "Barge_Hull";
            bargeHull.transform.SetParent(bargeGroup.transform, false);
            bargeHull.transform.position = new Vector3(0f, 1.2f, 0f);
            bargeHull.transform.localScale = new Vector3(95f, 2.4f, 52f);

            Material bargeDeckMat = new Material(Shader.Find("Standard"));
            bargeDeckMat.color = new Color(0.14f, 0.16f, 0.18f);
            bargeDeckMat.SetFloat("_Metallic", 0.45f);
            bargeDeckMat.SetFloat("_Glossiness", 0.55f);
            bargeHull.GetComponent<Renderer>().sharedMaterial = bargeDeckMat;

            // Жёлто-чёрная полоса безопасности по периметру палубы
            GameObject safetyBorder1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safetyBorder1.name = "Perimeter_Safety_Border_1";
            safetyBorder1.transform.SetParent(bargeGroup.transform, false);
            safetyBorder1.transform.position = new Vector3(0f, 2.45f, 25.5f);
            safetyBorder1.transform.localScale = new Vector3(95f, 0.1f, 1.2f);
            safetyBorder1.GetComponent<Renderer>().sharedMaterial = safetyYellowMat;

            GameObject safetyBorder2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safetyBorder2.name = "Perimeter_Safety_Border_2";
            safetyBorder2.transform.SetParent(bargeGroup.transform, false);
            safetyBorder2.transform.position = new Vector3(0f, 2.45f, -25.5f);
            safetyBorder2.transform.localScale = new Vector3(95f, 0.1f, 1.2f);
            safetyBorder2.GetComponent<Renderer>().sharedMaterial = safetyYellowMat;

            // Контейнерный пункт управления и купол спутниковой связи (Radome)
            GameObject controlBunker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            controlBunker.name = "Avionics_Bunker";
            controlBunker.transform.SetParent(bargeGroup.transform, false);
            controlBunker.transform.position = new Vector3(-43f, 3.5f, 0f);
            controlBunker.transform.localScale = new Vector3(5f, 2.5f, 14f);
            controlBunker.GetComponent<Renderer>().sharedMaterial = concreteMat;

            GameObject radome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            radome.name = "Telemetry_Radome";
            radome.transform.SetParent(bargeGroup.transform, false);
            radome.transform.position = new Vector3(-43f, 5.5f, 0f);
            radome.transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);
            Material whiteMat = new Material(Shader.Find("Standard"));
            whiteMat.color = new Color(0.96f, 0.96f, 0.96f);
            radome.GetComponent<Renderer>().sharedMaterial = whiteMat;

            // 4 Азипода (двигатели динамического позиционирования баржи)
            for (int k = 0; k < 4; k++)
            {
                float posX = (k < 2) ? -42f : 42f;
                float posZ = (k % 2 == 0) ? -23f : 23f;
                GameObject azipod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                azipod.name = $"Azipod_Thruster_{k + 1}";
                azipod.transform.SetParent(bargeGroup.transform, false);
                azipod.transform.position = new Vector3(posX, 0.2f, posZ);
                azipod.transform.localScale = new Vector3(2.5f, 1.2f, 2.5f);
                azipod.GetComponent<Renderer>().sharedMaterial = steelTrussMat;
            }

            // Внешнее белое посадочное кольцо (диаметр 24м)
            GameObject targetOuter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetOuter.name = "Target_Outer_Ring";
            targetOuter.transform.SetParent(bargeGroup.transform, false);
            targetOuter.transform.position = new Vector3(0f, 2.44f, 0f);
            targetOuter.transform.localScale = new Vector3(24f, 0.03f, 24f);
            targetOuter.GetComponent<Renderer>().sharedMaterial = whiteMat;

            // Внутренняя неоновая зеленая зона точного касания (диаметр 11м)
            GameObject targetInner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            targetInner.name = "Target_Inner_Green_Zone";
            targetInner.transform.SetParent(bargeGroup.transform, false);
            targetInner.transform.position = new Vector3(0f, 2.47f, 0f);
            targetInner.transform.localScale = new Vector3(11f, 0.03f, 11f);
            Material neonGreenMat = new Material(Shader.Find("Standard"));
            neonGreenMat.color = new Color(0.0f, 0.92f, 0.52f);
            neonGreenMat.SetFloat("_Glossiness", 0.85f);
            targetInner.GetComponent<Renderer>().sharedMaterial = neonGreenMat;

            // Центральная посадочная мишень 'X' (желтый светоотражатель)
            GameObject markX1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            markX1.name = "Landing_Mark_X1";
            markX1.transform.SetParent(bargeGroup.transform, false);
            markX1.transform.position = new Vector3(0f, 2.50f, 0f);
            markX1.transform.localScale = new Vector3(1.4f, 0.04f, 8.5f);
            markX1.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            markX1.GetComponent<Renderer>().sharedMaterial = safetyYellowMat;

            GameObject markX2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            markX2.name = "Landing_Mark_X2";
            markX2.transform.SetParent(bargeGroup.transform, false);
            markX2.transform.position = new Vector3(0f, 2.50f, 0f);
            markX2.transform.localScale = new Vector3(1.4f, 0.04f, 8.5f);
            markX2.transform.rotation = Quaternion.Euler(0f, -45f, 0f);
            markX2.GetComponent<Renderer>().sharedMaterial = safetyYellowMat;

            // =========================================================
            // 6. ОРБИТАЛЬНЫЙ КОРАБЛЬ VTVL-ORBITAL (STARSHIP-CLASS)
            // =========================================================
            GameObject rocketRoot = new GameObject("Rocket_VTVL_Orbital");
            rocketRoot.transform.position = new Vector3(0f, 25f, 0f); // Центр масс 50-метрового корабля на высоте 25м

            // Материалы корабля
            // 1. Зеркальная нержавеющая сталь AISI 304L (Leeward Side)
            Material steelBodyMat = new Material(Shader.Find("Standard"));
            steelBodyMat.name = "Starship_Stainless_Steel_304L";
            steelBodyMat.color = new Color(0.93f, 0.95f, 0.98f);
            steelBodyMat.SetFloat("_Metallic", 0.96f);
            steelBodyMat.SetFloat("_Glossiness", 0.91f);

            // 2. Черная керамическая теплозащита TPS (Windward Belly Side)
            Material heatShieldTpsMat = new Material(Shader.Find("Standard"));
            heatShieldTpsMat.name = "HeatShield_Ceramic_Tiles_TPS";
            heatShieldTpsMat.color = new Color(0.06f, 0.06f, 0.07f);
            heatShieldTpsMat.SetFloat("_Metallic", 0.05f);
            heatShieldTpsMat.SetFloat("_Glossiness", 0.22f);

            // 3. Темный титан / жаропрочный инконель для сопел Raptor
            Material raptorInconelMat = new Material(Shader.Find("Standard"));
            raptorInconelMat.name = "Raptor_Engine_Inconel";
            raptorInconelMat.color = new Color(0.18f, 0.19f, 0.22f);
            raptorInconelMat.SetFloat("_Metallic", 0.85f);
            raptorInconelMat.SetFloat("_Glossiness", 0.72f);

            // А. Основной цилиндрический корпус фюзеляжа (диаметр 4.5м / длина 36м, 48 граней)
            GameObject bodyObj = CreateDualTexturedCylinder(2.25f, 36.0f, 48, "Fuselage_Tanks", steelBodyMat, heatShieldTpsMat);
            bodyObj.transform.SetParent(rocketRoot.transform, false);
            bodyObj.transform.localPosition = new Vector3(0f, -2.0f, 0f);

            // Б. Аэродинамический параболический носовой обтекатель (длина 12м, 48 граней)
            GameObject noseObj = CreateOgiveNosecone(2.25f, 12.0f, 48, "Nosecone_Ogive", steelBodyMat, heatShieldTpsMat);
            noseObj.transform.SetParent(rocketRoot.transform, false);
            noseObj.transform.localPosition = new Vector3(0f, 16.0f, 0f);

            // В. Продольные кабель-каналы (Aerodynamic Raceways) по бортам
            GameObject racewayLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            racewayLeft.name = "Raceway_Avionics_Left";
            racewayLeft.transform.SetParent(rocketRoot.transform, false);
            racewayLeft.transform.localPosition = new Vector3(-2.3f, -2.0f, 0f);
            racewayLeft.transform.localScale = new Vector3(0.18f, 34.0f, 0.45f);
            racewayLeft.GetComponent<Renderer>().sharedMaterial = steelBodyMat;

            GameObject racewayRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            racewayRight.name = "Raceway_Avionics_Right";
            racewayRight.transform.SetParent(rocketRoot.transform, false);
            racewayRight.transform.localPosition = new Vector3(2.3f, -2.0f, 0f);
            racewayRight.transform.localScale = new Vector3(0.18f, 34.0f, 0.45f);
            racewayRight.GetComponent<Renderer>().sharedMaterial = steelBodyMat;

            // Г. 4 Подвижных аэродинамических закрылка-флэпа (Body Flaps)
            // Носовые закрылки (Forward Canard Flaps)
            GameObject flapFwdLeftPivot = new GameObject("Flap_Forward_Left_Pivot");
            flapFwdLeftPivot.transform.SetParent(rocketRoot.transform, false);
            flapFwdLeftPivot.transform.localPosition = new Vector3(-2.25f, 23.0f, 0f);
            GameObject flapFwdLeftMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flapFwdLeftMesh.name = "Mesh";
            flapFwdLeftMesh.transform.SetParent(flapFwdLeftPivot.transform, false);
            flapFwdLeftMesh.transform.localPosition = new Vector3(-1.4f, 0f, 0.35f);
            flapFwdLeftMesh.transform.localScale = new Vector3(2.8f, 3.2f, 0.22f);
            flapFwdLeftMesh.GetComponent<Renderer>().sharedMaterial = steelBodyMat;

            GameObject flapFwdRightPivot = new GameObject("Flap_Forward_Right_Pivot");
            flapFwdRightPivot.transform.SetParent(rocketRoot.transform, false);
            flapFwdRightPivot.transform.localPosition = new Vector3(2.25f, 23.0f, 0f);
            GameObject flapFwdRightMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flapFwdRightMesh.name = "Mesh";
            flapFwdRightMesh.transform.SetParent(flapFwdRightPivot.transform, false);
            flapFwdRightMesh.transform.localPosition = new Vector3(1.4f, 0f, 0.35f);
            flapFwdRightMesh.transform.localScale = new Vector3(2.8f, 3.2f, 0.22f);
            flapFwdRightMesh.GetComponent<Renderer>().sharedMaterial = steelBodyMat;

            // Кормовые закрылки (Aft Delta Flaps) с черной термозащитой
            GameObject flapAftLeftPivot = new GameObject("Flap_Aft_Left_Pivot");
            flapAftLeftPivot.transform.SetParent(rocketRoot.transform, false);
            flapAftLeftPivot.transform.localPosition = new Vector3(-2.3f, -14.0f, 0f);
            GameObject flapAftLeftMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flapAftLeftMesh.name = "Mesh";
            flapAftLeftMesh.transform.SetParent(flapAftLeftPivot.transform, false);
            flapAftLeftMesh.transform.localPosition = new Vector3(-2.2f, 0f, 0.5f);
            flapAftLeftMesh.transform.localScale = new Vector3(4.4f, 7.5f, 0.32f);
            flapAftLeftMesh.GetComponent<Renderer>().sharedMaterial = heatShieldTpsMat;

            GameObject flapAftRightPivot = new GameObject("Flap_Aft_Right_Pivot");
            flapAftRightPivot.transform.SetParent(rocketRoot.transform, false);
            flapAftRightPivot.transform.localPosition = new Vector3(2.3f, -14.0f, 0f);
            GameObject flapAftRightMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flapAftRightMesh.name = "Mesh";
            flapAftRightMesh.transform.SetParent(flapAftRightPivot.transform, false);
            flapAftRightMesh.transform.localPosition = new Vector3(2.2f, 0f, 0.5f);
            flapAftRightMesh.transform.localScale = new Vector3(4.4f, 7.5f, 0.32f);
            flapAftRightMesh.GetComponent<Renderer>().sharedMaterial = heatShieldTpsMat;

            // Д. Кормовая юбка двигателей и кластер Raptor (6 сопел)
            GameObject engineSkirt = CreateSmoothCylinder(2.28f, 2.5f, 48, "Engine_Skirt");
            engineSkirt.transform.SetParent(rocketRoot.transform, false);
            engineSkirt.transform.localPosition = new Vector3(0f, -20.5f, 0f);
            engineSkirt.GetComponent<Renderer>().sharedMaterial = heatShieldTpsMat;

            // Центральный узел подвеса сопел TVC (Gimbal Cluster)
            GameObject gimbalRoot = new GameObject("Engine_Gimbal_Cluster_TVC");
            gimbalRoot.transform.SetParent(rocketRoot.transform, false);
            gimbalRoot.transform.localPosition = new Vector3(0f, -21.0f, 0f);

            // 3 Центральных посадочных двигателя Raptor (качаются на кардане)
            for (int e = 0; e < 3; e++)
            {
                float angle = e * 120f * Mathf.Deg2Rad;
                GameObject raptorSL = CreateContouredBellNozzle(0.70f, 1.8f, 24, $"Raptor_Center_SL_{e + 1}");
                raptorSL.transform.SetParent(gimbalRoot.transform, false);
                raptorSL.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.75f, 0f, Mathf.Sin(angle) * 0.75f);
                raptorSL.transform.localRotation = Quaternion.Euler(180f, 0f, 0f); // Раструбом вниз
                raptorSL.GetComponent<Renderer>().sharedMaterial = raptorInconelMat;
            }

            // 3 Внешних вакуумных двигателя Raptor (Raptor Vacuum с широкими раструбами)
            for (int v = 0; v < 3; v++)
            {
                float angle = (v * 120f + 60f) * Mathf.Deg2Rad;
                GameObject raptorVac = CreateContouredBellNozzle(1.20f, 2.4f, 28, $"Raptor_Vacuum_{v + 1}");
                raptorVac.transform.SetParent(rocketRoot.transform, false);
                raptorVac.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.55f, -21.0f, Mathf.Sin(angle) * 1.55f);
                raptorVac.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                raptorVac.GetComponent<Renderer>().sharedMaterial = raptorInconelMat;
            }

            // Е. 6 Гидравлических посадочных опор шасси с амортизаторами
            GameObject landingGearRoot = new GameObject("Landing_Gear_Cluster");
            landingGearRoot.transform.SetParent(rocketRoot.transform, false);
            for (int legIdx = 0; legIdx < 6; legIdx++)
            {
                float angle = legIdx * 60f * Mathf.Deg2Rad;
                GameObject leg = new GameObject($"LandingLeg_{legIdx + 1}");
                leg.transform.SetParent(landingGearRoot.transform, false);
                leg.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2.25f, -19.5f, Mathf.Sin(angle) * 2.25f);

                // Основная стойка опоры
                GameObject strut = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                strut.name = "Hydraulic_Strut";
                strut.transform.SetParent(leg.transform, false);
                strut.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1.2f, -2.5f, Mathf.Sin(angle) * 1.2f);
                strut.transform.localScale = new Vector3(0.28f, 3.2f, 0.28f);
                strut.transform.localRotation = Quaternion.Euler(Mathf.Sin(angle) * 28f, 0f, -Mathf.Cos(angle) * 28f);
                strut.GetComponent<Renderer>().sharedMaterial = raptorInconelMat;

                // Башмак опоры (Footpad)
                GameObject footpad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                footpad.name = "Footpad_Disk";
                footpad.transform.SetParent(leg.transform, false);
                footpad.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2.5f, -5.2f, Mathf.Sin(angle) * 2.5f);
                footpad.transform.localScale = new Vector3(1.2f, 0.18f, 1.2f);
                footpad.GetComponent<Renderer>().sharedMaterial = heatShieldTpsMat;
            }

            // =========================================================
            // 7. СИСТЕМЫ ЧАСТИЦ (VFX): ПЛАМЯ, ДИСКИ МАХА, ПЛАЗМА, СВЕТ
            // =========================================================
            // А. Сверхзвуковая струя двигателей
            GameObject plumeObj = new GameObject("VFX_MainEnginePlume");
            plumeObj.transform.SetParent(gimbalRoot.transform, false);
            plumeObj.transform.localPosition = Vector3.zero;
            plumeObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem plumePs = plumeObj.AddComponent<ParticleSystem>();
            var pMain = plumePs.main;
            pMain.startLifetime = 0.55f;
            pMain.startSpeed = 45f;
            pMain.startSize = 2.8f;
            pMain.startColor = new Color(1.0f, 0.62f, 0.18f, 0.98f);
            pMain.playOnAwake = false;
            pMain.maxParticles = 650;
            pMain.simulationSpace = ParticleSystemSimulationSpace.World;
            var pEmission = plumePs.emission;
            pEmission.rateOverTime = 180f;
            var pShape = plumePs.shape;
            pShape.shapeType = ParticleSystemShapeType.Cone;
            pShape.angle = 8f;
            pShape.radius = 0.8f;

            // Б. Яркие диски Маха (Mach Diamonds)
            GameObject machObj = new GameObject("VFX_MachDiamonds");
            machObj.transform.SetParent(gimbalRoot.transform, false);
            machObj.transform.localPosition = new Vector3(0f, -2.5f, 0f);
            machObj.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem machPs = machObj.AddComponent<ParticleSystem>();
            var mMain = machPs.main;
            mMain.startLifetime = 0.18f;
            mMain.startSpeed = 15f;
            mMain.startSize = 1.4f;
            mMain.startColor = new Color(0.45f, 0.75f, 1.0f, 0.95f); // Голубовато-белые диски метанового пламени
            mMain.playOnAwake = false;
            var mEmission = machPs.emission;
            mEmission.rateOverTime = 90f;

            // В. Динамический источник света пламени
            GameObject engineLightObj = new GameObject("Engine_Flame_PointLight");
            engineLightObj.transform.SetParent(gimbalRoot.transform, false);
            engineLightObj.transform.localPosition = new Vector3(0f, -2.0f, 0f);
            Light engineLight = engineLightObj.AddComponent<Light>();
            engineLight.type = LightType.Point;
            engineLight.range = 75f;
            engineLight.intensity = 8.5f;
            engineLight.color = new Color(1.0f, 0.60f, 0.20f);
            engineLight.shadows = LightShadows.Soft;
            engineLight.enabled = false;

            // Г. Гиперзвуковая плазма входа (Reentry Plasma)
            GameObject plasmaObj = new GameObject("VFX_ReentryPlasmaGlow");
            plasmaObj.transform.SetParent(rocketRoot.transform, false);
            plasmaObj.transform.localPosition = new Vector3(0f, -10.0f, 1.8f); // Со стороны теплозащиты
            ParticleSystem plasmaPs = plasmaObj.AddComponent<ParticleSystem>();
            var plMain = plasmaPs.main;
            plMain.startLifetime = 0.45f;
            plMain.startSpeed = 16f;
            plMain.startSize = 8.0f;
            plMain.startColor = new Color(1.0f, 0.28f, 0.05f, 0.85f);
            plMain.playOnAwake = false;
            plMain.maxParticles = 400;
            plMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            var plEmission = plasmaPs.emission;
            plEmission.rateOverTime = 180f;

            // Д. Пар и клубы пыли при посадке на баржу
            GameObject steamObj = new GameObject("VFX_LandingSteamCloud");
            steamObj.transform.SetParent(rocketRoot.transform, false);
            steamObj.transform.localPosition = new Vector3(0f, -22.0f, 0f);
            ParticleSystem steamPs = steamObj.AddComponent<ParticleSystem>();
            var sMain = steamPs.main;
            sMain.startLifetime = 2.2f;
            sMain.startSpeed = 16f;
            sMain.startSize = 6.0f;
            sMain.startColor = new Color(0.92f, 0.95f, 1.0f, 0.7f);
            sMain.playOnAwake = false;
            sMain.maxParticles = 700;
            sMain.simulationSpace = ParticleSystemSimulationSpace.World;
            var sEmission = steamPs.emission;
            sEmission.rateOverTime = 220f;
            var sShape = steamPs.shape;
            sShape.shapeType = ParticleSystemShapeType.Circle;
            sShape.radius = 4.5f;

            // =========================================================
            // 8. 3D-ТРАЕКТОРИЯ ПОЛЁТА (LINE RENDERER)
            // =========================================================
            LineRenderer line = rocketRoot.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = 5.0f;
            line.endWidth = 2.5f;
            Material lineMat = new Material(Shader.Find("Sprites/Default"));
            lineMat.color = new Color(0.0f, 0.95f, 1.0f, 0.85f);
            line.sharedMaterial = lineMat;
            line.positionCount = 0;

            // =========================================================
            // 9. МОСТ ФИЗИКИ И КОНТРОЛЛЕР ВИЗУАЛА
            // =========================================================
            RocketPhysicsBridge bridge = rocketRoot.AddComponent<RocketPhysicsBridge>();
            TelemetryLogger logger = rocketRoot.AddComponent<TelemetryLogger>();
            logger.PhysicsBridge = bridge;

            bridge.EngineNozzle = gimbalRoot.transform;
            bridge.MainEnginePlume = plumePs;
            bridge.LandingSteamPlume = steamPs;
            bridge.ReentryPlasmaGlow = plasmaPs;
            bridge.TrajectoryLine = line;

            // Контроллер продвинутых визуальных эффектов корабля
            RocketVisualsController visualsCtrl = rocketRoot.AddComponent<RocketVisualsController>();
            visualsCtrl.PhysicsBridge = bridge;
            visualsCtrl.FlapForwardLeft = flapFwdLeftPivot.transform;
            visualsCtrl.FlapForwardRight = flapFwdRightPivot.transform;
            visualsCtrl.FlapAftLeft = flapAftLeftPivot.transform;
            visualsCtrl.FlapAftRight = flapAftRightPivot.transform;
            visualsCtrl.MainEnginePlume = plumePs;
            visualsCtrl.MachDiamondPlume = machPs;
            visualsCtrl.ReentryPlasma = plasmaPs;
            visualsCtrl.LandingSteam = steamPs;
            visualsCtrl.EnginePointLight = engineLight;
            visualsCtrl.EngineGimbalCluster = gimbalRoot.transform;

            atmoCtrl.PhysicsBridge = bridge;

            // =========================================================
            // 10. КИНЕМАТОГРАФИЧЕСКАЯ КАМЕРА
            // =========================================================
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 250000f; // Охват до 250 км космического масштаба
            cam.fieldOfView = 60f;
            cameraObj.tag = "MainCamera";

            RocketCameraFollow follow = cameraObj.AddComponent<RocketCameraFollow>();
            follow.TargetRocket = rocketRoot.transform;
            follow.PhysicsBridge = bridge;
            follow.PadAPosition = new Vector3(0f, 8f, -95f);
            follow.BargeBPosition = new Vector3(bargeX, 10f, 85f);

            // HUD Телеметрии
            GameObject hudObj = new GameObject("SimulatorHUD");
            SimulatorHUD hud = hudObj.AddComponent<SimulatorHUD>();
            hud.PhysicsBridge = bridge;

            // =========================================================
            // 11. СОХРАНЕНИЕ СЦЕНЫ
            // =========================================================
            string scenePath = "Assets/Scenes/VTVL_MainFlight.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };

            Debug.Log($"<color=lime>✅ Кинематографическая 3D-сцена VTVL-Orbital собрана: {scenePath}</color>");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("VTVL-Orbital Симулятор обновлен!",
                    "🚀 Собрана кинематографическая 3D-сцена орбитального корабля!\n\n" +
                    "✨ Что добавлено:\n" +
                    "• 50-метровый корабль Starship-класса со стальной обшивкой и черной термозащитой TPS\n" +
                    "• 4 подвижных закрылка-флэпа для режима Belly-Flop\n" +
                    "• Кластер из 6 двигателей Raptor с TVC-карданом и дисками Маха\n" +
                    "• Фотореалистичный анимированный океан с волнами и солнечными бликами\n" +
                    "• Динамическая стратосфера, искривление Земли и звёздное поле\n" +
                    "• Морская баржа ASDS Drone Ship с точной разметкой\n" +
                    "• Бортовые камеры (клавиши 1..6): 5 - Belly Cam, 6 - Engine Cam!\n\n" +
                    "Нажмите PLAY для запуска симуляции!", "Полетели!");
            }
        }

        /// <summary>
        /// Создает сглаженный цилиндр с раздельными материалами для подветренной (TPS) и наветренной (сталь) сторон.
        /// </summary>
        private static GameObject CreateDualTexturedCylinder(float radius, float height, int segments, string name, Material steelMat, Material tpsMat)
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
            }

            // Разделяем треугольники на 2 сабмеша: сталь (z < 0) и термоплитки TPS (z >= 0)
            System.Collections.Generic.List<int> steelTris = new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<int> tpsTris = new System.Collections.Generic.List<int>();

            for (int i = 0; i < segments; i++)
            {
                int baseIdx = i * 2;
                float midZ = (vertices[baseIdx].z + vertices[baseIdx + 2].z) * 0.5f;

                var triList = (midZ >= -0.05f) ? tpsTris : steelTris;

                triList.Add(baseIdx);
                triList.Add(baseIdx + 1);
                triList.Add(baseIdx + 3);

                triList.Add(baseIdx);
                triList.Add(baseIdx + 3);
                triList.Add(baseIdx + 2);
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(steelTris.ToArray(), 0);
            mesh.SetTriangles(tpsTris.ToArray(), 1);
            mesh.RecalculateBounds();

            filter.sharedMesh = mesh;
            renderer.sharedMaterials = new Material[] { steelMat, tpsMat };
            return obj;
        }

        /// <summary>
        /// Создает аэродинамический носовой конус формы оживало (Ogive) с раздельной термозащитой.
        /// </summary>
        private static GameObject CreateOgiveNosecone(float radius, float height, int segments, string name, Material steelMat, Material tpsMat)
        {
            GameObject obj = new GameObject(name);
            MeshFilter filter = obj.AddComponent<MeshFilter>();
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.name = name;

            int rings = 14;
            int vertCount = (rings + 1) * (segments + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];

            int vertIdx = 0;
            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                float y = v * height;
                // Формула параболического скругления носа Starship
                float ringRadius = radius * Mathf.Sqrt(Mathf.Clamp01(1f - v));

                for (int s = 0; s <= segments; s++)
                {
                    float u = (float)s / segments;
                    float angle = u * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * ringRadius;
                    float z = Mathf.Sin(angle) * ringRadius;

                    vertices[vertIdx] = new Vector3(x, y, z);
                    normals[vertIdx] = new Vector3(Mathf.Cos(angle), 0.4f, Mathf.Sin(angle)).normalized;
                    uvs[vertIdx] = new Vector2(u, v);
                    vertIdx++;
                }
            }

            System.Collections.Generic.List<int> steelTris = new System.Collections.Generic.List<int>();
            System.Collections.Generic.List<int> tpsTris = new System.Collections.Generic.List<int>();

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int i1 = r * (segments + 1) + s;
                    int i2 = (r + 1) * (segments + 1) + s;
                    int i3 = i1 + 1;
                    int i4 = i2 + 1;

                    float midZ = (vertices[i1].z + vertices[i3].z) * 0.5f;
                    var triList = (midZ >= -0.05f) ? tpsTris : steelTris;

                    triList.Add(i1);
                    triList.Add(i2);
                    triList.Add(i4);

                    triList.Add(i1);
                    triList.Add(i4);
                    triList.Add(i3);
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(steelTris.ToArray(), 0);
            mesh.SetTriangles(tpsTris.ToArray(), 1);
            mesh.RecalculateBounds();

            filter.sharedMesh = mesh;
            renderer.sharedMaterials = new Material[] { steelMat, tpsMat };
            return obj;
        }

        /// <summary>
        /// Создает сглаженный круглый цилиндр.
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
        /// Создает спрофилированное сопло Лаваля (Bell Nozzle) с расширением раструба.
        /// </summary>
        private static GameObject CreateContouredBellNozzle(float exitRadius, float height, int segments, string name)
        {
            GameObject obj = new GameObject(name);
            MeshFilter filter = obj.AddComponent<MeshFilter>();
            MeshRenderer renderer = obj.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.name = name;

            int rings = 8;
            int vertCount = (rings + 1) * (segments + 1);
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[rings * segments * 6];

            float throatRadius = exitRadius * 0.35f;

            int vertIdx = 0;
            for (int r = 0; r <= rings; r++)
            {
                float v = (float)r / rings;
                float y = v * height;
                // Параболический колокол сопла Лаваля (Рао контур)
                float rCurrent = Mathf.Lerp(throatRadius, exitRadius, Mathf.Pow(v, 0.65f));

                for (int s = 0; s <= segments; s++)
                {
                    float u = (float)s / segments;
                    float angle = u * Mathf.PI * 2f;
                    float x = Mathf.Cos(angle) * rCurrent;
                    float z = Mathf.Sin(angle) * rCurrent;

                    vertices[vertIdx] = new Vector3(x, y, z);
                    normals[vertIdx] = new Vector3(Mathf.Cos(angle), 0.25f, Mathf.Sin(angle)).normalized;
                    uvs[vertIdx] = new Vector2(u, v);
                    vertIdx++;
                }
            }

            int triIdx = 0;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int i1 = r * (segments + 1) + s;
                    int i2 = (r + 1) * (segments + 1) + s;
                    int i3 = i1 + 1;
                    int i4 = i2 + 1;

                    triangles[triIdx++] = i1;
                    triangles[triIdx++] = i2;
                    triangles[triIdx++] = i4;

                    triangles[triIdx++] = i1;
                    triangles[triIdx++] = i4;
                    triangles[triIdx++] = i3;
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
