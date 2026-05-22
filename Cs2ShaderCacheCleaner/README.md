# CS2 着色器缓存清理工具

这是一个基于 .NET Framework 4.8 的 Windows Forms 小工具，用于扫描并清理 CS2 相关着色器缓存。

## 清理范围

- `%USERPROFILE%\AppData\Local\NVIDIA\DXCache`：清空目录内容
- `Steam安装目录\steamapps\shadercache\730`：删除该缓存目录
- `CS2安装目录\game\core\shaders_*_*.vpk`：只删除匹配的 VPK 文件
- 当前用户 Windows `DirectX/D3D 着色器缓存`：只清理 `%LOCALAPPDATA%` 下的用户级 DirectX/D3D Shader Cache 目录；CS2 装在 D/E 盘时通常仍清理当前用户缓存所在盘

## 使用方式

1. 启动程序后会自动从注册表和 Steam library manifest 尝试定位 Steam 与 CS2。
2. 如果定位不准，可手动点击“浏览...”选择目录。
3. 点击“扫描”查看数量和大小。
4. 勾选需要清理的项目，点击“清理所选”，确认后执行。
5. 如需单独验证游戏文件，点击“验证CS2游戏完整性”。

建议清理前关闭 CS2、Steam 以及可能占用缓存的程序。

程序启动时会请求管理员权限，用于清理部分受权限影响的缓存文件。

点击“验证CS2游戏完整性”或执行了 `CS2安装目录\game\core\shaders_*_*.vpk` 清理项后，程序会请求 Steam 验证 CS2 游戏文件完整性；即使未发现匹配的 VPK 文件，也会请求验证以便 Steam 补齐或确认文件完整。Steam 可能会弹出窗口或要求你等待验证完成，这不是后台静默流程；如果 Steam 未登录或没有开始验证，说明本次未能执行验证游戏完整性，请登录 Steam 后手动执行。

列表中的 Windows DirectX/D3D 着色器缓存清理项不会再调用 Windows 自带“磁盘清理”，避免误清理 Internet 临时文件等其它项目。程序会在路径列展示当前用户 `%LOCALAPPDATA%` 下明确的 DirectX/D3D Shader Cache 目录，例如 `D3DSCache`；如果 CS2 不在 C 盘，也仍按 Windows 实际保存用户级着色器缓存的位置处理。

## 编译

需要安装 .NET Framework 4.8 Developer Pack 或 Visual Studio 2022。

```powershell
& "D:\devsoft\vs2022\MSBuild\Current\Bin\MSBuild.exe" .\Cs2ShaderCacheCleaner\Cs2ShaderCacheCleaner.csproj /p:Configuration=Release
```

输出文件位于：

```text
Cs2ShaderCacheCleaner\bin\Release\Cs2ShaderCacheCleaner.exe
```
