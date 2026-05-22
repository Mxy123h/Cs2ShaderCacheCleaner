# CS2 着色器缓存清理工具

这是一个基于 .NET Framework 4.8 的 Windows Forms 小工具，用于扫描并清理 Counter-Strike 2 相关的着色器缓存。程序会尝试自动定位 Steam 与 CS2 安装目录，也支持手动选择路径。

## 功能特点

- 自动从注册表和 Steam library manifest 定位 Steam/CS2 路径
- 扫描缓存项目的数量、大小和实际路径
- 支持勾选后清理指定缓存项
- 支持手动请求 Steam 验证 CS2 游戏文件完整性，清理 CS2 着色器 VPK 后也会自动请求验证
- 调用 Windows 自带“磁盘清理”处理 DirectX/D3D 着色器缓存

## 清理范围

- `%USERPROFILE%\AppData\Local\NVIDIA\DXCache`：清空目录内容
- `Steam安装目录\steamapps\shadercache\730`：删除 CS2 的 Steam 着色器缓存目录
- `CS2安装目录\game\core\shaders_*_*.vpk`：只删除匹配的着色器 VPK 文件
- `CS2所在盘符` 的 Windows `DirectX/D3D 着色器缓存`：通过系统“磁盘清理”处理
- `C:` 盘的 Windows `DirectX/D3D 着色器缓存`：通过系统“磁盘清理”处理；如果 CS2 就在 C 盘，则不会重复展示

## 使用方式

1. 启动程序，等待自动定位 Steam 与 CS2 路径。
2. 如果自动定位不准确，点击“浏览...”手动选择目录。
3. 点击“扫描”，查看各清理项的数量、大小和路径。
4. 勾选需要清理的项目，点击“清理所选”。
5. 确认提示后等待清理完成。
6. 如需单独验证游戏文件，点击“验证完整性”。

建议清理前关闭 CS2、Steam 以及可能占用缓存文件的程序。

## 权限与注意事项

程序启动时会请求管理员权限，用于配置 Windows 磁盘清理的着色器缓存清理项。

点击“验证完整性”或实际删除了 `CS2安装目录\game\core\shaders_*_*.vpk` 后，程序会请求 Steam 验证 CS2 游戏文件完整性。Steam 可能会弹出窗口或要求等待验证完成，这不是后台静默流程；如果 Steam 未登录或没有开始验证，说明本次未能执行验证游戏完整性，请登录 Steam 后手动执行。

列表中的 Windows DirectX/D3D 着色器缓存清理项会调用 Windows 自带“磁盘清理”处理，包括 CS2 所在盘和 C 盘。如果当前权限无法自动配置该清理项，程序会打开对应盘符的磁盘清理界面，并提示手动勾选 `DirectX 着色器缓存` 或 `D3D Shader Cache`。

## 编译

需要安装 .NET Framework 4.8 Developer Pack 或 Visual Studio 2022。

```powershell
& "D:\devsoft\vs2022\MSBuild\Current\Bin\MSBuild.exe" .\Cs2ShaderCacheCleaner\Cs2ShaderCacheCleaner.csproj /p:Configuration=Release
```

输出文件位于：

```text
Cs2ShaderCacheCleaner\bin\Release\Cs2ShaderCacheCleaner.exe
```

## 项目结构

```text
Cs2ShaderCacheCleaner\
├── Cs2ShaderCacheCleaner.csproj  # WinForms 工程文件
├── MainForm.cs                   # 主界面与交互流程
├── CacheCleaner.cs               # 缓存扫描与清理逻辑
├── SteamLibraryLocator.cs        # Steam/CS2 路径定位
├── SteamGameVerifier.cs          # 请求 Steam 验证游戏文件
├── WindowsDiskCleanup.cs         # Windows 磁盘清理集成
├── Program.cs                    # 程序入口
└── app.manifest                  # 管理员权限声明
```
