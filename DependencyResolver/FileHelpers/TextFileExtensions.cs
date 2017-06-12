using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;

namespace DependencyResolver.FileHelpers {
    public static class TextFileExtensions {
        public static IList<string> GetAllFileLines(this string filePath)
        {
            IList<string> allProjectsPaths = new List<string>();

            var fullPath = Path.GetFullPath(filePath);

            if (!File.Exists(fullPath))
            {
                Logger.Error("Missing input: " + fullPath);
                return new List<string>();
            }

            StreamReader file = new StreamReader(fullPath);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("--"))
                    continue;

                allProjectsPaths.Add(line);
            }

            file.Close();
            return allProjectsPaths;
        }
    }
}