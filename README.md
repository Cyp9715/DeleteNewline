# Delete Newline

[![Microsoft Store](https://img.shields.io/badge/Get_it_from-Microsoft_Store-blue?logo=microsoft)](https://apps.microsoft.com/detail/9nc17sl0vv5s)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows_11-0078D4)](#-requirements)
[![Built with](https://img.shields.io/badge/Built_with-WinUI_3-purple)](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)

> Stop reopening Notepad just to run Find & Replace. Save your regex rules once, assign a hotkey, then: **select → hotkey → paste.** Anywhere.

**Delete Newline** is a system-wide hotkey tool for instant text transformation. Highlight any text in any application — your browser, code editor, PDF reader, chat window — press a hotkey, and the transformed result lands on your clipboard, ready to paste.

No more *copy → open editor → paste → find/replace → copy → paste back*. Just one keystroke.

![Main interface](https://github.com/user-attachments/assets/f50d322d-ac33-4383-bb55-cd43c053038d)

---

## ✨ Features

- 🔗 **Chained regex rules** — apply multiple find/replace patterns in sequence
- ⌨️ **Multiple hotkey profiles** — bind different rule chains to different shortcuts
- 🌐 **Works anywhere** — capture selected text from any Windows application
- 📋 **Auto-clipboard** — transformed text is ready to paste immediately
- ⚡ **Zero friction** — no GUI popup, no extra clicks, no context switching

---

## 🚀 How It Works

### ⚙️ One-Time Setup

Tell Delete Newline what you want to do. This setup is required only once per workflow.

1. **Create a rule chain** — add one or more regex rules that define your transformation
2. **Assign a hotkey** — bind a unique keyboard shortcut to that rule chain

### ⚡ Daily Workflow

Once it's set up, the daily flow is dead simple:

1. **Select** any text (mouse drag or keyboard selection)
2. **Press** your custom hotkey
3. **Paste** — the transformed text is on your clipboard, ready to go

![Workflow demo](https://github.com/user-attachments/assets/3e82b981-9132-4b5c-ad44-fc4c62de8891)

---

## 💡 Use Cases

A few real-world examples of what you can build with regex chains:

### 📄 Clean up text copied from PDFs
PDFs frequently break sentences with hard line breaks. Restore the original flow in one keystroke.

| Pattern | Replace with |
|---|---|
| `[\r\n|\n]` | ` ` (single space) |

### 🔗 Strip tracking parameters from URLs
Remove `utm_*`, `fbclid`, `gclid`, and other junk before sharing a link. This is a great example of **chained rules** — three small rules combine into a robust cleaner:

| # | Pattern | Replace with | What it does |
|---|---|---|---|
| 1 | `[?&](utm_[^=]+\|fbclid\|gclid)=[^&]*` | (empty) | Removes each tracking param with its separator |
| 2 | `^([^?\n]*)&` | `$1?` | Repairs URLs that lost their leading `?` |
| 3 | `\?$` | (empty) | Trims a bare trailing `?` if all params were tracking |

### 📝 Convert Markdown to plain text
Chain rules to strip formatting — perfect for pasting into plain-text fields. **Rule order matters**: bold must run before italic, or `**word**` gets mangled into `*word*`.

| # | Pattern | Replace with | What it does |
|---|---|---|---|
| 1 | `\*\*(.+?)\*\*` | `$1` | Remove bold |
| 2 | `\*(.+?)\*` | `$1` | Remove italic (must run after rule 1) |
| 3 | `\[([^\]]+)\]\([^)]+\)` | `$1` | Convert links to plain text |
| 4 | `(?m)^#+\s+` | (empty) | Strip heading markers (`#`, `##`, …) |

### 📅 Reformat dates and data on the fly
Convert between date formats, normalize separators, fix inconsistent casing — without leaving the app you're already in.

| Pattern | Replace with | Result |
|---|---|---|
| `(\d{4})-(\d{2})-(\d{2})` | `$2/$3/$1` | `2025-04-28` → `04/28/2025` |

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
