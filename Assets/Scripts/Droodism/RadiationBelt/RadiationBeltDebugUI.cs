using System;
using System.Windows.Forms;
using Assets.Scripts;
using ModApi;
using ModApi.GameLoop;
using ModApi.Scenes;
using ModApi.Scenes.Events;
using ModApi.Ui.Inspector;
using UnityEngine;


namespace Assets.Scripts.Droodism.RadiationBelt
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
             inspectorModel = new InspectorModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InspectorTitle"), "<color=red>" + Locale.GetString("Droodism.RadiationBeltDebugUI.DebugInspectorTitle"));

             #region Debug
             inspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltDebugUI.ForceRebuildInspector"), (b) =>
             {
                 this.inspectorPanel.Visible = false;
                 this.inspectorPanel = null;
                 CreateInspectorPanel();
             }));
             inspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltDebugUI.ReGenerateBeltMesh"), (b) =>
             {
                 RadiationBeltManager.Instance.ReGenerateMeshes();
             }));
             inspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltDebugUI.SaveCurrentConfig"), (Action<TextButtonModel>)(b => 
             {
                 RadiationBeltManager.Instance.CurrentConfig.SaveToFile(RadiationBeltManager.Instance.CurrentFocusPlanet);
             })));
             inspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltDebugUI.LoadCurrentConfig"), (Action<TextButtonModel>)(b => 
             {
                 RadiationBeltManager.Instance.ReFreshCurrentConfig();
                 RadiationBeltManager.Instance.ReGenerateMeshes();
             })));
             
             inspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltDebugUI.ReturnToDefaultPreset"), (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplyDefaultPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltDebugUI.ApplyGiantPreset"), (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplyGiantPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltDebugUI.ApplyMetallicPreset"), (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplyMetallicPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltDebugUI.ApplySolidIronPreset"), (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplySolidIronPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new TextButtonModel(Locale.GetString("Droodism.RadiationBeltDebugUI.ApplyAnomalyPreset"), (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.CurrentConfig == null) return;
                 manager.CurrentConfig.ApplyAnomalyPreset();
                 manager.ReGenerateMeshes();
             })));
             inspectorModel.Add(new ToggleModel(Locale.GetString("Droodism.RadiationBeltDebugUI.Show"),()=>ShowGeneral,(b =>
             {
                 ShowGeneral = b;
             })));
             inspectorModel.Add(new ToggleModel(Locale.GetString("Droodism.RadiationBeltDebugUI.ShowInner"),()=>ShowInner,b =>
             {
                 ShowInner = b;
             }));
             inspectorModel.Add(new ToggleModel(Locale.GetString("Droodism.RadiationBeltDebugUI.ShowOuter"),()=>ShowOuter,b =>
             {
                 ShowOuter = b;
             }));
             #endregion

            #region General
            GroupModel GeneralGroupModel = new GroupModel(Locale.GetString("Droodism.RadiationBeltDebugUI.General"));

            GeneralGroupModel.Add(new TextModel(Locale.GetString("Droodism.RadiationBeltUI.CurrentPlanet"), ()=>
            
                RadiationBeltManager.Instance.CurrentFocusPlanet
            ));
            GeneralGroupModel.Add(new ToggleModel(Locale.GetString("Droodism.RadiationBeltDebugUI.MainEnabled"),()=> RadiationBeltManager.Instance.CurrentConfig.Enabled,b =>
            {
                RadiationBeltManager.Instance.CurrentConfig.Enabled = b;
            }));
            
            var renderMetersPerUnit = new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.RenderMetersPerUnit"), () => RadiationBeltManager.Instance.CurrentConfig.renderMetersPerUnit,
                 s => { RadiationBeltManager.Instance.CurrentConfig.renderMetersPerUnit = s; }, 10000f, 10000000f, false);
             renderMetersPerUnit.ValueFormatter = f => FormatValue(f, 0);
             GeneralGroupModel.Add(renderMetersPerUnit);
             inspectorModel.AddGroup(GeneralGroupModel);
             #endregion
            #region Tilt
            GroupModel TiltGroupModel = new GroupModel(Locale.GetString("Droodism.RadiationBeltDebugUI.TiltGroup"));

            var beltTilt = new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.BeltTiltDeg"), () => RadiationBeltManager.Instance.CurrentConfig.beltTiltDegrees,
                 s => { RadiationBeltManager.Instance.CurrentConfig.beltTiltDegrees = s; }, -90f, 90f);
             beltTilt.ValueFormatter = f => FormatValue(f, 2);
             TiltGroupModel.Add(beltTilt);

             var beltTiltAxisX = new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.BeltTiltAxisX"), () => RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis.x,
                 s =>
                 {
                     var axis = RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis;
                     axis.x = s;
                     RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis = axis;
                 }, -1f, 1f);
             beltTiltAxisX.ValueFormatter = f => FormatValue(f, 3);
             TiltGroupModel.Add(beltTiltAxisX);

             var beltTiltAxisY = new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.BeltTiltAxisY"), () => RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis.y,
                 s =>
                 {
                     var axis = RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis;
                     axis.y = s;
                     RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis = axis;
                 }, -1f, 1f);
             beltTiltAxisY.ValueFormatter = f => FormatValue(f, 3);
             TiltGroupModel.Add(beltTiltAxisY);

             var beltTiltAxisZ = new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.BeltTiltAxisZ"), () => RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis.z,
                 s =>
                 {
                     var axis = RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis;
                     axis.z = s;
                     RadiationBeltManager.Instance.CurrentConfig.beltTiltAxis = axis;
                 }, -1f, 1f);
             beltTiltAxisZ.ValueFormatter = f => FormatValue(f, 3);
             TiltGroupModel.Add(beltTiltAxisZ);

             var beltSpinSpeed = new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.BeltSpinSpeedDegPerSec"), () => RadiationBeltManager.Instance.CurrentConfig.beltSpinSpeedDegPerSec,
                 s => { RadiationBeltManager.Instance.CurrentConfig.beltSpinSpeedDegPerSec = s; }, -90f, 90f);
             beltSpinSpeed.ValueFormatter = f => FormatValue(f, 2);
             TiltGroupModel.Add(beltSpinSpeed);

             var beltSpinPhase = new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.BeltSpinPhaseDeg"), () => RadiationBeltManager.Instance.CurrentConfig.beltSpinPhaseDeg,
                 s => { RadiationBeltManager.Instance.CurrentConfig.beltSpinPhaseDeg = s; }, -180f, 180f);
             beltSpinPhase.ValueFormatter = f => FormatValue(f, 2);
             TiltGroupModel.Add(beltSpinPhase);
             #endregion
             
             inspectorModel.AddGroup(TiltGroupModel);
            #region Inner
            GroupModel InnerInspectorGroup = new GroupModel(Locale.GetString("Droodism.RadiationBeltDebugUI.Inner"));
            
            
            InnerInspectorGroup.Add( new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerDist"), () => RadiationBeltManager.Instance.CurrentConfig.innerDist,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerDist = s;}, 0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             InnerInspectorGroup.Add( new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerRadius"), () => RadiationBeltManager.Instance.CurrentConfig.innerRadius,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerRadius = s;}, 0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             InnerInspectorGroup.Add( new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerBorderDist"), () => RadiationBeltManager.Instance.CurrentConfig.innerBorderDist,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerBorderDist = s;}, 0.0001f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerBorderRadius"), () => RadiationBeltManager.Instance.CurrentConfig.innerBorderRadius,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerBorderRadius = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerDeform"), () => RadiationBeltManager.Instance.CurrentConfig.innerDeform,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerDeform = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerDeformXY"), () => RadiationBeltManager.Instance.CurrentConfig.innerDeformXY,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerDeformXY = s; }, 0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerBorderDeformXY"), () => RadiationBeltManager.Instance.CurrentConfig.innerBorderDeformXY,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerBorderDeformXY = s; }, 0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerCompression"), () => RadiationBeltManager.Instance.CurrentConfig.innerCompression,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerCompression = s; }, 0.1f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerExtension"), () => RadiationBeltManager.Instance.CurrentConfig.innerExtension,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerExtension = s; }, 0.1f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             
             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerHeightScale"), () => RadiationBeltManager.Instance.CurrentConfig.innerHeightScale,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerHeightScale = s;}, 0f, 2f,false)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });
             
           
             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerParticleCount"), () => RadiationBeltManager.Instance.CurrentConfig.innerParticleCount,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerParticleCount = (int)s;}, 0f, 20000f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 1)
             });
             
             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerQuality"), () => RadiationBeltManager.Instance.CurrentConfig.innerQuality,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerQuality = (int)s;}, 0f, 50f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 1)
             });

             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerBaseIntensity"), () => RadiationBeltManager.Instance.CurrentConfig.innerBaseIntensity,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerBaseIntensity = s;}, 0f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });

             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerIntensityEdgeWidth"), () => RadiationBeltManager.Instance.CurrentConfig.innerIntensityEdgeWidth,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerIntensityEdgeWidth = s;}, 0.001f, 1f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerIntensityExponent"), () => RadiationBeltManager.Instance.CurrentConfig.innerIntensityExponent,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerIntensityExponent = s;}, 0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });

             InnerInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.InnerPeakDoseRateRadPerHour"), () => RadiationBeltManager.Instance.CurrentConfig.innerPeakDoseRateRadPerHour,
                 s => { RadiationBeltManager.Instance.CurrentConfig.innerPeakDoseRateRadPerHour = s;}, 0f, 200f)
             {
                 ValueFormatter = (f) => FormatValue(f, 2)
             });
             
             inspectorModel.AddGroup(InnerInspectorGroup);
             #endregion

            #region Outer
            GroupModel OuterInspectorGroup = new GroupModel(Locale.GetString("Droodism.RadiationBeltDebugUI.Outer"));
            
            OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterDist"), () => RadiationBeltManager.Instance.CurrentConfig.outerDist,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerDist = s;},  0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterRadius"), () => RadiationBeltManager.Instance.CurrentConfig.outerRadius,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerRadius = s;},  0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterBorderDist"), () => RadiationBeltManager.Instance.CurrentConfig.outerBorderDist,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerBorderDist = s;},  0.0001f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterBorderRadius"), () => RadiationBeltManager.Instance.CurrentConfig.outerBorderRadius,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerBorderRadius =s;},  0.0001f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterCompression"), () => RadiationBeltManager.Instance.CurrentConfig.outerCompression,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerCompression = s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterExtension"), () => RadiationBeltManager.Instance.CurrentConfig.outerExtension,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerExtension = s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterDeformXY"), () => RadiationBeltManager.Instance.CurrentConfig.outerDeformXY,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerDeformXY = s;},  0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterBorderDeformXY"), () => RadiationBeltManager.Instance.CurrentConfig.outerBorderDeformXY,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerBorderDeformXY = s;},  0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterDeform"), () => RadiationBeltManager.Instance.CurrentConfig.outerDeform,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerDeform = s;}, 0.0f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });  
             
             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterHeightScale"), () => RadiationBeltManager.Instance.CurrentConfig.outerHeightScale,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerHeightScale = s;}, 0f, 2f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });
             
             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterParticleCount"), () => RadiationBeltManager.Instance.CurrentConfig.outerParticleCount,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerParticleCount = (int)s;}, 0f, 20000f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 1)
             });
             
             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterQuality"), () => RadiationBeltManager.Instance.CurrentConfig.outerQuality,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerQuality = (int)s;}, 0f, 50f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterBaseIntensity"), () => RadiationBeltManager.Instance.CurrentConfig.outerBaseIntensity,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerBaseIntensity = s;}, 0f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });

             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterIntensityEdgeWidth"), () => RadiationBeltManager.Instance.CurrentConfig.outerIntensityEdgeWidth,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerIntensityEdgeWidth = s;}, 0.001f, 1.5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterIntensityExponent"), () => RadiationBeltManager.Instance.CurrentConfig.outerIntensityExponent,
                 s => { RadiationBeltManager.Instance.CurrentConfig.outerIntensityExponent = s;}, 0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });

             OuterInspectorGroup.Add(new SliderModel(Locale.GetString("Droodism.RadiationBeltDebugUI.OuterPeakDoseRateRadPerHour"), () => RadiationBeltManager.Instance.CurrentConfig.outerPeakDoseRateRadPerHour,
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