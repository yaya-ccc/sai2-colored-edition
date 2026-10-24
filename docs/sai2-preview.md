# SAI2 preview capture

`res/sai2-preview.png` is the application-only screenshot captured during the local Sakura theme check on 2026-10-24, using SAI2 2024.08.14 and `themes/example_sakura_theme.json`. It contains a blank test canvas; no desktop, taskbar, user artwork or activation watermark.

`SaiScreenshotPreview` caches pixel-to-palette assignments once, then recolors the screenshot during editing. The crop and protected canvas/navigator/color-wheel coordinates refer to this exact capture; replacing it requires updating those masks and the preview regression test. This is an approximate screenshot preview, not a running embedded SAI instance. The color-wheel option takes effect when generating the executable.
