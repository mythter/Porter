using System.Text.Json.Serialization;

using Porter.Models;

namespace Porter.Services;

/// <summary>
/// Source-generated <see cref="System.Text.Json"/> context for Native AOT compatibility.
/// All types serialized/deserialized by the application must be declared here so the trimmer
/// preserves their members and the JSON serializer avoids runtime reflection.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppData))]
[JsonSerializable(typeof(CrashData))]
public partial class AppJsonContext : JsonSerializerContext
{
}
