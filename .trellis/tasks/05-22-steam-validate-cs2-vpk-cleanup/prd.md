# 集成 CS2 清理后的系统级修复动作

## Goal

当用户清理 `CS2 shaders_***_***.vpk` 后，程序自动请求 Steam 对 CS2 执行游戏文件完整性验证，并尽量调用 Windows 自带磁盘清理清理 CS2 所在盘符的 DirectX Shader Cache，降低着色器缓存异常导致的启动、材质或编译问题。

## Requirements

* 仅在实际删除了匹配的 VPK 文件后触发 Steam 验证。
* 使用 Steam 协议 `steam://validate/730` 请求验证 CS2。
* 触发成功、失败或无法启动 Steam 时，都要在日志中给出明确提示。
* Windows DirectX Shader Cache 必须作为独立清理项展示在列表中，允许用户单独勾选或取消。
* 用户勾选 Windows DirectX Shader Cache 清理项后，定位 CS2 所在盘符并请求 Windows 磁盘清理清理该盘的 DirectX Shader Cache。
* C: 盘的 Windows DirectX Shader Cache 也必须作为独立清理项展示；如果 CS2 所在盘已经是 C:，则不重复展示。
* Windows 磁盘清理注册表项需兼容 `D3D Shader Cache` 和 `DirectX Shader Cache` 两种名称。
* 程序启动时需要请求管理员权限，保证可配置 Windows 磁盘清理项。
* 如果无法自动配置 `cleanmgr` 的 DirectX Shader Cache 项，则打开该盘符的磁盘清理界面并提示用户手动勾选。
* NVIDIA DXCache 清理时，单个文件被占用或删除失败不能中断整个清理项，应按枚举顺序继续清理后续文件并汇总跳过数量。
* 不影响 NVIDIA DXCache 和 Steam shadercache 730 的普通清理流程。

## Acceptance Criteria

* [x] 清理 VPK 且删除数量大于 0 时，会请求 Steam 验证 CS2 完整性。
* [x] 未删除 VPK、未勾选 VPK 或清理失败时，不触发验证。
* [x] Windows DirectX Shader Cache 会作为独立清理项展示，并仅在用户勾选后执行。
* [x] 用户勾选 Windows DirectX Shader Cache 且能定位 CS2 盘符时，会请求 Windows 磁盘清理处理该盘 DirectX Shader Cache。
* [x] C: 盘 Windows DirectX Shader Cache 会作为独立清理项展示，且不会和 CS2 所在盘重复。
* [x] Windows 磁盘清理可匹配本机 `D3D Shader Cache` 注册表项。
* [x] 程序 exe 启动时会触发 UAC 管理员权限请求。
* [x] NVIDIA DXCache 中单个文件被占用时会跳过该文件并继续清理后续项目。
* [x] 非管理员权限导致无法自动配置时，有明确降级提示。
* [x] 构建通过。

## Definition of Done

* 构建通过。
* README 更新用户可见行为。
* 变更保持在当前 WinForms 工具范围内。

## Technical Approach

在 `CacheTarget`/`CleanResult` 中增加“需要 Steam 验证”的标记，由 `CacheCleaner` 在 VPK 文件实际删除后设置结果标记；`MainForm` 汇总清理结果后调用新的 Steam 验证帮助类打开 `steam://validate/730`。Windows DirectX Shader Cache 使用独立的 `CacheTargetKind.WindowsDirectXShaderCache` 清理项展示和执行，由 `CacheCleaner.Clean` 调用 Windows 磁盘清理帮助类。

## Out of Scope

* 不做 Steam 后台静默校验，因为 Steam 没有公开稳定的 headless 验证 API。
* 不检测 Steam 验证进度或验证结果。
* 不绕过 Windows 磁盘清理机制直接删除系统维护的 DirectX Shader Cache。

## Technical Notes

* 主要文件：`Cs2ShaderCacheCleaner/CacheCleaner.cs`、`Cs2ShaderCacheCleaner/MainForm.cs`、`Cs2ShaderCacheCleaner/Cs2ShaderCacheCleaner.csproj`、`Cs2ShaderCacheCleaner/README.md`、`Cs2ShaderCacheCleaner/WindowsDiskCleanup.cs`、`Cs2ShaderCacheCleaner/app.manifest`。
* 现有项目是 .NET Framework 4.8 Windows Forms 工具。
