# Polluted Develop: Both feat: pdf-export and feat: ai-engine are merged.
# The Release Branch (Stabilization): We cut release/v2.1.0.
# The "Kill Switch" Commit: A fix: commit is added to the release branch to disable the unstable AI code (Feature Toggle).
# RC Loop: Testing the "Disabled" state.
# Clean Changelog: How Conventional Commits handle the "hidden" feature.

# --- Configuration ---
$BundleName = 'scenario-3-selective-release.bundle'
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

    # --- Start v2.1 Cycle ---
    'Initial v2.1 setup' > init.txt
    git add .
    git commit -m 'chore: start v2.1 cycle'

    # --- Setup Develop Branch ---
    git checkout -b develop

    # --- Feature 1: PDF Export (Stable) ---
    git checkout -b feat/pdf
    'PDF Rendering Logic' > pdf-engine.ps1
    git add .
    git commit -m 'feat: pdf export engine'
    
    git checkout develop
    git merge --no-ff feat/pdf -m 'chore: merge pdf'

    # --- Feature 2: AI Integration (Unstable) ---
    git checkout -b feat/ai
    'AI Models and Logic' > ai-core.ps1
    git add .
    git commit -m 'feat: ai integration'
    
    git checkout develop
    git merge --no-ff feat/ai -m 'chore: merge ai'

    # --- Create Release Branch ---
    # At this point develop contains both PDF and AI
    git checkout -b release/v2.1.0

    # --- The Kill Switch ---
    # A fix specifically for the release branch to hide unstable features
    'EnableAI = false' > config.ini
    git add .
    git commit -m 'fix: disable ai-ui for v2.1 stable'

    # --- Tagging Release Candidate ---
    git tag v2.1.0-rc.1

    # --- Finalizing the Release ---
    git checkout main
    git merge --no-ff release/v2.1.0 -m 'chore: v2.1.0 ship'
    git tag v2.1.0

    # --- Sync Back to Develop ---
    git checkout develop
    git merge --no-ff release/v2.1.0 -m 'chore: sync release fixes'

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
