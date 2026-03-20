using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts;
using ModApi.CelestialData;
using ModApi;
using UnityEngine;

namespace Droodism.RadiationBelt
{
    public class ProceduralRadiationBelt : MonoBehaviour
    {
     
        public GameObject Parent;               // 行星的 ScaledSpace GameObject
        public Material   pointMaterial;
        
        private RadiationBeltConfig config;
        public ParticleMesh innerMesh;
        public ParticleMesh outerMesh;

        private int lastRenderedFrame = -1;
        
        private bool isRegenerating;

        public void LoadDataFromConfig(RadiationBeltConfig config)
        {
            this.config= config;
        }
        public async void RegenerateMeshesAsync()
        {

            if (isRegenerating || config == null)
            {
                return;
            }
            isRegenerating = true;

            Mod.Log("Rebuilding Meshes");

            try
            {
                
                Func<Vector3, float> innerSDF = p => TorusSDF(Deform(p, config.innerHeightScale, config.innerDeform), config.innerMajorRadius,config. innerMinorRadius);
                Func<Vector3, float> outerSDF = p => TorusSDF(Deform(p, config.outerHeightScale, config.outerDeform), config.outerMajorRadius,config. outerMinorRadius);

                Vector3 innerSize = new Vector3(
                    (config.innerMajorRadius + config.innerMinorRadius) * 2.2f,   // 加大
                    config.innerMinorRadius * 6f * config.innerHeightScale,
                    (config.innerMajorRadius + config.innerMinorRadius) * 2.2f
                );
                Vector3 outerSize = new Vector3(
                    (config.outerMajorRadius + config.outerMinorRadius) * 3.5f,   // 关键！拉伸后至少3.5倍
                    config.outerMinorRadius * 6f * config.outerHeightScale,
                    (config.outerMajorRadius +config.outerMinorRadius) * 3.5f
                );

                var innerTask = ParticleMesh.CreateAsync(innerSDF, innerSize, Vector3.zero, config.innerParticleCount, config.innerQuality);
                var outerTask = ParticleMesh.CreateAsync(outerSDF, outerSize * 1.2f, Vector3.zero, config.outerParticleCount, config.outerQuality);
                await Task.WhenAll(innerTask, outerTask);

                innerMesh = await innerTask;
                outerMesh = await outerTask;

                Mod.Log("Rebuilding Complete");
            }
            catch (System.Exception ex)
            {
                Mod.LogError($"Rebuilding failed: {ex.Message}");
            }
            finally
            {
                isRegenerating = false;
            }
        }
        private float TorusSDF(Vector3 p, float major, float minor)
        {
            Vector2 q = new Vector2(new Vector2(p.x, p.z).magnitude - major, p.y);
            return q.magnitude - minor;
        }
        private Vector3 Deform(Vector3 p, float heightScale, float deform)
        {
            //p = ApplyMagnetosphericDeformation(p, starDirection, outerCompression, outerExtension);
            p.y *= heightScale;
            p += ApplyDeform(p, deform);
            return p;
        }
        private Vector3 ApplyDeform(Vector3 p, float amount)
        {
            if (amount <= 0) return Vector3.zero;
            float n = Mathf.Sin(p.x*3.5f + p.y*1.1f) * Mathf.Cos(p.z*3.2f + p.y*2.3f) * 0.6f;
            return p.normalized * n * amount;
        }
        
        private void Update()
        {
            SycWithParent();
        }

        private void SycWithParent()
        {
            
            transform.position = Parent.transform.position;
        }
        

        void OnDestroy()
        {
            // 清理 mesh 防止内存泄漏
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