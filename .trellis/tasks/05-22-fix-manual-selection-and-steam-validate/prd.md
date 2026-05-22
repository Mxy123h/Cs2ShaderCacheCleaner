# brainstorm: 修复手动勾选清理并集成 CS2 完整性验证

## Goal

修复扫描后用户手动勾选的清理项不会进入清理流程的问题，并在主界面增加一个可手动触发 Steam 验证 CS2 游戏文件完整性的按钮，让用户不必依赖清理 VPK 后的自动触发。

## What I already know

* 用户反馈：扫描完成后，原本未勾选的项目被手动勾选后不会被清除。
* 用户希望新增一个按钮，用于执行 CS2 的“验证游戏完整性”。
* `MainForm` 当前通过 `targetListView.CheckedItems` 计算待清理项目。
* `RenderTargets()` 扫描后按 `target.ItemCount > 0` 自动设置 ListView 勾选状态。
* 项目已有 `SteamGameVerifier.TryRequestCs2Validation(out string message)`，会打开 `steam://validate/730`。

## Assumptions (temporary)

* 手动验证按钮应直接复用现有 Steam URI，不需要重新实现 Steam 客户端定位。
* 清理仍只执行 `ItemCount > 0` 的项目，空项目即使被勾选也不清理。
* 本次不改变缓存扫描范围和删除策略。

## Open Questions

* 无阻塞问题。

## Requirements (evolving)

* 清理按钮必须读取用户当前实际勾选状态，包含扫描后手动勾选的项目；不能再用 `ItemCount > 0` 过滤掉用户手动选择的项目。
* 清理后仍重新扫描并刷新列表。
* 新增“验证完整性”按钮，点击后请求 Steam 验证 CS2 app 730，并把结果写入日志。
* Steam 协议请求无法确认登录态和验证进度，日志必须提示用户确认 Steam 已登录；如果未登录或未开始验证，需要手动执行验证游戏完整性。
* 忙碌状态下禁用扫描、清理和验证按钮，避免重复操作。

## Acceptance Criteria (evolving)

* [x] 扫描后手动勾选一个未默认勾选但路径有效的清理项，再点击“清理所选”，确认框和清理流程都包含该项。
* [x] 手动取消勾选的项目不会被清理。
* [x] 点击“验证完整性”会调用现有 Steam 验证逻辑，并在日志显示成功或失败信息。
* [x] Steam 未登录或未开始验证时，日志文案会提示本次未能执行验证游戏完整性，需要登录 Steam 后手动执行。
* [x] 项目可以成功编译。

## Definition of Done (team quality bar)

* Tests added/updated where practical;无测试工程时至少完成编译验证。
* Lint / typecheck / CI green where available.
* Docs/notes updated if behavior changes.
* Rollout/rollback considered if risky.

## Out of Scope (explicit)

* 不新增 Steam 验证进度检测。
* 不新增清理目标类型。
* 不修改管理员权限策略。

## Technical Notes

* 影响文件：`Cs2ShaderCacheCleaner/MainForm.cs`、`Cs2ShaderCacheCleaner/CacheCleaner.cs`、`README.md`、`Cs2ShaderCacheCleaner/README.md`。
* 复用文件：`Cs2ShaderCacheCleaner/SteamGameVerifier.cs`。
* 当前项目没有单独测试工程。
* 验证命令：`D:/devsoft/vs2022/MSBuild/Current/Bin/MSBuild.exe .\Cs2ShaderCacheCleaner\Cs2ShaderCacheCleaner.csproj /p:Configuration=Release`，结果 0 警告 0 错误。
