**简体中文** | [English](README.en.md) | [Русский](README.ru.md)

# SAI 2 - Colored Edition

为 SAI2 更换界面配色，支持自定义主题，保留原版程序。

[下载](https://github.com/yaya-ccc/sai2-colored-edition/releases) · [问题反馈](https://github.com/yaya-ccc/sai2-colored-edition/issues)

## 使用

1. 解压工具包，保留全部文件，运行 `S2CE.exe`。普通包需要 .NET 8 Desktop Runtime，自包含包无需另装。
2. 关闭 SAI，点击 **“选择程序…”**，选择原版 `sai2.exe`，并保留它旁边的 INI 文件。
3. 确认显示 **“可换肤”**，选择一张主题卡片。
4. 点击 **“生成主题并启动 SAI”**。工具会生成独立副本，原 EXE 和 INI 保持不变。

下次打开工具会记住上次选择，也可直接运行 SAI 安装目录中的 `sai2-colored.exe`。

目前支持已验证的 **2024.02.22 x64** 和 **2024.08.14 x64 汉化构建**，是否可用以工具检测结果为准。

## 自定义配色

- 选中预设，点击 **“自定义配色…”**，调整六组颜色；可以点击色块选色，也可以输入 `#RRGGBB`。
- 输入主题名称，点击 **“保存主题”**。
- 修改已有主题时，点击 **“编辑配色…”** 或双击卡片。**“保存修改”** 更新当前主题，**“另存为…”** 保留原主题并创建副本。
- 保存后，回到主界面点击 **“生成主题并启动 SAI”** 应用。

![配色编辑器](docs/screenshots/theme-editor.png)

用户主题保存在 `%LOCALAPPDATA%\S2CE\themes`。分享时发送其中的 JSON 文件，接收方放入同一用户目录后重新打开工具即可。

## 恢复原版

关闭 SAI，点击 **“恢复原版并启动”**。工具会清理校验通过的主题副本，然后启动原版。

## SAI2 效果

![SAI2 自定义主题](docs/screenshots/sai2-theme.png)

---

由 [yaya-ccc](https://github.com/yaya-ccc) 维护，基于 [supchyan 的原项目](https://github.com/supchyan/sai2-colored-edition)。
