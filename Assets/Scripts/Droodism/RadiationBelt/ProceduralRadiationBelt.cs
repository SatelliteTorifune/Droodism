using System;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;

namespace Droodism.RadiationBelt
{


    public class ProceduralRadiationBelt : MonoBehaviour
    {
        public GameObject Parent;
        [Header("通用参数")] public Material pointMaterial; // 拖入 Particles/Additive 或自定义 Unlit/Transparent 材质
        public Vector3 starDirection = Vector3.left; // 模拟太阳方向 (normalized)

        [Header("内带参数 (类似 Kerbalism inner)")] public float innerDist = 2f; // 主半径
        public float innerRadius = 0.5f; // 管半径
        public float innerDeform = 0.2f; // 扰动幅度
        public int innerParticleCount = 8000; // 粒子数
        public float innerQuality = 30f; // 质量 (越高越薄)

        [Header("外带参数 (肾形 outer)")] public float outerDist = 5f;
        public float outerRadius = 1.5f;
        public float outerBorderStart = 0.1f; // 内减法渐变
        public float outerBorderEnd = 1.0f;
        public float outerCompression = 0.6f; // 太阳侧压缩
        public float outerExtension = 1.5f; // 尾侧拉伸
        public float outerDeform = 0.15f;
        public int outerParticleCount = 15000;
        public float outerQuality = 40f;

        public ParticleMesh innerMesh;
        public ParticleMesh outerMesh;

        private bool needsRegenerate = true;


        void Start()
        {
            RegenerateMeshes();
            
        }

        public void RegenerateMeshes()
        {
            Debug.Log("Regenerating radiation belts...");

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

            //this.transform.eulerAngles = Parent.transform.eulerAngles;
            //this.transform.rotation = Parent.transform.rotation;

        }




        void OnValidate()
        {
            needsRegenerate = true; // 只要改了参数，就标记为需要重生成
        }

        void Update()
        {
            SycWithParent();
            if (needsRegenerate)
            {
                RegenerateMeshes(); // ← 这里调用
                needsRegenerate = false;
            }
        }
    }
}