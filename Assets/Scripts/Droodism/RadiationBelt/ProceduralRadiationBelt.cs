using System;
using System.Collections.Generic;
using Assets.Scripts;
using ModApi.CelestialData;
using ModApi;
using UnityEngine;

namespace Droodism.RadiationBelt
{
    public class ProceduralRadiationBelt : MonoBehaviour
    {
     
        public GameObject Parent;               // 行星的 ScaledSpace GameObject
        public Material pointMaterial;

      
        public Vector3 starDirection = Vector3.left;

       
        public float innerDist = 2f;
        public float innerRadius = 0.5f;
        public float innerHeightScale = 2; 
        public float innerDeform = 0.2f;
        public int innerParticleCount = 8000;
        public float innerQuality = 30f;

        
        public float outerDist = 5f;
        public float outerRadius = 1.5f;
        public float outerHeightScale = 1.4f;  // 新增：外带通常更高
        public float outerBorderStart = 0.1f;
        public float outerBorderEnd = 1.0f;
        public float outerCompression = 0.6f;
        public float outerExtension = 1.5f;
        public float outerDeform = 0.15f;
        public int outerParticleCount = 15000;
        public float outerQuality = 40f;

        public ParticleMesh innerMesh;
        public ParticleMesh outerMesh;

        private int lastRenderedFrame = -1;

        public void LoadDataFromConfig(RadiationBeltConfig config)
        {
            this.enabled = config.Enabled;
            innerDist = config.innerDist;
            innerRadius = config.innerRadius;
            innerDeform = config.innerDeform;
            innerQuality = config.innerQuality;
            innerParticleCount = config.innerParticleCount;
            

            outerDist = config.outerDist;
            outerRadius = config.outerRadius;
            outerBorderStart = config.outerBorderStart;
            outerBorderEnd = config.outerBorderEnd;
            outerCompression = config.outerCompression;
            outerExtension = config.outerExtension;
            outerDeform = config.outerDeform;
            outerParticleCount = config.outerParticleCount;
            outerQuality = config.outerQuality;
        }

        public void RegenerateMeshes()
        {
            Debug.Log("Regenerating radiation belts...");

            // 彻底清理旧 mesh
            if (innerMesh != null)
            {
                foreach (var m in innerMesh.meshes ?? new List<Mesh>())
                    if (m) DestroyImmediate(m);
                innerMesh = null;
            }
            if (outerMesh != null)
            {
                foreach (var m in outerMesh.meshes ?? new List<Mesh>())
                    if (m) DestroyImmediate(m);
                outerMesh = null;
            }

            Func<Vector3, float> innerSDF = p =>
            {
                float dot = Vector3.Dot(p.normalized, starDirection);
                float deformFactor = Mathf.Lerp(outerCompression, outerExtension, (dot + 1f) / 2f);
                p /= deformFactor;

                // sine deform
                p += Mathf.Sin(p.magnitude * 5f) * innerDeform * p.normalized;

                // 新增：高度方向缩放，让它更圆
                p.y *= innerHeightScale;

                Vector2 q = new Vector2(new Vector2(p.x, p.z).magnitude - innerDist, p.y);
                return q.magnitude - innerRadius;
            };

            Func<Vector3, float> outerSDF = p =>
            {
                float dot = Vector3.Dot(p.normalized, starDirection);
                float deformFactor = Mathf.Lerp(outerCompression, outerExtension, (dot + 1f) / 2f);
                p /= deformFactor;

                p += Mathf.Sin(p.magnitude * 5f) * outerDeform * p.normalized;

                // 高度缩放
                p.y *= outerHeightScale;

                Vector2 q = new Vector2(new Vector2(p.x, p.z).magnitude - outerDist, p.y);
                float outer = q.magnitude - outerRadius;

                Vector2 q_sub = new Vector2(new Vector2(p.x, p.z).magnitude - outerDist * 0.8f, p.y);
                float subtract = q_sub.magnitude - outerRadius * 0.7f;

                float border = Mathf.Lerp(outerBorderStart, outerBorderEnd, Mathf.Clamp01(p.magnitude / outerDist));

                return Mathf.Max(outer, -subtract - border);
            };

            Vector3 hsize = new Vector3(outerDist + outerRadius * 2f, outerRadius * 2f * Mathf.Max(innerHeightScale, outerHeightScale), outerDist + outerRadius * 2f);
            Vector3 offset = Vector3.zero;

            innerMesh = new ParticleMesh(innerSDF, hsize, offset, innerParticleCount, innerQuality);
            outerMesh = new ParticleMesh(outerSDF, hsize * 1.2f, offset, outerParticleCount, outerQuality);
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