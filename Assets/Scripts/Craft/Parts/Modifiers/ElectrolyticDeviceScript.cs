using ModApi;
using ModApi.Craft;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.Math;
using ModApi.Ui.Inspector;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    
    public class ElectrolyticDeviceScript : ResourceProcessorPartScript<ElectrolyticDeviceData>
{
    private IFuelSource _waterSource, _oxygenSource, _hydrogenSource;
    private Transform _fanTransformBase, _fan1, _fan2;
    private float _fanSpeed;
    protected Transform Offset { get; set; }
    protected Vector3 OffsetPositionInverse { get; set; }

    
    protected override void UpdateFuelSources()
    {
       
        base.UpdateFuelSources();
        try
        {
            var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
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
        if (_waterSource == null || _oxygenSource == null || BatterySource == null)
            return;

        if (!BatterySource.IsEmpty && !_waterSource.IsEmpty && _oxygenSource.TotalCapacity - _oxygenSource.TotalFuel > 0.000001f)
        {
            _waterSource.RemoveFuel(Data.WaterComsuptionRate * frame.DeltaTimeWorld);
            BatterySource.RemoveFuel(Data.PowerConsumptionRate * frame.DeltaTimeWorld);
            _oxygenSource.AddFuel(Data.OxygenGenerationRate * frame.DeltaTimeWorld);
            if (_hydrogenSource != null && _hydrogenSource.TotalCapacity - _hydrogenSource.TotalFuel > 0.000001f)
            {
                _hydrogenSource.AddFuel(Data.HydrogenGenerationRate * frame.DeltaTimeWorld);
            }
        }
    }
    public override void OnGenerateInspectorModel(PartInspectorModel model)
    {
        base.OnGenerateInspectorModel(model);
        model.Add<TextModel>(new TextModel("<color=yellow>Water Consumption Rate ", (Func<string>)(() => Units.GetMassString(PartScript.Data.Activated?(float)Data.WaterComsuptionRate*_waterSource.FuelType.Density*0.00025f:0))));
        model.Add<TextModel>(new TextModel("<color=yellow>Oxygen Generation Rate ", (Func<string>)(() => Units.GetMassString(PartScript.Data.Activated?(float)Data.OxygenGenerationRate*_oxygenSource.FuelType.Density*0.145f:0))));
        model.Add<TextModel>(new TextModel("<color=green>Power Consumption Rate ", (Func<string>)(() => Units.GetPowerString(PartScript.Data.Activated?(float)Data.PowerConsumptionRate*231f:0))));

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

    private void UpdateComponents()
    {
        string[] strArray = "Device/DeviceFan".Split('/', StringSplitOptions.None);
        Transform subPart = transform;
        foreach (string n in strArray)
            subPart = subPart.Find(n) ?? subPart;
        if (subPart.name != strArray[strArray.Length - 1])
            subPart = Utilities.FindFirstGameObjectMyselfOrChildren("Device/DeviceFan", gameObject)?.transform;
        SetSubPartWithOffset(subPart, Data.PositionOffset, ref _fanTransformBase);
        _fan1 = _fanTransformBase?.Find("fan1");
        _fan2 = _fanTransformBase?.Find("fan2");
    }
    private void SetSubPartWithOffset(Transform subPart, Vector3 positionOffset, ref Transform targetTransform)
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
}