using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Droodism.RadiationBelt
{
    public sealed class ParticleMesh
    {
        
        public ParticleMesh(List<Vector3> points)
        {
            this.points = points;
        }

        // 我操你妈的,给我滚去异步计算去你妈滴
        public static async Task<ParticleMesh> CreateAsync
        (
            Func<Vector3, float> dist_func, 
            Vector3 domain_hsize, 
            Vector3 domain_offset,
            int particle_count, 
            float quality)
        {
            var points = await Task.Run(() => GeneratePoints(dist_func, domain_hsize, domain_offset, particle_count, quality));
            return new ParticleMesh(points);
        }
        
        
        private static List<Vector3> GeneratePoints(
            Func<Vector3, float> dist_func, 
            Vector3 domain_hsize, 
            Vector3 domain_offset,
            int particle_count, 
            float quality)
        {
            // store stuff
            Vector3 p;
            float D;

            // hard-limit on sample count, to avoid infinite sampling when the distance function is positive everywhere
            int sample_limit = particle_count * 1000;

            // divide once
            float thickness = 1.0f / quality;

            // preallocate position container
            var points = new List<Vector3>(particle_count);

            // particle-fitting
            int samples = 0;
            int i = 0;
            while (i < particle_count && samples < sample_limit)
            {
                // generate random position inside bounding volume
                p.x = UnityEngine.Random.value * domain_hsize.x * 2f + domain_offset.x - domain_hsize.x;
                p.y = UnityEngine.Random.value * domain_hsize.y * 2f + domain_offset.y - domain_hsize.y;
                p.z = UnityEngine.Random.value * domain_hsize.z * 2f + domain_offset.z - domain_hsize.z;

                // calculate signed distance
                D = dist_func(p);

                if (D <= 0.0f) // if inside
                {
                    // this displays the exact radiation field border
                    if (D <= 0.0 && D > -thickness)
                    {
                        points.Add(p);
                        ++i;
                    }
                }

                // count samples
                ++samples;
            }

            return points;
        }

        void Compile()
        {
            // max number of particles that can be stored in a unity mesh
            const int max_particles = 64000;

            // create the set of meshes
            meshes = new List<Mesh>(points.Count / max_particles + 1);
            Mesh m;
            List<Vector3> t_points = new List<Vector3>(max_particles);
            List<int> t_indexes = new List<int>(max_particles);
            for (var i = 0; i < points.Count; ++i)
            {
                t_points.Add(points[i]);
                t_indexes.Add(t_indexes.Count);
                if (t_indexes.Count > max_particles || i == points.Count - 1)
                {
                    m = new Mesh();
                    m.SetVertices(t_points);
                    m.SetIndices(t_indexes.ToArray(), MeshTopology.Points, 0);
                    m.UploadMeshData(true); // 优化内存
                    meshes.Add(m);
                    t_points.Clear();
                    t_indexes.Clear();
                }
            }

            points = null;
        }

        // render all the meshes
        public void Render(Matrix4x4 m)
        {
            if (meshes == null)
            {
                Compile();
            }

            foreach (var mesh in meshes)
            {
                Graphics.DrawMeshNow(mesh, m);
            }
        }

        private List<Vector3> points; // set of points
        public List<Mesh> meshes; // set of meshes
    }
}
