# Changelog

## 3.2.3 — 2026-10-18

- Add a localized Remove line breaks starter profile on first use, assigned to Alt+F1. Its `[\r\n]+` pattern replaces CRLF, LF, CR, and consecutive line breaks with a single space.
- Create the starter only when no profile collection has been saved. Preserve existing profiles and explicitly empty collections, so deleting the starter remains effective after restarting.
- Persist the starter before registering its shortcut and complete initialization before showing the app. Keep existing shortcut conflict handling when Alt+F1 is already in use.
- Document the starter workflow and cover first use, saved edits, deletion, restart, and mixed line endings with regression tests.

## 3.2.2 — 2026-10-18

- Restore global regex and OCR shortcuts when switching to another application while the shortcut recorder retains focus. Suspend shortcuts only while recording in the active app window, and restore them when the recorder unloads.
- Track configured and active native hotkeys separately to avoid duplicate registrations, preserve conflict detection during recording, and retry failed registrations on recovery. Read current keyboard modifiers when recording a shortcut so app switches cannot leave stale modifier keys.
- Fix clipboard transformations being skipped when another application's copied text is rendered on demand. Confirm the text against a stable clipboard version before transforming it, while retaining protection against overwriting a newer copy.
- Keep a single profile and incomplete rows in the first available column. Center the complete column grid with balanced outer margins while preserving fixed card dimensions and text positions during resizing.
- Restore the standard WinUI profile selection template and use a clear three-pixel accent border for multiple selection in both themes. Preserve native focus, drag, and reorder feedback.
- Add regression coverage for shortcut recording, window activation, native registration bookkeeping, delayed clipboard rendering, and newer clipboard changes.

## 3.2.1 — 2026-10-18

- Restore the original input/output layout in Test Regex Chain. Remove the line-ending selector and its extra guidance while keeping automatic newline correction and existing sample formats.
- Center the fixed-width regex profile grid so unused horizontal space is shared equally on both sides, while preserving card dimensions and text layout during resizing.
- Align Text capture with the regex editor's page margins, content width, and compact section headings. Move the introduction into the capture card and use the standard control height for Start capture.
- Share form layout and heading styles between capture and regex editing to keep their spacing and typography consistent.

## 3.2.0 — 2026-10-18

This release keeps text cleanup as the main workflow and improves the reliability of regex processing, optional OCR, and local MCP access.

### Text cleanup

- Bound regex execution with a 250 ms match timeout and a two-second chain budget. Invalid patterns, replacement escapes, and timeouts stop the chain without replacing copied text with an error message.
- Run transformations and debounced editor previews off the UI thread. Ignore stale previews and preserve newer clipboard content that arrives during processing.
- Fix the regex test editor's CR-only Enter behavior with explicit CRLF, LF, and CR input modes. Preserve the selected mode per profile, repair legacy CR-only samples when opened, and remove the obsolete workaround warning. Clipboard and MCP transformations continue to use their exact input.
- Support transformations whose valid result is an empty string. Leave unchanged clipboard data alone, including its other formats.
- Expire pending hotkey copy requests so failed copies do not transform unrelated clipboard changes later.
- Serialize settings writes and replace files atomically. Coalesce rapid profile edits and flush them before exiting from the tray.
- Keep page content centered when resizing the window. Restore the OCR page's viewport-sized Grid around its width-limited content, explicitly stretch vertical scroll content, and remove the settings footer's unnecessary fixed width.
- Use fixed-size regex profile cards that fit at least two columns at the minimum window width. Keep card contents, text wrapping, and spacing stable while resizing; allow additional columns in wider windows and show full truncated names in tooltips.
- Align OCR cards and the language manager with Settings: shared 15-pixel padding, five-pixel gaps, four-pixel corners, and matching dark surfaces. Hide closed language guidance from layout. Refresh light-theme page, card, input, and replacement-text colors throughout the app.

### OCR

- Keep OCR as an optional local capture tool. Give Start capture a prominent action button and group it with the recognition language. Keep shortcut settings compact and Windows language management collapsed by default.
- Add a shortcut clear button, accessible control names, a missing-language explanation, and progress feedback for language management.
- Recognize the selected area from the frozen screenshot, avoiding capture controls and changes to the live desktop.
- Fit large captures within Windows OCR limits before recognition, avoid unnecessary second passes, and preserve Windows' language-aware spacing.
- Keep screenshot ownership explicit and release it when capture ends. Ignore empty drag selections and use the final pointer position.

### MCP

- Return profile lists as `{ "profiles": [...] }` so `structuredContent` conforms to MCP's object requirement. Clients that read a top-level array should use the `profiles` field.
- Negotiate supported protocol versions instead of echoing arbitrary client versions. Support `2025-03-26`, `2025-06-18`, and `2025-11-25`.
- Use stateless Streamable HTTP JSON responses. Remove the fixed session identifier and placeholder SSE response; GET correctly returns 405.
- Validate JSON-RPC requests, UTF-8, HTTP framing, request size, Host and Origin, tool arguments, and existing profile indexes. Support chunked HTTP request bodies.
- Close active sockets on shutdown, bound concurrent connections and I/O waits, and serialize tool calls. Dispatch app state access to the UI thread.
- Reject invalid MCP regex edits before saving and return regex test failures as tool errors.

### Maintenance

- Correct the README's newline pattern, document optional OCR and MCP setup, and add Windows test/build CI for x64 and ARM64.
- Preserve the existing package identity for updates. Microsoft Store submission remains a separate distribution step.
