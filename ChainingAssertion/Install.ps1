param($installPath, $toolsPath, $package, $project)

function log ($text) {
    Invoke-WebRequest -Method Post -Uri "http://localhost:4000" -Body $text > $null
}

# $project.Object.References | % {
#     log (ConvertTo-Json $_.Name)
#     if ($_.Name -eq "ClassLibrary1") {
#         log "remove classLibrary1 !"
#         $_.Remove()
#     }
# }

$project.Save()
