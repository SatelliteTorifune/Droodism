using System.Collections.Generic;
using Assets.Scripts.Droodism.BackGround;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using RootMotion.FinalIK;
using Assets.Scripts.Craft.Parts.Modifiers;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using Assets.Scripts.Droodism.BackGround.Interface;
using ModApi.Ui.Inspector;
using UnityEngine;

public abstract class ResourceProcessorPartScript<T> : PartModifierScript<T>, IBackGroudCalculate, IFlightStart, IAnalyzePerformance, IFlightUpdate, IDesignerStart,IFlightFixedUpdate
    where T : PartModifierData
{
    protected IFuelSource BatterySource { get; set; }
  

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
        return PartScriptUtilities.FindCraftFuelSource(PartScript, fuelType);
    }

    protected STCommandPodPatchScript GetCommandPodPatch()
    {
        return PartScriptUtilities.GetCommandPodPatch(PartScript);
    }

    public virtual void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
    {
        
    }

    public bool UsesMachNumber { get; }

    #region 后台结算

    /// <summary>
    /// 是否参与后台补算。默认不参与：子类实现了 <see cref="BackgroundLogic"/> 后，
    /// 必须同时重写此属性返回 true（具体条件由子类判断，例如未激活、无燃料源时应返回 false）。
    /// </summary>
    public virtual bool IsBackgroundUpdateEnabled => false;

    /// <summary>
    /// 后台补算入口：先把燃料源引用刷新一遍，再交给子类按秒结算。
    /// </summary>
    public void BackgroundUpdate(double elapsedSeconds)
    {
        UpdateFuelSources();
        BackgroundLogic(elapsedSeconds);
    }

    /// <summary>
    /// 按秒（而不是按帧）结算的后台逻辑。
    /// 约定：与 <see cref="WorkingLogic"/> 使用同一套速率公式，避免前后台数值不一致。
    /// </summary>
    /// <param name="elapsedSeconds">距上次结算经过的游戏世界时间（秒）。</param>
    protected virtual void BackgroundLogic(double elapsedSeconds)
    {
    }

    /// <summary>
    /// 大型时间步长下按可用量限制可结算秒数：避免"输入已经耗尽、输出却按整段时间照发"的账目不平。
    /// 详见 <see cref="BackgroundSettlement.LimitSeconds"/>。
    /// </summary>
    protected static double LimitBackgroundSeconds(double requestedSeconds, double available, double perSecond)
    {
        return BackgroundSettlement.LimitSeconds(requestedSeconds, available, perSecond);
    }

    #endregion
}
