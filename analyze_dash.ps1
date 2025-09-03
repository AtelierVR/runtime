# Script pour analyser le flux DASH comme VLC
$response = Invoke-WebRequest -Uri 'https://livesim.dashif.org/livesim/chunkdur_1/ato_7/testpic4_8s/Manifest.mpd' -UseBasicParsing

Write-Host "=== ANALYSE DU FLUX DASH ==="
Write-Host "Heure UTC actuelle: $((Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss'))"
Write-Host ""

# Extraire les informations temporelles du manifest
$content = $response.Content
Write-Host "Contenu du manifest (extrait):"
if ($content -match 'availabilityStartTime="([^"]*)"') {
    Write-Host "availabilityStartTime: $($matches[1])"
}
if ($content -match 'publishTime="([^"]*)"') {
    Write-Host "publishTime: $($matches[1])"
}

# Calculer le segment actuel basé sur l'heure
$epochStart = Get-Date "1970-01-01 00:00:00Z"
$now = (Get-Date).ToUniversalTime()
$elapsedSeconds = ($now - $epochStart).TotalSeconds
$segmentDuration = 8  # 8 secondes par segment
$currentSegment = [Math]::Floor($elapsedSeconds / $segmentDuration)

Write-Host ""
Write-Host "=== CALCULS DE SEGMENTS ==="
Write-Host "Secondes écoulées depuis l'époque: $elapsedSeconds"
Write-Host "Segment actuel calculé: $currentSegment"
Write-Host "Estimation VLC (487994 heures): $([Math]::Floor(487994 * 3600 / 8))"

# Tester quelques segments autour de la valeur calculée
Write-Host ""
Write-Host "=== TEST DE SEGMENTS (GET avec Range) ==="

# Calculer manuellement les segments à tester
$seg1 = $currentSegment
$seg2 = $currentSegment - 1
$seg3 = $currentSegment - 2
$seg4 = $currentSegment - 5
$seg5 = $currentSegment - 10

$testSegments = @($seg1, $seg2, $seg3, $seg4, $seg5)

foreach ($segment in $testSegments) {
    $testUrl = "https://livesim.dashif.org/livesim/chunkdur_1/ato_7/testpic4_8s/V1200/$segment.m4s"
    try {
        # Utiliser GET avec Range header pour tester comme VLC
        $headers = @{ "Range" = "bytes=0-1023" }
        $testResponse = Invoke-WebRequest -Uri $testUrl -Headers $headers -TimeoutSec 3 -ErrorAction Stop
        Write-Host "Segment $segment : DISPONIBLE (Code: $($testResponse.StatusCode), Taille: $($testResponse.Content.Length) bytes)"
    } catch {
        Write-Host "Segment $segment : NON DISPONIBLE (Erreur: $($_.Exception.Message))"
    }
}
