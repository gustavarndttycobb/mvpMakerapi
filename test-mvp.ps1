# Test MVP Endpoints

# 1. Login to get token
$loginResponse = Invoke-WebRequest -Uri http://localhost:8080/api/login -Method POST -ContentType "application/json" -Body '{"email":"jwt@example.com","password":"password123"}'
$token = ($loginResponse.Content | ConvertFrom-Json).token
Write-Host "Token obtained"

# 2. Create MVP
$createMvpBody = @{
    name = "My First MVP"
    description = "An awesome MVP"
    technologies = @("C#", "React", "Docker")
    categories = @("SaaS", "Productivity")
    imageUrl = "http://example.com/image.png"
    price = 99.99
    highlights = @("Fast", "Secure")
    objective = "Solve X problem"
    mainFeatures = @("Feature A", "Feature B")
    status = "in progress"
    screenshots = @("http://example.com/s1.png")
} | ConvertTo-Json

$headers = @{ Authorization = "Bearer $token" }

Write-Host "`nCreating MVP..."
try {
    $createResponse = Invoke-WebRequest -Uri http://localhost:8080/api/mvp -Method POST -Headers $headers -ContentType "application/json" -Body $createMvpBody
    $mvp = $createResponse.Content | ConvertFrom-Json
    Write-Host "MVP Created: $($mvp.id) - $($mvp.name)"
    $mvpId = $mvp.id
} catch {
    Write-Host "Failed to create MVP: $_"
    exit
}

# 3. List MVPs
Write-Host "`nListing MVPs..."
$listBody = @{
    category = @("SaaS")
} | ConvertTo-Json

try {
    $listResponse = Invoke-WebRequest -Uri http://localhost:8080/api/mvp/list -Method POST -ContentType "application/json" -Body $listBody
    $list = $listResponse.Content | ConvertFrom-Json
    Write-Host "MVPs found: $($list.Count)"
    if ($list.Count -gt 0) {
        Write-Host "First MVP: $($list[0].name)"
    }
} catch {
    Write-Host "Failed to list MVPs: $_"
}

# 4. Get MVP Details
Write-Host "`nGetting MVP Details..."
try {
    $detailsResponse = Invoke-WebRequest -Uri "http://localhost:8080/api/mvp/details/$mvpId" -Method GET
    $details = $detailsResponse.Content | ConvertFrom-Json
    Write-Host "MVP Details: $($details.name)"
    Write-Host "Objective: $($details.objective)"
    Write-Host "Highlights: $($details.highlights -join ', ')"
} catch {
    Write-Host "Failed to get details: $_"
}
