# V1.x is in Long Term Support (LTS).
# V2.x is the current Stable.
# A Security Vulnerability is found. You must patch both versions.
# The V2 patch causes a regression (a "Day 0" bug), requiring a second immediate patch.
# All of this happens while Development has a breaking change sitting in it.

# Create a temp directory to build the history
$BundleName = 'scenario-4-enterprise-crisis.bundle'
$OutputPath = Join-Path $PSScriptRoot $BundleName

# 1. Create a clean temporary workspace
$buildPath = Join-Path $env:TEMP "git-build-$(New-Guid)"
New-Item -ItemType Directory -Path $buildPath
Push-Location $buildPath

try {
    # 2. Initialize Repository
    git init -b main
    git config user.name 'QA Bot'
    git config user.email 'qa@example.com'

    # V1 Baseline
    'v1 code' > app.ps1
    git add . ; git commit -m 'feat: initial legacy engine' ; git tag v1.0.0

    # Create LTS Branch
    git checkout -b support/v1.x
    'lts docs' > docs.md
    git add . ; git commit -m 'chore: establish lts policy'

    # V2 Major Release
    git checkout main
    git checkout -b develop
    'v2 overhaul' > app.ps1
    git add . ; git commit -m 'feat!: major overhaul for v2'
    git checkout main
    git merge --no-ff develop -m 'chore: release v2.0.0' ; git tag v2.0.0

    # Future Work
    git checkout develop
    'plugin system' >> plugins.ps1
    git add . ; git commit -m 'feat: add new plugin system'

    # The Security Hotfix (Branch from the last stable tag)
    git checkout v2.0.0
    git checkout -b hotfix/cve-2024
    'security fix' >> app.ps1
    git add . ; git commit -m 'fix: sanitize user input (CVE-2024)'

    # Apply to V1 LTS
    git checkout support/v1.x
    git merge --no-ff hotfix/cve-2024 -m 'fix: backport security fix to v1' ; git tag v1.0.1

    # Apply to V2 Stable
    git checkout main
    git merge --no-ff hotfix/cve-2024 -m 'fix: security patch for v2' ; git tag v2.0.1

    # The Panic Fix (Regression in v2.0.1)
    git checkout main
    git checkout -b hotfix/ui-regress
    'ui fix' >> app.ps1
    git add . ; git commit -m 'fix: restore login button visibility'
    git checkout main
    git merge --no-ff hotfix/ui-regress -m 'fix: emergency ui recovery' ; git tag v2.0.2

    # Final Sync
    git checkout develop
    git merge --no-ff main -m 'chore: sync all emergency patches'

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
