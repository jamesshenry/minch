# main: Represents the currently "Installed" stable version.
# develop: The long-lived branch where all new features are integrated for the next major release cycle.
# hotfix/: Temporary branches for immediate production patches.
# Scenario 1: The SMB Stable Desktop Release Cycle
# In this iteration, we see a feature being prepared for v1.1.0, but a critical "Client Crash" requires a v1.0.1 hotfix. As per your requirement, we ensure the hotfix is synced into the development line before the final release.


# --- Configuration ---
$BundleName = 'scenario-1.bundle'
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

    # --- Initial Release ---
    'Version 1.0.0 Base' > app.txt
    git add .
    git commit -m 'chore: initial desktop release'
    git tag v1.0.0

    # --- Setup Develop Branch ---
    git checkout -b develop

    # --- Feature Development ---
    git checkout -b feat/client-dashboard
    'Cache Logic' > cache.ps1
    git add .
    git commit -m 'feat: add local sqlite cache'
    'UI Logic' > ui.ps1
    git add .
    git commit -m 'feat: dashboard ui'

    # Merge Feature back to Develop
    git checkout develop
    git merge --no-ff feat/client-dashboard -m 'chore: integrate dashboard'

    # --- Critical Hotfix on Main ---
    git checkout main
    git checkout -b hotfix/crash-fix
    'Socket Fix' >> app.txt
    git add .
    git commit -m 'fix: client socket timeout crash'

    # Merge Hotfix to Main and Tag
    git checkout main
    git merge --no-ff hotfix/crash-fix -m 'chore: patch release'
    git tag v1.0.1

    # --- Sync Requirement: Pull Main (Hotfix) into Develop ---
    git checkout develop
    git merge --no-ff main -m 'chore: sync hotfix v1.0.1 into develop'

    # --- Final Release 1.1.0 ---
    git checkout main
    git merge --no-ff develop -m 'chore: release v1.1.0 stable'
    git tag v1.1.0

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
