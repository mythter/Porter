using Avalonia;
using Avalonia.Controls;

using Porter.ControlModels;

namespace Porter.Controls;

public partial class MiniSshTunnelControl : UserControl
{
	public static readonly StyledProperty<SshTunnelControlModel> ModelProperty =
		AvaloniaProperty.Register<SshTunnelControl, SshTunnelControlModel>(nameof(Model));

	public SshTunnelControlModel Model
	{
		get => GetValue(ModelProperty);
		set => SetValue(ModelProperty, value);
	}

	public MiniSshTunnelControl()
	{
		InitializeComponent();

		PropertyChanged += (_, e) =>
		{
			if (e.Property == ModelProperty)
			{
				DataContext = Model;
			}
		};
	}
}