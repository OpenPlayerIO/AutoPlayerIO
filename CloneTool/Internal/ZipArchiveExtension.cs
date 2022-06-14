using System.IO.Compression;

namespace CloneTool
{
    public static class ZipArchiveExtension
    {
        public static ZipArchiveDirectory CreateDirectory(this ZipArchive @this, string directoryPath)
        {
            return new ZipArchiveDirectory(@this, directoryPath);
        }
    }
}