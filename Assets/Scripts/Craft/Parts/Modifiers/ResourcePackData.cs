using System.Linq.Expressions;
using Assets.Scripts.Design;
using ModApi;
using ModApi.Design.PartProperties;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("ResourcePack")]
    [PartModifierTypeId("ResourcePack")]
    public class ResourcePackData : PartModifierData<ResourcePackScript>
    {
        [SerializeField]
        [DesignerPropertySlider(-2f, 2f, 5, Order = 1, Label = "Size", Tooltip = "The size of this ResourcePack.")]
        private float _scale = 1f;

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnValueLabelRequested<float>((Expression<Func<float>>)(() => this._scale),
                (Func<float, string>)(x => _scale.ToString("0.#") + "x"));
            d.OnPropertyChanged<float>((Expression<Func<float>>)(() => this._scale),
                (Action<float, float>)((newVal, oldVal) =>
                {
                    d.Manager.RefreshUI();
                    this.Script.UpdateScale();
                    Symmetry.SynchronizePartModifiers(this.Part.PartScript);
                    this.Part.PartScript.CraftScript.SetStructureChanged();
                }));
        }
        public float ResScale
        {
            get => this._scale;
            set
            {
                this._scale = value;
                this.Script.UpdateScale();
            }
        }
        
    }
}