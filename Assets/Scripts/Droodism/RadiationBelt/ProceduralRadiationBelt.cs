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
            if (isRegenerating || config == null) return;
            isRegenerating = true;

            Mod.Log("Rebuilding Meshes");

            try
            {
                Func<Vector3, float> innerSDF = config.GetInnerDist;
                Func<Vector3, float> outerSDF = config.GetOuterDist;

                Vector3 hsize = new Vector3(config.outerDist + config.outerRadius * 2f,
                    config.outerRadius * 2f * Mathf.Max(config.innerHeightScale, config.outerHeightScale),
                    config.outerDist + config.outerRadius * 2f);

                var innerTask = ParticleMesh.CreateAsync(innerSDF, hsize, Vector3.zero, config.innerParticleCount, config.innerQuality);
                var outerTask = ParticleMesh.CreateAsync(outerSDF, hsize * 1.2f, Vector3.zero, config.outerParticleCount, config.outerQuality);

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