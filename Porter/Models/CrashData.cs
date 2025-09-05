using System;

namespace Porter.Models
{
	public record CrashData(DateTimeOffset CrashDate, string Source, string ErrorMessage, string StackTrace);
}
