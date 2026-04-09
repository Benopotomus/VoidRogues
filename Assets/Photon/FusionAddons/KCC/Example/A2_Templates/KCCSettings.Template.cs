namespace Fusion.Addons.KCC
{
	/// <summary>
	/// Partial implementation of KCCSettings class - use this to extend with your own settings.
	/// This class doesn't support rollback, but is compatible with pooling.
	/// </summary>
	public partial class KCCSettings
	{
		// PUBLIC MEMBERS

		// Put your properties here
		/*
		public bool CustomProperty;
		*/

		// PARTIAL METHODS

		/*
		partial void CopyUserSettingsFromOther(KCCSettings other)
		{
			// Make a deep copy of your properties.
			// This method is also executed on spawn/despawn to store/restore backup.
			CustomProperty = other.CustomProperty;

			// Because this method is partial and cannot be implemented for each property separately, you have to copy all properties here.
			// Or use this method as a wrapper and split execution into multiple calls.
		}
		*/
	}
}
