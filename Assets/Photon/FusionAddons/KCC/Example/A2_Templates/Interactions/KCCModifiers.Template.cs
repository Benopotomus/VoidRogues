namespace Fusion.Addons.KCC
{
	// Partial implementation of KCCModifier class - use this to extend with your own functionality.
	// KCCModifier is a container which contains information related to single modifier.
	public partial class KCCModifier
	{
		// PUBLIC MEMBERS

		// Put your properties here.
		// This instance is pooled, only stateless getters!

		// Example: return HUD information if this modifier provides it => for displaying on screen.
		// Following variants are both fine.
		//public IHUDProvider HUDProvider => Processor as IHUDProvider;
		//public IHUDProvider HUD => Provider as IHUDProvider;
	}

	// Partial implementation of KCCModifiers class - use this to extend with your own functionality.
	// KCCModifiers is a collection which maintains all modifiers.
	public partial class KCCModifiers
	{
		// PUBLIC METHODS

		// Put your methods here.
		// You can iterate over All property and apply custom filter.

		// Example: returns HUD provider of a specific type.
		/*
		public T GetHUDProvider<T>() where T : IHUDProvider
		{
			for (int i = 0, count = All.Count; i < count; ++i)
			{
				if (All[i].HUDProvider is T hudProvider)
					return hudProvider;
			}

			return default;
		}
		*/
	}

	/*
	public interface IHUDProvider
	{
		string Description { get; }
	}
	*/
}
