$token = "14e9b2fc-9730-4c98-8891-193a4add1bbc"
$base = "C:\Users\pietr\progetti\simlife2\PitLife"
$id = "44f80d64-78df-4a0c-99fa-39b517f479e9"
$dest = "$base\Content\assets\ui\spawn_icon.png"

if (-not (Test-Path "$base\Content\assets\ui")) {
    New-Item -ItemType Directory -Path "$base\Content\assets\ui" -Force | Out-Null
}

$maxAttempts = 15
for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    Start-Sleep -Seconds 20
    Write-Output "Attempt $attempt..."
    $zipPath = "$base\temp_icon.zip"
    & curl.exe -s -o $zipPath "https://api.pixellab.ai/v2/characters/$id/zip" -H "Authorization: Bearer $token" 2>$null
    $size = (Get-Item $zipPath -ErrorAction SilentlyContinue).Length
    if ($size -gt 5000) {
        Expand-Archive -Path $zipPath -DestinationPath "$base\temp_icon" -Force
        $south = Get-ChildItem -Recurse -Path "$base\temp_icon" -Filter "south.png"
        if ($south) {
            Copy-Item $south[0].FullName $dest -Force
            $finalSize = (Get-Item $dest).Length
            Write-Output "Icon OK ($finalSize bytes)"
            Remove-Item -Recurse -Force "$base\temp_icon", $zipPath -ErrorAction SilentlyContinue
            exit 0
        }
    }
    Remove-Item $zipPath -ErrorAction SilentlyContinue
}
Write-Output "FAILED"
exit 1
