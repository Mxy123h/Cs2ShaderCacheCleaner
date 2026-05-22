using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cs2ShaderCacheCleaner
{
    internal enum WindowsDiskCleanupResult
    {
        Cleaned,
        Skipped,
        Failed
    }

    internal static class WindowsDiskCleanup
    {
        public static string GetDirectXShaderCacheRootPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        public static List<string> GetDirectXShaderCacheCandidatePaths()
        {
            var localAppData = GetDirectXShaderCacheRootPath();
            if (string.IsNullOrWhiteSpace(localAppData))
            {
                return new List<string>();
            }

            return new List<string>
            {
                Path.Combine(localAppData, "D3DSCache"),
                Path.Combine(localAppData, "Microsoft", "DirectX Shader Cache")
            };
        }

        public static List<string> GetExistingDirectXShaderCachePaths()
        {
            return GetDirectXShaderCacheCandidatePaths()
                .Where(Directory.Exists)
                .ToList();
        }

        public static bool TryCleanDirectXShaderCache(string cs2Path, out string message)
        {
            return TryCleanCurrentUserDirectXShaderCache(out message) == WindowsDiskCleanupResult.Cleaned;
        }

        public static WindowsDiskCleanupResult TryCleanDirectXShaderCacheOnDrive(string drivePath, out string message)
        {
            return TryCleanCurrentUserDirectXShaderCache(out message);
        }

        public static WindowsDiskCleanupResult TryCleanCurrentUserDirectXShaderCache(out string message)
        {
            var driveLetter = GetDriveLetter(GetDirectXShaderCacheRootPath());

            if (string.IsNullOrWhiteSpace(driveLetter))
            {
                message = "未能定位当前用户 LocalAppData 盘符，已跳过 Windows DirectX/D3D 着色器缓存清理。";
                return WindowsDiskCleanupResult.Failed;
            }

            try
            {
                var cacheDirectories = GetDirectXShaderCacheDirectories(driveLetter).ToList();
                if (cacheDirectories.Count == 0)
                {
                    message = driveLetter + ": 盘未发现可直接清理的 Windows DirectX/D3D 着色器缓存目录，已跳过。";
                    return WindowsDiskCleanupResult.Skipped;
                }

                var deleted = 0;
                var failed = 0;
                var failureMessages = new List<string>();
                foreach (var cacheDirectory in cacheDirectories)
                {
                    CleanDirectoryContents(cacheDirectory, ref deleted, ref failed, failureMessages);
                }

                message = BuildCleanMessage(driveLetter, cacheDirectories.Count, deleted, failed, failureMessages);
                return WindowsDiskCleanupResult.Cleaned;
            }
            catch (Exception ex)
            {
                message = "清理 Windows DirectX/D3D 着色器缓存失败：" + ex.Message;
                return WindowsDiskCleanupResult.Failed;
            }
        }

        private static IEnumerable<string> GetDirectXShaderCacheDirectories(string driveLetter)
        {
            var localAppData = GetDirectXShaderCacheRootPath();
            if (string.IsNullOrWhiteSpace(localAppData) || !IsOnDrive(localAppData, driveLetter))
            {
                yield break;
            }

            foreach (var candidate in GetExistingDirectXShaderCachePaths())
            {
                yield return candidate;
            }
        }

        private static bool IsOnDrive(string path, string driveLetter)
        {
            return string.Equals(GetDriveLetter(path), driveLetter, StringComparison.OrdinalIgnoreCase);
        }

        private static void CleanDirectoryContents(string path, ref int deleted, ref int failed, ICollection<string> failureMessages)
        {
            foreach (var file in Directory.EnumerateFiles(path))
            {
                try
                {
                    DeleteFile(file);
                    deleted++;
                }
                catch (Exception ex)
                {
                    failed++;
                    AddFailureMessage(failureMessages, file, ex);
                }
            }

            foreach (var directory in Directory.EnumerateDirectories(path))
            {
                try
                {
                    DeleteDirectory(directory);
                    deleted++;
                }
                catch (Exception ex)
                {
                    failed++;
                    AddFailureMessage(failureMessages, directory, ex);
                }
            }
        }

        private static void DeleteDirectory(string path)
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                DeleteFile(file);
            }

            foreach (var directory in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                .OrderByDescending(directory => directory.Length))
            {
                File.SetAttributes(directory, FileAttributes.Normal);
            }

            File.SetAttributes(path, FileAttributes.Normal);
            Directory.Delete(path, true);
        }

        private static void DeleteFile(string path)
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }

        private static void AddFailureMessage(ICollection<string> failureMessages, string path, Exception ex)
        {
            if (failureMessages.Count >= 3)
            {
                return;
            }

            failureMessages.Add(Path.GetFileName(path) + "：" + ex.Message);
        }

        private static string BuildCleanMessage(
            string driveLetter,
            int directoryCount,
            int deleted,
            int failed,
            IEnumerable<string> failureMessages)
        {
            var message = "已直接清理 " + driveLetter + ": 盘用户级 Windows DirectX/D3D 着色器缓存目录 " + directoryCount
                          + " 个，删除 " + deleted + " 项。";

            if (failed == 0)
            {
                return message;
            }

            message += " 跳过 " + failed + " 项被占用或无法访问的项目。";
            var details = failureMessages.ToList();
            if (details.Count > 0)
            {
                message += " 示例：" + string.Join("；", details);
            }

            return message;
        }

        public static string GetDriveLetter(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "";
            }

            var root = Path.GetPathRoot(path);
            if (string.IsNullOrWhiteSpace(root) || root.Length < 2 || root[1] != ':')
            {
                return "";
            }

            return root.Substring(0, 1).ToUpperInvariant();
        }

    }
}
