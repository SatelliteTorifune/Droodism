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
     
        public GameObject Parent;               
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
                Func<Vector3, float> innerSDF = p => EllipticalCrescentTorusSDF(
                    Deform(p, config.innerHeightScale, config.innerDeform),
                    config.innerMajorRadius,
                    config.innerOuterCenterX, config.innerOuterCenterY, 
                    config.innerOuterRadiusX, config.innerOuterRadiusY,
                    config.innerCoreCenterX, config.innerCoreCenterY,
                    config.innerCoreRadiusX, config.innerCoreRadiusY
                );

                Func<Vector3, float> outerSDF = p => EllipticalCrescentTorusSDF(
                    Deform(p, config.outerHeightScale, config.outerDeform),
                    config.outerMajorRadius,
                    config.outerOuterCenterX, config.outerOuterCenterY,
                    config.outerOuterRadiusX, config.outerOuterRadiusY,
                    config.outerCoreCenterX, config.outerCoreCenterY,
                    config.outerCoreRadiusX, config.outerCoreRadiusY
                );


                Vector3 innerSize = new Vector3(
                    (config.innerMajorRadius + config.innerMinorRadius) * 2.2f,
                    config.innerMinorRadius * 6f * config.innerHeightScale,
                    (config.innerMajorRadius + config.innerMinorRadius) * 2.2f
                );
                
                Vector3 outerSize = new Vector3(
                    (config.outerMajorRadius + config.outerMinorRadius) * 3.5f,
                    config.outerMinorRadius * 6f * config.outerHeightScale,
                    (config.outerMajorRadius + config.outerMinorRadius) * 3.5f
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
        
       
        private float CrescentTorusSDF(Vector3 worldPos, float majorRadius, float outerRadius, float coreOffset, float coreRadius)
        {
            // 转换到torus局部坐标系
            float radialDist = new Vector2(worldPos.x, worldPos.z).magnitude;
            Vector2 localPos = new Vector2(radialDist - majorRadius, worldPos.y);
    
            // 外圆（主环）- 可以调整中心偏移和半径
            Vector2 outerCenter = new Vector2(config.outerCoreCenterX, config.outerCoreCenterY); // 默认(0,0)
            Vector2 outerToPos = localPos - outerCenter;
            float outerCircle = outerToPos.magnitude - outerRadius;
    
            // 内圆（挖空部分）- 可以独立调整中心偏移和半径
            Vector2 coreCenter = new Vector2(config.innerCoreCenterX, config.innerCoreCenterY); // 默认可以是非同心的
            Vector2 coreToPos = localPos - coreCenter;
            float innerCircle = coreToPos.magnitude - coreRadius;
    
            // 月牙形 = 外圆内部 AND NOT(内圆内部)
            if (outerCircle <= 0 && innerCircle > 0)
            {
                return outerCircle;
            }
            else if (outerCircle > 0)
            {
                return outerCircle;
            }
            else
            {
                return 1000f;
            }
        }
        // 椭圆月牙形torus SDF
        private float EllipticalCrescentTorusSDF(Vector3 worldPos, float majorRadius,
            float outerCenterX, float outerCenterY, float outerRadiusX, float outerRadiusY,
            float coreCenterX, float coreCenterY, float coreRadiusX, float coreRadiusY)
        {
            // 转换到torus局部坐标系
            float radialDist = new Vector2(worldPos.x, worldPos.z).magnitude;
            Vector2 localPos = new Vector2(radialDist - majorRadius, worldPos.y);
            
            // 外椭圆（主环）- 使用椭圆距离场
            Vector2 outerCenter = new Vector2(outerCenterX, outerCenterY);
            Vector2 outerRelative = localPos - outerCenter;
            float outerEllipse = EllipseSDF(outerRelative, outerRadiusX, outerRadiusY);
            
            // 内椭圆（挖空部分）
            Vector2 coreCenter = new Vector2(coreCenterX, coreCenterY);
            Vector2 coreRelative = localPos - coreCenter;
            float innerEllipse = EllipseSDF(coreRelative, coreRadiusX, coreRadiusY);
            
            // 月牙形区域判定
            if (outerEllipse <= 0 && innerEllipse > 0)
            {
                return outerEllipse; // 在月牙形区域内
            }
            else if (outerEllipse > 0)
            {
                return outerEllipse; // 在外椭圆外部
            }
            else
            {
                return 1000f; // 在内椭圆内部（被挖掉）
            }
        }
        
        // 椭圆SDF函数
        private float EllipseSDF(Vector2 p, float radiusX, float radiusY)
        {
            // 将点归一化到椭圆坐标系
            Vector2 normalized = new Vector2(p.x / radiusX, p.y / radiusY);
            
            // 计算到椭圆的距离
            float distance = normalized.magnitude;
            
            // 椭圆SDF近似公式
            if (distance < 1e-6f) return -Mathf.Max(radiusX, radiusY);
            
            // 更精确的椭圆SDF
            float angle = Mathf.Atan2(normalized.y, normalized.x);
            float ellipseRadius = radiusX * radiusY / Mathf.Sqrt(
                radiusX * radiusX * Mathf.Sin(angle) * Mathf.Sin(angle) +
                radiusY * radiusY * Mathf.Cos(angle) * Mathf.Cos(angle)
            );
            
            return distance * Mathf.Max(radiusX, radiusY) - ellipseRadius;
        }
        
        // 简化版椭圆SDF（性能更好）
        private float SimpleEllipseSDF(Vector2 p, float radiusX, float radiusY)
        {
            Vector2 e = new Vector2(radiusX, radiusY);
            Vector2 normalized = new Vector2(p.x / e.x, p.y / e.y);
            float distNormalized = normalized.magnitude;
            if (distNormalized < 1e-6f) return -Mathf.Min(e.x, e.y);
            return distNormalized * Mathf.Min(e.x, e.y) - Mathf.Min(e.x, e.y);
        }


        
        private Vector3 Deform(Vector3 p, float heightScale, float deform)
        {
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
            transform.rotation = Parent.transform.rotation;
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
