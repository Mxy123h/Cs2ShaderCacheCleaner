using System;
using System.Diagnostics;

namespace Cs2ShaderCacheCleaner
{
    internal static class SteamGameVerifier
    {
        private const string Cs2ValidationUri = "steam://validate/730";

        public static bool TryRequestCs2Validation(out string message)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Cs2ValidationUri,
                    UseShellExecute = true
                });

                message = "已请求 Steam 验证 CS2 游戏文件完整性，请在 Steam 中等待验证完成后再启动游戏。";
                return true;
            }
            catch (Exception ex)
            {
                message = "无法自动请求 Steam 验证 CS2 游戏文件完整性，请手动在 Steam 中验证。原因：" + ex.Message;
                return false;
            }
        }
    }
}
