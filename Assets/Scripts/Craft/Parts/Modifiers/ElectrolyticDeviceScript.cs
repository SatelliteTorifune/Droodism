using ModApi;
using ModApi.GameLoop;
using ModApi.Math;
using ModApi.Ui.Inspector;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    
    public class ElectrolyticDeviceScript : ResourceProcessorPartScript<ElectrolyticDeviceData>, IPartSubPartSetUp
{
    private IFuelSource _waterSource, _oxygenSource, _hydrogenSource;
    private Transform _fanTransformBase, _fan1, _fan2;
    private float _fanSpeed;
    protected Transform Offset { get; set; }
    protected Vector3 OffsetPositionInverse { get; set; }

    public Transform SubPart => _fanTransformBase;

    
    protected override void UpdateFuelSources()
    {
       
        base.UpdateFuelSources();
        try
        {
            var patchScript = GetCommandPodPatch();
            if (patchScript == null)
            {
                _waterSource = _oxygenSource = _hydrogenSource = null;
                return;
            }
            _waterSource = patchScript.WaterFuelSource;
            _oxygenSource = patchScript.OxygenFuelSource;
            _hydrogenSource = GetRegularCraftFuelSource("LH2");
        }
        catch (Exception)
        {
            _waterSource = _oxygenSource = _hydrogenSource = null;
        }
    }

    protected override void WorkingLogic(in FlightFrameData frame)
    {
        BackgroundLogic(frame.DeltaTimeWorld);
    }

    #region 后台结算

    /// <summary>
    /// 通电且激活时参与后台补算（离视图期间继续电解水）。
    /// </summary>
    public override bool IsBackgroundUpdateEnabled => PartScript.Data.Activated;

    /// <summary>
    /// 按秒结算电解：水 + 电 → 氧气 + 氢气。
    /// 先用 <see cref="LimitBackgroundSeconds"/> 按可用水量/电量/氧气容量限制本步秒数，
    /// 避免大时间步长下"水空了还在产氧"。
    /// </summary>
    protected override void BackgroundLogic(double elapsedSeconds)
    {
        if (_waterSource == null || _oxygenSource == null || BatterySource == null)
        {
            return;
        }

        double waterPerSecond = Data.WaterComsuptionRate;
        double powerPerSecond = Data.PowerConsumptionRate;
        double oxygenPerSecond = Data.OxygenGenerationRate;
        double hydrogenPerSecond = Data.HydrogenGenerationRate;

        double seconds = elapsedSeconds;
        seconds = LimitBackgroundSeconds(seconds, _waterSource.TotalFuel, waterPerSecond);
        seconds = LimitBackgroundSeconds(seconds, BatterySource.TotalFuel, powerPerSecond);
        seconds = LimitBackgroundSeconds(seconds, _oxygenSource.TotalCapacity - _oxygenSource.TotalFuel, oxygenPerSecond);

        if (seconds <= 0.0)
        {
            return;
        }

        _waterSource.RemoveFuel(waterPerSecond * seconds);
        BatterySource.RemoveFuel(powerPerSecond * seconds);
        _oxygenSource.AddFuel(oxygenPerSecond * seconds);

        // 氢气只是副产物：储罐满了就排掉，不阻塞电解（与实时逻辑一致）
        if (_hydrogenSource != null)
        {
            double hydrogenSpace = _hydrogenSource.TotalCapacity - _hydrogenSource.TotalFuel;
            if (hydrogenSpace > 0.0)
            {
                _hydrogenSource.AddFuel(Math.Min(hydrogenPerSecond * seconds, hydrogenSpace));
            }
        }
    }

    #endregion
    public override void OnGenerateInspectorModel(PartInspectorModel model)
    {
        base.OnGenerateInspectorModel(model);
        model.Add<TextModel>(new TextModel("<color=yellow>" + Locale.GetString("Droodism.ElectrolyticDeviceScript.WaterConsumptionRate") + " ", (Func<string>)(() => Units.GetMassString(PartScript.Data.Activated?(float)Data.WaterComsuptionRate*_waterSource.FuelType.Density*0.00025f:0))));
        model.Add<TextModel>(new TextModel("<color=yellow>" + Locale.GetString("Droodism.ElectrolyticDeviceScript.OxygenGenerationRate") + " ", (Func<string>)(() => Units.GetMassString(PartScript.Data.Activated?(float)Data.OxygenGenerationRate*_oxygenSource.FuelType.Density*0.145f:0))));
        model.Add<TextModel>(new TextModel("<color=green>" + Locale.GetString("Droodism.ElectrolyticDeviceScript.PowerConsumptionRate") + " ", (Func<string>)(() => Units.GetPowerString(PartScript.Data.Activated?(float)Data.PowerConsumptionRate*231f:0))));

    }

    protected override void WorkingAnimation(bool active)
    {
        float targetSpeed = active ? 0.75f : 0.0f;
        _fanSpeed = Mathf.Lerp(_fanSpeed, targetSpeed, Time.deltaTime * 0.5f);
        if (_fanSpeed > 0.0f)
        {
            float zAngle = -_fanSpeed * 360.0f * 3.0f * Time.deltaTime;
            _fan1.Rotate(0.0f, 0.0f, zAngle);
            _fan2.Rotate(0.0f, 0.0f, -zAngle);
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        UpdateComponents();
    }

    protected override void UpdateComponents()
    {
        SetSubPart(IPartSubPartSetUp.FindSubPart(this, "Device/DeviceFan"));
        _fan1 = _fanTransformBase?.Find("fan1");
        _fan2 = _fanTransformBase?.Find("fan2");
    }

    public void SetSubPart(Transform subPart)
    {
        _fanTransformBase = subPart;
        Vector3 offsetPositionInverse;
        Offset = IPartSubPartSetUp.ApplySubPart(Offset, _fanTransformBase, Data.PositionOffset, out offsetPositionInverse);
        OffsetPositionInverse = offsetPositionInverse;
    }

    

 
}
}