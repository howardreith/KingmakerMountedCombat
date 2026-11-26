# The runtime loader profile a native check is judged against. The default is the measured owner installation
# (UMM 0.33.0.0 with its Harmony 2.3.6 shared-state creator); another reviewed profile is selected explicitly and its
# exact UnityModManager and Harmony 2 identities come from planning/UMM-LOADER-PROFILES.json ("runtime"). A profile
# without a runtime record is not an admitted runtime target. Historical records stay bound to their own profiles.
Set-StrictMode -Version Latest
$script:KmcDefaultRuntimeLoaderProfileId = 'umm-0.33.0'

# The profile a guarded native run is admitted for: the selected (or default owner) profile must be the measured
# environment's installed loader (planning/ENVIRONMENT-FINGERPRINT.json umm role), otherwise the run is refused.
function Resolve-KmcRuntimeLoaderProfileForEnvironment([string]$ProfileId, $Fingerprint) {
    $selected = if ([string]::IsNullOrWhiteSpace($ProfileId)) { Get-KmcRuntimeLoaderProfile } else { Get-KmcRuntimeLoaderProfile $ProfileId }
    $installed = @($Fingerprint.kingmaker.files | Where-Object role -eq 'umm')
    if ($installed.Count -ne 1 -or [string]$installed[0].sha256 -cne $selected.UmmSha256 -or [string]$installed[0].mvid -cne $selected.UmmMvid -or
        [string]$installed[0].identity -cne $selected.UmmFullName) {
        throw "Runtime loader profile $($selected.Id) is not the measured environment's installed loader; a native run needs a guarded environment with that loader."
    }
    return $selected
}

function Get-KmcRuntimeLoaderProfile([string]$Id = $script:KmcDefaultRuntimeLoaderProfileId) {
    $path = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\planning\UMM-LOADER-PROFILES.json'))
    $document = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    $selected = @($document.profiles | Where-Object { $_.id -is [string] -and $_.id -ceq $Id })
    if ($selected.Count -ne 1) { throw "Unknown runtime loader profile: $Id" }
    $loaderRecord = $selected[0]
    if (-not $loaderRecord.PSObject.Properties['runtime'] -or $null -eq $loaderRecord.runtime) { throw "Loader profile $Id has no runtime identity; it is not an admitted runtime target." }
    $runtime = $loaderRecord.runtime
    foreach ($part in @('umm', 'harmony2')) {
        foreach ($field in @('fullName', 'mvid', 'sha256')) {
            if (-not $runtime.$part.PSObject.Properties[$field] -or -not ($runtime.$part.$field -is [string])) { throw "Loader profile $Id runtime $part.$field is missing." }
        }
        if ($runtime.$part.sha256 -cnotmatch '\A[0-9a-f]{64}\z') { throw "Loader profile $Id runtime $part SHA-256 is malformed." }
    }
    if ($runtime.umm.sha256 -cne [string]$loaderRecord.sha256 -or $runtime.umm.mvid -cne [string]$loaderRecord.mvid -or
        $runtime.umm.fullName -cne ('UnityModManager, Version=' + [string]$loaderRecord.version + ', Culture=neutral, PublicKeyToken=null')) {
        throw "Loader profile $Id runtime UnityModManager identity disagrees with the profile."
    }
    return [pscustomobject]@{
        Id = $Id
        UmmVersion = [string]$loaderRecord.version
        UmmFullName = [string]$runtime.umm.fullName
        UmmMvid = [string]$runtime.umm.mvid
        UmmSha256 = [string]$runtime.umm.sha256
        Harmony2FullName = [string]$runtime.harmony2.fullName
        Harmony2Mvid = [string]$runtime.harmony2.mvid
        Harmony2Sha256 = [string]$runtime.harmony2.sha256
    }
}
