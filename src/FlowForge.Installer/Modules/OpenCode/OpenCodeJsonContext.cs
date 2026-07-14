using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FlowForge.Installer.Modules.OpenCode;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(OpenCodeConfigGenerator.AgentModelsManifest))]
[JsonSerializable(typeof(OpenCodeConfigGenerator.AgentEntry))]
[JsonSerializable(typeof(OpenCodeConfigGenerator.ProviderDef))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class OpenCodeJsonContext : JsonSerializerContext
{
}
