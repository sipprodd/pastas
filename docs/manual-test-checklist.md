# Manual Test Checklist (MVP)

Use this checklist before releasing MVP builds.

## Setup
- Clear existing clipboard history if needed.
- Start with a fresh app session.
- Optional: delete `%LocalAppData%/Pastas/logs/pastas.log` before testing.

## Core behaviors
- [ ] App launch opens the main window.
- [ ] Tray icon appears after launch.
- [ ] Main window close button hides the app (does not fully exit).
- [ ] Tray menu **Open Pastas** restores the main window.
- [ ] Tray menu **Exit** closes the app process.
- [ ] Global hotkey **Alt+V** toggles window visibility.

## Clipboard capture behaviors
- [ ] Copy plain text in another app; item appears in Pastas history.
- [ ] Copy image in another app; item appears in Pastas history.
- [ ] Image item thumbnail displays in history list.
- [ ] Use copy-back action on a text item; clipboard receives the selected item.
- [ ] Capture notification appears and auto-hides.

## Cleanup behavior
- [ ] Add enough items to exceed cleanup limit.
- [ ] Verify non-pinned and non-protected oldest items are removed first.
- [ ] Verify item count settles at configured max limit.

## Diagnostics log safety checks
Log file path: `%LocalAppData%/Pastas/logs/pastas.log`

- [ ] File is created after running the app.
- [ ] Lifecycle events are logged (composition, watcher, tray, hotkey, capture, cleanup).
- [ ] Logs do **not** contain copied text values.
- [ ] Logs do **not** contain image bytes or binary data.
- [ ] Logs do **not** contain protected or secret clipboard content.
