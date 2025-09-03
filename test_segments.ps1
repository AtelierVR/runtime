# Test simple des segments DASH
$epochStart = Get-Date "1970-01-01 00:00:00Z"
$now = (Get-Date).ToUniversalTime()
$elapsedSeconds = ($now - $epochStart).TotalSeconds
$segmentDuration = 8
$currentSegment = [Math]::Floor($elapsedSeconds / $segmentDuration)

Write-Host "Segment actuel calculé: $currentSegment"
Write-Host "Test de segments sans Range header..."

# Tester quelques segments récents
$testSegments = @($currentSegment-1, $currentSegment-2, $currentSegment-5, $currentSegment-10)

foreach ($segment in $testSegments) {
    $testUrl = "https://livesim.dashif.org/livesim/chunkdur_1/ato_7/testpic4_8s/V1200/$segment.m4s"
    try {
        # Simple requête GET
        $testResponse = Invoke-WebRequest -Uri $testUrl -TimeoutSec 3 -ErrorAction Stop
        Write-Host "Segment $segment : DISPONIBLE (Code: $($testResponse.StatusCode), Taille: $($testResponse.Content.Length) bytes)"
        break  # Arrêter dès qu'on trouve un segment valide
    } catch {
        $statusCode = "N/A"
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode
        }
        Write-Host "Segment $segment : NON DISPONIBLE (Code: $statusCode)"
    }
}
