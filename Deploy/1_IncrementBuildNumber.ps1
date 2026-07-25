. "$PSScriptRoot\Config.ps1"

# Snake.Maui is a modern MAUI SingleProject app: it has no separate MajorVersion/MinorVersion
# nodes or a hand-maintained android:versionCode like an older-style project might - Android's
# versionCode/versionName are derived automatically at build time from ApplicationVersion and
# ApplicationDisplayVersion in the .csproj, so those are the only two values to bump.
#
# Edits the file as plain text (not via [xml]/.Save()) so the rest of the .csproj - tab
# indentation, comment formatting - is left byte-for-byte untouched; loading through
# System.Xml.XmlDocument would silently reformat the whole file (tabs to spaces) on every run.
function Update-CsprojVersion
{
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $content = [System.IO.File]::ReadAllText($CsprojFull, $utf8NoBom)

    $versionMatch = [regex]::Match($content, '<ApplicationVersion>(\d+)</ApplicationVersion>')
    $displayMatch = [regex]::Match($content, '<ApplicationDisplayVersion>([\d.]+)</ApplicationDisplayVersion>')

    if (-not $versionMatch.Success -or -not $displayMatch.Success)
    {
        Write-Host "Error: ApplicationVersion or ApplicationDisplayVersion not found in the .csproj file."
        return
    }

    try
    {
        $currentVersion = $versionMatch.Groups[1].Value
        $currentDisplay = $displayMatch.Groups[1].Value
        Write-Host "Current ApplicationVersion: $currentVersion"
        Write-Host "Current ApplicationDisplayVersion: $currentDisplay"

        $newVersion = [Convert]::ToInt32($currentVersion) + 1

        $displayParts = $currentDisplay -split '\.'
        $displayParts[-1] = [string]([Convert]::ToInt32($displayParts[-1]) + 1)
        $newDisplay = $displayParts -join '.'

        $content = $content.Replace("<ApplicationVersion>$currentVersion</ApplicationVersion>", "<ApplicationVersion>$newVersion</ApplicationVersion>")
        $content = $content.Replace("<ApplicationDisplayVersion>$currentDisplay</ApplicationDisplayVersion>", "<ApplicationDisplayVersion>$newDisplay</ApplicationDisplayVersion>")

        [System.IO.File]::WriteAllText($CsprojFull, $content, $utf8NoBom)

        Write-Host "ApplicationVersion updated to $newVersion, ApplicationDisplayVersion updated to $newDisplay in .csproj file."
    }
    catch
    {
        Write-Host "Error updating .csproj version: $_"
    }
}

Update-CsprojVersion
