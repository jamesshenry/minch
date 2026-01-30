# Breaking Change (feat!:): This triggers the jump to v2.0.0.
# Support Branch: We fork support/v1.x from the last stable v1 commit. This becomes a "Long-Term Support" (LTS) branch.
# Cross-Branch Hotfixing: A security vulnerability (fix:) is discovered. It must be applied to:
# main (the new v2 line).
# support/v1.x (the legacy line).
# develop (to ensure it's in the next feature release).

# --- Configuration ---
$BundleName = 'scenario-2-lts-support.bundle'
$OutputPath = Join-Path $PSScriptRoot $BundleName

# 1. Create a clean temporary workspace
$BuildPath = Join-Path $env:TEMP "git-build-$(New-Guid)"
New-Item -ItemType Directory -Path $BuildPath | Out-Null
Push-Location $BuildPath

try {
    # 2. Initialize Repository
    git init -b main
    git config user.name 'QA Automation'
    git config user.email 'qa@example.com'

    # --- Initial State (v1.0.1) ---
    'V1 Socket Logic' > network.ps1
    git add .
    git commit -m 'fix: socket-fix'
    git tag v1.0.1

    # --- Preparations for Major V2 ---
    git checkout -b develop
    'Breaking DB Changes' > schema.sql
    git add .
    git commit -m 'feat!: new db schema'
    
    'Cloud Logic' > cloud.ps1
    git add .
    git commit -m 'feat: cloud sync'

    # --- THE MAJOR RELEASE ---
    git checkout main
    git merge --no-ff develop -m 'chore: release v2'
    git tag v2.0.0

    # --- CREATE SUPPORT BRANCH (Forked from v1.0.1) ---
    git checkout v1.0.1
    git checkout -b support/v1.x
    'V1 Support Policy' > support.txt
    git add .
    git commit -m 'docs: update v1 support policy'

    # --- FEATURE WORK CONTINUES ON V2 ---
    git checkout develop
    'Dark Mode Styles' > styles.css
    git add .
    git commit -m 'feat: dark mode'

    # --- EMERGENCY: Security bug affecting ALL versions ---
    # We branch off main (v2.0.0) to create the patch
    git checkout main
    git checkout -b hotfix/cve-patch
    'Security Hardening' >> network.ps1
    git add .
    git commit -m 'fix: encrypt local logs'

    # 1. Patch the current version (V2)
    git checkout main
    git merge --no-ff hotfix/cve-patch -m 'chore: v2 patch'
    git tag v2.0.1

    # 2. Patch the legacy version (V1)
    git checkout support/v1.x
    git merge --no-ff hotfix/cve-patch -m 'chore: v1 patch'
    # Tagging as v1.1.1 per graph requirement
    git tag v1.1.1

    # 3. Sync into development to prevent regression
    git checkout develop
    git merge --no-ff main -m 'chore: sync security fix'

    # 3. Create the Bundle
    if (Test-Path $OutputPath) { Remove-Item $OutputPath }
    git bundle create $OutputPath --all

    Write-Host "`nSuccess: Bundle generated at $OutputPath" -ForegroundColor Green
}
catch {
    Write-Error "An error occurred: $_"
}
finally {
    # 4. Cleanup
    Pop-Location
    Remove-Item -Recurse -Force $BuildPath
}
