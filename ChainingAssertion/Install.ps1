param($installPath, $toolsPath, $package, $project)

function log ($text) {
    Invoke-WebRequest -Method Post -Uri "http://localhost:4000" -Body $text > $null
}

$refNames = $project.Object.References | % { $_.Name }
$noVSQualityTools = -not ($refNames -contains "Microsoft.VisualStudio.QualityTools.UnitTestFramework")
$noVSTestPlatform = -not ($refNames -contains "Microsoft.VisualStudio.TestPlatform.TestFramework")
log "noVSQualityTools is $noVSQualityTools"
log "noVSTestPlatform is $noVSTestPlatform"

$project.Object.References | % {
    log (ConvertTo-Json $_.Name)
    if ($noVSQualityTools -and $_.Name -eq "ChainingAssertion") {
        log "-- remove ChainingAssertion.dll --"
        $_.Remove()
    }
    if ($noVSTestPlatform -and $_.Name -eq "ChainingAssertion.MSTest") {
        log "-- remove ChainingAssertion.MSTest.dll --"
        $_.Remove()
    }
}

$project.Save()
