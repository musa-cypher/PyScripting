using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace PyScripting
{
    public class Utils
    {

        public static string? TryGetFullPathFromPathEnvironmentVariable(string fileName)
        {
            int MAXPATH = 256;
            if (fileName.Length >= MAXPATH)
                throw new ArgumentException($"The executable name '{fileName}' must have less than {MAXPATH} characters.", nameof(fileName));

            var sb = new StringBuilder(fileName, MAXPATH);
            return PathFindOnPath(sb, null) ? sb.ToString() : null;
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In] string[]? ppszOtherDirs);

        public static bool IsModuleInstalled(string module, string PythonHome)
        {
            string moduleDir = Path.Combine(PythonHome, "Lib", "site-packages", module);
            return Directory.Exists(moduleDir) && File.Exists(Path.Combine(moduleDir, "__init__.py"));
        }
    }
}
