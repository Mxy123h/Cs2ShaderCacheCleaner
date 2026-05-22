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

                message = "已尝试请求 Steam 验证 CS2 游戏文件完整性。请确认 Steam 已登录并开始验证；如果 Steam 未登录或没有开始验证，则本次未能执行验证游戏完整性，请登录 Steam 后手动执行。";
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
