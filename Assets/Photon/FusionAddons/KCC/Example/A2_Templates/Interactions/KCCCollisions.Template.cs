namespace Fusion.Addons.KCC
{
	// Partial implementation of KCCCollision class - use this to extend with your own functionality.
	// KCCCollision is a container which contains information related to a single networked collision.
	public partial class KCCCollision
	{
		// PUBLIC MEMBERS

		// Put your properties here.
		// This instance is pooled, only stateless getters!

		// Example: return HUD information if this collision provides it => for displaying on screen.
		// Following variants are both fine.
		//public IHUDProvider HUDProvider => Processor as IHUDProvider;
		//public IHUDProvider HUDProvider => Provider as IHUDProvider;
	}

	// Partial implementation of KCCCollisions class - use this to extend with your own functionality.
	// KCCCollisions is a collection which maintains all networked collisions.
	public partial class KCCCollisions
	{
		// PUBLIC METHODS

		// Put your methods here.
		// You can iterate over All property and apply custom filtering.

		// Example: returns first HUD provider of a specific type.
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
