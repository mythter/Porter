using Myth.Avalonia.Services.Abstractions;

namespace Porter.Services.Interfaces;

public interface IDialogContextProvider
{
	IDialogContext GetMainDialogContext();
}
