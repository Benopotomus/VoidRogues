namespace Fusion.Addons.KCC
{
	using System.Collections.Generic;

	/// <summary>
	/// Partial implementation of KCC class - use this to extend API with your own functionality - sprint, crouch, ...
	/// Storing information usually requires adding a property in KCCData which has support for rollback (using local history).
	/// </summary>
	public partial class KCC
	{
		// PUBLIC METHODS

		// Here you can add your own methods.
		/*
		public void SetCustomProperty(bool customProperty)
		{
			// Solution No. 1
			// Set CustomProperty property in KCCData instance. This assignment is done ONLY on current KCCData instance (_fixedData for fixed update, _renderData for render update).
			// If you call this method after KCC fixed update, it will NOT propagate to render for the same frame.
			// Data.CustomProperty = customProperty;

			// Solution No. 2
			// More correct approach in this case is to explicitly set CustomProperty for render data and fixed data.
			// This way you'll not lose customProperty information for following render frames if the method is called after fixed update.
			_renderData.CustomProperty = customProperty;

			if (IsInFixedUpdate == true)
			{
				_fixedData.CustomProperty = customProperty;
			}

			// To prevent visual glitches, it is highly recommended to call SetSprint() always before the KCC update.
			// Ideally put some asserts to make sure execution order is correct.
		}
		*/

		// PARTIAL METHODS

		/*
		partial void InitializeUserNetworkProperties(KCCNetworkContext networkContext, List<IKCCNetworkProperty> networkProperties)
		{
			// By default KCC supports network synchronization for position, look rotation, settings properties, collisions, modifiers and ignored colliders.
			// If you need more properties to synchronize which cannot be deduced or precision of the deduced value is not sufficient, you can add your own entries.

			networkProperties.Add(new KCCNetworkBool<KCCNetworkContext>(networkContext, (context, value) => context.Data.CustomProperty = value, (context) => context.Data.CustomProperty, null));

			// Because this method is partial and cannot be implemented for each property separately, you have to add all properties here.
			// Or use this method as a wrapper and split execution into multiple calls.
		}
		*/

		/*
		partial void InterpolateUserNetworkData(KCCData data, KCCInterpolationInfo interpolationInfo)
		{
			// At this point, all networked properties are already interpolated in data, including your own added in InitializeUserNetworkProperties().
			// Properties that are not networked need to be deduced somehow because they might not be available at all (proxies).

			// For example, KCCData.RealVelocity and KCCData.RealSpeed are not networked.
			// On proxies they are calculated from position difference in snapshots you are interpolating between.

			// All changes should be done on data passed as parameter.

			// Because this method is partial and cannot be implemented for each feature separately, you have to implement all features here.
			// Or use this method as a wrapper and split execution into multiple calls.
		}
		*/
	}
}
