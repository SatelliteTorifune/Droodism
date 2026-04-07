using UnityEngine;

namespace Droodism.RadiationBelt
{
    public class RadiationBeltCameraRenderer : MonoBehaviour
    {
        public ProceduralRadiationBelt beltRenderer; 


        private int lastRenderedFrame = -1;

        void OnPostRender()
        {
           
#if !UNITY_EDITOR
        if (!ModApi.Common.Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible)
        {
            return;
        }
#endif

            if (beltRenderer == null || beltRenderer.pointMaterial == null)
            {
                return;
            }

         
            if (Time.frameCount == lastRenderedFrame) return;
            lastRenderedFrame = Time.frameCount;

            if (!beltRenderer.pointMaterial.SetPass(0)||!RadiationBeltDebugUI.Instance.ShowGeneral||!RadiationBeltManager.Instance.CurrentConfig.Enabled)
            {
                return;
            }
            if (RadiationBeltDebugUI.Instance.ShowInner)
            {
                float radiusScale = RadiationBeltManager.Instance != null
                    ? RadiationBeltManager.Instance.GetCurrentPlanetRenderRadiusUnits()
                    : 1f;
                Matrix4x4 renderMatrix = beltRenderer.transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * radiusScale);
                beltRenderer.innerMesh?.Render(renderMatrix);
            }
            
            if (RadiationBeltDebugUI.Instance.ShowOuter)
            {
                float radiusScale = RadiationBeltManager.Instance != null
                    ? RadiationBeltManager.Instance.GetCurrentPlanetRenderRadiusUnits()
                    : 1f;
                Matrix4x4 renderMatrix = beltRenderer.transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * radiusScale);
                beltRenderer.outerMesh?.Render(renderMatrix);
            }
           
            
            
        }
    }
}