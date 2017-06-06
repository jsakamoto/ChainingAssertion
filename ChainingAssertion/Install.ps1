param($installPath, $toolsPath, $package, $project)

$refNames = $project.Object.References | % { $_.Name }
$noVSQualityTools = -not ($refNames -contains "Microsoft.VisualStudio.QualityTools.UnitTestFramework")
$noVSTestPlatform = -not ($refNames -contains "Microsoft.VisualStudio.TestPlatform.TestFramework")

$project.Object.References | % {
    if ($noVSQualityTools -and $_.Name -eq "ChainingAssertion") {
        $_.Remove()
    }
    if ($noVSTestPlatform -and $_.Name -eq "ChainingAssertion.MSTest") {
        $_.Remove()
    }
}

$project.Save()
