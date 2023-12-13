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
    }

    public string Name { get; }

    public string Path { get; }
}
