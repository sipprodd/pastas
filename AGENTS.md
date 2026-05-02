# AGENTS.md — Pastas project rules

Scope: entire repository.

## Mission
Pastas is a local Windows 10/11 clipboard manager. Keep early changes small, practical, and maintainable.

## Architecture and boundaries
- Follow layered architecture documented in `docs/architecture.md`.
- Keep dependencies pointing inward (Presentation -> Application -> Domain).
- Do not bypass layers for convenience.

## Task scope rule
- Only implement the feature explicitly requested by the current task. Do not implement future-stage features unless the current task explicitly asks for them.

## Coding guidance
- Target .NET 8.
- Prefer small, focused PRs.
- Keep naming explicit and readable.
- Add tests only for implemented behavior; avoid speculative tests.

## Documentation
- Update `README.md` when project structure or setup changes.
- Keep `docs/mvp-scope.md` aligned with actual MVP intent.
