using ModApi;
using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class GravityRingScript : PartModifierScript<GravityRingData>, IFlightUpdate, IFlightStart, IDesignerStart
    {
        private Transform _mainBase, _rotateBase;
        private Transform _sideA, _struc1A, _struc2A, _struc3A,_struc4A, _ringA;
        private Transform _sideB, _struc1B, _struc2B, _struc3B,_struc4B, _ringB;
        private Transform _sideC,_struc1C, _struc2C, _struc3C,_struc4C, _ringC;
        private Transform _sideD, _struc1D, _struc2D, _struc3D,_struc4D, _ringD;
        private Transform _offset;
        private Vector3 _offsetPositionInverse;
        
        private string partState;
        
        public bool isDeployed=false;
        
        public void FlightUpdate(in FlightFrameData frame)
        {
            RotatingBase(isDeployed&&PartScript.Data.Activated);
            if (PartScript.Data.Activated)
            {
                Deploy(frame);
            }

            if (PartScript.Data.Activated == false) 
            {
                Undeploy(frame);
            }
            void Deploy(in FlightFrameData frame)
            {
                

                float targetExtent = -1.4f;
                float targetRotation = 0;
                if (Data.CurrentExtentPercent == targetExtent&&Data.CurrentRotation == targetRotation)
                {
                    isDeployed = true;
                    return;
                }
                else
                {
                    isDeployed = false;


                    if (Data.CurrentExtentPercent != targetExtent)
                    {
                        this.Data.CurrentExtentPercent = Mathf.MoveTowards(Data.CurrentExtentPercent, targetExtent,
                            frame.DeltaTime * this.Data.ExtendSpeed);
                        ExtentPart(Data.CurrentExtentPercent);
                    }

                    if (Data.CurrentExtentPercent != targetExtent || Data.CurrentRotation == targetRotation)
                        return;
                    Data.CurrentRotation = Mathf.MoveTowards(Data.CurrentRotation, targetRotation,
                        frame.DeltaTime * this.Data.DeployRotationSpeed);
                    RotateCompartments(Data.CurrentRotation);

                }

            }

            void Undeploy(in FlightFrameData frame)
            {
                float targetExtent = 0f;
                float targetRotation = 90;
                if (Data.CurrentExtentPercent == targetExtent&&Data.CurrentRotation == targetRotation)
                {
                    isDeployed = false;
                    return;
                }
                else
                {
                    isDeployed = true;
                }

                if (Data.CurrentRotation == targetRotation)
                        return;
                Data.CurrentRotation=Mathf.MoveTowards(Data.CurrentRotation, targetRotation, frame.DeltaTime * this.Data.DeployRotationSpeed);
                RotateCompartments(Data.CurrentRotation);
                
                
                if (Data.CurrentExtentPercent!=targetExtent)
                {
                    this.Data.CurrentExtentPercent = Mathf.MoveTowards(Data.CurrentExtentPercent, targetExtent, frame.DeltaTime * this.Data.ExtendSpeed);
                    ExtentPart(Data.CurrentExtentPercent);
                }

            }
        }

        public void FlightStart(in FlightFrameData frame)
        { 
            UpdateComponents();
        }

        public void DesignerStart(in DesignerFrameData frame)
        {

        }

        #region Animation Methods

        private void RotatingBase( bool active)
        {
            if ( _rotateBase == null )
            {
                Debug.LogFormat( "_rotateBase is null" );
                return;
            }
           Data.RotationSpeed = Mathf.Lerp(Data.RotationSpeed, active ?0.1f : 0.0f, Time.deltaTime * 0.4f);

           if (Mathf.Abs(Data.RotationSpeed) > 0.01f)
           {
               float yAngle = -(Data.RotationSpeed * 360.0f * 3.0f) * Time.deltaTime;
               _rotateBase.Rotate(0.0f, yAngle * (Data.IsReverse ? -1 : 1), 0.0f); 
           }
                   
               
        }
        private void ExtentPart(float target)
        {
            _struc2A.transform.localPosition = new Vector3(_struc2A.transform.localPosition.x,_struc2A.transform.localPosition.y,target);
            _struc3A.transform.localPosition = new Vector3(_struc3A.transform.localPosition.x,_struc3A.transform.localPosition.y,target);
            _struc4A.transform.localPosition = new Vector3(_struc4A.transform.localPosition.x, _struc4A.transform.localPosition.y,target);
            
            _struc2B.localPosition = new Vector3(_struc2B.localPosition.x, _struc2B.localPosition.y,target);
            _struc3B.localPosition = new Vector3(_struc3B.localPosition.x, _struc3B.localPosition.y,target);
            _struc4B.localPosition = new Vector3(_struc4B.localPosition.x, _struc4B.localPosition.y,target);
            
            _struc2C.localPosition = new Vector3(_struc2C.localPosition.x, _struc2C.localPosition.y,target);
            _struc3C.localPosition = new Vector3(_struc3C.localPosition.x, _struc3C.localPosition.y,target);
            _struc4C.localPosition = new Vector3(_struc4C.localPosition.x, _struc4C.localPosition.y,target);
            
            _struc2D.localPosition = new Vector3(_struc2D.localPosition.x, _struc2D.localPosition.y,target);
            _struc3D.localPosition = new Vector3(_struc3D.localPosition.x, _struc3D.localPosition.y,target);
            _struc4D.localPosition = new Vector3(_struc4D.localPosition.x, _struc4D.localPosition.y,target);

        }

        private void RotateCompartments(float angle)
        {
            _ringA.localRotation = Quaternion.Euler(0, 0, angle);
            _ringB.localRotation = Quaternion.Euler(0, 0, angle);
            _ringC.localRotation = Quaternion.Euler(0, 0, angle);
            _ringD.localRotation = Quaternion.Euler(0, 0, angle);
        }
        

        #endregion

        #region PrefabSetup Methods
        private void UpdateComponents()
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
                _sideA= _rotateBase.Find("SideA");
                _sideB = _rotateBase.Find("SideB");
                _sideC = _rotateBase.Find("SideC");
                _sideD = _rotateBase.Find("SideD");
                
                _struc1A = _sideA.Find("Strut1");
                _struc2A = _struc1A.Find("Strut2");
                _struc3A = _struc2A.Find("Strut3");
                _struc4A = _struc3A.Find("Strut4");
                _ringA = _struc4A.Find("Ring");
                
                _struc1B = _sideB.Find("Strut1");
                _struc2B = _struc1B.Find("Strut2");
                _struc3B = _struc2B.Find("Strut3");
                _struc4B = _struc3B.Find("Strut4");
                _ringB = _struc4B.Find("Ring");
                
                _struc1C = _sideC.Find("Strut1");
                _struc2C = _struc1C.Find("Strut2");
                _struc3C = _struc2C.Find("Strut3");
                _struc4C = _struc3C.Find("Strut4");
                _ringC = _struc4C.Find("Ring");
                
                _struc1D = _sideD.Find("Strut1");
                _struc2D = _struc1D.Find("Strut2");
                _struc3D = _struc2D.Find("Strut3");
                _struc4D = _struc3D.Find("Strut4");
                _ringD = _struc4D.Find("Ring");

                
                



            }

            
        }


        public void SetSubPart(Transform subPart)
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
            this._offsetPositionInverse = this._offset.InverseTransformPoint(this._rotateBase.position);
        }

        #endregion

        

}
}