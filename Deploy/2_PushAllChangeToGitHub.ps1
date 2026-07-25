. "$PSScriptRoot\Config.ps1"

Set-Location $SolutionRoot

$xml = [xml](Get-Content $CsprojFull)
$propertyGroup = $xml.Project.PropertyGroup

git add .
git commit -m "Deployed version: $($propertyGroup.ApplicationDisplayVersion) (build $($propertyGroup.ApplicationVersion))"
git push origin $GitBranch
