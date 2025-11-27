$response = Invoke-WebRequest -Uri http://localhost:8080/api/login -Method POST -ContentType "application/json" -Body '{"email":"jwt@example.com","password":"password123"}'
$json = $response.Content | ConvertFrom-Json
$token = $json.token
Write-Host "Token: $token"

Write-Host "`nTesting protected endpoint WITHOUT token..."
try {
    Invoke-WebRequest -Uri http://localhost:8080/api/user/me -Method GET
} catch {
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)"
}

Write-Host "`nTesting protected endpoint WITH token..."
try {
    $headers = @{ Authorization = "Bearer $token" }
    $me = Invoke-WebRequest -Uri http://localhost:8080/api/user/me -Method GET -Headers $headers
    Write-Host "Success! Content: $($me.Content)"
} catch {
    Write-Host "Failed: $_"
}
