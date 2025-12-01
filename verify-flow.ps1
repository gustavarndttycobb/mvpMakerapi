# verify-flow.ps1

$baseUrl = "http://localhost:8080/api"

# Dados fornecidos
$sellerEmail = "gustavo.tycobb@gmail.com"
$sellerPass = "1234"
$buyerEmail = "gustavoarndt1988@gmail.com"
$buyerPass = "1234"
$mvpName = "Test 10"
$githubUrl = "https://github.com/gustavoarndtufv/transaction-test"
$buyerGithubUser = "gustavarndttycobb"
$pat = "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"

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
        $jsonBody = $null
        if ($Method -ne "GET" -and $Body.Count -gt 0) {
            $jsonBody = ($Body | ConvertTo-Json -Depth 5)
        }

        $params = @{
            Method = $Method
            Uri = "$baseUrl$Uri"
            Headers = $headers
        }
        if ($jsonBody) {
            $params.Body = $jsonBody
            # Write-Host "DEBUG JSON: $jsonBody" -ForegroundColor DarkGray
        }

        return Invoke-RestMethod @params
    }
    catch {
        Write-Host "Erro em $Uri : $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $errorBody = $reader.ReadToEnd()
            Write-Host "Detalhes: $errorBody" -ForegroundColor Red
        }
        return $null
    }
}

Write-Host "Iniciando Teste de Fluxo de Transferencia GitHub..." -ForegroundColor Cyan

# 1. Login/Registro Vendedor
Write-Host "`nAutenticando Vendedor..." -ForegroundColor Green
$sellerLogin = Invoke-Api -Method POST -Uri "/login" -Body @{ email = $sellerEmail; password = $sellerPass }
if (-not $sellerLogin) {
    Write-Host "Vendedor nao encontrado, registrando..." -ForegroundColor Yellow
    Invoke-Api -Method POST -Uri "/register" -Body @{ name = "Gustavo Seller"; email = $sellerEmail; password = $sellerPass } | Out-Null
    $sellerLogin = Invoke-Api -Method POST -Uri "/login" -Body @{ email = $sellerEmail; password = $sellerPass }
}
$sellerToken = $sellerLogin.token
Write-Host "Vendedor logado."

# 2. Login/Registro Comprador
Write-Host "`nAutenticando Comprador..." -ForegroundColor Green
$buyerLogin = Invoke-Api -Method POST -Uri "/login" -Body @{ email = $buyerEmail; password = $buyerPass }
if (-not $buyerLogin) {
    Write-Host "Comprador nao encontrado, registrando..." -ForegroundColor Yellow
    Invoke-Api -Method POST -Uri "/register" -Body @{ name = "Gustavo Buyer"; email = $buyerEmail; password = $buyerPass } | Out-Null
    $buyerLogin = Invoke-Api -Method POST -Uri "/login" -Body @{ email = $buyerEmail; password = $buyerPass }
}
$buyerToken = $buyerLogin.token
$buyerId = $buyerLogin.id
Write-Host "Comprador logado."

# 3. Adicionar Saldo ao Comprador
Write-Host "`nAdicionando fundos..." -ForegroundColor Green
Invoke-Api -Method POST -Uri "/wallet/add-funds" -Body @{ userId = $buyerId; amount = 1000.00 } -Token $buyerToken | Out-Null

# 4. Criar MVP
Write-Host "`nCriando MVP '$mvpName'..." -ForegroundColor Green
# Forçando array no PowerShell usando @(val1, val2) ou ,@(val)
$mvpBody = @{
    name = $mvpName
    description = "MVP de teste para transferencia GitHub"
    price = 10.00
    productType = "GitHubRepo"
    link = $githubUrl
    previewLink = "https://example.com"
    categories = @("Test", "Dev") 
    technologies = @("Test", "Debug")
    imageUrl = "https://placehold.co/600x400"
}
$mvp = Invoke-Api -Method POST -Uri "/mvp" -Body $mvpBody -Token $sellerToken

if (-not $mvp) {
    Write-Host "Falha ao criar MVP. Abortando." -ForegroundColor Red
    exit
}

$mvpId = $mvp.id
Write-Host "MVP criado. ID: $mvpId"

# 5. Iniciar Compra
Write-Host "`nIniciando Compra..." -ForegroundColor Green
# Body vazio precisa ser hashtable vazio
$purchase = Invoke-Api -Method POST -Uri "/mvp/$mvpId/purchase" -Body @{ dummy = "value" } -Token $buyerToken

if (-not $purchase) {
    Write-Host "Falha ao iniciar compra. Abortando." -ForegroundColor Red
    exit
}

$transactionId = $purchase.transactionId
Write-Host "Compra iniciada. Transacao ID: $transactionId"
Write-Host "Status Inicial: $($purchase.status)"

if ($purchase.status -ne "PENDING_TRANSFER") {
    Write-Host "Status incorreto! Esperado: PENDING_TRANSFER" -ForegroundColor Red
    exit
}

# 6. Iniciar Transferência GitHub
Write-Host "`nIniciando Transferencia GitHub..." -ForegroundColor Green
Write-Host "Usando PAT: ${pat:0:4}..."
Write-Host "Comprador GitHub: $buyerGithubUser"

$transfer = Invoke-Api -Method POST -Uri "/transactions/$transactionId/transfer-github" -Body @{
    sellerToken = $pat
    buyerUsername = $buyerGithubUser
} -Token $sellerToken

if ($transfer) {
    Write-Host "Transferencia iniciada com sucesso!"
    Write-Host "Mensagem: $($transfer.message)"
} else {
    Write-Host "Falha na transferencia."
    exit
}

# 7. Verificar Status da Transação (Deve ser WAITING_ACCEPTANCE)
Write-Host "`nVerificando Status (Pos-Transferencia)..." -ForegroundColor Green
$transDetails = Invoke-Api -Method GET -Uri "/transactions/$transactionId" -Token $sellerToken
Write-Host "Status Atual: $($transDetails.status)"

if ($transDetails.status -ne "WAITING_ACCEPTANCE") {
    Write-Host "Status incorreto! Esperado: WAITING_ACCEPTANCE" -ForegroundColor Red
    exit
} else {
    Write-Host "Status correto: WAITING_ACCEPTANCE" -ForegroundColor Green
}

# 8. Simular Aceite do Comprador (Verify Transfer)
Write-Host "`nSimulando Aceite do Comprador..." -ForegroundColor Green
$verify = Invoke-Api -Method POST -Uri "/transactions/$transactionId/verify-transfer" -Body @{ dummy = "value" } -Token $buyerToken

if ($verify) {
    Write-Host "Verificacao concluida!"
    Write-Host "Status Final: $($verify.status)"
    Write-Host "CompletedAt: $($verify.completedAt)"
    
    if ($verify.status -eq "COMPLETED") {
        Write-Host "FLUXO COMPLETO COM SUCESSO!" -ForegroundColor Green
    } else {
        Write-Host "Status final incorreto." -ForegroundColor Red
    }
} else {
    Write-Host "Falha na verificacao."
}
