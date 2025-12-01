# reset-and-seed.ps1

Write-Host "🔄 Reiniciando ambiente Docker (limpando volumes)..." -ForegroundColor Cyan
docker-compose down -v
docker-compose up -d

Write-Host "⏳ Aguardando API iniciar (20s)..." -ForegroundColor Cyan
Start-Sleep -Seconds 20

$baseUrl = "http://localhost:8080/api"

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Body = @{},
        [string]$Token = $null
    )
    
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }

    try {
        $params = @{
            Method = $Method
            Uri = "$baseUrl$Uri"
            Headers = $headers
        }
        
        if ($Method -ne "GET" -and $Body.Count -gt 0) {
            $params.Body = ($Body | ConvertTo-Json -Depth 5)
        }

        return Invoke-RestMethod @params
    }
    catch {
        Write-Host "❌ Erro em $Uri : $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            Write-Host "Detalhes: $($reader.ReadToEnd())" -ForegroundColor Red
        }
    }
}

# 1. Criar Vendedor
Write-Host "`n👤 Criando Vendedor..." -ForegroundColor Green
$sellerReg = Invoke-Api -Method POST -Uri "/register" -Body @{
    name = "Vendedor Teste"
    email = "seller@test.com"
    password = "Password123!"
}

# Login Vendedor
$sellerLogin = Invoke-Api -Method POST -Uri "/login" -Body @{
    email = "seller@test.com"
    password = "Password123!"
}
$sellerToken = $sellerLogin.token
$sellerId = $sellerLogin.id
Write-Host "✅ Vendedor logado. ID: $sellerId"

# 2. Criar Comprador
Write-Host "`n👤 Criando Comprador..." -ForegroundColor Green
$buyerReg = Invoke-Api -Method POST -Uri "/register" -Body @{
    name = "Comprador Teste"
    email = "buyer@test.com"
    password = "Password123!"
}

# Login Comprador
$buyerLogin = Invoke-Api -Method POST -Uri "/login" -Body @{
    email = "buyer@test.com"
    password = "Password123!"
}
$buyerToken = $buyerLogin.token
$buyerId = $buyerLogin.id
Write-Host "✅ Comprador logado. ID: $buyerId"

# 3. Adicionar Saldo ao Comprador
Write-Host "`n💰 Adicionando fundos ao Comprador..." -ForegroundColor Green
Invoke-Api -Method POST -Uri "/wallet/add-funds" -Body @{
    userId = $buyerId
    amount = 500.00
} -Token $buyerToken | Out-Null
Write-Host "✅ Saldo adicionado: R$ 500,00"

# 4. Criar MVP GitHubRepo
Write-Host "`n📦 Criando MVP GitHubRepo..." -ForegroundColor Green
$mvpGithub = Invoke-Api -Method POST -Uri "/mvp" -Body @{
    name = "SaaS Starter Kit"
    description = "Um kit completo para iniciar seu SaaS com .NET e React."
    price = 150.00
    productType = "GitHubRepo"
    link = "https://github.com/gustavarndttycobb/mvpMakerApi"
    previewLink = "https://demo.saas-starter.com"
    categories = @("SaaS", "DevTools")
    technologies = @(".NET 9", "React", "PostgreSQL")
    imageUrl = "https://placehold.co/600x400/png"
} -Token $sellerToken
Write-Host "✅ MVP GitHub criado: $($mvpGithub.name)"

# 5. Criar MVP Drive
Write-Host "`n📦 Criando MVP Drive..." -ForegroundColor Green
$mvpDrive = Invoke-Api -Method POST -Uri "/mvp" -Body @{
    name = "E-book de Marketing"
    description = "Guia definitivo para marketing de produtos digitais."
    price = 49.90
    productType = "Drive"
    link = "https://drive.google.com/file/d/example-id/view"
    categories = @("Marketing", "Education")
    technologies = @("PDF")
    imageUrl = "https://placehold.co/600x400/orange/white"
} -Token $sellerToken
Write-Host "✅ MVP Drive criado: $($mvpDrive.name)"

Write-Host "`n✨ Script finalizado com sucesso!" -ForegroundColor Cyan
Write-Host "Vendedor: seller@test.com / Password123!"
Write-Host "Comprador: buyer@test.com / Password123!"
