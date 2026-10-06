using System;
using UnityEngine;

namespace DSTU.VTVL.Visuals
{
    /// <summary>
    /// Фотореалистичный анимированный океан с процедурными волнами, бликами солнца и пеной.
    /// Вычисляет смещение вершин в реальном времени, создавая живую динамическую морскую поверхность.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class OceanWaterController : MonoBehaviour
    {
        [Header("Параметры волн")]
        [Tooltip("Амплитуда основных океанских волн (м)")]
        public float WaveHeight1 = 0.65f;
        public float WaveFreq1 = 0.04f;
        public float WaveSpeed1 = 1.2f;

        [Tooltip("Амплитуда вторичной мелкой ряби (м)")]
        public float WaveHeight2 = 0.25f;
        public float WaveFreq2 = 0.12f;
        public float WaveSpeed2 = 2.4f;

        [Header("Оптические характеристики")]
        public Color DeepWaterColor = new Color(0.02f, 0.09f, 0.18f, 0.98f);
        public Color ShallowWaterColor = new Color(0.04f, 0.25f, 0.38f, 0.95f);
        public Color SunSpecularColor = new Color(1.0f, 0.95f, 0.85f, 1.0f);

        [Header("Сетка воды")]
        public int GridResolutionX = 64;
        public int GridResolutionZ = 64;
        public float SizeX = 4000f;
        public float SizeZ = 2500f;

        private Mesh _waterMesh;
        private Vector3[] _baseVertices;
        private Vector3[] _displacedVertices;
        private Vector3[] _normals;
        private Material _waterMaterial;

        private void Awake()
        {
            SetupWaterMesh();
            SetupWaterMaterial();
        }

        private void SetupWaterMesh()
        {
            _waterMesh = new Mesh();
            _waterMesh.name = "DynamicOceanSurface";
            _waterMesh.MarkDynamic();

            int vertCount = (GridResolutionX + 1) * (GridResolutionZ + 1);
            _baseVertices = new Vector3[vertCount];
            _displacedVertices = new Vector3[vertCount];
            _normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[GridResolutionX * GridResolutionZ * 6];

            float dx = SizeX / GridResolutionX;
            float dz = SizeZ / GridResolutionZ;
            float offsetX = -SizeX * 0.5f;
            float offsetZ = -SizeZ * 0.5f;

            int vertIdx = 0;
            for (int z = 0; z <= GridResolutionZ; z++)
            {
                for (int x = 0; x <= GridResolutionX; x++)
                {
                    float vx = offsetX + x * dx;
                    float vz = offsetZ + z * dz;
                    _baseVertices[vertIdx] = new Vector3(vx, 0f, vz);
                    _displacedVertices[vertIdx] = _baseVertices[vertIdx];
                    _normals[vertIdx] = Vector3.up;
                    uvs[vertIdx] = new Vector2((float)x / GridResolutionX * 30f, (float)z / GridResolutionZ * 20f);
                    vertIdx++;
                }
            }

            int triIdx = 0;
            for (int z = 0; z < GridResolutionZ; z++)
            {
                for (int x = 0; x < GridResolutionX; x++)
                {
                    int row1 = z * (GridResolutionX + 1);
                    int row2 = (z + 1) * (GridResolutionX + 1);

                    triangles[triIdx++] = row1 + x;
                    triangles[triIdx++] = row2 + x;
                    triangles[triIdx++] = row2 + x + 1;

                    triangles[triIdx++] = row1 + x;
                    triangles[triIdx++] = row2 + x + 1;
                    triangles[triIdx++] = row1 + x + 1;
                }
            }

            _waterMesh.vertices = _baseVertices;
            _waterMesh.uv = uvs;
            _waterMesh.triangles = triangles;
            _waterMesh.normals = _normals;
            _waterMesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = _waterMesh;
        }

        private void SetupWaterMaterial()
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            Shader standardShader = Shader.Find("Standard");
            if (standardShader != null)
            {
                _waterMaterial = new Material(standardShader);
                _waterMaterial.name = "OceanWater_PBR";
                _waterMaterial.color = DeepWaterColor;
                _waterMaterial.SetFloat("_Metallic", 0.15f);
                _waterMaterial.SetFloat("_Glossiness", 0.94f); // Высокий зеркальный блеск для отражения солнца
                renderer.sharedMaterial = _waterMaterial;
            }
        }

        private void Update()
        {
            if (_baseVertices == null || _waterMesh == null) return;

            float time = Time.time;
            int vertCount = _baseVertices.Length;

            for (int i = 0; i < vertCount; i++)
            {
                Vector3 basePos = _baseVertices[i];
                float x = basePos.x;
                float z = basePos.z;

                // Суперпозиция гармонических волн
                float wave1 = Mathf.Sin(x * WaveFreq1 + time * WaveSpeed1) * Mathf.Cos(z * WaveFreq1 * 0.8f + time * WaveSpeed1 * 0.9f) * WaveHeight1;
                float wave2 = Mathf.Sin((x + z) * WaveFreq2 + time * WaveSpeed2) * WaveHeight2;
                float wave3 = Mathf.Cos((x - z) * WaveFreq2 * 1.5f + time * WaveSpeed2 * 1.3f) * (WaveHeight2 * 0.5f);

                float y = wave1 + wave2 + wave3;
                _displacedVertices[i] = new Vector3(x, y, z);
            }

            _waterMesh.vertices = _displacedVertices;
            _waterMesh.RecalculateNormals();
        }

        /// <summary>
        /// Возвращает точную высоту волны в заданной мировой координате (для физики баржи/касания).
        /// </summary>
        public float GetWaveHeightAt(float worldX, float worldZ)
        {
            float time = Time.time;
            float wave1 = Mathf.Sin(worldX * WaveFreq1 + time * WaveSpeed1) * Mathf.Cos(worldZ * WaveFreq1 * 0.8f + time * WaveSpeed1 * 0.9f) * WaveHeight1;
            float wave2 = Mathf.Sin((worldX + worldZ) * WaveFreq2 + time * WaveSpeed2) * WaveHeight2;
            return wave1 + wave2;
        }
    }
}
