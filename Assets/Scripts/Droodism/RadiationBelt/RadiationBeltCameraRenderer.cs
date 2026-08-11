using ModApi;
using UnityEngine;

namespace Assets.Scripts.Droodism.RadiationBelt
{
    public class RadiationBeltCameraRenderer : MonoBehaviour
    {
        public ProceduralRadiationBelt beltRenderer; 

        private int lastRenderedFrame = -1;

        void OnPostRender()
        {
            if (beltRenderer == null || beltRenderer.pointMaterial == null) return;
            if (Time.frameCount == lastRenderedFrame) return;
            lastRenderedFrame = Time.frameCount;

            if (!beltRenderer.pointMaterial.SetPass(0)) return;

            if (Game.InMenuScene)
                RenderMenuScene();
            else
                RenderFlightScene();
        }

        private void RenderFlightScene()
        {
            if (RadiationBeltDebugUI.Instance == null || !RadiationBeltDebugUI.Instance.ShowGeneral) return;

            var mgr = RadiationBeltManager.Instance;
            if (mgr?.CurrentConfig == null || !mgr.CurrentConfig.Enabled) return;

            float radiusScale = mgr.GetCurrentPlanetRenderRadiusUnits();

            if (RadiationBeltDebugUI.Instance.ShowInner && beltRenderer.innerMesh != null)
            {
                Matrix4x4 renderMatrix = beltRenderer.transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * radiusScale);
                beltRenderer.innerMesh.Render(renderMatrix);
            }

            if (RadiationBeltDebugUI.Instance.ShowOuter && beltRenderer.outerMesh != null)
            {
                Matrix4x4 renderMatrix = beltRenderer.transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * radiusScale);
                beltRenderer.outerMesh.Render(renderMatrix);
            }
        }

        private void RenderMenuScene()
        {
            var menuMgr = MenuMapRadiationBeltManager.Instance;
            if (menuMgr == null) return;

            float visualRadius = menuMgr.GetCurrentPlanetRenderRadiusUnits();

            Matrix4x4 renderMatrix = Matrix4x4.TRS(
                beltRenderer.transform.position,
                beltRenderer.transform.rotation,
                Vector3.one * visualRadius
            );

            bool showInner = true, showOuter = true;
            if (RadiationBeltDebugUI.Instance != null)
            {
                showInner = RadiationBeltDebugUI.Instance.ShowInner;
                showOuter = RadiationBeltDebugUI.Instance.ShowOuter;
            }

            if (showInner && beltRenderer.innerMesh != null)
                beltRenderer.innerMesh.Render(renderMatrix);
            if (showOuter && beltRenderer.outerMesh != null)
                beltRenderer.outerMesh.Render(renderMatrix);
        }
    }
}
