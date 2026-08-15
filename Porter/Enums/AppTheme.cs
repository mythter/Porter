using System.ComponentModel;

namespace Porter.Enums;

public enum AppTheme
{
	[Description("System Default")]
	SystemDefault = 0,

	[Description("Light")]
	Light = 1,

	[Description("Dark")]
	Dark = 2,
}
