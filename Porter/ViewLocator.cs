using Avalonia.Controls;
using Avalonia.Controls.Templates;

using Porter.ViewModels;
using Porter.ViewModels.Pages;
using Porter.Views;

namespace Porter;

public class ViewLocator : IDataTemplate
{
	public Control Build(object? data)
	{
		if (data is null)
		{
			return new TextBlock { Text = "data was null" };
		}

		return data switch
		{
			SshTunnelsPageViewModel => new SshTunnelsPageView(),
			SshServersPageViewModel => new SshServersPageView(),
			RemoteServersPageViewModel => new RemoteServersPageView(),
			PrivateKeysPageViewModel => new PrivateKeysPageView(),
			_ => new TextBlock { Text = $"Not Found: {data?.GetType().Name}" }
		};
	}

	public bool Match(object? data) => data is ViewModelBase;
}
