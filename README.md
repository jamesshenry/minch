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
