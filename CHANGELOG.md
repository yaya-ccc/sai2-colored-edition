# v1.0.0

Start the maintained edition at v1.0.0 with the existing feature set:

- Separate themed EXE/INI generation and restoration, with exact build checks.
- Theme cards, remembered selections and a six-color editor with live preview.
- User theme editing, save-as support and consistent scrolling.
- Built-in Sakura Pink theme with soft pink backgrounds and dark text.

## Historical development builds (before v1.0.0 numbering)

The entries below retain the version numbers used during development.

1.4.1 - Edit saved themes and consistent scrolling
Update existing user themes in place or save a separate copy from the editor.
Refresh edited theme cards without duplicates and keep the current selection.
Use dark scrollbars and let the mouse wheel scroll over theme cards.

1.4.0 - Custom theme colors
Edit six interface colors with a native color picker, hex input and a live preview.
Save new user themes independently of the application and built-in presets.
Keep the color-circle option with each theme and reload custom themes on startup.

1.3.0 - Simplified Chinese launcher
Replace the changelog sidebar with a focused source/theme/action flow.
Add theme cards, collapsed detection details and a separate restore action.
Remember the selected source and theme; keep the launcher open after applying.

1.2.0 - Automatic build detection and separate themed executable
Select the original SAI2 EXE to detect supported builds before applying.
Generate sai2-colored.exe and sai2-colored.ini alongside the original.
Keep the original EXE and INI unchanged; classic launches the original.
Refuse to overwrite or remove unrecognized/externally modified outputs.

1.1.0 - Local continuation
Added verified 2024.08.14 x64 localized build support.
Executable hashes select version-specific color addresses.
Backups are checked before switching themes or restoring classic SAI2.
Color-circle processing completes before saving.
Keep the matching localized sai2.ini when packaging the new build.
Fix default UI text and selected-input contrast on dark 20240814 themes.
Add self-contained packaging and captured GUI drawing/restore validation.

За этот бортик можно потянуть, чтобы было удобнее читать логи -->

[Drag this border to make a change log looks bigger] ---------------------->

Покрашено несколько новых адресов.

[Several new addresses are theme-friendly]

В файлах темы теперь можно включить или выключить покраску цветового круга. В последнем релизе на GitHub есть гайд

[You can enable or disable color circle paint feature. Read the latest release on GitHub to get into]

</>

Thanks to these folks to make this project better:

cherlormur, turnipmano, Allison
