using System;
using System.Windows.Forms;
using Assets.Scripts;
using ModApi.GameLoop;
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

        public bool ShowGeneral { get; private set; } = true;
        public bool ShowInner { get; private set; }= true;
        public bool ShowOuter { get; private set; }= true;
        
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
             inspectorModel = new InspectorModel("Radiation Belt Inspector", "<color=red>Radiation Belt Inspector");

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
                 RadiationBeltManager.Instance.currentConfig.SaveToFile(RadiationBeltManager.Instance.CurrentFocusPlanet);
             })));
             inspectorModel.Add(new TextButtonModel("Load Current Config", (Action<TextButtonModel>)(b => 
             {
                 RadiationBeltManager.Instance.ReFreshCurrentConfig();
             })));
             inspectorModel.Add(new TextButtonModel("Apply Kerbalism Earth Preset", (Action<TextButtonModel>)(b =>
             {
                 var manager = RadiationBeltManager.Instance;
                 if (manager.currentConfig == null) return;
                 manager.currentConfig.ApplyKerbalismEarthPreset();
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
             GeneralGroupModel.Add(new ToggleModel("Main Enabled",()=> RadiationBeltManager.Instance.currentConfig.Enabled,b =>
             {
                 RadiationBeltManager.Instance.currentConfig.Enabled = b;
             }));
             
             
             var scaleX = new SliderModel("Scale X", () => RadiationBeltManager.Instance.currentConfig.Scale.x,
                 s => { RadiationBeltManager.Instance.currentConfig.Scale.x = s;}, 0.1f, 40f);
             scaleX.ValueFormatter = (f) => FormatValue(f, 4);
             GeneralGroupModel.Add(scaleX);
             
             var scaleY = new SliderModel("Scale Y", () => RadiationBeltManager.Instance.currentConfig.Scale.y,
                 s => { RadiationBeltManager.Instance.currentConfig.Scale.y = s;}, 0.1f, 40f);
             scaleY.ValueFormatter = (f) => FormatValue(f, 4);
             GeneralGroupModel.Add(scaleY);
             
             var scaleZ = new SliderModel("Scale Z", () => RadiationBeltManager.Instance.currentConfig.Scale.z,
                 s => { RadiationBeltManager.Instance.currentConfig.Scale.z = s;}, 0.1f, 40f);
             scaleZ.ValueFormatter = (f) => FormatValue(f, 4);
             GeneralGroupModel.Add(scaleZ);

             var renderMetersPerUnit = new SliderModel("Render Meters Per Unit", () => RadiationBeltManager.Instance.currentConfig.renderMetersPerUnit,
                 s => { RadiationBeltManager.Instance.currentConfig.renderMetersPerUnit = s; }, 10000f, 10000000f, false);
             renderMetersPerUnit.ValueFormatter = f => FormatValue(f, 0);
             GeneralGroupModel.Add(renderMetersPerUnit);
             inspectorModel.AddGroup(GeneralGroupModel);
             #endregion

             #region Inner
             GroupModel InnerInspectorGroup = new GroupModel("Inner");
             
             
             InnerInspectorGroup.Add( new SliderModel("Inner Dist", () => RadiationBeltManager.Instance.currentConfig.innerDist,
                 s => { RadiationBeltManager.Instance.currentConfig.innerDist = s;}, 0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             InnerInspectorGroup.Add( new SliderModel("Inner Radius", () => RadiationBeltManager.Instance.currentConfig.innerRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.innerRadius = s;}, 0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             InnerInspectorGroup.Add( new SliderModel("Inner Border Dist", () => RadiationBeltManager.Instance.currentConfig.innerBorderDist,
                 s => { RadiationBeltManager.Instance.currentConfig.innerBorderDist = s;}, 0.0001f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Border Radius", () => RadiationBeltManager.Instance.currentConfig.innerBorderRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.innerBorderRadius = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Deform", () => RadiationBeltManager.Instance.currentConfig.innerDeform,
                 s => { RadiationBeltManager.Instance.currentConfig.innerDeform = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Deform XY", () => RadiationBeltManager.Instance.currentConfig.innerDeformXY,
                 s => { RadiationBeltManager.Instance.currentConfig.innerDeformXY = s; }, 0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Border Deform XY", () => RadiationBeltManager.Instance.currentConfig.innerBorderDeformXY,
                 s => { RadiationBeltManager.Instance.currentConfig.innerBorderDeformXY = s; }, 0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Compression", () => RadiationBeltManager.Instance.currentConfig.innerCompression,
                 s => { RadiationBeltManager.Instance.currentConfig.innerCompression = s; }, 0.1f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             InnerInspectorGroup.Add(new SliderModel("Inner Extension", () => RadiationBeltManager.Instance.currentConfig.innerExtension,
                 s => { RadiationBeltManager.Instance.currentConfig.innerExtension = s; }, 0.1f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             
             InnerInspectorGroup.Add(new SliderModel("Inner Height Scale", () => RadiationBeltManager.Instance.currentConfig.innerHeightScale,
                 s => { RadiationBeltManager.Instance.currentConfig.innerHeightScale = s;}, 0f, 2f,false)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });
             
           
             InnerInspectorGroup.Add(new SliderModel("Inner Particle Count", () => RadiationBeltManager.Instance.currentConfig.innerParticleCount,
                 s => { RadiationBeltManager.Instance.currentConfig.innerParticleCount = (int)s;}, 0f, 20000f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 1)
             });
             
             InnerInspectorGroup.Add(new SliderModel("Inner Quality", () => RadiationBeltManager.Instance.currentConfig.innerQuality,
                 s => { RadiationBeltManager.Instance.currentConfig.innerQuality = (int)s;}, 0f, 50f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 1)
             });
             
             inspectorModel.AddGroup(InnerInspectorGroup);
             #endregion

             #region Outer
             GroupModel OuterInspectorGroup = new GroupModel("Outer");
             
             OuterInspectorGroup.Add(new SliderModel("Outer Dist", () => RadiationBeltManager.Instance.currentConfig.outerDist,
                 s => { RadiationBeltManager.Instance.currentConfig.outerDist = s;},  0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Radius", () => RadiationBeltManager.Instance.currentConfig.outerRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.outerRadius = s;},  0.1f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Border Dist", () => RadiationBeltManager.Instance.currentConfig.outerBorderDist,
                 s => { RadiationBeltManager.Instance.currentConfig.outerBorderDist = s;},  0.0001f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             OuterInspectorGroup.Add(new SliderModel("Outer Border Radius", () => RadiationBeltManager.Instance.currentConfig.outerBorderRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.outerBorderRadius =s;},  0.0001f, 5f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Compression", () => RadiationBeltManager.Instance.currentConfig.outerCompression,
                 s => { RadiationBeltManager.Instance.currentConfig.outerCompression = s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Extension", () => RadiationBeltManager.Instance.currentConfig.outerExtension,
                 s => { RadiationBeltManager.Instance.currentConfig.outerExtension = s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel("Outer Deform XY", () => RadiationBeltManager.Instance.currentConfig.outerDeformXY,
                 s => { RadiationBeltManager.Instance.currentConfig.outerDeformXY = s;},  0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });

             OuterInspectorGroup.Add(new SliderModel("Outer Border Deform XY", () => RadiationBeltManager.Instance.currentConfig.outerBorderDeformXY,
                 s => { RadiationBeltManager.Instance.currentConfig.outerBorderDeformXY = s;},  0.05f, 4f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             OuterInspectorGroup.Add(new SliderModel("Outer Deform", () => RadiationBeltManager.Instance.currentConfig.outerDeform,
                 s => { RadiationBeltManager.Instance.currentConfig.outerDeform = s;}, 0.0f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });  
             
             OuterInspectorGroup.Add(new SliderModel("Outer Height Scale ", () => RadiationBeltManager.Instance.currentConfig.outerHeightScale,
                 s => { RadiationBeltManager.Instance.currentConfig.outerHeightScale = s;}, 0f, 2f)
             {
                 ValueFormatter = (f) => FormatValue(f, 3)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Particle Count", () => RadiationBeltManager.Instance.currentConfig.outerParticleCount,
                 s => { RadiationBeltManager.Instance.currentConfig.outerParticleCount = (int)s;}, 0f, 20000f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 1)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Quality", () => RadiationBeltManager.Instance.currentConfig.outerQuality,
                 s => { RadiationBeltManager.Instance.currentConfig.outerQuality = (int)s;}, 0f, 50f,true)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
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