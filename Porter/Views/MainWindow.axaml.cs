using System;
using System.Threading.Tasks;

using Avalonia.Controls;

using Myth.Avalonia.Controls;
using Myth.Avalonia.Controls.Enums;
using Myth.Avalonia.Services.Abstractions;

using Porter.Models;
using Porter.Services;
using Porter.Services.Interfaces;

namespace Porter.Views;

public partial class MainWindow : Window
{
	#region Private Fields

	private bool _isClosing;

	private readonly IAppDataProvider<AppData> _appDataProvider;

	private readonly IWindowStateService _windowStateService;

	#endregion

	#region Public Properties

	public TrayIcon? TrayIcon { get; set; }

	public MiniWindow? MiniWindow { get; set; }

	#endregion

	#region Constructors

	public MainWindow(IAppDataProvider<AppData> appDataProvider, IWindowStateService windowStateService)
	{
		_appDataProvider = appDataProvider;
		_windowStateService = windowStateService;

		InitializeComponent();

		_windowStateService.Restore(this, _appDataProvider.Value.WindowSettings);

		PropertyChanged += OnWindowPropertyChanged;
	}

	#endregion

	#region Overrides

	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);

		TrayIcon?.IsVisible = false;

		_ = ShowCrashInfoIfExistsSafeAsync();
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		base.OnClosing(e);

		_windowStateService.Save(this, _appDataProvider.Value.WindowSettings);

		_appDataProvider.Save();

		if (!_appDataProvider.Value.Settings.OnCloseMinimizeToTray)
		{
			_isClosing = true;
			MiniWindow?.Close();
			return;
		}

		e.Cancel = true;
		Hide();

		TrayIcon?.IsVisible = true;
	}

	#endregion

	#region Private Methods

	private void OnWindowPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
	{
		if (e.Property != IsVisibleProperty)
			return;

		if (!IsVisible && !(MiniWindow?.IsVisible ?? false) && TrayIcon is not null && !_isClosing)
		{
			TrayIcon.IsVisible = true;
		}
	}

	private async Task ShowCrashInfoIfExists()
	{
		if (CrashService.GetCrashData() is null)
		{
			return;
		}

		if (DataContext is not IDialogContext context)
			return;

		await context.ShowMessageBoxDialog(
			"The application has been restarted due to a critical error. " +
			"For details, see the crash.log file in the application folder.",
			"Error occurred",
			icon: MessageBoxIcon.Error);

		CrashService.RemoveCrashData();
	}

	private async Task ShowCrashInfoIfExistsSafeAsync()
	{
		try
		{
			await ShowCrashInfoIfExists();
		}
		catch
		{
			// Failure to show the crash dialog must not crash the app on startup.
		}
	}

	#endregion
}
