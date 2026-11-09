using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
	using System;
	using ModApi.Craft.Parts;
	using ModApi.GameLoop.Interfaces;
	using UnityEngine;

	public class CarbonDeoxideFilterScript : ResourceProcessorPartScript<CarbonDeoxideFilterData>,IPartSubPartSetUp
	{
		private IFuelSource co2Source;
		private Transform	_offset, FanA, FanB;
		private Vector3		_offsetPositionInverse;
		
		
		public override void FlightStart( in FlightFrameData frame )
		{
			UpdateFuelSources();
			UpdateComponents();
		}


		public override void FlightUpdate( in FlightFrameData frame )
		{
			if ( co2Source == null || BatterySource == null )
			{
				return;
			}

			if ( co2Source.IsEmpty || BatterySource.IsEmpty )
			{
				WorkingAnimation( false );
				return;
			}
			WorkingAnimation( this.PartScript.Data.Activated );
			WorkingLogic( frame );
		}

		


		protected override void WorkingLogic( in FlightFrameData frame )
		{
			BackgroundLogic( frame.DeltaTimeWorld );
		}


		#region 后台结算

		/// <summary>
		/// 在飞船离开游戏视图期间也照样工作（CO2 过滤不依赖动画），只要零件是激活状态即可参与补算。
		/// 燃料源是否可用由 <see cref="BackgroundLogic"/> 内部再判断。
		/// </summary>
		public override bool IsBackgroundUpdateEnabled => PartScript.Data.Activated;


		/// <summary>
		/// 按秒结算，与 <see cref="WorkingLogic"/> 共用同一套速率，保证前后台一致。
		/// </summary>
		protected override void BackgroundLogic( double elapsedSeconds )
		{
			if ( !PartScript.Data.Activated )
			{
				return;
			}

			if ( co2Source == null || BatterySource == null )
			{
				return;
			}

			if ( co2Source.IsEmpty || BatterySource.IsEmpty )
			{
				return;
			}

			co2Source.RemoveFuel( Data.Co2ConsumptionRate * elapsedSeconds );
			BatterySource.RemoveFuel( Data.ElectricityPowerConsumptionRatePerCo2 * Data.Co2ConsumptionRate * elapsedSeconds );
		}

		#endregion


		protected override void WorkingAnimation( bool active )
		{
			if ( FanA == null )
			{
				return;
			}

			if ( FanB == null )
			{
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


		public Transform SubPart => FanA;

		protected override void UpdateComponents()
		{
			SetSubPart( IPartSubPartSetUp.FindSubPart( this, "DeviceBase/DeviceFanA" ) );
			if ( this.FanA != null )
			{
				FanB = FanA.Find( "DeviceFanB" );
			}
		}

		public void SetSubPart( Transform subPart )
		{
			this.FanA = subPart;
			this._offset = IPartSubPartSetUp.ApplySubPart( this._offset, this.FanA, Data.PositionOffset, out this._offsetPositionInverse );
		}


		#region fuelsource related
		protected override void UpdateFuelSources()
		{
			base.UpdateFuelSources();
			try
			{
				var patchScript = GetCommandPodPatch();
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
		


		#endregion
	}
}