using System;

using Myth.Avalonia.Services.Abstractions;

using Porter.Services.Interfaces;

namespace Porter.Services;

public class DialogContextProvider : IDialogContextProvider
{
	private IDialogContext? _mainContext;

	public void SetMainDialogContext(IDialogContext context)
	{
		_mainContext = context;
	}

	public IDialogContext GetMainDialogContext()
	{
		return _mainContext
			?? throw new InvalidOperationException("Main dialog context is not set");
	}
}
