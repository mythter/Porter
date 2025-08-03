using System.Linq;

namespace Porter.Helpers
{
	public static class StringHelper
	{
		public static bool IsAnyNullOrEmpty(params string?[] strings)
		{
			return strings.Any(string.IsNullOrEmpty);
		}

		public static bool IsAllNullOrEmpty(params string?[] strings)
		{
			return strings.All(string.IsNullOrEmpty);
		}
	}
}
