namespace Porter.ViewModels;

/// <summary>
/// Implemented by view models whose <c>Items</c> collection supports
/// reordering by the user (drag &amp; drop).
/// </summary>
public interface IReorderableViewModel
{
	/// <summary>
	/// Moves the item at <paramref name="oldIndex"/> to <paramref name="newIndex"/>.
	/// </summary>
	void MoveItem(int oldIndex, int newIndex);
}
