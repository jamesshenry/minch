# Scenario 3: Stabilization via Revert
# ---------------------------------------------------------
# 1. feat: pdf-export and feat: ai-engine are merged into develop.
# 2. release/v2.1.0 is cut.
# 3. Decision: AI is too unstable for v2.1.
# 4. ACTION: 'git revert' is used on the release branch.
# 5. CLI Handling: CLI ignores 'revert' + original 'feat' for a clean log.
# 6. Sync: The revert is merged back to develop (requiring a 'revert-of-revert' later).

$BundleName = 'scenario-3.bundle'
$OutputPath = Join-Path $PSScriptRoot $BundleName

$BuildPath = Join-Path $env:TEMP "git-build-$(New-Guid)"
New-Item -ItemType Directory -Path $BuildPath | Out-Null
Push-Location $BuildPath

try {
    git init -b main
    git config user.name 'QA Automation'
    git config user.email 'qa@example.com'

    # --- Start v2.1 Cycle ---
    'Initial v2.1 setup' > init.txt
    git add .
    git commit -m 'chore: start v2.1 cycle'

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
    git checkout -b release/v2.1.0

    # --- THE REVERT (The "Standard" Way) ---
    # We revert the AI feature specifically on this branch.
    # We find the commit hash of the AI feature to revert it.
    $AiHash = git rev-parse HEAD^2 # Reverts the second parent of the last merge (the ai branch)
    git revert $AiHash --no-edit   # Creates "revert: feat: ai integration"

    # --- Tagging Release Candidate ---
    git tag v2.1.0-rc.1

    # --- Finalizing the Release ---
    git checkout main
    git merge --no-ff release/v2.1.0 -m 'chore: v2.1.0 ship'
    git tag v2.1.0

    # --- Sync Back to Develop ---
    # WARNING: This removes the AI code from develop! 
    # AI devs will need to 'revert the revert' to continue.
    git checkout develop
    git merge --no-ff release/v2.1.0 -m 'chore: sync release fixes'

    # Create the Bundle
    if (Test-Path $OutputPath) { Remove-Item $OutputPath }
    git bundle create $OutputPath --all

    Write-Host "`nSuccess: Bundle generated at $OutputPath" -ForegroundColor Green
    Write-Host "Changelog Logic: Your CLI should now see both the 'feat' and 'revert' and can omit both." -ForegroundColor Cyan
}
catch { Write-Error "An error occurred: $_" }
finally { Pop-Location; Remove-Item -Recurse -Force $BuildPath }
