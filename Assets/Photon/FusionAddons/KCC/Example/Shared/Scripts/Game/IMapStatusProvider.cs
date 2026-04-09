namespace Example
{
	using Fusion;

	/// <summary>
	///	Interface to get custom status from providers.
	/// </summary>
	public interface IMapStatusProvider
	{
		bool   IsActive(PlayerRef player);
		string GetStatus(PlayerRef player);
	}
}
