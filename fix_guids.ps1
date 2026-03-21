$mapping = @{
    "Client.cs"                        = "src/Client.cs"
    "Editor.cs"                        = "src/Editor.cs"
    "Main.cs"                          = "src/Main.cs"
    "audio/AudioPlay.cs"               = "src/audio/AudioPlay.cs"
    "colors/ButtonThemeSetter.cs"      = "src/colors/ButtonThemeSetter.cs"
    "colors/ImageThemeSetter.cs"       = "src/colors/ImageThemeSetter.cs"
    "colors/Theme.cs"                  = "src/colors/Theme.cs"
    "colors/ThemeSetter.cs"            = "src/colors/ThemeSetter.cs"
    "defaults/DefaultPagesClient.cs"   = "src/defaults/DefaultPagesClient.cs"
    "defaults/ExamplePage.cs"          = "src/defaults/ExamplePage.cs"
    "defaults/HomePage.cs"             = "src/defaults/HomePage.cs"
    "elements/IKeyboardLayout.cs"      = "src/elements/IKeyboardLayout.cs"
    "elements/KeepSize.cs"             = "src/elements/KeepSize.cs"
    "elements/Keyboard.cs"             = "src/elements/Keyboard.cs"
    "elements/KeyboardKey.cs"          = "src/elements/KeyboardKey.cs"
    "elements/MenuGridder.cs"          = "src/elements/MenuGridder.cs"
    "elements/MenuGridderItem.cs"      = "src/elements/MenuGridderItem.cs"
    "histories/HistoryList.cs"         = "src/histories/HistoryList.cs"
    "layouts/NumericKeyboardLayout.cs" = "src/layouts/NumericKeyboardLayout.cs"
    "layouts/QwertyKeyboardLayout.cs"  = "src/layouts/QwertyKeyboardLayout.cs"
    "layouts/base/Element.cs"          = "src/layouts/base/Element.cs"
    "layouts/base/Orbiter.cs"          = "src/layouts/base/Orbiter.cs"
    "layouts/base/Part.cs"             = "src/layouts/base/Part.cs"
    "layouts/bottom/Applications.cs"   = "src/layouts/bottom/Applications.cs"
    "layouts/bottom/BottomOrbiter.cs"  = "src/layouts/bottom/BottomOrbiter.cs"
    "layouts/bottom/Favorites.cs"      = "src/layouts/bottom/Favorites.cs"
    "layouts/bottom/Specials.cs"       = "src/layouts/bottom/Specials.cs"
    "layouts/top/Actions.cs"           = "src/layouts/top/Actions.cs"
    "layouts/top/Histories.cs"         = "src/layouts/top/Histories.cs"
    "layouts/top/TopOrbiter.cs"        = "src/layouts/top/TopOrbiter.cs"
    "menus/ExternalAudioMenu.cs"       = "src/menus/ExternalAudioMenu.cs"
    "menus/Menu.cs"                    = "src/menus/Menu.cs"
    "menus/MenuManager.cs"             = "src/menus/MenuManager.cs"
    "modals/BaseModal.cs"              = "src/modals/BaseModal.cs"
    "modals/ModalBuilder.cs"           = "src/modals/ModalBuilder.cs"
    "pages/ActionPage.cs"              = "src/pages/ActionPage.cs"
    "pages/PageManager.cs"             = "src/pages/PageManager.cs"
}

$baseNew = "d:\Projets\nox\runtime\Packages\nox.ui\Runtime"
$baseOld = "Packages/api.nox.ui"
$commit = "a879a224"

foreach ($entry in $mapping.GetEnumerator()) {
    $newMetaPath = Join-Path $baseNew ($entry.Key + ".meta")
    $oldGitPath = "$baseOld/$($entry.Value).meta"

    $oldContent = git show "${commit}:${oldGitPath}" 2>$null
    if (-not $oldContent) { Write-Host "MISSING old: $oldGitPath" -ForegroundColor Red; continue }

    $oldGuidLine = $oldContent | Where-Object { $_ -match "^guid:" } | Select-Object -First 1
    if (-not $oldGuidLine) { Write-Host "NO GUID in: $oldGitPath" -ForegroundColor Red; continue }
    $oldGuid = ($oldGuidLine -split ": ")[1].Trim()

    if (-not (Test-Path $newMetaPath)) { Write-Host "MISSING new: $newMetaPath" -ForegroundColor Yellow; continue }

    $newContent = Get-Content $newMetaPath -Raw
    $newGuidMatch = [regex]::Match($newContent, "guid: ([a-f0-9]+)")
    if (-not $newGuidMatch.Success) { Write-Host "NO GUID in new: $newMetaPath" -ForegroundColor Red; continue }
    $newGuid = $newGuidMatch.Groups[1].Value

    if ($oldGuid -ne $newGuid) {
        $updated = $newContent -replace "guid: $newGuid", "guid: $oldGuid"
        Set-Content $newMetaPath $updated -NoNewline
        Write-Host "FIXED: $($entry.Key)  $newGuid -> $oldGuid" -ForegroundColor Green
    } else {
        Write-Host "OK: $($entry.Key)" -ForegroundColor Gray
    }
}
