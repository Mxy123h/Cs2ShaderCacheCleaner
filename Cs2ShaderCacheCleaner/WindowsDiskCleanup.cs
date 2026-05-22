using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Security;

namespace Cs2ShaderCacheCleaner
{
    internal enum WindowsDiskCleanupResult
    {
        AutomaticStarted,
        ManualStarted,
        Failed
    }

    internal static class WindowsDiskCleanup
    {
        private const string CleanupProfileId = "0730";
        private const string VolumeCachesRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\VolumeCaches";
        private const string StateFlagName = "StateFlags" + CleanupProfileId;
        private static readonly string[] ShaderCacheRegistryNames =
        {
            "D3D Shader Cache",
            "DirectX Shader Cache"
        };

        public static bool TryCleanDirectXShaderCache(string cs2Path, out string message)
        {
            var driveLetter = GetDriveLetter(cs2Path);
            if (string.IsNullOrWhiteSpace(driveLetter))
            {
                message = "未能定位 CS2 所在盘符，已跳过 Windows DirectX 着色器缓存清理。";
                return false;
            }

            return TryCleanDirectXShaderCacheOnDrive(driveLetter + ":", out message) == WindowsDiskCleanupResult.AutomaticStarted;
        }

        public static WindowsDiskCleanupResult TryCleanDirectXShaderCacheOnDrive(string drivePath, out string message)
        {
            var driveLetter = GetDriveLetter(drivePath);
            if (string.IsNullOrWhiteSpace(driveLetter))
            {
                message = "未能定位 CS2 所在盘符，已跳过 Windows DirectX 着色器缓存清理。";
                return WindowsDiskCleanupResult.Failed;
            }

            try
            {
                ConfigureDirectXShaderCacheCleanup();
                StartCleanManager("/d " + driveLetter + ": /sagerun:" + CleanupProfileId);
                message = "已请求 Windows 磁盘清理清理 " + driveLetter + ": 盘的 DirectX/D3D 着色器缓存。";
                return WindowsDiskCleanupResult.AutomaticStarted;
            }
            catch (Exception ex) when (IsRegistryPermissionException(ex))
            {
                return TryOpenManualDiskCleanup(driveLetter, "当前权限无法自动配置 DirectX 着色器缓存清理项", out message);
            }
            catch (Exception ex)
            {
                return TryOpenManualDiskCleanup(driveLetter, "自动请求 Windows 磁盘清理失败：" + ex.Message, out message);
            }
        }

        private static void ConfigureDirectXShaderCacheCleanup()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(VolumeCachesRegistryPath, true))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("系统未找到 VolumeCaches 磁盘清理配置。");
                }

                foreach (var registryName in ShaderCacheRegistryNames)
                {
                    using (var shaderCacheKey = key.OpenSubKey(registryName, true))
                    {
                        if (shaderCacheKey == null)
                        {
                            continue;
                        }

                        shaderCacheKey.SetValue(StateFlagName, 2, RegistryValueKind.DWord);
                        return;
                    }
                }

                throw new InvalidOperationException("系统未找到 D3D/DirectX Shader Cache 磁盘清理项。");
            }
        }

        private static WindowsDiskCleanupResult TryOpenManualDiskCleanup(string driveLetter, string reason, out string message)
        {
            try
            {
                StartCleanManager("/d " + driveLetter + ":");
                message = reason + "，已打开 " + driveLetter + ": 盘的磁盘清理，请手动勾选“DirectX 着色器缓存”。";
                return WindowsDiskCleanupResult.ManualStarted;
            }
            catch (Exception ex)
            {
                message = reason + "，且无法打开磁盘清理：" + ex.Message;
                return WindowsDiskCleanupResult.Failed;
            }
        }

        private static void StartCleanManager(string arguments)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cleanmgr.exe",
                Arguments = arguments,
                UseShellExecute = true
            });
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

        private static bool IsRegistryPermissionException(Exception ex)
        {
            return ex is UnauthorizedAccessException || ex is SecurityException;
        }
    }
}
