using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cs2ShaderCacheCleaner
{
    internal enum CacheTargetKind
    {
        DirectoryContents,
        Directory,
        FilePattern,
        WindowsDirectXShaderCache
    }

    internal sealed class CacheTarget
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Pattern { get; set; }
        public CacheTargetKind Kind { get; set; }
        public bool RequiresSteamValidation { get; set; }
        public bool IsSelected { get; set; }
        public bool Exists { get; set; }
        public int ItemCount { get; set; }
        public long SizeBytes { get; set; }
        public string Status { get; set; }
    }

    internal sealed class CleanResult
    {
        public string Name { get; set; }
        public bool Success { get; set; }
        public int DeletedCount { get; set; }
        public bool RequiresSteamValidation { get; set; }
        public string Message { get; set; }
    }

    internal static class CacheCleaner
    {
        public static List<CacheTarget> BuildTargets(string steamPath, string cs2Path)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var nvidiaDxCache = Path.Combine(
                string.IsNullOrWhiteSpace(localAppData) ? Path.Combine(userProfile, "AppData", "Local") : localAppData,
                "NVIDIA",
                "DXCache");

            var targets = new List<CacheTarget>
            {
                new CacheTarget
                {
                    Name = "NVIDIA DXCache",
                    Path = nvidiaDxCache,
                    Kind = CacheTargetKind.DirectoryContents
                }
            };

            if (!string.IsNullOrWhiteSpace(steamPath))
            {
                targets.Add(new CacheTarget
                {
                    Name = "Steam shadercache 730",
                    Path = Path.Combine(steamPath, "steamapps", "shadercache", "730"),
                    Kind = CacheTargetKind.Directory
                });
            }
            else
            {
                targets.Add(new CacheTarget
                {
                    Name = "Steam shadercache 730",
                    Path = "",
                    Kind = CacheTargetKind.Directory,
                    Status = "未找到 Steam 安装目录"
                });
            }

            if (!string.IsNullOrWhiteSpace(cs2Path))
            {
                targets.Add(new CacheTarget
                {
                    Name = "CS2 shaders_***_***.vpk",
                    Path = Path.Combine(cs2Path, "game", "core"),
                    Pattern = "shaders_*_*.vpk",
                    Kind = CacheTargetKind.FilePattern,
                    RequiresSteamValidation = true
                });

            }
            else
            {
                targets.Add(new CacheTarget
                {
                    Name = "CS2 shaders_***_***.vpk",
                    Path = "",
                    Pattern = "shaders_*_*.vpk",
                    Kind = CacheTargetKind.FilePattern,
                    RequiresSteamValidation = true,
                    Status = "未找到 CS2 安装目录"
                });
            }

            AddWindowsDirectXShaderCacheTarget(targets);

            foreach (var target in targets)
            {
                Analyze(target);
            }

            return targets;
        }

        private static void AddWindowsDirectXShaderCacheTarget(ICollection<CacheTarget> targets)
        {
            var candidatePaths = WindowsDiskCleanup.GetDirectXShaderCacheCandidatePaths();
            if (candidatePaths.Count == 0)
            {
                targets.Add(new CacheTarget
                {
                    Name = "Windows DirectX/D3D 着色器缓存（当前用户）",
                    Path = "",
                    Kind = CacheTargetKind.WindowsDirectXShaderCache,
                    Status = "未找到当前用户 LocalAppData 目录"
                });
                return;
            }

            targets.Add(new CacheTarget
            {
                Name = "Windows DirectX/D3D 着色器缓存（当前用户）",
                Path = string.Join("；", candidatePaths),
                Kind = CacheTargetKind.WindowsDirectXShaderCache
            });
        }

        public static CleanResult Clean(CacheTarget target)
        {
            try
            {
                if (target == null || string.IsNullOrWhiteSpace(target.Path))
                {
                    return Failed(target, "路径为空，已跳过。");
                }

                if (target.Kind == CacheTargetKind.FilePattern)
                {
                    return CleanFilePattern(target);
                }

                if (target.Kind == CacheTargetKind.WindowsDirectXShaderCache)
                {
                    return CleanWindowsDirectXShaderCache(target);
                }

                if (!Directory.Exists(target.Path))
                {
                    return new CleanResult
                    {
                        Name = target.Name,
                        Success = true,
                        DeletedCount = 0,
                        Message = "目录不存在，无需清理。"
                    };
                }

                return target.Kind == CacheTargetKind.Directory
                    ? CleanDirectory(target)
                    : CleanDirectoryContents(target);
            }
            catch (Exception ex)
            {
                return Failed(target, ex.Message);
            }
        }

        private static void Analyze(CacheTarget target)
        {
            if (!string.IsNullOrEmpty(target.Status) && string.IsNullOrWhiteSpace(target.Path))
            {
                target.Exists = false;
                return;
            }

            try
            {
                if (target.Kind == CacheTargetKind.WindowsDirectXShaderCache)
                {
                    var existingPaths = WindowsDiskCleanup.GetExistingDirectXShaderCachePaths().ToList();
                    target.Exists = existingPaths.Count > 0;
                    target.ItemCount = existingPaths.Count;
                    target.SizeBytes = existingPaths.Sum(GetDirectorySize);
                    target.Path = existingPaths.Count > 0
                        ? string.Join("；", existingPaths)
                        : target.Path;
                    target.Status = existingPaths.Count > 0 ? "可清理（当前用户）" : "未发现缓存目录";
                    return;
                }

                if (target.Kind == CacheTargetKind.FilePattern)
                {
                    var files = GetPatternFiles(target).ToList();
                    target.Exists = files.Count > 0;
                    target.ItemCount = files.Count;
                    target.SizeBytes = files.Sum(GetFileSize);
                    target.Status = files.Count > 0 ? "可清理" : "未发现匹配文件";
                    return;
                }

                if (!Directory.Exists(target.Path))
                {
                    target.Exists = false;
                    target.Status = "目录不存在";
                    return;
                }

                var entries = Directory.EnumerateFileSystemEntries(target.Path).ToList();
                target.Exists = true;
                target.ItemCount = entries.Count;
                target.SizeBytes = GetDirectorySize(target.Path);
                target.Status = entries.Count > 0 ? "可清理" : "目录为空";
            }
            catch (Exception ex)
            {
                target.Exists = false;
                target.Status = "扫描失败：" + ex.Message;
            }
        }

        private static CleanResult CleanDirectory(CacheTarget target)
        {
            DeleteDirectory(target.Path);
            return new CleanResult
            {
                Name = target.Name,
                Success = true,
                DeletedCount = 1,
                Message = "已删除缓存目录，程序或 Steam 会在需要时重新创建。"
            };
        }

        private static CleanResult CleanDirectoryContents(CacheTarget target)
        {
            var deleted = 0;
            var failed = 0;
            var failureMessages = new List<string>();

            foreach (var file in Directory.EnumerateFiles(target.Path))
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

            foreach (var directory in Directory.EnumerateDirectories(target.Path))
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

            return new CleanResult
            {
                Name = target.Name,
                Success = deleted > 0 || failed == 0,
                DeletedCount = deleted,
                Message = BuildDirectoryContentsCleanMessage(deleted, failed, failureMessages)
            };
        }

        private static string BuildDirectoryContentsCleanMessage(int deleted, int failed, IEnumerable<string> failureMessages)
        {
            if (failed == 0)
            {
                return "已清空缓存目录内容。";
            }

            var message = "已继续清理可删除项目，跳过 " + failed + " 项被占用或无法访问的项目。";
            var details = failureMessages.ToList();
            if (details.Count > 0)
            {
                message += " 示例：" + string.Join("；", details);
            }

            if (deleted == 0)
            {
                message += " 未删除任何项目。";
            }

            return message;
        }

        private static void AddFailureMessage(ICollection<string> failureMessages, string path, Exception ex)
        {
            if (failureMessages.Count >= 3)
            {
                return;
            }

            failureMessages.Add(Path.GetFileName(path) + "：" + ex.Message);
        }

        private static CleanResult CleanFilePattern(CacheTarget target)
        {
            if (!Directory.Exists(target.Path))
            {
                return new CleanResult
                {
                    Name = target.Name,
                    Success = true,
                    DeletedCount = 0,
                    Message = "目录不存在，无需清理。"
                };
            }

            var deleted = 0;
            var failed = 0;
            var failureMessages = new List<string>();
            foreach (var file in GetPatternFiles(target))
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

            return new CleanResult
            {
                Name = target.Name,
                Success = deleted > 0 || failed == 0,
                DeletedCount = deleted,
                RequiresSteamValidation = target.RequiresSteamValidation,
                Message = BuildFilePatternCleanMessage(deleted, failed, failureMessages)
            };
        }

        private static string BuildFilePatternCleanMessage(int deleted, int failed, IEnumerable<string> failureMessages)
        {
            if (deleted == 0 && failed == 0)
            {
                return "未发现匹配文件。";
            }

            var message = deleted > 0
                ? "已删除匹配的 VPK 着色器缓存文件。"
                : "未删除任何匹配的 VPK 着色器缓存文件。";

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

        private static CleanResult CleanWindowsDirectXShaderCache(CacheTarget target)
        {
            string message;
            var result = WindowsDiskCleanup.TryCleanCurrentUserDirectXShaderCache(out message);
            return new CleanResult
            {
                Name = target.Name,
                Success = result != WindowsDiskCleanupResult.Failed,
                DeletedCount = result == WindowsDiskCleanupResult.Cleaned ? 1 : 0,
                Message = message
            };
        }

        private static IEnumerable<string> GetPatternFiles(CacheTarget target)
        {
            if (!Directory.Exists(target.Path))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(target.Path, target.Pattern, SearchOption.TopDirectoryOnly);
        }

        private static long GetDirectorySize(string path)
        {
            long total = 0;

            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    total += GetFileSize(file);
                }
            }
            catch
            {
                foreach (var file in SafeEnumerateFiles(path))
                {
                    total += GetFileSize(file);
                }
            }

            return total;
        }

        private static IEnumerable<string> SafeEnumerateFiles(string path)
        {
            var pending = new Stack<string>();
            pending.Push(path);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                string[] files;
                string[] directories;

                try
                {
                    files = Directory.GetFiles(current);
                    directories = Directory.GetDirectories(current);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                foreach (var directory in directories)
                {
                    pending.Push(directory);
                }
            }
        }

        private static long GetFileSize(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return 0;
            }
        }

        private static void DeleteFile(string path)
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }

            File.Delete(path);
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

        private static CleanResult Failed(CacheTarget target, string message)
        {
            return new CleanResult
            {
                Name = target == null ? "未知目标" : target.Name,
                Success = false,
                DeletedCount = 0,
                Message = message
            };
        }
    }
}
