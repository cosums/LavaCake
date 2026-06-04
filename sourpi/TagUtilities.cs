using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text;
using OllamaSharp;
using System.Text.Json;

namespace LavaCake.SourPi;

public static class TagUtilities
{
    public static void RemoveTag(string tagToRemove, string vaultPath)
    {
        var files = Directory.EnumerateFiles(vaultPath, "*.md", SearchOption.AllDirectories);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        int filesRemovedWithTag = 0;

        foreach (var file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName == "_tags") continue; // skip the tags file
            
            try {
                string content = File.ReadAllText(file);

                if (!content.StartsWith("---")) continue; // doesn't have a yaml tag, skip for now

                string[] parts = content.Split("---", 3);
                string frontMatterText = parts[1];
                string noteBody = parts[2];
                    
                var frontmatter = deserializer.Deserialize<NoteFrontMatter>(frontMatterText);

                if (frontmatter.Tags.Remove(tagToRemove))
                {
                    filesRemovedWithTag++;
                    Console.WriteLine($"Removed tag {tagToRemove} from file {fileName}.");

                    string updatedFrontMatter = serializer.Serialize(frontmatter);

                    var finalContentBuilder = new StringBuilder();
                    finalContentBuilder.AppendLine("---");
                    finalContentBuilder.AppendLine(updatedFrontMatter);
                    finalContentBuilder.AppendLine("---");
                    finalContentBuilder.Append(noteBody);

                    File.WriteAllText(file, finalContentBuilder.ToString());
                }
            } 
            catch (YamlException ex)
            {
                Console.WriteLine($"Skipping {file}: {ex.Message}");
                continue;
            }
        }

        Console.WriteLine($"Removed tag {tagToRemove} from {filesRemovedWithTag} file(s).");
    }
}