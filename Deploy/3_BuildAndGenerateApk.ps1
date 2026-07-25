param(
    # apk = directly installable/sideloadable; aab = Play Store submission format.
    [ValidateSet("apk", "aab")]
    [string]$Format = "apk"
)

. "$PSScriptRoot\Config.ps1"

Write-Host "Cleaning..."
if (Test-Path $PublishOutputDirectory)
{
    Remove-Item -Path "$PublishOutputDirectory\*" -Recurse -Force -ErrorAction SilentlyContinue
}

Set-Location $SolutionRoot
Write-Host "Publishing $CsprojFull ($Framework, $Format)..."

dotnet publish $CsprojFull -c Release -f $Framework "/p:AndroidPackageFormat=$Format"

if (-not (Test-Path -Path $PublishOutputDirectory))
{
    throw "Publish output directory not found: $PublishOutputDirectory"
}

Write-Host "Searching generated package..."
$searchPattern = if ($Format -eq "aab") { "*-Signed.aab" } else { "*-Signed.apk" }
$packagePath = Get-ChildItem -Path (Join-Path $PublishOutputDirectory $searchPattern) -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty FullName -First 1

if (-not $packagePath)
{
    throw "No signed $Format found in $PublishOutputDirectory"
}

Write-Host "Package generated at: $packagePath"
return $packagePath
