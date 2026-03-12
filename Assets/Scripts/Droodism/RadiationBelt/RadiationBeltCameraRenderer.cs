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

            if (!beltRenderer.pointMaterial.SetPass(0)||!RadiationBeltDebugUI.Instance.ShowGeneral)
            {
                return;
            }
            if (RadiationBeltDebugUI.Instance.ShowInner)
            {
                beltRenderer.innerMesh?.Render( beltRenderer.transform.localToWorldMatrix);
            }
            
            if (RadiationBeltDebugUI.Instance.ShowOuter)
            {
                beltRenderer.outerMesh?.Render( beltRenderer.transform.localToWorldMatrix);
            }
           
            
            
        }
    }
}