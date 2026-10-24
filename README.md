**简体中文** | [English](README.en.md) | [Русский](README.ru.md)

<div align="center">
  <img src="https://github.com/supchyan/yum2-theme-toolkit/assets/123704468/6a6c2116-0c8e-4f8d-ac0f-d6c1996d273f" height="150" />
</div>
<br />

# <p align=center>SAI 2 - Colored Edition</p>
<div align="center" style="padding-left: 100px;">
<table style="width:100%">
  <th>
    <a href="https://github.com/supchyan/sai2-colored-edition/tree/main/themes/">
      <img src="https://github.com/supchyan/sai2-colored-edition/assets/123704468/e174f1c7-a8bd-424f-801e-025930177841" width="24"/>
      </br>
      <span>主题示例</span>
    </a>
  </th>
  <th>
    <a href="https://github.com/supchyan/sai2-colored-edition/tree/main/ref/init/data">
      <img src="https://github.com/supchyan/sai2-colored-edition/assets/123704468/691a8bad-a32d-4ef2-a8a4-d3c7e210c11e" width="24"/>
      </br>
      <span>颜色与地址</span>
    </a>
  </th>
  <th>
    <a href="https://github.com/supchyan/sai2-colored-edition/releases">
      <img src="https://github.com/supchyan/sai2-colored-edition/assets/123704468/f2c7603f-030b-4dc1-8984-f49a4532097e" width="24"/>
      </br>
      <span>最新版本</span>
    </a>
  </th>
  <th>
    <a href="https://discord.gg/dGF8p9UGyM">
      <img src="https://github.com/supchyan/sai2-colored-edition/assets/123704468/037e74ed-d5cc-4f4c-b668-0c004180b3ab" width="24"/>
      </br>
      <span>Discord 社区</span>
    </a>
  </th>
</table>
</div>

> [!IMPORTANT]
> 原作者目前暂未继续开发此项目，欢迎有兴趣的开发者参与逆向工程。<br>
> 目前可通过这些工具修改 sai2 约 `95%` 的组件颜色，欢迎体验！

## 简介

这套工具基于 `sai2 64bit 2024.02.22` 制作，是支持自定义主题的**完整 sai2**。

如有疑问，可以前往原作者的 [Discord 服务器](https://discord.gg/dGF8p9UGyM)提问，原作者会尽力提供帮助。

祝使用愉快！^^

## 安装

从 [Releases 页面](https://github.com/supchyan/sai2-colored-edition/releases)下载最新版本，解压到任意目录，然后运行 `S2CE.exe` 即可。😎

要正常使用 sai2，需要自行准备许可证文件，本项目不提供许可证。

*原文补充：也许其他与 SAI 相关的 Git 仓库能帮上忙，不过就当我没说。*

将许可证文件放入 `ref` 文件夹，文件名可以自行命名。

## 自定义主题

有关主题的创建方法及内部结构，请参阅[主题指南](https://github.com/supchyan/sai2-colored-edition/tree/main/themes)。

## 截图

![SAI 2 自定义主题截图 1](https://github.com/supchyan/sai2-colored-edition/assets/123704468/dbe7efca-5865-4585-93d7-fc3541c33575)
![SAI 2 自定义主题截图 2](https://github.com/supchyan/sai2-colored-edition/assets/123704468/9e74f3b5-ac05-4890-bb7e-abeae43bb404)
![SAI 2 自定义主题截图 3](https://github.com/supchyan/sai2-colored-edition/assets/123704468/5389d452-b83f-4a30-b50d-ac655947be77)

## 开发

当前分支已适配经过验证的 **2024.08.14 x64 汉化构建**，打包与回归验证方式见[兼容性指南](docs/20240814-support.md)。两个受支持构建均按可执行文件的精确哈希识别。

1.3.0 提供简体中文启动器、主题卡片、选择记忆和独立恢复入口，在所选原程序旁生成独立的换肤 EXE/INI。见[使用与验证说明](docs/multiversion-copy.md)。

如需从源码构建，请安装 `.NET SDK 8.0+`，并使用 `Visual Studio` 编译项目。

🐳
