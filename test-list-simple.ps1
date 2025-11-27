# Test MVP List with different formats

Write-Host "Testing MVP List Endpoint" -ForegroundColor Cyan

# Test 1: Empty object
Write-Host "`n1. Empty object {}"
$r1 = Invoke-WebRequest -Uri http://localhost:8080/api/mvp/list -Method POST -ContentType "application/json" -Body '{}'
$list1 = $r1.Content | ConvertFrom-Json
Write-Host "   Found: $($list1.Count) MVPs"

# Test 2: With category filter
Write-Host "`n2. With category filter (SaaS)"
$r2 = Invoke-WebRequest -Uri http://localhost:8080/api/mvp/list -Method POST -ContentType "application/json" -Body '{"category":["SaaS"]}'
$list2 = $r2.Content | ConvertFrom-Json
Write-Host "   Found: $($list2.Count) MVPs"

# Test 3: With technology filter
Write-Host "`n3. With technology filter (React)"
$r3 = Invoke-WebRequest -Uri http://localhost:8080/api/mvp/list -Method POST -ContentType "application/json" -Body '{"technology":["React"]}'
$list3 = $r3.Content | ConvertFrom-Json
Write-Host "   Found: $($list3.Count) MVPs"

# Show all MVPs from test 1
if ($list1.Count -gt 0) {
    Write-Host "`nAll MVPs in database:"
    foreach ($mvp in $list1) {
        Write-Host "  - $($mvp.name)"
        Write-Host "    Categories: $($mvp.categories -join ', ')"
        Write-Host "    Technologies: $($mvp.technologies -join ', ')"
        Write-Host "    Price: $($mvp.price)"
    }
}
