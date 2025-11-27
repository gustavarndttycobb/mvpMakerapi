# Comprehensive MVP List Test

Write-Host "=== Testing MVP List Endpoint ===" -ForegroundColor Cyan

# Test different request formats
$tests = @(
    @{ Name = "Empty object {}"; Body = '{}' },
    @{ Name = "No body (default)"; Body = $null },
    @{ Name = "Null properties"; Body = '{"category":null,"technology":null,"priceRange":null,"dateRange":null}' },
    @{ Name = "Empty arrays"; Body = '{"category":[],"technology":[]}' },
    @{ Name = "With category filter"; Body = '{"category":["SaaS"]}' },
    @{ Name = "With technology filter"; Body = '{"technology":["React"]}' },
    @{ Name = "With price range"; Body = '{"priceRange":[0,100]}' }
)

foreach ($test in $tests) {
    Write-Host "`n--- $($test.Name) ---" -ForegroundColor Yellow
    
    try {
        if ($test.Body) {
            $response = Invoke-WebRequest -Uri http://localhost:8080/api/mvp/list -Method POST -ContentType "application/json" -Body $test.Body
        } else {
            $response = Invoke-WebRequest -Uri http://localhost:8080/api/mvp/list -Method POST -ContentType "application/json"
        }
        
        $list = $response.Content | ConvertFrom-Json
        Write-Host "✓ Success: Found $($list.Count) MVP(s)" -ForegroundColor Green
        
        if ($list.Count -gt 0) {
            foreach ($mvp in $list) {
                Write-Host "  - $($mvp.name) (ID: $($mvp.id.Substring(0,8))...)"
            }
        }
    } catch {
        Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "All tests completed. The endpoint should return all MVPs when filters are empty or null."
