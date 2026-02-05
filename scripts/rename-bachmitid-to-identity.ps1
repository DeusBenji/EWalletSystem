# BachMitID to IdentityService Rename Script
# This script automates the renaming of BachMitID service to IdentityService

param(
    [string]$RootPath = "e:\Ny mappe (2)\EWalletSystem",
    [switch]$DryRun = $false
)

Write-Host "=== BachMitID → IdentityService Rename Script ===" -ForegroundColor Cyan
Write-Host "Root Path: $RootPath" -ForegroundColor Yellow
Write-Host "Dry Run: $DryRun" -ForegroundColor Yellow
Write-Host ""

# Step 1: Rename main directory
$oldServicePath = Join-Path $RootPath "src\Services\BachMitID"
$newServicePath = Join-Path $RootPath "src\Services\IdentityService"

if (Test-Path $oldServicePath) {
    Write-Host "[1/10] Renaming service directory..." -ForegroundColor Green
    if (-not $DryRun) {
        Rename-Item -Path $oldServicePath -NewName "IdentityService"
        Write-Host "  ✓ Renamed: BachMitID → IdentityService" -ForegroundColor Gray
    } else {
        Write-Host "  [DRY RUN] Would rename: $oldServicePath → $newServicePath" -ForegroundColor Gray
    }
} else {
    Write-Host "[1/10] Service directory not found: $oldServicePath" -ForegroundColor Red
    exit 1
}

# Step 2: Rename project directories
Write-Host "[2/10] Renaming project directories..." -ForegroundColor Green

$projectRenames = @{
    "BachMitID.Domain" = "IdentityService.Domain"
    "BachMitID.Application" = "IdentityService.Application"
    "BachMitID.Infrastructure" = "IdentityService.Infrastructure"
    "BachMitID.Api" = "IdentityService.API"
}

foreach ($old in $projectRenames.Keys) {
    $oldPath = Join-Path $newServicePath $old
    $newPath = Join-Path $newServicePath $projectRenames[$old]
    
    if (Test-Path $oldPath) {
        if (-not $DryRun) {
            Rename-Item -Path $oldPath -NewName $projectRenames[$old]
            Write-Host "  ✓ Renamed: $old → $($projectRenames[$old])" -ForegroundColor Gray
        } else {
            Write-Host "  [DRY RUN] Would rename: $old → $($projectRenames[$old])" -ForegroundColor Gray
        }
    }
}

# Step 3: Rename .csproj files
Write-Host "[3/10] Renaming .csproj files..." -ForegroundColor Green

$csprojRenames = @{
    "BachMitID.Domain.csproj" = "IdentityService.Domain.csproj"
    "BachMitID.Application.csproj" = "IdentityService.Application.csproj"
    "BachMitID.Infrastructure.csproj" = "IdentityService.Infrastructure.csproj"
    "BachMitID.Api.csproj" = "IdentityService.API.csproj"
}

foreach ($old in $csprojRenames.Keys) {
    $files = Get-ChildItem -Path $newServicePath -Filter $old -Recurse -File
    foreach ($file in $files) {
        $newName = $csprojRenames[$old]
        $newPath = Join-Path $file.DirectoryName $newName
        
        if (-not $DryRun) {
            Rename-Item -Path $file.FullName -NewName $newName
            Write-Host "  ✓ Renamed: $($file.Name) → $newName" -ForegroundColor Gray
        } else {
            Write-Host "  [DRY RUN] Would rename: $($file.FullName) → $newPath" -ForegroundColor Gray
        }
    }
}

# Step 4: Rename solution file
Write-Host "[4/10] Renaming solution file..." -ForegroundColor Green

$oldSln = Join-Path $newServicePath "BachMitID.sln"
$newSln = Join-Path $newServicePath "IdentityService.sln"

if (Test-Path $oldSln) {
    if (-not $DryRun) {
        Rename-Item -Path $oldSln -NewName "IdentityService.sln"
        Write-Host "  ✓ Renamed: BachMitID.sln → IdentityService.sln" -ForegroundColor Gray
    } else {
        Write-Host "  [DRY RUN] Would rename: $oldSln → $newSln" -ForegroundColor Gray
    }
}

# Step 5: Update file contents (namespaces, project references, etc.)
Write-Host "[5/10] Updating file contents..." -ForegroundColor Green

$filesToUpdate = Get-ChildItem -Path $newServicePath -Include *.cs,*.csproj,*.sln,*.json,Dockerfile -Recurse -File

$replacements = @{
    "BachMitID.Domain" = "IdentityService.Domain"
    "BachMitID.Application" = "IdentityService.Application"
    "BachMitID.Infrastructure" = "IdentityService.Infrastructure"
    "BachMitID.Api" = "IdentityService.API"
    "BachMitID" = "IdentityService"
    "bachmitid" = "identity-service"
}

foreach ($file in $filesToUpdate) {
    if (-not $DryRun) {
        $content = Get-Content $file.FullName -Raw
        $updated = $false
        
        foreach ($old in $replacements.Keys) {
            if ($content -match [regex]::Escape($old)) {
                $content = $content -replace [regex]::Escape($old), $replacements[$old]
                $updated = $true
            }
        }
        
        if ($updated) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            Write-Host "  ✓ Updated: $($file.Name)" -ForegroundColor Gray
        }
    } else {
        Write-Host "  [DRY RUN] Would update: $($file.FullName)" -ForegroundColor Gray
    }
}

# Step 6: Update docker-compose.yml
Write-Host "[6/10] Updating docker-compose.yml..." -ForegroundColor Green

$dockerComposePath = Join-Path $RootPath "docker-compose.yml"

if (Test-Path $dockerComposePath) {
    if (-not $DryRun) {
        $content = Get-Content $dockerComposePath -Raw
        $content = $content -replace 'bachmitid:', 'identity-service:'
        $content = $content -replace 'container_name: bachmitid', 'container_name: identity-service'
        $content = $content -replace './src/Services/BachMitID/BachMitID.Api', './src/Services/IdentityService/IdentityService.API'
        Set-Content -Path $dockerComposePath -Value $content -NoNewline
        Write-Host "  ✓ Updated docker-compose.yml" -ForegroundColor Gray
    } else {
        Write-Host "  [DRY RUN] Would update: $dockerComposePath" -ForegroundColor Gray
    }
}

# Step 7: Update API Gateway configuration
Write-Host "[7/10] Updating API Gateway configuration..." -ForegroundColor Green

$ocelotPath = Join-Path $RootPath "src\ApiGateway\ocelot.json"

if (Test-Path $ocelotPath) {
    if (-not $DryRun) {
        $content = Get-Content $ocelotPath -Raw
        $content = $content -replace '"Host": "bachmitid"', '"Host": "identity-service"'
        $content = $content -replace '"/mitid/', '"/identity/'
        Set-Content -Path $ocelotPath -Value $content -NoNewline
        Write-Host "  ✓ Updated ocelot.json" -ForegroundColor Gray
    } else {
        Write-Host "  [DRY RUN] Would update: $ocelotPath" -ForegroundColor Gray
    }
}

# Step 8: Update test projects
Write-Host "[8/10] Updating test projects..." -ForegroundColor Green

$testPath = Join-Path $RootPath "test"
if (Test-Path $testPath) {
    $testFiles = Get-ChildItem -Path $testPath -Include *.cs,*.csproj -Recurse -File
    
    foreach ($file in $testFiles) {
        if (-not $DryRun) {
            $content = Get-Content $file.FullName -Raw
            if ($content -match "BachMitID") {
                $content = $content -replace "BachMitID", "IdentityService"
                Set-Content -Path $file.FullName -Value $content -NoNewline
                Write-Host "  ✓ Updated: $($file.Name)" -ForegroundColor Gray
            }
        } else {
            Write-Host "  [DRY RUN] Would update: $($file.FullName)" -ForegroundColor Gray
        }
    }
}

# Step 9: Update cross-service references
Write-Host "[9/10] Updating cross-service references..." -ForegroundColor Green

$servicesToUpdate = @(
    "AccountService",
    "TokenService",
    "ValidationService"
)

foreach ($service in $servicesToUpdate) {
    $servicePath = Join-Path $RootPath "src\Services\$service"
    if (Test-Path $servicePath) {
        $files = Get-ChildItem -Path $servicePath -Include *.cs,*.json -Recurse -File
        
        foreach ($file in $files) {
            if (-not $DryRun) {
                $content = Get-Content $file.FullName -Raw
                if ($content -match "bachmitid" -or $content -match "BachMitID") {
                    $content = $content -replace "bachmitid", "identity-service"
                    $content = $content -replace "BachMitID", "IdentityService"
                    Set-Content -Path $file.FullName -Value $content -NoNewline
                    Write-Host "  ✓ Updated: $service\$($file.Name)" -ForegroundColor Gray
                }
            }
        }
    }
}

# Step 10: Summary
Write-Host ""
Write-Host "[10/10] Summary" -ForegroundColor Green
Write-Host "  ✓ Service directory renamed" -ForegroundColor Gray
Write-Host "  ✓ Project directories renamed" -ForegroundColor Gray
Write-Host "  ✓ .csproj files renamed" -ForegroundColor Gray
Write-Host "  ✓ Solution file renamed" -ForegroundColor Gray
Write-Host "  ✓ File contents updated" -ForegroundColor Gray
Write-Host "  ✓ Docker configuration updated" -ForegroundColor Gray
Write-Host "  ✓ API Gateway configuration updated" -ForegroundColor Gray
Write-Host "  ✓ Test projects updated" -ForegroundColor Gray
Write-Host "  ✓ Cross-service references updated" -ForegroundColor Gray
Write-Host ""

if ($DryRun) {
    Write-Host "=== DRY RUN COMPLETE ===" -ForegroundColor Yellow
    Write-Host "Run without -DryRun to apply changes" -ForegroundColor Yellow
} else {
    Write-Host "=== RENAME COMPLETE ===" -ForegroundColor Green
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Build solution: dotnet build" -ForegroundColor Gray
    Write-Host "  2. Run tests: dotnet test" -ForegroundColor Gray
    Write-Host "  3. Update documentation" -ForegroundColor Gray
    Write-Host "  4. Commit changes: git add . && git commit -m 'Rename BachMitID to IdentityService'" -ForegroundColor Gray
}
