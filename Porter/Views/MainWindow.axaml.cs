using System;

using Avalonia.Controls;

using Porter.Storage;

namespace Porter.Views;

public partial class MainWindow : Window
{
	private bool _isClosing = false;

	public TrayIcon? TrayIcon { get; set; }

	public MiniWindow? MiniWindow { get; set; }

	public MainWindow()
	{
		InitializeComponent();

		PropertyChanged += (sender, e) =>
		{
			if (e.Property == IsVisibleProperty &&
				!IsVisible && !(MiniWindow?.IsVisible ?? true)
				&& TrayIcon is not null
				&& !_isClosing)
			{
				TrayIcon.IsVisible = true;
			}
		};
	}

	protected override void OnOpened(EventArgs e)
	{
		base.OnOpened(e);

		if (TrayIcon is not null)
			TrayIcon.IsVisible = false;
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		base.OnClosing(e);

		if (!StorageManager.Settings.OnCloseMinimizeToTray)
		{
			_isClosing = true;
			MiniWindow?.Close();
			return;
		}

		e.Cancel = true;
		Hide();

		if (TrayIcon is not null) 
			TrayIcon.IsVisible = true;
	}
}
