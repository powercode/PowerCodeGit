param()

$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$repositoryRoot = Resolve-Path (Join-Path -Path $scriptDirectory -ChildPath "..")
$artifactsPath = Join-Path -Path $repositoryRoot -ChildPath "artifacts"

if (Test-Path -Path $artifactsPath)
{
    Write-Host "Removing artifacts folder: $artifactsPath"
    Remove-Item -Path $artifactsPath -Recurse -Force
}
else
{
    Write-Host "Artifacts folder does not exist: $artifactsPath"
}

Write-Host "Cleanup complete."