namespace Fusion.Addons.KCC
{
	/// <summary>
	/// Partial implementation of KCCData class - use this to extend with your own properties - sprint, crouch, ...
	/// This class is used to store properties which require support for rollback (using local history).
	/// </summary>
	public partial class KCCData
	{
		// PUBLIC MEMBERS

		// Here you can add your own properties.
		/*
		public bool CustomProperty;
		*/

		// PARTIAL METHODS

		/*
		partial void ClearUserData()
		{
			// Full cleanup here (lists, pools, cached data, ...)

			// Because this method is partial and cannot be implemented for each property separately, you have to cleanup all properties here.
			// Or use this method as a wrapper and split execution into multiple calls.
		}
		*/

		/*
		partial void CopyUserDataFromOther(KCCData other)
		{
			// Make a deep copy of your properties for correct rollback from local history.
			// This method is executed when you get a new state from server and rollback is triggered.
			// This method is also executed after fixed updates to copy fixed data to render data.

			CustomProperty = other.CustomProperty;

			// Because this method is partial and cannot be implemented for each property separately, you have to copy all properties here.
			// Or use this method as a wrapper and split execution into multiple calls.
		}
		*/
	}
}
