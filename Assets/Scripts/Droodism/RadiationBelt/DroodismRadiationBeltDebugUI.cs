using System;
using Assets.Scripts;
using ModApi.GameLoop;
using ModApi.Scenes.Events;
using ModApi.Ui;
using ModApi.Ui.Inspector;
using UnityEngine;

namespace Droodism.RadiationBelt
{
    public class DroodismRadiationBeltDebugUI : MonoBehaviourBase
    {
        public static DroodismRadiationBeltDebugUI Instance;

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
             inspectorModel.Add(new ToggleModel("show",()=>ShowGeneral,(b =>
             {
                 ShowGeneral = b;
             })));
             //
             GroupModel InnerInspectorGroup = new GroupModel("Inner");
             GroupModel OuterInspectorGroup = new GroupModel("Outer");
             //  
             InnerInspectorGroup.Add(new ToggleModel("show inner",()=>ShowInner,b =>
             {
                 ShowInner = b;
             }));
             
             //
             OuterInspectorGroup.Add(new ToggleModel("show outer",()=>ShowOuter,b =>
             {
                 ShowOuter = b;
             }));
             
             
             
             inspectorModel.AddGroup(InnerInspectorGroup);
             inspectorModel.AddGroup(OuterInspectorGroup);
             inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel,
                 new InspectorPanelCreationInfo()
                 {
                     PanelWidth = 400,
                     Resizable = true,
                 });
         }
    }
}