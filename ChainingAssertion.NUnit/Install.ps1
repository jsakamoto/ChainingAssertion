param($installPath, $toolsPath, $package, $project)

$nunitMajorVer = $project.Object.References | where { $_.Name -eq "nunit.framework" } | % { $_.MajorVersion }

$project.Object.References | % {
    if (($nunitMajorVer -ne 2) -and ($_.Name -eq "ChainingAssertion.NUnit")) {
        $_.Remove()
    }
    if (($nunitMajorVer -ne 3) -and ($_.Name -eq "ChainingAssertion.NUnit3")) {
        $_.Remove()
    }
}

$project.Save()
