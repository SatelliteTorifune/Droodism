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

    // TODO: 命名待修（用户手动处理）——文件名 FirstAddKitData.cs 与类名 FirstAidKitData 不一致；
    // 且 Add/Aid 拼写不统一：FirstAddKitScript 用 "Add"，FirstAidKitData 用 "Aid"，应统一为 FirstAidKit。
    [Serializable]
    [DesignerPartModifier("FirstAidKit")]
    [PartModifierTypeId("FirstAidKit")]
    public class FirstAidKitData : PartModifierData<FirstAidKitScript>
    {
        [SerializeField] [PartModifierProperty]
        private float healHp=300f;
        
        [SerializeField] [PartModifierProperty]
        private float healRate=1f;
       
        public float HealHp
        {
            get => healHp;
            internal set => healHp = value;
        }

        public float HealRate
        {
            get => healRate;
        }
    }
}