using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text;
using System.Text.Json;
using LavaCake.Ollama;

namespace LavaCake.SourPi;

public enum TagType
{
    NoNewTags,
    AskNewTags,
    AutoNewTags
}

/// <summary>
/// Uses Ollama model to generate tags for each note according to a database.
/// </summary>
public static class YamlTagger
{
    public static async Task<int> TagAllFiles(string vaultPath, TagType tagType)
    {

        var tagFiles = Directory.GetFiles(vaultPath, "_tags.md", SearchOption.AllDirectories);
        string tagsPath = tagFiles[0]; // only one should exist
        
        var lines = File.ReadAllLines(tagsPath);
        var tags = new List<string>();
        int tagCount = 0;
        foreach (var line in lines)
        {
            if (line.StartsWith("* "))
            {
                tags.Add(line.Substring(2).Trim());
                Console.WriteLine("Added tag: " + line.Substring(2).Trim());
                tagCount++;
            }
        }
        Console.WriteLine("Found " + tagCount + " tags.");

        var files = Directory.EnumerateFiles(vaultPath, "*.md", SearchOption.AllDirectories);

        int taggedCount = 0;

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

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .Build();
                    
                var frontmatter = deserializer.Deserialize<NoteFrontMatter>(frontMatterText);

                string llmPrompt = $$"""
                You are a tagging assistant. Given a note and a list of approved tags, return ONLY a json object with no explanation, no markdown, no backticks.
                Try to match a given note to an approved tag. If no approve tag matches, you MUST suggest a new descriptive tag in newTags.
                Only use "untagged" if the note content is completely empty or gibberish.
                Be specific with new tags - prefer "mealPlanning" over "untagged", "gameDesign" over "programming".

                Approved tags:
                {{string.Join(", ", tags)}}

                Note title:
                {{fileName}}

                Note content:
                {{noteBody}}

                Respond only with this exact JSON format:
                {
                    "matchedTags" : ["tag1", "tag2"],
                    "newTags": ["proposedNewTag1"]
                }

                matchedTags: tags from the approved list that fit this note.
                newTags: tags you think should exist but are not in the approved list. Empty array if none.
                """;

                string output = await OllamaClient.Instance.GenerateOneOffAsync(llmPrompt);

                var tagResponse = ParseLlmResponse(output);
                if (tagResponse == null)
                {
                    Console.WriteLine($"Failed to parse response for {fileName}, skipping.");
                    continue;
                }

                if (tagType != TagType.NoNewTags) {
                    foreach (var newTag in tagResponse.NewTags)
                    {
                        Console.WriteLine($"New tag proposed: '{newTag}' for note '{fileName}'. Approve?");
                        if (tagType == TagType.AskNewTags) {
                            string? input = Console.ReadLine();
                            if (input?.ToLower() == "y")
                            {
                                tags.Add(newTag);
                                File.AppendAllText(tagsPath, "\n* " + newTag);
                                tagResponse.MatchedTags.Add(newTag);
                            }
                        } else
                        {
                            tags.Add(newTag);
                            File.AppendAllText(tagsPath, "\n* " + newTag);
                            tagResponse.MatchedTags.Add(newTag);
                        }
                    }
                }

                if (tagResponse.MatchedTags.Count == 0)
                {
                    tagResponse.MatchedTags.Add("untagged");
                }

                frontmatter.Tags = tagResponse.MatchedTags;

                string updatedFrontMatter = serializer.Serialize(frontmatter);

                var finalContentBuilder = new StringBuilder();
                finalContentBuilder.AppendLine("---");
                finalContentBuilder.AppendLine(updatedFrontMatter);
                finalContentBuilder.AppendLine("---");
                finalContentBuilder.AppendLine(noteBody);

                File.WriteAllText(file, finalContentBuilder.ToString());

                taggedCount++;
            } 
            catch (YamlException ex)
            {
                Console.WriteLine($"Skipping {file}: {ex.Message}");
                continue;
            }
        }

        return taggedCount;
    }

    public static TagResponse? ParseLlmResponse(string output)
    {
        try
        {
            output = output.Trim();
            if (output.StartsWith("```"))
            {
                output = output.Substring(output.IndexOf('\n') + 1);
                output = output.Substring(0, output.LastIndexOf("```")).Trim();
            }

            int start = output.IndexOf('{');
            int end = output.LastIndexOf('}');
            if (start == -1 || end == -1) return null;
        
            output = output.Substring(start, end - start + 1);

            return JsonSerializer.Deserialize<TagResponse>(output, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true 
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse LLM response: {ex.Message}");
            return null;
        }
    }
}

public class TagResponse
{
    public List<string> MatchedTags { get; set; } = new();
    public List<string> NewTags { get; set; } = new();
}

public class NoteFrontMatter
{
    public required string Title { get; set; }
    public required string Created { get; set; }
    public required List<string> Tags { get; set; }
    public required List<string> Aliases { get; set; }
}