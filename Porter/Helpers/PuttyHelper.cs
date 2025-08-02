using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Porter.Helpers
{
	public static class PuttyHelper
	{
		private static readonly string _puttygenPath = Path.Combine(AppContext.BaseDirectory, "Tools", "puttygen.exe");

		public static async Task<string?> ConvertPpkToPemAsync(string ppkFilePath)
		{
			var psi = new ProcessStartInfo
			{
				FileName = _puttygenPath,
				Arguments = $"\"{ppkFilePath}\" -O private-openssh",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8
			};

			var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

			var outputBuilder = new StringBuilder();

			process.OutputDataReceived += (s, e) => { outputBuilder.AppendLine(e.Data); };

			process.Start();
			process.BeginOutputReadLine();

			await Task.Run(process.WaitForExit);

			if (process.ExitCode != 0)
			{
				return null;
			}

			return outputBuilder.ToString();
		}
	}
}
