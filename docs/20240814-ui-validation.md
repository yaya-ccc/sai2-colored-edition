# 20240814 界面、绘图与还原实测

测试日期：2026-10-24。基于 `main` 的本地改动，Windows 沙盒、x64、自带 .NET 运行时测试包、鼠标键盘输入。SAI 标题实际显示 `Preview.2024.08.14`，使用适配文档列出的精确汉化构建。

## 结果

| 项目 | 实测结果 | 证据 |
| --- | --- | --- |
| 启动与汉化 | 启动器和 SAI 正常启动；复制匹配 INI 后中文界面、工具名称正常 | [中文界面](evidence/20240814-ui/05-classic-locale-fixed.png) |
| cappuccino | 主窗口、工具面板、图层及对话框可使用；已修复默认文字不可读 | [修复后对话框](evidence/20240814-ui/14-dark-dialog-fixed.png) |
| 输入框选中状态 | 修复浅灰选区与浅色文字接近的问题，选中宽度数值可读 | [选中状态](evidence/20240814-ui/21-dark-selection-fixed.png) |
| 绘画 | 黑色铅笔笔画、撤销、重做、红色笔画正常 | [撤销](evidence/20240814-ui/09-dark-undo.png)、[重做](evidence/20240814-ui/10-dark-redo.png) |
| 图层和文件 | 创建文字图层、普通图层，三图层 SAI2 保存后重新打开正常 | [三图层作品](evidence/20240814-ui/20-dark-final-saved.png) |
| retro | 退出 SAI 后从启动器切换主题，重开同一作品，笔画及三图层正常 | [retro 效果](evidence/20240814-ui/23-retro-artwork.png) |
| classic 还原 | 从启动器还原并启动，重开作品正常；EXE 与原文件哈希相同，备份及状态记录删除 | [还原后作品](evidence/20240814-ui/25-restored-artwork.png)、[还原记录](evidence/20240814-ui/after-classic-restore.json) |
| 导出保真 | 同一 SAI2 分别在深色主题与还原后导出 512×512 PNG，所有 RGBA 像素及文件 SHA-256 相同 | [深色导出](evidence/20240814-ui/dark-artwork.png)、[还原后导出](evidence/20240814-ui/restored-artwork.png) |

PNG SHA-256：`3a2b005a61c68f4ac52138cc69c9b16daa07a9c790799cea1b7d9bf52a10dd33`。包含 4,280 个黑色像素和 4,455 个红色像素；黑色文字图层也未被界面文字修复染色。

## 本轮修复

1. 打包时复制输入 EXE 配套的 `sai2.ini`。此汉化构建使用 `lang=ja` 资源，沿用原项目的英文配置会出现英文界面和工具名称乱码。
2. 补上地址表未覆盖的三个默认界面文字转换路径。只在指定调用位置把默认黑色替换为主题文字色；原转换函数及其他调用保持原样。新画布对话框同一文字区域修复前有 1,014 个暗色像素、0 个亮色像素，修复后有 0 个暗色像素、907 个亮色像素。
3. 根据主题文字亮度设置输入框选中背景，修复浅色文字与原生浅灰选区对比度不足。
4. 提供 `-SelfContained` 打包选项，沙盒无需安装额外运行时即可执行。

二进制验证单独校验三个跳转、17 字节辅助函数和选中背景常量；除此之外的代码变化只位于已解码指令的颜色立即数。两个构建的 66 项兼容性断言全部通过。

## 复核

安装开发依赖 `Pillow` 后，在仓库根目录运行：

```powershell
python tools\verify_ui_evidence.py docs\evidence\20240814-ui
```

该命令复核保存的截图文字区域、PNG 全像素一致性、原 EXE 哈希及还原状态记录；它不重新操作 GUI。结果保存在 [verification.json](evidence/20240814-ui/verification.json)。实际 GUI 测试按表中顺序执行，每次换肤前关闭 SAI。

测试使用独立沙盒副本。宿主安装目录的 24 个非授权文件 SHA-256 均未变化；个人设置中 100 个可读取文件 SHA-256 未变化，正在使用的 `settings2.ssd` 仅核对长度和修改时间，均未变化。见 [宿主对比记录](evidence/20240814-ui/host-after-comparison.json)。

## 验证边界

尚未验证数位板压感、长时间大文件、其他 DPI、其他同日期构建或全部工具。字体列表示例仍随作品前景色显示，某些提示仍使用系统背景；主题覆盖并非全部界面元素。此次通过的是表中实际执行的流程。
