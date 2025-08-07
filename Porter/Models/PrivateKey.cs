using System;

namespace Porter.Models
{
	public class PrivateKey
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public string? Name { get; set; }

		public string FilePath { get; set; }

		public PrivateKey(string filePath)
		{
			FilePath = filePath;
		}

		public override bool Equals(object? obj)
		{
			if (obj is not PrivateKey other)
				return false;

			return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase);
		}

		public override int GetHashCode()
		{
			return FilePath is not null
				? StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath)
				: 0;
		}
	}
}
