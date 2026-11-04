# Selected F1 cumulative launch cap. Called before admission; failed attempts count.
function Assert-KmcFinalizeF1LaunchAllowance($Record, [bool]$WhatIfOnly) {
    if ([string]$Record.budgetId -cne 'finalize-f1' -or $WhatIfOnly) { return }
    $launches=@($Record.entries | Where-Object { $_.launch -eq $true }).Count
    if ($launches -ge 3) { throw 'F1 has used its maximum three real launch attempts; no new launch is authorized.' }
}
