$fox = Get-ChildItem -Recurse -Path 'C:\Users\pietr\progetti\simlife2\PitLife\temp_fox' -Filter 'south.png'
Copy-Item $fox[0].FullName 'C:\Users\pietr\progetti\simlife2\PitLife\Content\assets\creatures\mammals\carnivores\canids\fox.png' -Force

$fish = Get-ChildItem -Recurse -Path 'C:\Users\pietr\progetti\simlife2\PitLife\temp_fish' -Filter 'south.png'
Copy-Item $fish[0].FullName 'C:\Users\pietr\progetti\simlife2\PitLife\Content\assets\creatures\fish\fish.png' -Force

$croc = Get-ChildItem -Recurse -Path 'C:\Users\pietr\progetti\simlife2\PitLife\temp_croc' -Filter 'south.png'
Copy-Item $croc[0].FullName 'C:\Users\pietr\progetti\simlife2\PitLife\Content\assets\creatures\reptiles\crocodilians\crocodile.png' -Force

Write-Output 'all copied'

# Cleanup
Remove-Item -Recurse -Force 'C:\Users\pietr\progetti\simlife2\PitLife\temp_fox', 'C:\Users\pietr\progetti\simlife2\PitLife\temp_fish', 'C:\Users\pietr\progetti\simlife2\PitLife\temp_croc'
Remove-Item -Force 'C:\Users\pietr\progetti\simlife2\PitLife\temp_fox.zip', 'C:\Users\pietr\progetti\simlife2\PitLife\temp_fish.zip', 'C:\Users\pietr\progetti\simlife2\PitLife\temp_croc.zip', 'C:\Users\pietr\progetti\simlife2\PitLife\gen_pine.mjs', 'C:\Users\pietr\progetti\simlife2\PitLife\call_pixellab.bat', 'C:\Users\pietr\progetti\simlife2\PitLife\pixellab_helper.mjs'

# Verify sizes
Get-Item 'C:\Users\pietr\progetti\simlife2\PitLife\Content\assets\creatures\plants\trees\pine.png',
         'C:\Users\pietr\progetti\simlife2\PitLife\Content\assets\creatures\reptiles\crocodilians\crocodile.png',
         'C:\Users\pietr\progetti\simlife2\PitLife\Content\assets\creatures\mammals\carnivores\canids\fox.png',
         'C:\Users\pietr\progetti\simlife2\PitLife\Content\assets\creatures\fish\fish.png' | Select-Object Name, Length
