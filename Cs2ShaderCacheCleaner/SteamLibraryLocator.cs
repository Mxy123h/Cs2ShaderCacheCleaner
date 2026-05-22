using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cs2ShaderCacheCleaner
{
    internal static class SteamLibraryLocator
    {
        public static string FindSteamPath()
        {
            foreach (var registryPath in new[]
            {
                @"Software\Valve\Steam",
                @"Software\WOW6432Node\Valve\Steam"
            })
            {
                var path = ReadRegistryValue(Registry.CurrentUser, registryPath, "SteamPath")
                           ?? ReadRegistryValue(Registry.CurrentUser, registryPath, "InstallPath")
                           ?? ReadRegistryValue(Registry.LocalMachine, registryPath, "InstallPath");

                if (Directory.Exists(path))
                {
                    return NormalizePath(path);
                }
            }

            return "";
        }

        public static string FindCs2Path(string steamPath)
        {
            if (string.IsNullOrWhiteSpace(steamPath) || !Directory.Exists(steamPath))
            {
                return "";
            }

            foreach (var library in GetSteamLibraries(steamPath))
            {
                var manifestPath = Path.Combine(library, "steamapps", "appmanifest_730.acf");
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                var installDir = ReadAcfValue(manifestPath, "installdir");
                if (string.IsNullOrWhiteSpace(installDir))
                {
                    installDir = "Counter-Strike Global Offensive";
                }

                var candidate = Path.Combine(library, "steamapps", "common", installDir);
                if (Directory.Exists(candidate))
                {
                    return NormalizePath(candidate);
                }
            }

            return "";
        }

        private static IEnumerable<string> GetSteamLibraries(string steamPath)
        {
            var libraries = new List<string> { steamPath };
            var libraryFile = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(libraryFile))
            {
                return libraries;
            }

            try
            {
                foreach (var line in File.ReadAllLines(libraryFile))
                {
                    var value = ReadVdfLineValue(line, "path");
                    if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
                    {
                        libraries.Add(value);
                    }
                }
            }
            catch
            {
                return libraries;
            }

            return libraries
                .Select(NormalizePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string ReadRegistryValue(RegistryKey root, string subKey, string valueName)
        {
            try
            {
                using (var key = root.OpenSubKey(subKey))
                {
                    return key == null ? "" : Convert.ToString(key.GetValue(valueName));
                }
            }
            catch
            {
                return "";
            }
        }

        private static string ReadAcfValue(string filePath, string key)
        {
            try
            {
                foreach (var line in File.ReadAllLines(filePath))
                {
                    var value = ReadVdfLineValue(line, key);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }
            }
            catch
            {
                return "";
            }

            return "";
        }

        private static string ReadVdfLineValue(string line, string key)
        {
            if (line == null)
            {
                return "";
            }

            var parts = line
                .Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
                .ToArray();

            if (parts.Length >= 2 && string.Equals(parts[0], key, StringComparison.OrdinalIgnoreCase))
            {
                return parts[1].Replace(@"\\", @"\");
            }

            return "";
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? ""
                : path.Replace('/', '\\').TrimEnd('\\');
        }
    }
}
