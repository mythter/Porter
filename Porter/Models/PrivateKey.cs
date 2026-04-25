using System;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace Porter.Models;

public partial class PrivateKey : ObservableObject
{
	public Guid Id { get; set; } = Guid.NewGuid();

	[ObservableProperty]
	private string? name;

	[ObservableProperty]
	private string filePath;

	public PrivateKey(string filePath)
	{
		FilePath = filePath;
	}

	[JsonConstructor]
	public PrivateKey()
	{
		filePath = null!;
	}
}
