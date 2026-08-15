namespace Porter.Services.Ssh;

public record SshConnectionOptions(string User, string Host, int? Port, string? PrivateKeyFilePath = null);
