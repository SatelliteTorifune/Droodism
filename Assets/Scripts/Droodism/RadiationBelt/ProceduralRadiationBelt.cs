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
        public GameObject Parent;
        public Material pointMaterial; 
        public Vector3 starDirection = Vector3.left;

        private float innerDist = 2f; // 主半径
        private float innerRadius = 0.5f; // 管半径
        private float innerDeform = 0.2f; // 扰动幅度
        private int innerParticleCount = 8000; // 粒子数
        private float innerQuality = 30f; // 质量 (越高越薄)

        private float outerDist = 5f;
        private float outerRadius = 1.5f;
        private float outerBorderStart = 0.1f; // 内减法渐变
        private float outerBorderEnd = 1.0f;
        private float outerCompression = 0.6f; // 太阳侧压缩
        private float outerExtension = 1.5f; // 尾侧拉伸
        private float outerDeform = 0.15f;
        private int outerParticleCount = 15000;
        private float outerQuality = 40f;

        public ParticleMesh innerMesh;
        public ParticleMesh outerMesh;
        

        public void LoadDataFromConfig(RadiationBeltConfig config)
        {
            this.innerDist = config.innerDist;
            this.innerRadius = config.innerRadius;
            this.innerDeform=config.innerDeform;
            this.innerQuality=config.innerQuality;
            this.innerParticleCount=config.innerParticleCount;
            
            this.outerDist=config.outerDist;
            this.outerRadius=config.outerRadius;
            this.outerBorderStart=config.outerBorderStart;
            this.outerBorderEnd=config.outerBorderEnd;
            this.outerCompression=config.outerCompression;
            this.outerExtension=config.outerExtension;
            this.outerParticleCount=config.outerParticleCount;
            this.outerQuality=config.outerQuality;
        }

        public void RegenerateMeshes()
        {

            // 彻底销毁旧 mesh，防止鬼影
            if (innerMesh != null)
            {
                foreach (var m in innerMesh.meshes ?? new List<Mesh>())
                {
                    if (m != null) DestroyImmediate(m); // 编辑器用 DestroyImmediate
                }

                innerMesh = null;
            }

            if (outerMesh != null)
            {
                foreach (var m in outerMesh.meshes ?? new List<Mesh>())
                {
                    if (m != null) DestroyImmediate(m);
                }

                outerMesh = null;
            }

            Func<Vector3, float> innerSDF = (Vector3 p) =>
            {
                // 沿太阳方向变形
                float dot = Vector3.Dot(p.normalized, starDirection);
                float deformFactor = Mathf.Lerp(outerCompression, outerExtension, (dot + 1f) / 2f);
                p /= deformFactor;

                // 加 sine deform
                p += Mathf.Sin(Vector3.Magnitude(p * 5f)) * innerDeform * p.normalized;

                // torus SDF
                Vector2 q = new Vector2(new Vector2(p.x, p.z).magnitude - innerDist, p.y);
                return q.magnitude - innerRadius;
            };

            // 定义外带 SDF (肾形: torus - subtract torus + border)
            Func<Vector3, float> outerSDF = (Vector3 p) =>
            {
                // 变形同上
                float dot = Vector3.Dot(p.normalized, starDirection);
                float deformFactor = Mathf.Lerp(outerCompression, outerExtension, (dot + 1f) / 2f);
                p /= deformFactor;

                p += Mathf.Sin(Vector3.Magnitude(p * 5f)) * outerDeform * p.normalized;

                // base torus
                Vector2 q = new Vector2(new Vector2(p.x, p.z).magnitude - outerDist, p.y);
                float outer = q.magnitude - outerRadius;

                // subtract inner torus
                Vector2 q_sub = new Vector2(new Vector2(p.x, p.z).magnitude - outerDist * 0.8f, p.y);
                float subtract = q_sub.magnitude - outerRadius * 0.7f;

                // border fade
                float border = Mathf.Lerp(outerBorderStart, outerBorderEnd, Mathf.Clamp01(p.magnitude / outerDist));

                return Mathf.Max(outer, -subtract - border);
            };

            // 包围盒 (domain): 稍大于辐射带大小
            Vector3 hsize = new Vector3(outerDist + outerRadius * 2f, outerRadius * 2f, outerDist + outerRadius * 2f);
            Vector3 offset = Vector3.zero;

            // 生成新 mesh
            innerMesh = new ParticleMesh(innerSDF, hsize, offset, innerParticleCount, innerQuality);
            outerMesh = new ParticleMesh(outerSDF, hsize * 1.2f, offset, outerParticleCount, outerQuality);
        }

        private void SycWithParent()
        {
            //和星球同步位置和rotation,但是rotation同步不了
            this.transform.position = Parent.transform.position;

            //这破玩意是干啥的,没测
            //this.transform.eulerAngles = Parent.transform.eulerAngles;
            //this.transform.rotation = Parent.transform.rotation;

        }

        void Update()
        {
            SycWithParent();
            //Mod.LOG($"current is { Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.MapViewInspector.SelectedItem.AssociatedPlanet.Name}");
           
            
        }
    }
}