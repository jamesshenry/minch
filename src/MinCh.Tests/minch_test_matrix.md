# MinCh v1 Testing Matrix

## 1. GitRef / Ref Resolution

| Test Case | Input | Expected Behavior |
|-----------|-------|-----------------|
| Last tag | `"last-tag"` in repo with multiple tags | Resolves to most recent reachable tag commit SHA |
| Explicit tag | `"v1.2.3"` | Resolves to tag commit SHA |
| Non-existent tag | `"v9.9.9"` | Throws / returns error |
| Branch name | `"main"` | Resolves to branch tip SHA |
| Non-existent branch | `"not-a-branch"` | Throws / error |
| Commit SHA | `"a1b2c3d"` | Resolves to full SHA |
| HEAD | `"HEAD"` | Resolves to current commit SHA |
| Detached HEAD | Detached HEAD at a commit | HEAD resolves to that commit |
| Shallow clone | Tag or branch in shallow clone | Resolves correctly, or fails gracefully |
| Ambiguous ref | Name exists as both tag and branch | Should pick a consistent resolution (documented behavior) |

## 2. ChangeSetBuilder / GetChangeSet

| Test Case | Git State | Expected Behavior |
|-----------|-----------|-----------------|
| Clean working tree, commits exist | Normal | `IsDirty = false`, commits & files match `git log` & `git diff` |
| Dirty working tree, allowDirty=false | Any change | Throws or returns error |
| Dirty working tree, allowDirty=true | Any change | `IsDirty = true`, ChangeSet returned |
| No commits between refs | Any repo | `CommitCount = 0`, empty Commits & Files |
| One commit | Any repo | `CommitCount = 1`, commits list populated correctly |
| Merge commits in range | Merge present | All merge commits included |
| File renames | File renamed | Appears in `Files` list (normalized) |
| File deletions | File deleted | Appears in `Files` list |
| Binary files | Commit changes binary | Appears in `Files` list |
| Untracked files | Working tree contains untracked files | Only included if dirty check allows it (optional) |

## 3. Renderer / Output

| Test Case | Input | Expected Behavior |
|-----------|-------|-----------------|
| Text output | ChangeSet with multiple commits & files | Human-readable summary matches expected format |
| JSON output | ChangeSet with multiple commits & files | Valid JSON, full data included |
| Empty ChangeSet | No commits | Text renderer shows “No changes”, JSON shows empty arrays |
| Dirty working tree | IsDirty = true | Renderer indicates dirty state |
| Future formats | e.g., Markdown (for v2) | Correct rendering of commits/files |

## 4. CLI Integration

| Test Case | Command | Expected Behavior |
|-----------|--------|-----------------|
| Default run | `minch` | Uses last-tag → HEAD, default output text |
| Explicit refs | `minch --from main --to HEAD` | Correct ChangeSet returned & rendered |
| Output formats | `--output text` / `--output json` | Correct renderer used, output matches format |
| Allow dirty flag | `--allow-dirty` | ChangeSet returned even if repo is dirty |
| Missing refs | Invalid `--from` or `--to` | CLI exits >1 with meaningful error |
| Combined flags | `--files --commits` | Output matches requested subset |
| Exit codes | No changes | Exit code = 1 |
| Exit codes | Changes detected | Exit code = 0 |
| Exit codes | Error | Exit code >1 |

## 5. Complex repository scenarios

- Detached HEAD at a tag → ensure `from`/`to` works
- Merge commits + multiple authors → commit counting accurate
- Branch divergence → commits between refs include only relevant commits
- Tag not on current branch → resolved correctly (or error gracefully)
- Repos with unusual commit messages → special characters, Unicode

## 6. Synthetic Repository Test Plan

1. **Minimal repo**: 1 commit, 1 file
2. **Branch/tag repo**: multiple commits, multiple branches, tags
3. **Merge repo**: branch with merges, multiple authors
4. **Dirty repo**: staged + unstaged changes, untracked files
5. **Rename/delete repo**: files renamed/deleted across commits
6. **Detached HEAD / shallow clone**: simulate CI clone edge cases

- Script repo creation, commit sequences, tag creation, and working tree changes for reproducible tests.

## 7. Property-based tests (optional but strong)

- Random commits + file changes
- Random tag/branch names (including unusual characters)
- Assert properties:
  - `CommitCount` matches `git rev-list`
  - `Files` match `git diff --name-only`
  - `IsDirty` matches `git status --porcelain`
- Ensures coverage of unexpected edge cases

---

**Summary:**

- Cover **ref resolution, change detection, rendering, CLI behavior, exit codes**
- Test **edge Git states**: dirty trees, detached HEADs, merges, renames, deletions
- Use **synthetic repos** for reproducible tests
- Optionally add **property-based tests** for random scenarios
- Keep **regression tests** with a few real-world repos for Unicode/odd tag names

