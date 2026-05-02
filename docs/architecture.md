# Pastas Architecture

Pastas follows a layered architecture to keep the MVP simple, testable, and extensible.

## Layers

### 1) Presentation
**Purpose:** WPF desktop UI (Windows 10/11), user interaction, view composition.

**Contains:**
- App startup and shell window
- ViewModels and UI state binding
- Input handling and user commands

**Rules:**
- No direct infrastructure or database calls.
- Delegate business use-cases to Application layer.

### 2) Application
**Purpose:** Orchestrates use-cases and application workflows.

**Contains:**
- Use-case handlers/services
- Command/query contracts
- Ports/interfaces for external dependencies

**Rules:**
- Can depend on Domain and Shared.
- Must not depend on concrete Infrastructure implementations.

### 3) Domain
**Purpose:** Core business model and invariants.

**Contains:**
- Entities/value objects
- Domain rules and policies
- Domain services (when needed)

**Rules:**
- No UI, IO, or framework-specific logic.
- Should remain stable and framework-agnostic.

### 4) Infrastructure
**Purpose:** Technical implementations of external concerns.

**Contains (future):**
- Persistence adapters
- OS integration adapters
- Logging adapters

**Rules:**
- Implements Application ports.
- No domain rule ownership; only technical plumbing.

### 5) Shared
**Purpose:** Cross-cutting primitives reused across layers.

**Contains:**
- Common result/error abstractions
- Lightweight shared constants/types

**Rules:**
- Keep minimal; avoid creating a dumping ground.

## Dependency direction
- Presentation -> Application -> Domain
- Infrastructure -> Application/Domain contracts
- Shared may be referenced by all layers where appropriate

This ensures domain and use-cases remain isolated from frameworks and external tooling.
