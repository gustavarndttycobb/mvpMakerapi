# Test MVP List with empty filters
Write-Host "Testing MVP List endpoint with empty body..."

try {
    # Test 1: Empty body
    $response1 = Invoke-WebRequest -Uri http://localhost:8080/api/mvp/list -Method POST -ContentType "application/json" -Body '{}'
    $list1 = $response1.Content | ConvertFrom-Json
    Write-Host "`nTest 1 - Empty object {}: Found $($list1.Count) MVPs"
    
    # Test 2: Null filters
    $response2 = Invoke-WebRequest -Uri http://localhost:8080/api/mvp/list -Method POST -ContentType "application/json" -Body '{"category":null,"technology":null,"priceRange":null,"dateRange":null}'
    $list2 = $response2.Content | ConvertFrom-Json
    Write-Host "`nTest 2 - Null filters: Found $($list2.Count) MVPs"
    
    # Test 3: Empty arrays
    $response3 = Invoke-WebRequest -Uri http://localhost:8080/api/mvp/list -Method POST -ContentType "application/json" -Body '{"category":[],"technology":[]}'
    $list3 = $response3.Content | ConvertFrom-Json
    Write-Host "`nTest 3 - Empty arrays: Found $($list3.Count) MVPs"
    
    if ($list1.Count -gt 0) {
        Write-Host "`nFirst MVP:"
        Write-Host "  ID: $($list1[0].id)"
        Write-Host "  Name: $($list1[0].name)"
        Write-Host "  Categories: $($list1[0].categories -join ', ')"
        Write-Host "  Technologies: $($list1[0].technologies -join ', ')"
    }
    
} catch {
    Write-Host "Error: $_"
    Write-Host "Response: $($_.Exception.Response)"
}
