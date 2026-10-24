# 自动识别版本与独立换肤副本（1.2.0）

打开 `S2CE.exe` 后，界面会检查工具自带的 `ref/sai2.exe`。点击“选择原版 SAI2…”可以选择任意安装目录中的原 EXE，立即显示构建名称、支持状态和生成路径。把鼠标放在原文件路径上可查看 SHA-256。

## 使用

1. 关闭该安装目录中正在运行的 SAI，打开主题工具。
2. 选择原版 `sai2.exe`，确认界面显示“支持”。对应的原 INI 必须在 EXE 旁边。
3. 选择主题，点击 `Save & Run`。
4. 工具在同目录生成 `sai2-colored.exe`、`sai2-colored.ini` 和 `sai2-colored.theme-state.json`，随后运行新 EXE。原 EXE 和原 INI 保持不变。
5. 下次启动可直接运行生成的 EXE。需要换主题或还原时，重新打开主题工具并选择同一个原 EXE。工具不会记忆上次选择的外部目录。

选择 `classic_sai2` 并执行会校验生成文件的归属，删除本工具生成的 EXE、INI 和状态记录，再启动原版。它不需要把旧字节写回原 EXE。

此模式复用安装目录的资源和现有授权，无需复制授权到主题工具中。非破坏性指原 EXE/INI 不被修改；SAI 运行时仍按自身行为读写个人设置，与原版共享这些设置。

## 如何判断支持

支持判断使用精确 SHA-256、文件长度、颜色地址范围和该构建的文字补丁指令基线。当前目录包含两个已经验证的构建：

- 仓库自带的 SAI2 2024.02.22 x64。
- 本次适配的 SAI2 2024.08.14 x64 汉化构建。

相同日期的不同汉化版、官方版或其他修改版不一定匹配。未知哈希会显示“不可换肤”，禁用执行按钮；检测本身不写入文件。自动识别不等于自动适配任意未来版本，新增版本需要补充并验证 `BinaryProfiles.json` 和对应地址表。

如果同目录已有无法确认归属的 `sai2-colored.exe`、INI 或状态记录，或者其中任意生成文件被外部修改，工具会停止，避免覆盖或删除它们。请自行保留并移走冲突文件后重试。生成中断留下的不完整副本也采用这一处理方式。

## 验证记录（2026-10-24）

186 项自动化断言全部通过，覆盖两个版本的原有换肤流程，以及只读版本检测、独立 EXE、配套 INI、原文件保留、确定性生成、classic 清理、同名文件冲突和外部修改后的拒绝处理。运行方式：

```powershell
dotnet run --project tests\Compatibility -c Release -- 'C:\code\sai2-colored-edition' 'C:\[Saier]SAI Ver.2\sai2.exe'
```

Windows 沙盒实测确认：

- [启动器识别 20240814 并显示支持状态](evidence/multiversion-copy/03-detection-final-ui.png)。
- [改名后的 EXE 启动正常](evidence/multiversion-copy/04-colored-copy-running.png)，[三图层作品重开正常](evidence/multiversion-copy/05-colored-copy-artwork.png)。
- [重开启动器能识别已有副本](evidence/multiversion-copy/06-recognized-generated-copy.png)。
- [classic 清理副本并启动原版](evidence/multiversion-copy/07-classic-source-running.png)。
- [生成时的哈希记录](evidence/multiversion-copy/generated.json)和[classic 后的记录](evidence/multiversion-copy/classic.json)证明原 EXE/INI 始终未变，生成文件已清理。

实测发现 SAI 会按执行文件名读取同名 INI，已通过生成配套 INI 修复改名启动失败。早先的完整绘图与导出验证见 [20240814 界面实测报告](20240814-ui-validation.md)。
