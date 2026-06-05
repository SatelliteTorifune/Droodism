using System;
using System.Windows.Forms;
using Assets.Scripts;
using ModApi.GameLoop;
using ModApi.Scenes;
using ModApi.Scenes.Events;
using ModApi.Ui.Inspector;
using UnityEngine;


namespace Droodism.RadiationBelt
{
    public class RadiationBeltDebugUI : MonoBehaviourBase
    {
        public static RadiationBeltDebugUI Instance;

        private IInspectorPanel inspectorPanel;
        private InspectorModel inspectorModel;

        public bool ShowGeneral { get; set; } = true;
        public bool ShowInner { get; set; }= true;
        public bool ShowOuter { get; set; }= true;
        
        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Instance = this;
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            
            if (e.Scene != "Flight")
            {
                return;
            }
        }

        public void OnToggleInspectorPanelState()
        {
            try
            {
                inspectorPanel.Visible = !inspectorPanel.Visible;
            }
            catch (Exception)
            {

                CreateInspectorPanel();
                inspectorPanel.Visible = true;
            }
        }
         public void CreateInspectorPanel()
         {
             inspectorModel = new InspectorModel("Radiation Belt Inspector", "<color=red>Radiation Belt Debug Inspector");

             #region Debug
             inspectorModel.Add(new TextButtonModel("Force Rebuild Inspector", (b) =>
             {
                 this.inspectorPanel.Visible = false;
                 this.inspectorPanel = null;
                 CreateInspectorPanel();
             }));
             inspectorModel.Add(new TextButtonModel("ReGenerate Belt Mesh", (b) =>
             {
                 RadiationBeltManager.Instance.ReGenerateMeshes();
             }));
             inspectorModel.Add(new TextButtonModel("Save Current Config", (Action<TextButtonModel>)(b => 
             {
                 RadiationBeltManager.Instance.CurrentConfig.SaveToFile(RadiationBeltManager.Instance.CurrentFocusPlanet);
             })));
             inspectorModel.Add(new TextButtonModel("Load Current Config", (Action<TextButtonModel>)(b => 
             {
                 RadiationBeltManager.Instance.ReFreshCurrentConfig();
                 RadiationBeltManager.Instance.ReGenerateMeshes();
             })));
             
             inspectorModel.Add(new TextButtonModel("Return to Default Preset", (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplyDefaultPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new TextButtonModel("Apply Giant Preset", (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplyGiantPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new TextButtonModel("Apply Metallic Preset", (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplyMetallicPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new TextButtonModel("Apply SolidIron Preset", (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplySolidIronPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new TextButtonModel("Apply Anomaly Preset", (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplyAnomalyPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new ToggleModel("show",()=>ShowGeneral,(b =>
             {
                 ShowGeneral = b;
             })));
             inspectorModel.Add(new ToggleModel("show inner",()=>ShowInner,b =>
             {
                 ShowInner = b;
             }));
             inspectorModel.Add(new ToggleModel("show outer",()=>ShowOuter,b =>
             {
                 ShowOuter = b;
             }));
             #endregion

             #region General
             GroupModel GeneralGroupModel = new GroupModel("General");

             GeneralGroupModel.Add(new TextModel("Current Planet", ()=>
             
                 RadiationBeltManager.Instance.CurrentFocusPlanet
             ));
             GeneralGroupModel.Add(new ToggleModel("Main Enabled",()=> RadiationBeltManager.Instance.CurrentConfig.Enabled,b =>
             {
                 RadiationBeltManager.Instance.CurrentConfig.Enabled = b;
             }));
             
             var renderMetersPerUnit = new SliderModel("Render Meters Per Unit", () => RadiationBeltManager.Instance.CurrentConfig.renderMetersPerUnit,
                 s => { RadiationBeltManager.Instance.CurrentConfig.renderMetersPerUnit = s; }, 10000f, 10000000f, false);
             renderMetersPerUnit.ValueFormatter = f => FormatValue(f, 0);
             GeneralGroupModel.Add(renderMetersPerUnit);
             inspectorModel.AddGroup(GeneralGroupModel);
             #endregion
             #region Tilt
             GroupModel TiltGroupModel = new GroupModel("Tilt Group");

             var beltTilt = new SliderModel("Belt Tilt (Deg)", () => RadiationBeltManager.Instance.CurrentConfig.beltTiltDegrees,
                 s => { RadiationBeltManager.Instance.CurrentConfig.beltTiltDegrees = s; }, -90f, 90f);
             beltTilt.ValueFormatter = f => FormatValue(f, 2);
             TiltGroupModel.Add(beltTilt);

             var beltTiltAxisX = new SliderModel("Belt Tilt Axis X", () => RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis.x,
                 s =>
                 {
                     var axis = RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis;
                     axis.x = s;
                     RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis = axis;
                 }, -1f, 1f);
             beltTiltAxisX.ValueFormatter = f => FormatValue(f, 3);
             TiltGroupModel.Add(beltTiltAxisX);

             var beltTiltAxisY = new SliderModel("Belt Tilt Axis Y", () => RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis.y,
                 s =>
                 {
                     var axis = RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis;
                     axis.y = s;
                     RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis = axis;
                 }, -1f, 1f);
             beltTiltAxisY.ValueFormatter = f => FormatValue(f, 3);
             TiltGroupModel.Add(beltTiltAxisY);

             var beltTiltAxisZ = new SliderModel("Belt Tilt Axis Z", () => RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis.z,
                 s =>
                 {
                     var axis = RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis;
                     axis.z = s;
                     RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis = axis;
                 }, -1f, 1f);
             beltTiltAxisZ.ValueFormatter = f => FormatValue(f, 3);
             TiltGroupModel.Add(beltTiltAxisZ);

             var beltSpinSpeed = new SliderModel("Belt Spin Speed (Deg/s)", () => RadiationBeltManager.Instance.CurrentConfig.beltSpinSpeedDegPerSec,
                 s => { RadiationBeltManager.Instance.CurrentConfig.beltSpinSpeedDegPerSec = s; }, -90f, 90f);
             beltSpinSpeed.ValueFormatter = f => FormatValue(f, 2);
             TiltGroupModel.Add(beltSpinSpeed);

             var beltSpinPhase = new SliderModel("Belt Spin Phase (Deg)", () => RadiationBeltManager.Instance.CurrentConfig.beltSpinPhaseDeg,
                 s => { RadiationBeltManager.Instance.CurrentConfig.beltSpinPhaseDeg = s; }, -180f, 180f);
             beltSpinPhase.ValueFormatter = f => FormatValue(f, 2);
             TiltGroupModel.Add(beltSpinPhase);
             #endregion
             
             inspectorModel.AddGroup(TiltGroupModel);
             #region Inner
             GroupModel InnerInspectorGroup = new GroupModel("Inner");
             
             
             InnerInspectorGroup.Add( new SliderModel("Inner Dist", () => RadiationBeltManager.Instance.CurrentConfig.innerDist,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerDist = s;}, 0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             InnerInspectorGroup.Add( new SliderModel("Inner Radius", () => RadiationBeltManager.Instance.CurrentConfig.innerRadius,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerRadius = s;}, 0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             InnerInspectorGroup.Add( new SliderModel("Inner Border Dist", () => RadiationBeltManager.Instance.CurrentConfig.innerBorderDist,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerBorderDist = s;}, 0.0001f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Border Radius", () => RadiationBeltManager.Instance.CurrentConfig.innerBorderRadius,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerBorderRadius = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Deform", () => RadiationBeltManager.Instance.CurrentConfig.innerDeform,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerDeform = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Deform XY", () => RadiationBeltManager.Instance.CurrentConfig.innerDeformXY,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerDeformXY = s; }, 0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Border Deform XY", () => RadiationBeltManager.Instance.CurrentConfig.innerBorderDeformXY,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerBorderDeformXY = s; }, 0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Compression", () => RadiationBeltManager.Instance.CurrentConfig.innerCompression,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerCompression = s; }, 0.1f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Extension", () => RadiationBeltManager.Instance.CurrentConfig.innerExtension,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerExtension = s; }, 0.1f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             
             InnerInspectorGroup.Add(new SliderModel("Inner Height Scale", () => RadiationBeltManager.Instance.CurrentConfig.innerHeightScale,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerHeightScale = s;}, 0f, 2f,false)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });
             
           
             InnerInspectorGroup.Add(new SliderModel("Inner Particle Count", () => RadiationBeltManager.Instance.CurrentConfig.innerParticleCount,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerParticleCount = (int)s;}, 0f, 20000f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 1)
             });
             
             InnerInspectorGroup.Add(new SliderModel("Inner Quality", () => RadiationBeltManager.Instance.CurrentConfig.innerQuality,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerQuality = (int)s;}, 0f, 50f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 1)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Base Intensity", () => RadiationBeltManager.Instance.CurrentConfig.innerBaseIntensity,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerBaseIntensity = s;}, 0f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Intensity Edge Width", () => RadiationBeltManager.Instance.CurrentConfig.innerIntensityEdgeWidth,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerIntensityEdgeWidth = s;}, 0.001f, 1f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Intensity Exponent", () => RadiationBeltManager.Instance.CurrentConfig.innerIntensityExponent,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerIntensityExponent = s;}, 0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Peak Dose Rate (rad/h)", () => RadiationBeltManager.Instance.CurrentConfig.innerPeakDoseRateRadPerHour,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerPeakDoseRateRadPerHour = s;}, 0f, 200f)
             {
                 ValueFormatter = (f) => FormatValue(f, 2)
             });
             
             inspectorModel.AddGroup(InnerInspectorGroup);
             #endregion

             #region Outer
             GroupModel OuterInspectorGroup = new GroupModel("Outer");
             
             OuterInspectorGroup.Add(new SliderModel("Outer Dist", () => RadiationBeltManager.Instance.CurrentConfig.outerDist,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerDist = s;},  0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Radius", () => RadiationBeltManager.Instance.CurrentConfig.outerRadius,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerRadius = s;},  0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Border Dist", () => RadiationBeltManager.Instance.CurrentConfig.outerBorderDist,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerBorderDist = s;},  0.0001f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             OuterInspectorGroup.Add(new SliderModel("Outer Border Radius", () => RadiationBeltManager.Instance.CurrentConfig.outerBorderRadius,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerBorderRadius =s;},  0.0001f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Compression", () => RadiationBeltManager.Instance.CurrentConfig.outerCompression,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerCompression = s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Extension", () => RadiationBeltManager.Instance.CurrentConfig.outerExtension,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerExtension = s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel("Outer Deform XY", () => RadiationBeltManager.Instance.CurrentConfig.outerDeformXY,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerDeformXY = s;},  0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel("Outer Border Deform XY", () => RadiationBeltManager.Instance.CurrentConfig.outerBorderDeformXY,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerBorderDeformXY = s;},  0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             OuterInspectorGroup.Add(new SliderModel("Outer Deform", () => RadiationBeltManager.Instance.CurrentConfig.outerDeform,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerDeform = s;}, 0.0f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });  
             
             OuterInspectorGroup.Add(new SliderModel("Outer Height Scale ", () => RadiationBeltManager.Instance.CurrentConfig.outerHeightScale,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerHeightScale = s;}, 0f, 2f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Particle Count", () => RadiationBeltManager.Instance.CurrentConfig.outerParticleCount,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerParticleCount = (int)s;}, 0f, 20000f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 1)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Quality", () => RadiationBeltManager.Instance.CurrentConfig.outerQuality,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerQuality = (int)s;}, 0f, 50f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel("Outer Base Intensity", () => RadiationBeltManager.Instance.CurrentConfig.outerBaseIntensity,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerBaseIntensity = s;}, 0f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });

             OuterInspectorGroup.Add(new SliderModel("Outer Intensity Edge Width", () => RadiationBeltManager.Instance.CurrentConfig.outerIntensityEdgeWidth,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerIntensityEdgeWidth = s;}, 0.001f, 1.5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel("Outer Intensity Exponent", () => RadiationBeltManager.Instance.CurrentConfig.outerIntensityExponent,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerIntensityExponent = s;}, 0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });

             OuterInspectorGroup.Add(new SliderModel("Outer Peak Dose Rate (rad/h)", () => RadiationBeltManager.Instance.CurrentConfig.outerPeakDoseRateRadPerHour,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerPeakDoseRateRadPerHour = s;}, 0f, 50f)
             {
                 ValueFormatter = (f) => FormatValue(f, 2)
             });
             
             inspectorModel.AddGroup(OuterInspectorGroup);
             #endregion
             inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel,
                 new InspectorPanelCreationInfo()
                 {
                     PanelWidth = 400,
                     Resizable = true,
                 });
         }
         private string FormatValue(float arg, int decimals) 
         { 
             return arg.ToString("n" + Mathf.Max(0, decimals)); 
         }

        
    }
}