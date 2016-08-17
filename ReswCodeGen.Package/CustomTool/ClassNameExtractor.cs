using System.IO;

namespace ReswCodeGen.CustomTool
{
    public static class ClassNameExtractor
    {
        public static string GetClassName(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            var fileInfo = new FileInfo(path);
            return fileInfo.Name.Replace(fileInfo.Extension, string.Empty);
        }
    }
}