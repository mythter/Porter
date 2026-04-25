namespace Porter.Services.Ssh;

public record LocalPortForwardKey(string? BoundHost, uint? BoundPort, string Host, uint Port);
