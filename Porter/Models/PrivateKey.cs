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
	}
}
