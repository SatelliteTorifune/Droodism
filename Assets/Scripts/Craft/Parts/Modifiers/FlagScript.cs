using System.Net.NetworkInformation;
using ModApi;
using ModApi.GameLoop;
using ModApi.Ui.Inspector;
using UnityEngine.Rendering;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class FlagScript : ResourceProcessorPartScript<FlagData>
    {
        private Transform _mainBase, _rotateBase,p2,p3,flagDown,flagFace;
        private Transform _offset;

        private Sprite _sprite;
        private Texture2D _customFlagTexture;
        
        


        public override void FlightStart(in FlightFrameData frame)
        {
            base.FlightStart(in frame);
            if (Data.StayDeployed)
            {
                
                return;
                this.Data.IsDeployed = true;
                this.Data.CurrentRotationPercent = 0;
                this.Data.CurrentExtentPercent = -1.4f;
            }
        }

       
        

        public override void FlightUpdate(in FlightFrameData frame)
        {
            Data.StayDeployed = Data.IsDeployed && PartScript.Data.Activated;
            if (PartScript.Data.Activated)
            {
                Deploy(frame);
               
            }
            else
            {
                Undeploy(frame);
            }

            void Deploy(in FlightFrameData frame)
            {
                float targetExtent = 0.5f;
                float targetRotationPercent = 1;
                float targetFlagExtent = 1;
                if (Data.CurrentExtentPercent != targetExtent)
                {
                    this.Data.CurrentExtentPercent = Mathf.MoveTowards(Data.CurrentExtentPercent, targetExtent,
                        frame.DeltaTime * this.Data.ExtendSpeed);
                    //伸长旗杆
                    _rotateBase.transform.localPosition=new Vector3(_rotateBase.transform.localPosition.x,Data.CurrentExtentPercent,_rotateBase.transform.localPosition.z);
                    p2.transform.localPosition=new Vector3(p2.transform.localPosition.x,Data.CurrentExtentPercent,p2.transform.localPosition.z);
                    p3.transform.localPosition=new  Vector3(p3.transform.localPosition.x,Data.CurrentExtentPercent,p3.transform.localPosition.z);
                }

                if (Data.CurrentRotationPercent != targetRotationPercent&&Data.CurrentExtentPercent == targetExtent)
                {
                    //打开旗杆夹角
                    this.Data.CurrentRotationPercent = Mathf.MoveTowards(Data.CurrentRotationPercent, targetRotationPercent,
                        frame.DeltaTime * this.Data.RotationSpeed);
                    flagDown.localRotation =  Quaternion.Euler(Vector3.Lerp(new Vector3(180, 0, 0),
                        new Vector3(90, 0, 0), Data.CurrentRotationPercent));
                }
                if (Data.CurrentRotationPercent == targetRotationPercent&&Data.CurrentExtentPercent2 != targetFlagExtent)
                {
                    //放下旗帜
                    this.Data.CurrentExtentPercent2 = Mathf.MoveTowards(Data.CurrentExtentPercent2, targetFlagExtent,
                        frame.DeltaTime * this.Data.ExtendSpeed*5);
                   flagFace.transform.localScale=new Vector3(flagFace.transform.localScale.x,Data.CurrentExtentPercent2,flagFace.transform.localScale.z);
                }
                
            }
            void Undeploy(in FlightFrameData frame)
            {
                
            }
        }

        private void SetFlagPhoto(Sprite  _sprite)
        {
            flagFace.Find("Cav").Find("CavRenA").GetComponent<SpriteRenderer>().sprite = _sprite;
            flagFace.Find("Cav").Find("CavRenB").GetComponent<SpriteRenderer>().sprite = _sprite;
        }
        

    

        #region PrefabSetup Methods
        protected override void UpdateComponents()
        {
            string[] strArray = "Base/RotateBase".Split('/', StringSplitOptions.None);
            Transform subPart = this.transform;
            foreach (string n in strArray)
                subPart = subPart.Find(n) ?? subPart;
            if (subPart.name == strArray[strArray.Length - 1])
                this.SetSubPart(subPart);
            else
                this.SetSubPart(Utilities.FindFirstGameObjectMyselfOrChildren("Base/RotateBase", this.gameObject)?.transform);
            if (_rotateBase != null)
            {
                p2 = _rotateBase.Find("P2");
                p3 = p2.Find("P3");
                flagDown=p3.Find("FlagDown");
                flagFace=flagDown.Find("FlagFace");
            }
            
        }


        private void SetSubPart(Transform subPart)
        {
            if ((UnityEngine.Object) this._offset != (UnityEngine.Object) null)
            {
                UnityEngine.Object.Destroy((UnityEngine.Object) this._offset.gameObject);
                this._offset = (Transform) null;
            }
            this._rotateBase = subPart;
            if (!((UnityEngine.Object) this._rotateBase != (UnityEngine.Object) null) || (double) this.Data.PositionOffset1.magnitude <= 0.0)
                return;
            this._offset = new GameObject("SubPartRotatorOffset").transform;
            this._offset.SetParent(this._rotateBase.parent, false);
            this._offset.position = this._rotateBase.TransformPoint(Data.PositionOffset1);
        }

        #endregion

    
        
        
    }
}