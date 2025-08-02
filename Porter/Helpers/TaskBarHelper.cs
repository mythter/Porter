using Avalonia.Controls;
using Avalonia.Platform;

using Porter.Enums;

namespace Porter.Helpers
{
	public static class TaskBarHelper
	{
		public static (TaskBarLocation Location, int Size) GetTaskBarLocationAndSize(Screen? screen)
		{
			if (screen is null)
				return default;

			var bounds = screen.Bounds;
			var workArea = screen.WorkingArea;

			double left = workArea.X - bounds.X;
			double top = workArea.Y - bounds.Y;
			double right = (bounds.X + bounds.Width) - (workArea.X + workArea.Width);
			double bottom = (bounds.Y + bounds.Height) - (workArea.Y + workArea.Height);

			if (left > 0)
				return (TaskBarLocation.Left, (int)left);
			if (right > 0)
				return (TaskBarLocation.Right, (int)right);
			if (top > 0)
				return (TaskBarLocation.Top, (int)top);
			if (bottom > 0)
				return (TaskBarLocation.Bottom, (int)bottom);

			return default;
		}
	}
}
