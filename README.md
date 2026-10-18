# Delete Newline

[![Microsoft Store](https://img.shields.io/badge/Get_it_from-Microsoft_Store-blue?logo=microsoft)](https://apps.microsoft.com/detail/9nc17sl0vv5s)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows_11-0078D4)](#-requirements)
[![Built with](https://img.shields.io/badge/Built_with-WinUI_3-purple)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)

> Stop reopening Notepad just to run Find & Replace. Save your regex rules once, assign a hotkey, then: **select → hotkey → paste.** Anywhere.

**Delete Newline** cleans up text with a global hotkey. Select text in an application, press your hotkey, and paste the result. Start with removing unwanted line breaks from PDFs, then save other repeatable cleanup rules as named profiles.

No more *copy → open editor → paste → find/replace → copy → paste back*. Just one keystroke.

![Main interface](https://github.com/user-attachments/assets/f50d322d-ac33-4383-bb55-cd43c053038d)

---

## ✨ Features

- 🔗 **Chained regex rules** - apply multiple find/replace patterns in sequence
- ⌨️ **Multiple hotkey profiles** - bind different rule chains to different shortcuts
- 🌐 **Works anywhere** - capture selected text from any Windows application
- 📋 **Auto-clipboard** - transformed text is ready to paste immediately
- 🤖 **MCP server built in** - let an AI assistant author, edit, and test your regex profiles for you
- 🔎 **Optional local OCR** - capture text that cannot be selected, using Windows OCR

---

## 🚀 How It Works

### ⚙️ One-Time Setup

Tell Delete Newline what you want to do. This setup is required only once per workflow.

On first use, **Remove line breaks** is ready to use with **Alt+F1**. Its `[\r\n]+` rule replaces CRLF, LF, CR, and consecutive line breaks with a single space. You can edit or delete this starter profile; deleted profiles stay deleted after restarting. Existing saved profiles are kept as they are.

On first use, the interface follows your Windows language, and the starter profile's name and description use that language. English is the fallback when the Windows language is unsupported. You can choose another interface language in Settings; your saved choice is used on subsequent launches.

1. **Create a rule chain** - add one or more regex rules that define your transformation
2. **Assign a hotkey** - bind a unique keyboard shortcut to that rule chain

### ⚡ Daily Workflow

Once it's set up, the daily flow is dead simple:

1. **Select** any text (mouse drag or keyboard selection)
2. **Press** your custom hotkey
3. **Paste** - the transformed text is on your clipboard, ready to go

![Workflow demo](https://github.com/user-attachments/assets/3e82b981-9132-4b5c-ad44-fc4c62de8891)

---

## 💡 Use Cases

A few real-world examples of what you can build with regex chains:

### 📄 Clean up text copied from PDFs
PDFs frequently break sentences with hard line breaks. Restore the original flow in one keystroke.

| Pattern | Replace with |
|---|---|
| `(?:\r\n\|\r\|\n)+` | ` ` (single space) |

This handles Windows, Unix, and classic Mac line endings without removing literal `|` characters. For example, `first\r\nsecond` becomes `first second`.

Rules run in order with .NET multiline regex semantics. Replacements support capture groups such as `$1` and escapes such as `\n` and `\t`. Each regex has a 250 ms match timeout, and chains have a two-second processing budget. Invalid patterns, invalid replacement escapes, and timeouts stop the whole transformation and leave the copied text intact. A valid empty result clears the clipboard text. The editor updates its preview after a short typing delay.

**Test Regex Chain** uses a simple input/output layout. The editor automatically preserves the sample's saved line-ending format and uses Windows CRLF for new profiles, so `\n` and `\r\n` can be tested directly without an extra setting. Legacy samples containing only the TextBox's CR line breaks are repaired to CRLF when opened. This conversion is limited to the test editor; hotkeys and MCP process the supplied text unchanged.

### 🔗 Strip tracking parameters from URLs
Remove `utm_*`, `fbclid`, `gclid`, and other junk before sharing a link. This is a great example of **chained rules** - three small rules combine into a robust cleaner:

| # | Pattern | Replace with | What it does |
|---|---|---|---|
| 1 | `[?&](utm_[^=]+\|fbclid\|gclid)=[^&]*` | (empty) | Removes each tracking param with its separator |
| 2 | `^([^?\n]*)&` | `$1?` | Repairs URLs that lost their leading `?` |
| 3 | `\?$` | (empty) | Trims a bare trailing `?` if all params were tracking |

### 📝 Convert Markdown to plain text
Chain rules to strip formatting - perfect for pasting into plain-text fields. **Rule order matters**: bold must run before italic, or `**word**` gets mangled into `*word*`.

| # | Pattern | Replace with | What it does |
|---|---|---|---|
| 1 | `\*\*(.+?)\*\*` | `$1` | Remove bold |
| 2 | `\*(.+?)\*` | `$1` | Remove italic (must run after rule 1) |
| 3 | `\[([^\]]+)\]\([^)]+\)` | `$1` | Convert links to plain text |
| 4 | `(?m)^#+\s+` | (empty) | Strip heading markers (`#`, `##`, …) |

### 📅 Reformat dates on the fly
Swap date formats.

| Pattern | Replace with | Result |
|---|---|---|
| `(\d{4})-(\d{2})-(\d{2})` | `$2/$3/$1` | `2025-04-28` → `04/28/2025` |

---

## 🤖 AI control via MCP

Specifying complex regular expressions is incredibly tricky - and asking an AI to write one, then copy-pasting it back, is an exhausting cycle. To address this pain point, Delete Newline ships with a built-in **[Model Context Protocol](https://modelcontextprotocol.io/) server**. Connect any MCP-aware AI assistant (Claude Code, GPT Codex, Hermes Agent, local agents, …) and let it manage your settings profiles through conversation:

> "There's an MCP server on 127.0.0.1:39333/mcp (streamable HTTP). Probe it, then make a hotkey that strips Markdown bold/italic and bind it to Ctrl+Shift+M."

<img width="2373" height="1155" alt="image" src="https://github.com/user-attachments/assets/2123f442-5641-4080-bce1-e8bc13c8519e" />

### Enable it

1. Open **Settings** in Delete Newline.
2. Toggle **MCP server** on. (The default port is `39333`; change it while MCP is disabled if you need to.)
3. Chat with your AI! (Only MCP-capable clients work - Claude Code, GPT Codex, or other local LLMs.)

That's it - your assistant can now read and write Delete Newline state through the same surface the GUI uses. Hotkey changes are re-registered the moment they're saved, so transformations are usable on the next keystroke.

Configure your client's **Streamable HTTP** connection with `http://127.0.0.1:39333/mcp` (or the port shown in Settings). Keep Delete Newline running. The server is off by default and listens only on the local computer; connect trusted local clients. It has no remote authentication endpoint or legacy SSE endpoint. A plain browser GET returns `405 Method Not Allowed`, which is expected for this stateless JSON transport.

The server supports MCP protocol versions `2025-03-26`, `2025-06-18`, and `2025-11-25`. Requests are limited to 1 MiB. Malformed JSON, unsupported protocol headers, and non-loopback Host/Origin headers are rejected.

| Workflow | Tools |
|---|---|
| Read and manage profiles | `get_regex_profiles`, `upsert_regex_profile`, `delete_regex_profile` |
| Edit individual rules | `insert_regex_chain_item`, `update_regex_chain_item`, `delete_regex_chain_item` |
| Preview a transformation | `test_regex_chain` |
| Manage OCR | `get_ocr_settings`, `set_ocr_settings`, `get_ocr_languages`, `install_ocr_language`, `delete_ocr_language` |
| Read and change app settings | `get_app_settings`, `set_app_setting` |

Read profiles before editing, test a chain before saving it, and use the single-rule tools for small changes. Omit `index` when creating a profile; a supplied index must already exist. `get_regex_profiles` returns an object containing a `profiles` array. In MCP JSON, use `"regex": "(?:\\r\\n|\\r|\\n)+"` for the PDF cleanup rule and `"replace": " "` for a space. Hotkeys accept strings such as `Ctrl+Shift+M` or objects such as `{"modifiers":"Control, Shift","key":"M"}`.

### How do I check the MCP tools?

You normally don't need to check the MCP tools at all. But if you're a developer - or just want to see how MCP works under the hood - run `npx @modelcontextprotocol/inspector` in your command prompt (Node.js required).

<img width="2817" height="1320" alt="image" src="https://github.com/user-attachments/assets/3d502f4c-8229-455d-b7b5-271b66b93d8a" />

---

## Optional OCR

Use **OCR → Start capture** or assign an OCR hotkey. Choose the recognition language in the capture card, drag a rectangle over the text, then paste the result. Press **Esc** to cancel. The language selector is also available in the capture overlay. To remove an OCR shortcut, use the clear button beside its field.

OCR uses the same frozen screenshot you see while selecting. Recognition runs locally with the selected Windows language; images and recognized text are not uploaded. Large captures are resized to the Windows OCR engine's image limit, and Windows supplies language-specific word spacing. The OCR result is copied as recognized; use a cleanup profile afterward if needed.

Expand **OCR language management** to install or remove Windows OCR languages. Windows may ask for administrator approval and a download. The current language cannot be removed. OCR does not need to be configured to use ordinary text cleanup.

---

## 📥 Download

Get **Delete Newline** from the Microsoft Store:

[![Get it from the Microsoft Store](https://img.shields.io/badge/Get_it_from-Microsoft_Store-blue?logo=microsoft&style=for-the-badge)](https://apps.microsoft.com/detail/9nc17sl0vv5s?hl=en-US&gl=US)

---

## 🖥️ Requirements
- Windows 11 (build 26100 or later)
- .NET 10

---

## 🤝 Contributing

Bug reports, feature ideas, and pull requests are welcome. [#](https://github.com/Cyp9715/DeleteNewline/issues)

### Build and test

Use Windows 11, the .NET 10 SDK, and the Visual Studio Windows application development tools/Windows SDK. Clone the repository's `WinUI3` branch, then run:

```powershell
dotnet test "Delete Newline.Tests/Delete Newline.Tests.csproj" -c Release
dotnet build "Delete Newline/Delete Newline.csproj" -c Release -p:Platform=x64 -p:AppxPackageSigningEnabled=false -p:GenerateAppxPackageOnBuild=false
```

Use `-p:Platform=ARM64` for the ARM64 build. GitHub Actions runs tests and both builds. These commands validate unsigned builds; Microsoft Store publication is a separate packaging step. Keep the package name and publisher identity unchanged so existing installations can update.

See [CHANGELOG.md](CHANGELOG.md) for release notes.
