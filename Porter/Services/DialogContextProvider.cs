using System;

using Myth.Avalonia.Services.Abstractions;

using Porter.Services.Interfaces;

namespace Porter.Services;

/// <summary>
/// Resolves the main dialog context lazily via a factory delegate. The factory must defer
/// resolution until first use to break the construction cycle between <c>MainViewModel</c>
/// (which navigates to a page during construction) and pages that take this provider.
/// </summary>
public class DialogContextProvider(Func<IDialogContext> mainContextFactory) : IDialogContextProvider
{
	public IDialogContext GetMainDialogContext()
	{
		return mainContextFactory()
			?? throw new InvalidOperationException("Main dialog context is not set");
	}
}
