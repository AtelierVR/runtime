# Test avec /livesim2/ comme VLC
$now = (Get-Date).ToUniversalTime()
$epochStart = Get-Date "1970-01-01 00:00:00Z"
$elapsedSeconds = ($now - $epochStart).TotalSeconds
$segmentDuration = 8
$currentSegment = [Math]::Floor($elapsedSeconds / $segmentDuration)

Write-Host "Test avec /livesim2/ comme VLC..."
Write-Host "Segment actuel calculé: $currentSegment"

# Utiliser la même plage que VLC (219597442-219597446)
$vlcSegments = @(219597447, 219597448, 219597449, 219597450)  # Segments plus récents

foreach ($segment in $vlcSegments) {
    # Tester avec /livesim2/ comme VLC
    $testUrl = "https://livesim.dashif.org/livesim2/chunkdur_1/ato_7/testpic4_8s/V1200/$segment.m4s"
    try {
        $response = Invoke-WebRequest -Uri $testUrl -TimeoutSec 3 -ErrorAction Stop
        Write-Host "✓ Segment $segment DISPONIBLE avec livesim2 (Code: $($response.StatusCode), Taille: $($response.Content.Length) bytes)"
        break
    } catch {
        Write-Host "✗ Segment $segment non disponible avec livesim2: $($_.Exception.Message)"
    }
}
