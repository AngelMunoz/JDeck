# JDeck

JDeck is a [Thoth.Json](https://github.com/thoth-org/Thoth.Json)-like JSON decoder for F# built on top of `System.Text.Json`, with no external dependencies. It plays well with other libraries that use `System.Text.Json` (such as [FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson)).

General setup and usage instructions can be found in the [README.md](README.md) file.

## Imperatives

1. **NEVER PUSH WITHOUT PERMISSION.** Always ask before pushing to the remote.
2. **NEVER FORCE PUSH.** Tell the user they have to force push instead of you.
3. **Always run `dotnet fantomas .` before committing code.** Format all F# files before staging.
4. **Never use `Option.get` or `ValueOption.get`.** Always pattern match (`match`, `function`, `if ... then`) or use safe alternatives (`Option.defaultValue`, `Option.map`, `Array.choose`, etc.) to handle option values. Unchecked `.get` calls crash at runtime on `None`.
5. Pull requests made with the `gh` command should use a markdown file as the PR body, not inline escaped markdown strings.

## Project Structure

- `JDeck/` contains the packable library.
- `JDeck.Tests/` contains the test project (not packable).
- `docs/` contains the [FsDocs](https://fsprojects.github.io/FSharp.Formatting/) documentation site.

Multi-targeting: `JDeck` targets `netstandard2.0;net8.0;net9.0;net10.0`. The test project targets `net8.0`.

## Changelog Management

We follow https://github.com/ionide/KeepAChangelog guidelines.

Changelog format:

```markdown
# Changelog

## [Unreleased]

Content that is pending for release goes here.

## [1.0.0] - 2026-06-24

### Added

- Initial release
```

Each section may contain the following categories:

- Added
- Changed
- Deprecated
- Removed
- Fixed
- Security

When adding entries to the changelog, make sure to follow format and categories.

### Writing style

The changelog is written for **developers upgrading their version**, not as a development journal. Keep these rules in mind:

1. **Concise and reader-focused.** Each entry is one bullet that says what changed and why a user cares — not how it's implemented internally. No internal module/file paths, no build/milestone/phase numbers (e.g. "B12", "Phase 3"), no section references (e.g. "§6.2"), and no "mirrors the canonical X" narration. A reader should understand the entry without reading the code.

2. **Group by user-facing concern, not by task.** One bullet per feature/fix area. If multiple commits touch the same subsystem, collapse them into one bullet that names each fix briefly, rather than one bullet per commit.

3. **Only released code can be Changed or Fixed.** Features that have never shipped belong in `Added` — there is no prior version to change from or fix against. Design choices and implementation details of a new feature are part of its `Added` description, not separate `Fixed`/`Changed` entries. Use `Changed`/`Fixed` only for modifications to already-released behavior (and mark breakage with **Breaking:** or **Breaking (behavioral):**).

4. **Lead with the affected surface.** Bold-prefix each bullet with the area: `**JDeck:**`, `**CI:**`, `**Project:**`, etc. Keep breaking changes at the top of their category.

5. **Plain language.** Describe the user-visible effect, not the code diff. The reader wants to know what they'll observe, not what line changed.

### Versioning

The package version is derived from `CHANGELOG.md` at pack time by `Ionide.KeepAChangelog.Tasks`. There is no need to edit versions in project files or CI manually. Cutting a release is done by moving the desired content from `[Unreleased]` into a new version section, then triggering the release workflow.
