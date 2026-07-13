using System.IO;
using System.Text.RegularExpressions;

namespace FlowForge.Installer.Modules.OpenCode;

public sealed class AgentFrontmatterPatcher
{
    static readonly Regex ModelLineRegex = new(@"^model:\s+.+$", RegexOptions.Compiled);

    public void Patch(string agentPath, string model)
    {
        var lines = File.ReadAllLines(agentPath).ToArray();
        var patched = false;

        for (var i = 0; i < lines.Length; i++)
        {
            if (ModelLineRegex.IsMatch(lines[i]))
            {
                lines[i] = $"model: {model}";
                patched = true;
                break;
            }
        }

        if (!patched)
            throw new InvalidOperationException($"No frontmatter 'model:' line found in {agentPath}");

        File.WriteAllLines(agentPath, lines);
    }
}
