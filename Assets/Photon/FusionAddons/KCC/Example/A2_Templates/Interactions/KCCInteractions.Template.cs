namespace Fusion.Addons.KCC
{
	// Partial implementation of generic KCCInteraction class - use this to extend with your own functionality.
	// KCCInteraction is a container which contains information related to single interaction.
	// KCCInteraction is abstract class, extending it also affects derived classes (all other interaction container types)!
	public abstract partial class KCCInteraction<TInteraction>
	{
		// PUBLIC MEMBERS

		// Put your properties here.
		// This instance is pooled, only stateless getters!

		// Example: return HUD information if this interaction provides it => for displaying on screen.
		//public IHUDProvider HUDProvider => Provider as IHUDProvider;
	}

	// Partial implementation of KCCInteractions class - use this to extend with your own functionality.
	// KCCInteractions is a collection which maintains all interactions.
	// KCCInteractions is abstract class, extending it also affects derived classes (all other interaction collection types)!
	public abstract partial class KCCInteractions<TInteraction>
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
