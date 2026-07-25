# ==== GLOBAL CONFIG FOR DEPLOY SCRIPTS ====

# Base folders
$Solution = "Snake"
$SolutionRoot = "J:\Work\$Solution"
$Project = "Snake.Maui"
$ProjectAssembly = "SnakeGameEngine.Maui"

# Project paths
$CsprojFull = Join-Path $SolutionRoot "$Project\$ProjectAssembly.csproj"

# Build paths
$Framework = "net10.0-android"
$PublishOutputDirectory = Join-Path $SolutionRoot "$Project\bin\Release\$Framework\publish"

# Git
$GitBranch = "main"
