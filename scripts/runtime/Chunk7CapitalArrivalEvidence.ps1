Set-StrictMode -Version Latest
# Matrix rows 58-59 (owner decision after b195 x4c). In a native capital only the main character and
# the main character's pet are directly controllable, so KMC retires (dismounts) a pair refused only by
# that rule and keeps the controllable pairs. Row 58 cold-loads a two-owner mounted Manual, makes one
# transfer into the reviewed capital pin and writes one native Manual there; row 59 cold-loads that
# Manual in a separate process. This is the only route admitted into a capital: every other case keeps
# Assert-KmcChunk7RouteAvoidsKnownCapitals and the in-game IsCapital refusal.
function Test-KmcChunk7CapitalArrivalCase([string]$Case){$Case-ceq'multi-exploration-capital-arrival-manual'}
function Test-KmcChunk7CapitalColdCase([string]$Case){$Case-ceq'multi-exploration-capital-arrival-cold-manual'}
function Test-KmcChunk7CapitalCase([string]$Case){(Test-KmcChunk7CapitalArrivalCase $Case)-or(Test-KmcChunk7CapitalColdCase $Case)}
function Get-KmcChunk7CapitalArrivalManualName {'KMC_C7_CAPITAL'}
# The single reviewed capital pin, from the same native observation the avoidance guard uses.
function Get-KmcChunk7CapitalArrivalPin {
    $pins=@(Get-KmcChunk7KnownCapitalAreaPins)
    if($pins.Count-ne1){throw 'Capital arrival requires exactly one reviewed capital pin.'}
    $pins[0]
}
function Get-KmcChunk7CapitalArrivalArea {[string](Get-KmcChunk7CapitalArrivalPin).area}
# Runner-side scoped admission: row 58 must name exactly the reviewed capital entry and area, once each.
function Assert-KmcChunk7CapitalArrivalRoute($EnterPoints,$Areas) {
    $pin=Get-KmcChunk7CapitalArrivalPin
    $enter=@($EnterPoints);$area=@($Areas)
    if($enter.Count-ne1-or$area.Count-ne1-or$enter[0]-isnot[string]-or$area[0]-isnot[string]-or
       $enter[0]-cne$pin.enterPoint-or$area[0]-cne$pin.area){throw 'Capital arrival admits only the reviewed capital entry point and area.'}
}
# Request-side scoped admission: the producer declares exactly that pin with no native autosave; the
# cold consumer declares no area transfer at all.
function Assert-KmcChunk7CapitalArrivalTarget($Request) {
    if($Request-is[Collections.IDictionary]){$Request=[pscustomobject]$Request}
    $case=if($null-ne$Request.PSObject.Properties['persistenceCase']){$Request.persistenceCase}else{$null}
    $has=$null-ne$Request.PSObject.Properties['persistenceAreaTarget']-and$null-ne$Request.persistenceAreaTarget
    if(Test-KmcChunk7CapitalArrivalCase $case){
        if(-not$has){throw 'Capital arrival requires its exact declared capital target.'}
        $t=$Request.persistenceAreaTarget;Assert-KmcExactProperties $t @('enterPoint','area','autoSaveMode') 'capital arrival target'
        Assert-KmcChunk7CapitalArrivalRoute @($t.enterPoint) @($t.area)
        if($t.autoSaveMode-isnot[string]-or$t.autoSaveMode-cne'None'){throw 'Capital arrival forbids a native transition autosave.'}
        return
    }
    if((Test-KmcChunk7CapitalColdCase $case)-and$has){throw 'The capital cold consumer declares no area transfer.'}
}
