$token = "14e9b2fc-9730-4c98-8891-193a4add1bbc"
$base = "C:\Users\pietr\progetti\simlife2\PitLife"
$id = "1d53f6a3-001d-4bb7-8e56-75954c58ea2e"
$dest = "$base\Content\assets\creatures\reptiles\testudines\turtle.png"

$maxAttempts = 15
for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    Start-Sleep -Seconds 20
    Write-Output "Attempt $attempt..."
    $zipPath = "$base\temp_turtle.zip"
    & curl.exe -s -o $zipPath "https://api.pixellab.ai/v2/characters/$id/zip" -H "Authorization: Bearer $token" 2>$null
    $size = (Get-Item $zipPath -ErrorAction SilentlyContinue).Length
    if ($size -gt 5000) {
        Expand-Archive -Path $zipPath -DestinationPath "$base\temp_turtle" -Force
        $south = Get-ChildItem -Recurse -Path "$base\temp_turtle" -Filter "south.png"
        if ($south) {
            Copy-Item $south[0].FullName $dest -Force
            $finalSize = (Get-Item $dest).Length
            Write-Output "Turtle OK ($finalSize bytes)"
            Remove-Item -Recurse -Force "$base\temp_turtle", $zipPath -ErrorAction SilentlyContinue
            exit 0
        }
    }
    Remove-Item $zipPath -ErrorAction SilentlyContinue
}
Write-Output "FAILED after $maxAttempts attempts"
exit 1
