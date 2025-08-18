using ModApi;
using ModApi.Craft;
using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
	using System;
	using ModApi.Craft.Parts;
	using ModApi.GameLoop.Interfaces;
	using UnityEngine;

	public class CarbonDeoxideFilterScript : PartModifierScript<CarbonDeoxideFilterData>,
		IFlightStart,
		IFlightUpdate,
		IDesignerStart
	{
		private IFuelSource	co2Source, batterySource;
		private Transform	_offset, FanA, FanB;
		private Vector3		_offsetPositionInverse;
		public void FlightStart( in FlightFrameData frame )
		{
			UpdateFuelSources();
			UpdateComponents();
		}


		public void FlightUpdate( in FlightFrameData frame )
		{
			if ( co2Source == null || batterySource == null )
			{
				return;
			}

			if ( co2Source.IsEmpty || batterySource.IsEmpty )
			{
				WorkingAnimation( false );
				return;
			}
			WorkingAnimation( this.PartScript.Data.Activated );
			WorkingLogic( frame );
		}


		public void DesignerStart( in DesignerFrameData frame )
		{
			UpdateFuelSources();
		}


		private void WorkingLogic( in FlightFrameData frame )
		{
			if ( PartScript.Data.Activated )
			{
				co2Source.RemoveFuel( Data.Co2ConsumptionRate * frame.DeltaTimeWorld );
				batterySource.RemoveFuel( Data.ElectricityPowerConsumptionRatePerCo2 * Data.Co2ConsumptionRate * frame.DeltaTimeWorld );
			}
		}


		private void WorkingAnimation( bool active )
		{
			if ( FanA == null )
			{
				Debug.LogFormat( "FanA is null" );
				return;
			}

			if ( FanB == null )
			{
				Debug.LogFormat( "FanB is null" );
				return;
			}
			float b = 0.0f;
			if ( active )
				b = 0.25f + 0.5f;
			Data.FanSpeed = Mathf.Lerp( this.Data.FanSpeed, b, Time.deltaTime * 0.4f );
			if ( (double) this.Data.FanSpeed > 0.0 )
			{
				float zAngle = (float) (-(double) this.Data.FanSpeed * 360.0 * 3.0) * Time.deltaTime;
				FanA.Rotate( 0.0f, 0.0f, zAngle );
				FanB.Rotate( 0.0f, 0.0f, -1.5f * zAngle );
			}
		}


		private void UpdateComponents()
		{
			string[]	strArray	= "DeviceBase/DeviceFanA".Split( '/', StringSplitOptions.None );
			Transform	subPart		= this.transform;
			foreach ( string n in strArray )
				subPart = subPart.Find( n ) ?? subPart;
			if ( subPart.name == strArray[strArray.Length - 1] )
				this.SetSubPart( subPart );
			else
				this.SetSubPart( Utilities.FindFirstGameObjectMyselfOrChildren( "DeviceBase/DeviceFanA", this.gameObject ) ?.transform );
			if ( this.FanA != null )
			{
				FanB = FanA.Find( "DeviceFanB" );
			}
		}


		public void SetSubPart( Transform subPart )
		{
			if ( (UnityEngine.Object) this._offset != (UnityEngine.Object) null )
			{
				UnityEngine.Object.Destroy( (UnityEngine.Object) this._offset.gameObject );
				this._offset = (Transform) null;
			}
			this.FanA = subPart;
			if ( !( (UnityEngine.Object) this.FanA != (UnityEngine.Object) null) || (double) this.Data.PositionOffset1.magnitude <= 0.0 )
				return;
			this._offset = new GameObject( "SubPartRotatorOffset" ).transform;
			this._offset.SetParent( this.FanA.parent, false );
			this._offset.position		= this.FanA.TransformPoint( Data.PositionOffset1 );
			this._offsetPositionInverse	= this._offset.InverseTransformPoint( this.FanA.position );
		}


		#region fuelsource related
		private void UpdateFuelSources()
		{
			batterySource = PartScript.BatteryFuelSource;
			try
			{
				var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
				if (patchScript == null)
				{
					co2Source = null;
				}

				if (patchScript!= null)
				{
					co2Source = patchScript.CO2FuelSource;
				
				}
			}
			catch (Exception)
			{
				co2Source = null;
			}
			
			
		}


		private void OnCraftFuelSourceChanged( object sender, EventArgs e ) => this.UpdateFuelSources();


		public override void OnCraftLoaded( ICraftScript craftScript, bool movedToNewCraft )
		{
			this.OnCraftStructureChanged( craftScript );
			UpdateFuelSources();
		}


		public override void OnCraftStructureChanged( ICraftScript craftScript )
		{
			UpdateFuelSources();
		}


		private IFuelSource GetCraftFuelSource( string fuelType )
		{
			var craftSources = PartScript.CraftScript.FuelSources.FuelSources;

			foreach ( var source in craftSources )
			{
				if ( source.FuelType.Id.Contains( fuelType ) )
				{
					return(source);
				}
			}
			return(null);
		}


		#endregion
	}
}