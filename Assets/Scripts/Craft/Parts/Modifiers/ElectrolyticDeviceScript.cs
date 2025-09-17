using ModApi;
using ModApi.Craft;
using ModApi.Design;
using ModApi.GameLoop;

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

    protected override void UpdateFuelSources()
    {
        BatterySource = PartScript.BatteryFuelSource;
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
            _hydrogenSource = GetCraftFuelSource("LH2");
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
            BatterySource.RemoveFuel(Data.OxygenGenerationRate * frame.DeltaTimeWorld);
            _oxygenSource.AddFuel(Data.PowerConsumptionRate * frame.DeltaTimeWorld);
            if (_hydrogenSource != null && _hydrogenSource.TotalCapacity - _hydrogenSource.TotalFuel > 0.000001f)
            {
                _hydrogenSource.AddFuel(Data.HydrogenGenerationRate * frame.DeltaTimeWorld);
            }
        }
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
        SetSubPart(subPart, Data.PositionOffset, ref _fanTransformBase);
        _fan1 = _fanTransformBase?.Find("fan1");
        _fan2 = _fanTransformBase?.Find("fan2");
    }
}
}