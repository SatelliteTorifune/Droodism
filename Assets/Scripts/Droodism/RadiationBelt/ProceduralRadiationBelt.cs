using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts;
using ModApi.GameLoop;
using ModApi.CelestialData;
using ModApi;
using UnityEngine;

namespace Assets.Scripts.Droodism.RadiationBelt
{
    public class ProceduralRadiationBelt : MonoBehaviour
    {
        public GameObject Parent;
        public Material pointMaterial;
        
        private RadiationBeltConfig config;
        public ParticleMesh innerMesh;
        public ParticleMesh outerMesh;

        private int lastRenderedFrame = -1;
        private bool isRegenerating;
        private double accumulatedSpinTimeSeconds;

        public void LoadDataFromConfig(RadiationBeltConfig config)
        {
            this.config = config;
        }
        
        public async void RegenerateMeshesAsync()
        {
            if (isRegenerating || config == null) return;
            isRegenerating = true;
            Mod.Log($"ReBuilding Radiation Belt meshes for {Parent.name}");
            try
            {
                Func<Vector3, float> innerSDF = Inner_func;
                Func<Vector3, float> outerSDF = Outer_func;

                Vector3 innerDomain = Inner_domain();
                Vector3 innerOffset = Inner_offset();
                Vector3 outerDomain = Outer_domain();
                Vector3 outerOffset = Outer_offset();

                var innerTask = ParticleMesh.CreateAsync(innerSDF, innerDomain, innerOffset, config.innerParticleCount, config.innerQuality);
                var outerTask = ParticleMesh.CreateAsync(outerSDF, outerDomain, outerOffset, config.outerParticleCount, config.outerQuality);
                
                await Task.WhenAll(innerTask, outerTask);

                innerMesh = await innerTask;
                outerMesh = await outerTask;
                Mod.Log($"ReBuilding Radiation Belt Mesh Complete for {Parent.name}");
            }
            catch (System.Exception ex)
            {
                Mod.LogError($"Rebuilding failed: {ex.StackTrace}");
            }
            finally
            {
                isRegenerating = false;
            }
        }

        #region  今天拼了
    
        private float Inner_func(Vector3 p)
        {
            float innerCompression = Mathf.Max(0.01f, config.innerCompression);
            float innerExtension = Mathf.Max(0.01f, config.innerExtension);
            p.x *= p.x < 0.0f ? innerExtension : innerCompression;

            float innerDeformXY = Mathf.Max(0.01f, config.innerDeformXY);
            float innerBorderDeformXY = Mathf.Max(0.01f, config.innerBorderDeformXY);
            float q1 = Mathf.Sqrt((p.x * p.x + p.z * p.z) * innerDeformXY) - config.innerDist;
            float d1 = Mathf.Sqrt(q1 * q1 + p.y * p.y) - config.innerRadius;
            float q2 = Mathf.Sqrt((p.x * p.x + p.z * p.z) * innerBorderDeformXY) - config.innerBorderDist;
            float d2 = Mathf.Sqrt(q2 * q2 + p.y * p.y) - config.innerBorderRadius;
            return Mathf.Max(d1, -d2) + (config.innerDeform > 0.001 ? (Mathf.Sin(p.x * 5.0f) * Mathf.Sin(p.y * 7.0f) * Mathf.Sin(p.z * 6.0f)) * config.innerDeform : 0.0f);
        }

        private Vector3 Inner_domain()
        {
            float p = Mathf.Max((config.innerDist + config.innerRadius), (config.innerBorderDist + config.innerBorderRadius));
            float innerDeformXY = Mathf.Max(0.01f, config.innerDeformXY);
            float innerBorderDeformXY = Mathf.Max(0.01f, config.innerBorderDeformXY);
            float innerCompression = Mathf.Max(0.01f, config.innerCompression);
            float innerExtension = Mathf.Max(0.01f, config.innerExtension);

            float w = p * Mathf.Sqrt(1f / Mathf.Min(innerDeformXY, innerBorderDeformXY));
            return new Vector3((w / innerCompression + w / innerExtension) * 0.5f, Mathf.Max(config.innerRadius, config.innerBorderRadius), w) * (1.0f + Mathf.Max(0f, config.innerDeform));
        }

        private Vector3 Inner_offset()
        {
            float p = Mathf.Max((config.innerDist + config.innerRadius), (config.innerBorderDist + config.innerBorderRadius));
            float innerDeformXY = Mathf.Max(0.01f, config.innerDeformXY);
            float innerBorderDeformXY = Mathf.Max(0.01f, config.innerBorderDeformXY);
            float innerCompression = Mathf.Max(0.01f, config.innerCompression);
            float innerExtension = Mathf.Max(0.01f, config.innerExtension);

            float w = p * Mathf.Sqrt(1f / Mathf.Min(innerDeformXY, innerBorderDeformXY));
            return new Vector3(w / innerCompression - (w / innerCompression + w / innerExtension) * 0.5f, 0.0f, 0.0f);
        }

        private float Outer_func(Vector3 p)
        {
            float outerCompression = Mathf.Max(0.01f, config.outerCompression);
            float outerExtension = Mathf.Max(0.01f, config.outerExtension);
            p.x *= p.x < 0.0f ? outerExtension : outerCompression;

            float outerDeformXY = Mathf.Max(0.01f, config.outerDeformXY);
            float outerBorderDeformXY = Mathf.Max(0.01f, config.outerBorderDeformXY);
            float q1 = Mathf.Sqrt((p.x * p.x + p.z * p.z) * outerDeformXY) - config.outerDist;
            float d1 = Mathf.Sqrt(q1 * q1 + p.y * p.y) - config.outerRadius;
            float q2 = Mathf.Sqrt((p.x * p.x + p.z * p.z) * outerBorderDeformXY) - config.outerBorderDist;
            float d2 = Mathf.Sqrt(q2 * q2 + p.y * p.y) - config.outerBorderRadius;
            return Mathf.Max(d1, -d2) + (config.outerDeform > 0.001 ? (Mathf.Sin(p.x * 5.0f) * Mathf.Sin(p.y * 7.0f) * Mathf.Sin(p.z * 6.0f)) * config.outerDeform : 0.0f);
        }

        private Vector3 Outer_domain()
        {
            float p = Mathf.Max((config.outerDist + config.outerRadius), (config.outerBorderDist + config.outerBorderRadius));
            float outerDeformXY = Mathf.Max(0.01f, config.outerDeformXY);
            float outerBorderDeformXY = Mathf.Max(0.01f, config.outerBorderDeformXY);
            float outerCompression = Mathf.Max(0.01f, config.outerCompression);
            float outerExtension = Mathf.Max(0.01f, config.outerExtension);

            float w = p * Mathf.Sqrt(1f / Mathf.Min(outerDeformXY, outerBorderDeformXY));
            return new Vector3((w / outerCompression + w / outerExtension) * 0.5f, Mathf.Max(config.outerRadius, config.outerBorderRadius), w) * (1.0f + Mathf.Max(0f, config.outerDeform));
        }

        private Vector3 Outer_offset()
        {
            float p = Mathf.Max((config.outerDist + config.outerRadius), (config.outerBorderDist + config.outerBorderRadius));
            float outerDeformXY = Mathf.Max(0.01f, config.outerDeformXY);
            float outerBorderDeformXY = Mathf.Max(0.01f, config.outerBorderDeformXY);
            float outerCompression = Mathf.Max(0.01f, config.outerCompression);
            float outerExtension = Mathf.Max(0.01f, config.outerExtension);

            float w = p * Mathf.Sqrt(1f / Mathf.Min(outerDeformXY, outerBorderDeformXY));
            return new Vector3(w / outerCompression - (w / outerCompression + w / outerExtension) * 0.5f, 0.0f, 0.0f);
        }

        #endregion
        
        private void Update()
        {
            SycWithParent();
        }

        private void SycWithParent()
        {
            transform.localPosition = Vector3.zero;
            if (config == null)
            {
                transform.localRotation = Quaternion.identity;
                return;
            }

            // First tilt the belt axis around a configurable local axis,
            // then spin around the tilted local up axis.
            Vector3 tiltAxis = config.beltTiltAxis.sqrMagnitude > 1e-6f
                ? config.beltTiltAxis.normalized
                : Vector3.right;
            Quaternion tilt = Quaternion.AngleAxis(config.beltTiltDegrees, tiltAxis);
            accumulatedSpinTimeSeconds += GetSpinDeltaTimeSeconds();
            float spinAngle = config.beltSpinPhaseDeg + (float)accumulatedSpinTimeSeconds * config.beltSpinSpeedDegPerSec;
            Quaternion spin = Quaternion.AngleAxis(spinAngle, Vector3.up);
            transform.localRotation = tilt * spin;
        }

        private float GetSpinDeltaTimeSeconds()
        {
            if (Game.Instance?.FlightScene?.TimeManager == null)
            {
                return Time.deltaTime;
            }

            // Warp-aware game delta time (pauses/timewarp already reflected by TimeManager).
            return Mathf.Max(0f, (float)Game.Instance.FlightScene.TimeManager.DeltaTime);
        }
        
        void OnDestroy()
        {
            if (innerMesh != null)
            {
                foreach (var m in innerMesh.meshes ?? new List<Mesh>()) if (m) Destroy(m);
            }
            if (outerMesh != null)
            {
                foreach (var m in outerMesh.meshes ?? new List<Mesh>()) if (m) Destroy(m);
            }
        }
    }
}