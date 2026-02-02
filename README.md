# MinCh

**MinCh** is a minimal changelog generator. It reads conventional commits between Git references and generates [KeepAChangelog](https://keepachangelog.com/en/1.1.0/)-compliant changelog entries.

## Philosophy

MinCh follows the principle of **minimal effort**. You have one Git workflow, one changelog format (KeepAChangelog), and one tool to bridge them. It's opinionated by design—the tool makes decisions so you don't have to.

If you need extensive regex customization or multiple output modes, use [git-cliff](https://git-cliff.org/). If you want changelog generation that gets out of your way, MinCh is for you.

## Installation

### As a .NET Tool

```shell
dotnet tool install --global MinCh
```

### Standalone

Download the latest release from [Releases](https://github.com/jamesshenry/minch/releases).

## Usage

### Inspect Changes

See what commits MinCh found between two Git references:

```shell
minch
```

Output:

```text
Changes from 0.1.0 to HEAD
- improve command help text
- add generate command skeleton
```

Options:

- `--from <ref>`: Baseline reference (default: last tag)
- `--to <ref>`: Target reference (default: HEAD)
- `--output {text|json}`: Output format (default: text)
- `--check {None|Dirty}`: Validate working directory (default: Dirty)

### Generate Changelog

Create or append KeepAChangelog entries:

```shell
minch generate
```

This reads conventional commits since the last tag and inserts an "Unreleased" section into CHANGELOG.md (creating it if it doesn't exist).

```shell
minch generate --version 0.2.0
```

This inserts a `[0.2.0]` section and skips the "Unreleased" handling. If the version already exists, MinCh warns and does nothing.

Options:

- `--version <X.Y.Z>`: Version header for the changelog entry. If omitted, creates "Unreleased" section.
- `--output <path>`: Output file (default: CHANGELOG.md)
- `--keep-prefix`: Preserve the conventional commit type (e.g., `feat:`, `fix:`) in the output. Default: stripped.
- `--keep-hash`: Include commit hashes in the output. Default: stripped.

## Conventional Commits

MinCh parses commits following the [Conventional Commits](https://www.conventionalcommits.org/) spec:

```text
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Supported Types

MinCh maps conventional commit types to KeepAChangelog sections:

| Commit Type | Changelog Section | Example |
| --- | --- | --- |
| `feat` | Added | New functionality |
| `fix` | Fixed | Bug fixes |
| `breaking change` (in footer) | Changed | Breaking changes |

Other types (`chore`, `docs`, `style`, `refactor`, `test`, `ci`) are parsed but not included in the changelog.

### Breaking Changes

Include `BREAKING CHANGE:` in the commit footer to mark a breaking change:

```text
feat: new authentication system

BREAKING CHANGE: The old auth API is no longer supported
```

## Changelog Format

MinCh generates CHANGELOG.md in strict KeepAChangelog format:

```markdown
# Changelog

## Unreleased

### Added
- New feature X
- New feature Y

### Fixed
- Bug fix A

## [0.1.0] - 2025-01-15

### Added
- Initial release

[0.1.0]: https://github.com/jamesshenry/minch/releases/tag/0.1.0
```

**Rules:**

- Line 1 is always `# Changelog` (KeepAChangelog spec)
- New versions are inserted at the top (after the header)
- Sections appear in order: Added, Changed, Deprecated, Removed, Fixed, Security
- Commits are listed as bullet points (`-`)
- Versions are immutable once written—if you try to generate the same version twice, MinCh warns and skips

## Workflow Example

Typical release workflow:

1. Feature branches are merged to `main` (via PR/MR)
2. Switch to `main` and pull the latest:

   ```shell
   git switch main
   git pull
   ```

3. Tag the current commit:

   ```shell
   git tag v0.2.0
   ```

4. Generate the changelog:

   ```shell
   minch generate --version 0.2.0
   ```

5. Review CHANGELOG.md, commit if needed:

   ```shell
   git add CHANGELOG.md
   git commit -m "docs: changelog for 0.2.0"
   ```

If you make a mistake and want to regenerate, discard and retry:

```shell
git checkout -- CHANGELOG.md
minch generate --version 0.2.0
```

## Immutability & Safety

Once a version section is written to CHANGELOG.md, MinCh treats it as immutable. Rerunning `minch generate --version X.Y.Z` will skip and warn if that version already exists. If needed, discard changes and regenerate.
