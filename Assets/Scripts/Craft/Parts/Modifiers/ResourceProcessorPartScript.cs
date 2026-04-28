using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using RootMotion.FinalIK;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using ModApi.Ui.Inspector;
using UnityEngine;

public abstract class ResourceProcessorPartScript<T> : PartModifierScript<T>, IFlightStart, IAnalyzePerformance, IFlightUpdate, IDesignerStart,IFlightFixedUpdate
    where T : PartModifierData
{
    protected IFuelSource BatterySource { get; private set; }
  

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

    protected IFuelSource GetRegularCraftFuelSource(string fuelType)
    {
        foreach (var source in PartScript.CraftScript.FuelSources.FuelSources)
        {
            if (source.FuelType.Id == fuelType)
            {
                return source;
            }
        }
        return null;
    }

    public virtual void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
    {
        
    }

    public bool UsesMachNumber { get; }
}