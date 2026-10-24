# 自动识别版本与独立换肤副本（v1.0.0）

首次打开 `S2CE.exe`，点击“选择程序…”选择自己安装目录中的原 `sai2.exe`，界面会自动显示构建名称和支持状态。之后会记住上次选择的程序与主题。SHA-256、生成路径和已适配构建收在“检测详情”中。

## 使用

1. 关闭该安装目录中正在运行的 SAI，打开主题工具。
2. 选择原版 `sai2.exe`，确认界面显示“可换肤”。对应的原 INI 必须在 EXE 旁边。
3. 点击主题卡片，选择“卡布奇诺”“薄荷复古”或自己的主题，然后点击“生成主题并启动 SAI”。卡片是配色示意。
4. 工具在同目录生成 `sai2-colored.exe`、`sai2-colored.ini` 和 `sai2-colored.theme-state.json`，随后运行新 EXE。原 EXE 和原 INI 保持不变。
5. 生成后启动器会保留并显示结果。下次可直接运行生成的 EXE；需要换主题或还原时重新打开主题工具即可，上次的选择会自动恢复。

已有本工具生成的副本时，底部显示“恢复原版并启动”。执行后会校验生成文件的归属，删除本工具生成的 EXE、INI 和状态记录，再启动原版。恢复入口独立于主题列表，不需要把旧字节写回原 EXE。

启动器偏好保存在当前用户的 `%LOCALAPPDATA%\S2CE\preferences.json`，只记录程序路径和主题 ID，不修改 SAI 的配置。

## 自定义配色

内置“卡布奇诺”“薄荷复古”和“樱花浅粉”三套预设，另有原版恢复入口。可以直接在界面中创建自己的主题：

1. 选中一张主题卡片，点击“自定义配色…”，以当前配色作为起点。
2. 输入新主题名称。点击色块打开系统选色器，或直接输入六位 `#RRGGBB` 色值，右侧示意图会同步更新。
3. 按需勾选“色环背景也跟随主题”，然后点击“保存主题”。会新增并选中主题卡片；当前主题保留，同名主题不会被覆盖。
4. 关闭正在运行的 SAI，点击“生成主题并启动 SAI”应用。保存主题本身不会修改 SAI。

可独立调整以下六组颜色，每项支持 24 位 RGB，共 16,777,216 种颜色：

| 配色项 | 作用 |
| --- | --- |
| 主背景 | 工作区和主要背景 |
| 次背景 | 次级控件和背景区域 |
| 面板底色 | 工具、图层等面板 |
| 文字 | 已适配的界面文字 |
| 强调色 | 主要选中和高亮区域 |
| 辅助强调色 | 另一组选中和高亮区域 |

这些是界面颜色，不会替换作品中的绘画颜色。部分系统控件或未适配文字仍沿用原程序颜色。卡片和编辑器提供配色示意，实际效果以 SAI 为准；文字与背景过于接近时会显示阅读提示。

自建主题保存在 `%LOCALAPPDATA%\S2CE\themes`，更换工具版本后仍可加载。可复制其中的 JSON 文件分享；接收方放入自己的同一目录后重开工具即可。

选中已有的用户主题后点击“编辑配色…”，或直接双击主题卡片，即可修改原主题。点击“保存修改”会更新同一份主题文件和卡片，不会新增副本；主题名称保持不变。要保留当前配色并制作变体，点击“另存为…”，输入新名称后保存。内置预设仍以创建副本的方式使用。

保存修改后，如需应用到 SAI，请先关闭 SAI，再点击“生成主题并启动 SAI”。主界面与编辑器使用统一的深色滚动条，鼠标放在主题卡片上也可使用滚轮。

此模式复用安装目录的资源和现有授权，无需复制授权到主题工具中。非破坏性指原 EXE/INI 不被修改；SAI 运行时仍按自身行为读写个人设置，与原版共享这些设置。

## 如何判断支持

支持判断使用精确 SHA-256、文件长度、颜色地址范围和该构建的文字补丁指令基线。当前目录包含两个已经验证的构建：

- 仓库自带的 SAI2 2024.02.22 x64。
- 本次适配的 SAI2 2024.08.14 x64 汉化构建。

相同日期的不同汉化版、官方版或其他修改版不一定匹配。未知哈希会显示“暂不可用”，禁用执行按钮；检测本身不修改 SAI 文件。自动识别不等于自动适配任意未来版本，新增版本需要补充并验证 `BinaryProfiles.json` 和对应地址表。

如果同目录已有无法确认归属的 `sai2-colored.exe`、INI 或状态记录，或者其中任意生成文件被外部修改，工具会停止，避免覆盖或删除它们。请自行保留并移走冲突文件后重试。生成中断留下的不完整副本也采用这一处理方式。

## 历史验证记录（2026-10-24）

以下记录、截图和校验文件保留重编号前的开发版本号；当前软件与使用说明统一使用 v1.0.0。

221 项自动化断言全部通过，覆盖两个版本的原有换肤流程，以及只读版本检测、独立 EXE、配套 INI、原文件保留、确定性生成、classic 清理、同名文件冲突、外部修改后的拒绝处理、程序/主题记忆和损坏偏好的恢复。新增覆盖 RGB 校验、六色保存重开、同名保护、对比度提示计算、自定义主题实际字节写入及还原，以及原位修改、另存保留原主题、限制修改范围、文件丢失处理和临时文件清理。运行方式：

```powershell
dotnet run --project tests\Compatibility -c Release -- 'C:\code\sai2-colored-edition' 'C:\[Saier]SAI Ver.2\sai2.exe'
```

Windows 沙盒实测确认：

- 1.4.1 已验证[原位保存入口](evidence/theme-edit-1.4.1/02-save-changes-editor.png)、[卡片更新](evidence/theme-edit-1.4.1/03-updated-existing-card.png)、[重开保留修改](evidence/theme-edit-1.4.1/04-reopened-editor.png)、[另存独立副本](evidence/theme-edit-1.4.1/06-saved-separate-copy.png)和[卡片区域滚轮](evidence/theme-edit-1.4.1/07-final-wheel-up.png)。原位保存、滚轮和拖动先在主机开发运行时验证，最终 EXE 的双击编辑、保存重开、另存和滚轮在沙盒验证。主机应用控制拦截新编译的 EXE/DLL（0x800711C7），主机直接启动尚未通过；见[验证记录](evidence/theme-edit-1.4.1/verification.json)。
- 1.4.0 的[六色编辑与实时预览](evidence/custom-themes-1.4/02-custom-editor.png)、[保存卡片](evidence/custom-themes-1.4/03-saved-theme.png)、[重开记忆](evidence/custom-themes-1.4/04-reopened-selection.png)通过；[夜蓝主题实际运行并打开三图层作品](evidence/custom-themes-1.4/05-custom-sai-artwork.png)，[恢复原版](evidence/custom-themes-1.4/06-original-restored.png)通过。原 EXE/INI 哈希未变，生成副本已清理，见[校验记录](evidence/custom-themes-1.4/verification.json)。编辑器选色、保存与重开在主机验证，实际 SAI 启动与还原在沙盒验证。
- 1.3.0 的[主题卡片与选择记忆](evidence/launcher-1.3/02-remembered-selection.png)、[生成后反馈和独立恢复入口](evidence/launcher-1.3/04-result-and-restore-action.png)、[恢复后反馈](evidence/launcher-1.3/07-restore-feedback.png)通过；新入口实际启动了主题 SAI 和原版 SAI，原 EXE/INI 哈希未变，生成文件已清理。见[校验结果](evidence/launcher-1.3/verification.json)。

以下为 1.2.0 验证留档，界面已由上述新版取代：

- [启动器识别 20240814 并显示支持状态](evidence/multiversion-copy/03-detection-final-ui.png)。
- [改名后的 EXE 启动正常](evidence/multiversion-copy/04-colored-copy-running.png)，[三图层作品重开正常](evidence/multiversion-copy/05-colored-copy-artwork.png)。
- [重开启动器能识别已有副本](evidence/multiversion-copy/06-recognized-generated-copy.png)。
- [classic 清理副本并启动原版](evidence/multiversion-copy/07-classic-source-running.png)。
- [生成时的哈希记录](evidence/multiversion-copy/generated.json)和[classic 后的记录](evidence/multiversion-copy/classic.json)证明原 EXE/INI 始终未变，生成文件已清理。

实测发现 SAI 会按执行文件名读取同名 INI，已通过生成配套 INI 修复改名启动失败。早先的完整绘图与导出验证见 [20240814 界面实测报告](20240814-ui-validation.md)。
