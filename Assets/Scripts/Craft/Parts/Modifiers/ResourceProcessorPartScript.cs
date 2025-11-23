using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using RootMotion.FinalIK;
using UnityEngine;

public abstract class ResourceProcessorPartScript<T> : PartModifierScript<T>, IFlightStart, IFlightUpdate, IDesignerStart,IFlightFixedUpdate
    where T : PartModifierData
{
    protected IFuelSource BatterySource { get; private set; }
    protected Transform Offset { get; private set; }
    protected Vector3 OffsetPositionInverse { get; set; }

    public virtual void FlightStart(in FlightFrameData frame)
    {
        UpdateFuelSources();
        UpdateComponents();
    }
    
    

    protected virtual void UpdateComponents()
    {
        
    }

    public virtual void FlightFixedUpdate(in FlightFrameData frame)
    {
        
    }
    public virtual void FlightUpdate(in FlightFrameData frame)
    {
        if (!PartScript.Data.Activated)
        {
            WorkingAnimation(false);
            return;
        }
        WorkingLogic(frame);
        WorkingAnimation(true);
    }

    public virtual void DesignerStart(in DesignerFrameData frame)
    {
        UpdateFuelSources();
    }

    public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
    {
        UpdateFuelSources();
    }

    public override void OnCraftStructureChanged(ICraftScript craftScript)
    {
        base.OnCraftStructureChanged(craftScript);
        UpdateFuelSources();
    }

    protected virtual void UpdateFuelSources()
    {
        BatterySource = PartScript.BatteryFuelSource;
    }

    protected virtual void WorkingLogic(in FlightFrameData frame)
    {
      
    }

    protected virtual void WorkingAnimation(bool active)
    {
    }

    protected IFuelSource GetCraftFuelSource(string fuelType)
    {
        var craftSources = PartScript.CraftScript.FuelSources.FuelSources;
        foreach (var source in craftSources)
        {
            if (source.FuelType.Id == fuelType)
            {
                return source;
            }
        }
        return null;
    }

    protected void SetSubPartWithOffset(Transform subPart, Vector3 positionOffset, ref Transform targetTransform)
    {
        if (Offset != null)
        {
            UnityEngine.Object.Destroy(Offset.gameObject);
            Offset = null;
        }
        targetTransform = subPart;
        if (targetTransform == null || positionOffset.magnitude <= 0.0f)
            return;

        Offset = new GameObject("SubPartRotatorOffset").transform;
        Offset.SetParent(targetTransform.parent, false);
        Offset.position = targetTransform.TransformPoint(positionOffset);
        OffsetPositionInverse = Offset.InverseTransformPoint(targetTransform.position);
    }
}