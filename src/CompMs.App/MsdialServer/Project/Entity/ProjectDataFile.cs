namespace CompMs.App.MsdialServer.Project.Entity;

public sealed class ProjectDataFile
{
    public ProjectDataFile(string path)
    {
        if (!path.EndsWith(".mdproject"))
        {
            throw new ArgumentException("Invalid project file.");
        }

        Path = path;
        Name = System.IO.Path.GetFileNameWithoutExtension(path);
        Folder = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
    }

    public string Name { get; }

    public string Path { get; }

    public string Folder { get; }
}
