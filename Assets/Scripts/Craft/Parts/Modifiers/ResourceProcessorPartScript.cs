using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using UnityEngine;

public abstract class ResourceProcessorPartScript<T> : PartModifierScript<T>, IFlightStart, IFlightUpdate, IDesignerStart
    where T : PartModifierData
{
    protected IFuelSource BatterySource { get; set; }
    protected Transform Offset { get; set; }
    protected Vector3 OffsetPositionInverse { get; set; }

    public virtual void FlightStart(in FlightFrameData frame)
    {
        UpdateFuelSources();
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
        UpdateFuelSources();
    }

    protected abstract void UpdateFuelSources();

    protected virtual void WorkingLogic(in FlightFrameData frame)
    {
        // 子类实现具体的工作逻辑
    }

    protected virtual void WorkingAnimation(bool active)
    {
        // 子类实现具体的动画逻辑（如果需要）
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

    protected void SetSubPart(Transform subPart, Vector3 positionOffset, ref Transform targetTransform)
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