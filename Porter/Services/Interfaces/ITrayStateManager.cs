namespace Porter.Services.Interfaces;

public interface ITrayStateManager
{
	/// <summary>
	/// Forces a recomputation of the tray indicator from the current tunnel states. Useful at app
	/// start, after settings reload, etc.
	/// </summary>
	void Refresh();
}
