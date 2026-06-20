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
        private Transform _mainBase, _rotateBase, p2, p3, flagDown, flagFace;
        private Transform _offset;

        private float _poleVel;
        private float _hingeVel;
        private float _flagVel;

        private float _prevPolePos;
        private float _prevHingeRot;
        private float _prevFlagScale;

        private const float SPRING_DAMP = 3f;
        private const float SPRING_RESPONSE = 18f;
        // 旗帜展开用更软的弹簧：响应慢、阻尼小，回弹更明显
        private const float FLAG_DAMP = 1.2f;
        private const float FLAG_RESPONSE = 10f;
        
        


        public override void FlightStart(in FlightFrameData frame)
        {
            base.FlightStart(in frame);
            _poleVel = 0f;
            _hingeVel = 0f;
            _flagVel = 0f;
            SetFlagPhoto();
            Data.StayDeployed = Data.IsDeployed && PartScript.Data.Activated;
            if (Data.StayDeployed)
            {
                // 目标值与 Deploy() 中保持一致
                float targetExtent = 0.5f;
                float targetRotationPercent = 1;
                float targetFlagExtent = 2;
                
                this.Data.CurrentExtentPercent = targetExtent;
                this.Data.CurrentRotationPercent = targetRotationPercent;
                this.Data.CurrentExtentPercent2 = targetFlagExtent;

                _rotateBase.transform.localPosition = new Vector3(
                    _rotateBase.transform.localPosition.x, targetExtent, _rotateBase.transform.localPosition.z);
                p2.transform.localPosition = new Vector3(
                    p2.transform.localPosition.x, targetExtent, p2.transform.localPosition.z);
                p3.transform.localPosition = new Vector3(
                    p3.transform.localPosition.x, targetExtent, p3.transform.localPosition.z);
                flagDown.localRotation = Quaternion.Euler(
                    Vector3.Lerp(new Vector3(180, 0, 0), new Vector3(90, 0, 0), targetRotationPercent));
                flagFace.transform.localScale = new Vector3(
                   2,2,2);
                

            }
        }

       
        

        public override void FlightUpdate(in FlightFrameData frame)
        {
            Data.StayDeployed = Data.IsDeployed && PartScript.Data.Activated;
            if (PartScript.Data.Activated&&!Data.IsDeployed)
            {
                Deploy(frame);
               
            }
            void Deploy(in FlightFrameData frame)
            {
                float targetExtent = 0.5f;
                float targetRotationPercent = 1;
                float targetFlagExtent = 2;
                
                if (!Mathf.Approximately(Data.CurrentExtentPercent, targetExtent))
                {
                    float err = targetExtent - Data.CurrentExtentPercent;
                    _poleVel += err * SPRING_RESPONSE * frame.DeltaTime;
                    _poleVel -= _poleVel * SPRING_DAMP * frame.DeltaTime;
                    _prevPolePos = Data.CurrentExtentPercent;
                    Data.CurrentExtentPercent = Mathf.Clamp(
                        Data.CurrentExtentPercent + _poleVel * frame.DeltaTime,
                        -Mathf.Infinity, targetExtent);

                    // 只有确实移动了才更新 transform（避免浮点误差导致微小抖动）
                    if (Mathf.Abs(Data.CurrentExtentPercent - _prevPolePos) > 1e-5f)
                    {
                        Vector3 poleBase = new Vector3(_rotateBase.transform.localPosition.x, Data.CurrentExtentPercent, _rotateBase.transform.localPosition.z);
                        Vector3 poleP2   = new Vector3(p2.transform.localPosition.x,         Data.CurrentExtentPercent, p2.transform.localPosition.z);
                        Vector3 poleP3   = new Vector3(p3.transform.localPosition.x,         Data.CurrentExtentPercent, p3.transform.localPosition.z);
                        _rotateBase.transform.localPosition = poleBase;
                        p2.transform.localPosition          = poleP2;
                        p3.transform.localPosition          = poleP3;
                    }
                }
                if (Mathf.Approximately(Data.CurrentExtentPercent, targetExtent) &&
                    !Mathf.Approximately(Data.CurrentRotationPercent, targetRotationPercent))
                {
                    float err = targetRotationPercent - Data.CurrentRotationPercent;
                    _hingeVel += err * SPRING_RESPONSE * frame.DeltaTime;
                    _hingeVel -= _hingeVel * SPRING_DAMP * frame.DeltaTime;
                    _prevHingeRot = Data.CurrentRotationPercent;
                    Data.CurrentRotationPercent = Mathf.Clamp(
                        Data.CurrentRotationPercent + _hingeVel * frame.DeltaTime,
                        Data.CurrentRotationPercent, targetRotationPercent);

                    if (Mathf.Abs(Data.CurrentRotationPercent - _prevHingeRot) > 1e-5f)
                    {
                        flagDown.localRotation = Quaternion.Euler(
                            Vector3.Lerp(
                                new Vector3(180, 0, 0),
                                new Vector3(90,  0, 0),
                                Data.CurrentRotationPercent));
                        flagFace.transform.localScale = new Vector3(
                            flagFace.transform.localScale.x,
                            flagFace.transform.localScale.y,
                            Data.CurrentRotationPercent+1);
                    }
                }
                if (Mathf.Approximately(Data.CurrentRotationPercent, targetRotationPercent) &&
                    !Mathf.Approximately(Data.CurrentExtentPercent2, targetFlagExtent))
                {
                    float err = targetFlagExtent - Data.CurrentExtentPercent2;
                    _flagVel += err * FLAG_RESPONSE * frame.DeltaTime;
                    _flagVel -= _flagVel * FLAG_DAMP * frame.DeltaTime;
                    _prevFlagScale = Data.CurrentExtentPercent2;
                    Data.CurrentExtentPercent2 = Mathf.Clamp(
                        Data.CurrentExtentPercent2 + _flagVel * frame.DeltaTime,
                        Data.CurrentExtentPercent2, targetFlagExtent);
                   

                    if (Mathf.Abs(Data.CurrentExtentPercent2 - _prevFlagScale) > 1e-5f)
                    {
                        flagFace.transform.localScale = new Vector3(
                            flagFace.transform.localScale.x,
                            Data.CurrentExtentPercent2,
                            flagFace.transform.localScale.z);
                    }
                }

                if (flagFace.transform.localScale==new Vector3(2,2,2))
                {
                    Data.IsDeployed = true;
                }
            }
        }

      

        public void SetFlagPhoto()
        {
            string imgPath = Path.Combine(Application.persistentDataPath, "UserData", "DroodismConfig", "FlagImage", "CustomImage.jpg");
            if (!File.Exists(imgPath))
            {
                return;
            }

            if (flagFace == null)
            {
                return;
            }

            byte[] imgBytes;
            try
            {
                imgBytes = File.ReadAllBytes(imgPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[FlagScript] Failed to read flag image at '" + imgPath + "': " + e);
                return;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.name = "FlagCustomImage";
            if (!tex.LoadImage(imgBytes))
            {
                Debug.LogError("[FlagScript] Failed to decode image bytes from '" + imgPath + "'.");
                UnityEngine.Object.Destroy(tex);
                return;
            }

            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);

            Transform cav = flagFace.Find("Cav");
            cav.Find("CavRenA").GetComponent<SpriteRenderer>().sprite=sprite;
            cav.Find("CavRenB").GetComponent<SpriteRenderer>().sprite=sprite;
        }


        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            model.Add(new TextButtonModel("Load Image",(b)=>SetFlagPhoto()));
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