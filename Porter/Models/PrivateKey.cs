using System;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class PrivateKey : ObservableObject
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[ObservableProperty]
	public partial string? Name { get; set; }

	[ObservableProperty]
	public partial string? FilePath { get; set; }

	public PrivateKey(string filePath)
	{
		FilePath = filePath;
	}

	[JsonConstructor]
	public PrivateKey()
	{
		FilePath = null!;
	}
}
