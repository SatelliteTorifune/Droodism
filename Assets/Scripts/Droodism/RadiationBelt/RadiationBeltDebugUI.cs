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
            
             var innerDistModel = new SliderModel("Inner Dist", () => RadiationBeltManager.Instance.currentConfig.innerDist,
                 s => { RadiationBeltManager.Instance.currentConfig.innerDist = s;}, 0.1f, 3f);
             innerDistModel.ValueFormatter = (f) => FormatValue(f, 4);
             InnerInspectorGroup.Add(innerDistModel);
             
             var innerRadius = new SliderModel("Inner Radius", () => RadiationBeltManager.Instance.currentConfig.innerRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.innerRadius = s;}, 0.1f, 3f);
             innerRadius.ValueFormatter = (f) => FormatValue(f, 4);
             InnerInspectorGroup.Add(innerRadius);
             
             var innerDeform=new SliderModel("Inner Deform", () => RadiationBeltManager.Instance.currentConfig.innerDeform,
                 s => { RadiationBeltManager.Instance.currentConfig.innerDeform = s;}, 0.1f, 3f);
             innerDeform.ValueFormatter = (f) => FormatValue(f, 4);
             InnerInspectorGroup.Add(innerDeform);  
             
             var innerParticleCount=new SliderModel("Inner Particle Count", () => RadiationBeltManager.Instance.currentConfig.innerParticleCount,
                 s => { RadiationBeltManager.Instance.currentConfig.innerParticleCount = (int)s;}, 0f, 20000f,true);
             innerParticleCount.ValueFormatter = (f) => FormatValue(f, 1);
             InnerInspectorGroup.Add(innerParticleCount);
             
             var innerQuality=new SliderModel("Inner Quality", () => RadiationBeltManager.Instance.currentConfig.innerQuality,
                 s => { RadiationBeltManager.Instance.currentConfig.innerQuality = (int)s;}, 0f, 50f,true);
             innerQuality.ValueFormatter = (f) => FormatValue(f, 1);
             InnerInspectorGroup.Add(innerQuality);
             
             inspectorModel.AddGroup(InnerInspectorGroup);
             #endregion

             #region Outer
             GroupModel OuterInspectorGroup = new GroupModel("Outer");
             
             var outerDist=new SliderModel("Outer Disk", () => RadiationBeltManager.Instance.currentConfig.outerDist,
                 s => { RadiationBeltManager.Instance.currentConfig.outerDist = (int)s;},  0.1f, 3f);
             outerDist.ValueFormatter = (f) => FormatValue(f, 4);
             OuterInspectorGroup.Add(outerDist);
             
             var outerRadius=new SliderModel("Outer Radius", () => RadiationBeltManager.Instance.currentConfig.outerRadius,
                 s => { RadiationBeltManager.Instance.currentConfig.outerRadius = (int)s;},  0.1f, 3f);
             outerRadius.ValueFormatter = (f) => FormatValue(f, 4);
             OuterInspectorGroup.Add(outerRadius);
             
             var outerBorderStart=new SliderModel("Outer Border Start", () => RadiationBeltManager.Instance.currentConfig.outerBorderStart,
                 s => { RadiationBeltManager.Instance.currentConfig.outerBorderStart = (int)s;},  0.1f, 3f);
             outerBorderStart.ValueFormatter = (f) => FormatValue(f, 4);
             OuterInspectorGroup.Add(outerBorderStart);
             
             var outerBorderEnd=new SliderModel("Outer Border End", () => RadiationBeltManager.Instance.currentConfig.outerBorderEnd,
                 s => { RadiationBeltManager.Instance.currentConfig.outerBorderEnd = (int)s;},  0.1f, 3f);
             outerBorderEnd.ValueFormatter = (f) => FormatValue(f, 4);
             OuterInspectorGroup.Add(outerBorderEnd);
             
             var outerCompression=new SliderModel("Outer Compression", () => RadiationBeltManager.Instance.currentConfig.outerCompression,
                 s => { RadiationBeltManager.Instance.currentConfig.outerCompression = (int)s;},  0.1f, 3f);
             outerCompression.ValueFormatter = (f) => FormatValue(f, 4);
             OuterInspectorGroup.Add(outerCompression);
             
             var outerExtension=new SliderModel("Outer Extension", () => RadiationBeltManager.Instance.currentConfig.outerExtension,
                 s => { RadiationBeltManager.Instance.currentConfig.outerExtension = (int)s;},  0.1f, 3f);
             outerExtension.ValueFormatter = (f) => FormatValue(f, 4);
             OuterInspectorGroup.Add(outerExtension);
             
             var outerDeform=new SliderModel("Outer Deform", () => RadiationBeltManager.Instance.currentConfig.outerDeform,
                 s => { RadiationBeltManager.Instance.currentConfig.outerDeform = s;}, 0.1f, 3f);
             outerDeform.ValueFormatter = (f) => FormatValue(f, 4);
             OuterInspectorGroup.Add(outerDeform);  
             
             var outerParticleCount=new SliderModel("Outer Particle Count", () => RadiationBeltManager.Instance.currentConfig.outerParticleCount,
                 s => { RadiationBeltManager.Instance.currentConfig.outerParticleCount = (int)s;}, 0f, 20000f,true);
             outerParticleCount.ValueFormatter = (f) => FormatValue(f, 1);
             OuterInspectorGroup.Add(outerParticleCount);
             
             var outerQuality=new SliderModel("Outer Quality", () => RadiationBeltManager.Instance.currentConfig.outerQuality,
                 s => { RadiationBeltManager.Instance.currentConfig.outerQuality = (int)s;}, 0f, 50f,true);
             outerQuality.ValueFormatter = (f) => FormatValue(f, 4);
             OuterInspectorGroup.Add(outerQuality);
             
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