# Changelog

## Unreleased — planned 3.2.0

This release keeps text cleanup as the main workflow and improves the reliability of regex processing, optional OCR, and local MCP access.

### Text cleanup

- Bound regex execution with a 250 ms match timeout and a two-second chain budget. Invalid patterns, replacement escapes, and timeouts stop the chain without replacing copied text with an error message.
- Run transformations and debounced editor previews off the UI thread. Ignore stale previews and preserve newer clipboard content that arrives during processing.
- Fix the regex test editor's CR-only Enter behavior with explicit CRLF, LF, and CR input modes. Preserve the selected mode per profile, repair legacy CR-only samples when opened, and remove the obsolete workaround warning. Clipboard and MCP transformations continue to use their exact input.
- Support transformations whose valid result is an empty string. Leave unchanged clipboard data alone, including its other formats.
- Expire pending hotkey copy requests so failed copies do not transform unrelated clipboard changes later.
- Serialize settings writes and replace files atomically. Coalesce rapid profile edits and flush them before exiting from the tray.
- Keep page content centered when resizing the window. Restore the OCR page's viewport-sized Grid around its width-limited content, explicitly stretch vertical scroll content, and remove the settings footer's unnecessary fixed width.
- Keep regex profile cards in at least two columns at the minimum window width. Size complete card slots to the available viewport, preserve additional columns in wider windows, and trim long profile names within the card.
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
- Preserve the existing package identity for updates. Release publication and Microsoft Store submission are pending.
