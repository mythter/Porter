using System.Threading.Tasks;

namespace Porter.Services.Interfaces
{
	public interface IDialogService
	{
		Task ShowErrorAsync(string message, string? title = null);
	}
}
