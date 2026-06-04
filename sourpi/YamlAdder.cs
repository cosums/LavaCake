namespace LavaCake.SourPi;

public static class YamlAdder
{
    static string yamlTemplate = """
    ---
    title: {title}
    created: {date}
    tags:
    - untagged
    aliases:
    ---

    """;

    public static int AddTemplateYaml(string vaultPath)
    {
        int taggedCount = 0;

        var files = Directory.EnumerateFiles(vaultPath, "*.md", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            string content = File.ReadAllText(file);

            if (content.StartsWith("---")) continue; // already has a yaml tag
            string fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName == "_tags") continue; // skip the tags file

            string created = File.GetCreationTime(file).ToString("yyyy-MM-dd");

            string frontmatter = yamlTemplate
                .Replace("{title}", fileName)
                .Replace("{date}", created);

            File.WriteAllText(file, frontmatter + content);

            Console.WriteLine("Tagged file: " + fileName);

            taggedCount++;
        }

        return taggedCount;
    }
}

