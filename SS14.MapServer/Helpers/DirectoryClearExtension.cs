namespace SS14.MapServer.Helpers;

public static class DirectoryClearExtension
{
    public static void Clear(this DirectoryInfo directory)
    {
        foreach(var file in directory.GetFiles())
            file.Delete();
            
        foreach(var subDirectory in directory.GetDirectories())
            subDirectory.Delete(true);
    }
}