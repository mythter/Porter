using System;

namespace Porter.Services.Ssh;

public record LocalPortForwardOptions
{
	public Guid TunnelId { get; init; }

	public int? LocalPort { get; init; }

	public string? SshServerUser { get; init; }

	public string? SshServerHost { get; init; }

	public int? SshServerPort { get; init; }

	public string? RemoteServerHost { get; init; }

	public int? RemoteServerPort { get; init; }

	public string? PrivateKeyFilePath { get; init; }
}
