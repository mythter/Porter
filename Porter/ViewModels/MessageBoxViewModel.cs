using Porter.Enums;

namespace Porter.ViewModels
{
	public class MessageBoxViewModel : ViewModelBase
	{
		public string Title { get; set; }

		public string Message { get; set; }

		public MessageBoxIcon Icon { get; set; }

		public MessageBoxViewModel(string title, string message, MessageBoxIcon icon = MessageBoxIcon.Info)
		{
			Title = title;
			Message = message;
			Icon = icon;
		}
	}
}
