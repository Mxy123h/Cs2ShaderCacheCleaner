# CS2 着色器缓存清理工具

这是一个基于 .NET Framework 4.8 的 Windows Forms 小工具，用于扫描并清理 CS2 相关着色器缓存。

## 清理范围

- `%USERPROFILE%\AppData\Local\NVIDIA\DXCache`：清空目录内容
- `Steam安装目录\steamapps\shadercache\730`：删除该缓存目录
- `CS2安装目录\game\core\shaders_*_*.vpk`：只删除匹配的 VPK 文件
- `CS2所在盘符` 的 Windows `DirectX/D3D 着色器缓存`：通过系统自带“磁盘清理”处理
- `C:` 盘的 Windows `DirectX/D3D 着色器缓存`：通过系统自带“磁盘清理”处理；如果 CS2 就在 C 盘，则不会重复展示

## 使用方式

1. 启动程序后会自动从注册表和 Steam library manifest 尝试定位 Steam 与 CS2。
2. 如果定位不准，可手动点击“浏览...”选择目录。
3. 点击“扫描”查看数量和大小。
4. 勾选需要清理的项目，点击“清理所选”，确认后执行。

建议清理前关闭 CS2、Steam 以及可能占用缓存的程序。

程序启动时会请求管理员权限，用于配置 Windows 磁盘清理的着色器缓存清理项。

如果实际删除了 `CS2安装目录\game\core\shaders_*_*.vpk`，程序会自动请求 Steam 验证 CS2 游戏文件完整性。Steam 可能会弹出窗口或要求你等待验证完成，这不是后台静默流程。

列表中的 Windows DirectX/D3D 着色器缓存清理项会调用 Windows 自带“磁盘清理”处理，包括 CS2 所在盘和 C 盘。如果当前权限无法自动配置该清理项，程序会打开对应盘符的磁盘清理界面，并提示你手动勾选 `DirectX 着色器缓存` 或 `D3D Shader Cache`。

## 编译

需要安装 .NET Framework 4.8 Developer Pack 或 Visual Studio 2022。

```powershell
& "D:\devsoft\vs2022\MSBuild\Current\Bin\MSBuild.exe" .\src\Cs2ShaderCacheCleaner\Cs2ShaderCacheCleaner.csproj /p:Configuration=Release
```

输出文件位于：

```text
src\Cs2ShaderCacheCleaner\bin\Release\Cs2ShaderCacheCleaner.exe
```
