# Delete Newline(WinUI3)

## Project Status
- The current Master branch is built on WPF.
- The Master branch will be maintained without updates.
- Future versions of Delete Newline will be built based on WinUI3.
- We plan to release version 3.0 based on WinUI 3.

## Version 3.0 preview.
- Based on WinUI3.
- Supports both Light and Dark themes.
- Korean language support has been added.
- Supports multiple RegexChains, allowing for multi-keybind functionality.
- Instead of using `SetWindowsHookEx` based hooking, it operates based on `RegisterHotkey`.
- The settings now operate based on a `.json` file, and functionality for importing and exporting has been added.
- We have significantly improved the stability of the hotkey feature (copy → apply regular expression → paste to clipboard).
- will be able to convert images to text using OCR and immediately apply regular expressions to the extracted text.
- ~~A small advertisement window will be added to the bottom.~~ 

## Recommended Build Tools
1. [Multilingual App Toolkit](https://marketplace.visualstudio.com/items?itemName=dts-publisher.mat2022)
