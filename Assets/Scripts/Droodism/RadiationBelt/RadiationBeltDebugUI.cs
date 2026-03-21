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
             inspectorModel.AddGroup(GeneralGroupModel);
             #endregion

             #region Inner
             GroupModel InnerInspectorGroup = new GroupModel("Inner");
             
             
             InnerInspectorGroup.Add( new SliderModel("Inner Major Radius", () => RadiationBeltManager.Instance.currentConfig.innerMajorRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.innerMajorRadius = s;}, 0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             InnerInspectorGroup.Add( new SliderModel("Inner Minor Radius", () => RadiationBeltManager.Instance.currentConfig.innerMinorRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.innerMinorRadius = s;}, 0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Core Offset", () => RadiationBeltManager.Instance.currentConfig.innerCoreOffset,
                 s => { RadiationBeltManager.Instance.currentConfig.innerCoreOffset = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
                InnerInspectorGroup.Add( new SliderModel("Inner Core CenterX", () => RadiationBeltManager.Instance.currentConfig.innerCoreCenterX,
                 s => { RadiationBeltManager.Instance.currentConfig.innerCoreCenterX = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Core CenterY", () => RadiationBeltManager.Instance.currentConfig.innerCoreCenterY,
                 s => { RadiationBeltManager.Instance.currentConfig.innerCoreCenterY = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Outer CenterX", () => RadiationBeltManager.Instance.currentConfig.innerOuterCenterX,
                 s => { RadiationBeltManager.Instance.currentConfig.innerOuterCenterX = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Outer CenterY", () => RadiationBeltManager.Instance.currentConfig.innerOuterCenterY,
                 s => { RadiationBeltManager.Instance.currentConfig.innerOuterCenterY = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Outer RadiusX", () => RadiationBeltManager.Instance.currentConfig.innerOuterRadiusX,
                 s => { RadiationBeltManager.Instance.currentConfig.innerOuterRadiusX = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Outer RadiusY", () => RadiationBeltManager.Instance.currentConfig.innerOuterRadiusY,
                 s => { RadiationBeltManager.Instance.currentConfig.innerOuterRadiusY = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Core RadiusX", () => RadiationBeltManager.Instance.currentConfig.innerCoreRadiusX,
                 s => { RadiationBeltManager.Instance.currentConfig.innerCoreRadiusX = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add( new SliderModel("Inner Core RadiusY", () => RadiationBeltManager.Instance.currentConfig.innerCoreRadiusY,
                 s => { RadiationBeltManager.Instance.currentConfig.innerCoreRadiusY = s;}, -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             InnerInspectorGroup.Add(new SliderModel("Inner Deform", () => RadiationBeltManager.Instance.currentConfig.innerDeform,
                 s => { RadiationBeltManager.Instance.currentConfig.innerDeform = s;}, 0.1f, 3f)
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
             
             OuterInspectorGroup.Add(new SliderModel("Outer Major Radius", () => RadiationBeltManager.Instance.currentConfig.outerMajorRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.outerMajorRadius = (int)s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer Minor Radius", () => RadiationBeltManager.Instance.currentConfig.outerMinorRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.outerMinorRadius = s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer core Radius", () => RadiationBeltManager.Instance.currentConfig.outerCoreRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.outerCoreRadius = s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer core Offset", () => RadiationBeltManager.Instance.currentConfig.outerCoreOffset,
                 s => { RadiationBeltManager.Instance.currentConfig.outerCoreOffset = s;},  -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer core Center X", () => RadiationBeltManager.Instance.currentConfig.outerCoreCenterX,
                 s => { RadiationBeltManager.Instance.currentConfig.outerCoreCenterX = s;},  -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             OuterInspectorGroup.Add(new SliderModel("Outer core Center Y", () => RadiationBeltManager.Instance.currentConfig.outerCoreCenterY,
                 s => { RadiationBeltManager.Instance.currentConfig.outerCoreCenterY = s;},  -3f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
            
             OuterInspectorGroup.Add(new SliderModel("Outer Border Start", () => RadiationBeltManager.Instance.currentConfig.outerBorderStart,
                 s => { RadiationBeltManager.Instance.currentConfig.outerBorderStart = s;},  0.1f, 3f)
             {
                 ValueFormatter = (f) => FormatValue(f, 4)
             });
             
             
             OuterInspectorGroup.Add(new SliderModel("Outer Border End", () => RadiationBeltManager.Instance.currentConfig.outerBorderEnd,
                 s => { RadiationBeltManager.Instance.currentConfig.outerBorderEnd =s;},  0.1f, 3f)
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
             
             
             OuterInspectorGroup.Add(new SliderModel("Outer Deform", () => RadiationBeltManager.Instance.currentConfig.outerDeform,
                 s => { RadiationBeltManager.Instance.currentConfig.outerDeform = s;}, 0.1f, 3f)
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