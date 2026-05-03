# MVP Manual QA Results Checklist

Use this checklist to manually validate the first Pastas MVP before release.

- Date: __________
- Tester: __________
- Build/Commit: __________
- Environment (Windows version): __________

## 1) Startup

- [ ] App launches.
- [ ] MainWindow appears.
- [ ] App does not hang on SQLite migration.
- [ ] Logs show successful composition.

## 2) Clipboard capture

- [ ] Text copy appears in history.
- [ ] Image copy appears in history.
- [ ] Screenshot copy appears in history (if supported by environment).
- [ ] Duplicate copy updates existing item instead of creating a duplicate.

## 3) UI behavior

- [ ] Search works.
- [ ] Filters work: All / Text / Images / Pinned / Protected.
- [ ] Sort works.
- [ ] Empty state works.
- [ ] Refresh works.

## 4) Item actions

- [ ] Copy works.
- [ ] Pin works.
- [ ] Delete works.

## 5) Window lifecycle

- [ ] Close button hides app.
- [ ] Alt+V toggles app.
- [ ] Tray icon appears.
- [ ] Tray **Open Pastas** restores app.
- [ ] Tray **Exit** closes process.

## 6) Storage/cleanup

- [ ] App keeps history under configured limit.
- [ ] Pinned/protected items are not deleted by cleanup.
- [ ] Oversized captures are skipped safely.

## 7) Diagnostics

- [ ] Log file is created.
- [ ] Logs do not contain copied text content.
- [ ] Logs do not contain image bytes.
- [ ] Logs show startup/capture/cleanup lifecycle.

## 8) Known issues

- [ ] _(placeholder)_
- [ ] _(placeholder)_
- [ ] _(placeholder)_
