# Relatório de Integração Stripe - Frontend

## Resumo
A API agora suporta pagamentos via Stripe Checkout. O fluxo usa sessões de checkout hospedadas pelo Stripe, eliminando a necessidade de lidar com dados de cartão no frontend.

---

## Fluxo de Pagamento

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Frontend  │────▶│   Backend   │────▶│   Stripe    │────▶│   Webhook   │
│             │     │             │     │   Checkout  │     │   Backend   │
└─────────────┘     └─────────────┘     └─────────────┘     └─────────────┘
      │                   │                   │                    │
      │  1. Iniciar       │                   │                    │
      │     Compra        │                   │                    │
      │──────────────────▶│                   │                    │
      │                   │  2. Criar         │                    │
      │                   │     Session       │                    │
      │                   │──────────────────▶│                    │
      │  3. Redirect URL  │                   │                    │
      │◀──────────────────│                   │                    │
      │                   │                   │                    │
      │  4. Redirecionar  │                   │                    │
      │     para Stripe   │                   │                    │
      │──────────────────────────────────────▶│                    │
      │                   │                   │                    │
      │                   │                   │  5. Pagamento OK   │
      │                   │                   │───────────────────▶│
      │                   │                   │                    │
      │  6. Redirect      │                   │   7. Atualiza      │
      │     Success URL   │                   │      Transação     │
      │◀──────────────────────────────────────│                    │
```

---

## Endpoints

### 1. Iniciar Compra (Existente - Atualizado)

```http
POST /api/mvp/{mvpId}/purchase
Authorization: Bearer {token}
```

**Response:**
```json
{
  "transactionId": "guid-da-transacao",
  "status": "PENDING_TRANSFER",
  "amount": 99.90,
  "message": "Purchase initiated...",
  "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_test_..."
}
```

> ⚠️ **IMPORTANTE**: O campo `checkoutUrl` será adicionado na resposta. O frontend deve redirecionar o usuário para essa URL.

---

### 2. Verificar Status do Pagamento

```http
GET /api/payments/{transactionId}/status
Authorization: Bearer {token}
```

**Response:**
```json
{
  "transactionId": "guid-da-transacao",
  "status": "PENDING_TRANSFER",
  "isPaid": true
}
```

---

### 3. Webhook (Apenas Backend)

```http
POST /api/payments/webhook
```

> Este endpoint é chamado automaticamente pelo Stripe. O frontend não precisa interagir com ele.

---

## URLs de Retorno

Após o pagamento no Stripe, o usuário é redirecionado para:

| Resultado | URL |
|-----------|-----|
| **Sucesso** | `http://localhost:3000/payment/success?session_id={CHECKOUT_SESSION_ID}` |
| **Cancelado** | `http://localhost:3000/payment/cancel` |

---

## Implementação no Frontend

### 1. Página de Compra

```typescript
// Ao clicar em "Comprar"
const handlePurchase = async (mvpId: string) => {
  try {
    const response = await api.post(`/mvp/${mvpId}/purchase`);
    
    // Redirecionar para Stripe Checkout
    if (response.data.checkoutUrl) {
      window.location.href = response.data.checkoutUrl;
    }
  } catch (error) {
    // Tratar erro
  }
};
```

### 2. Página de Sucesso (`/payment/success`)

```typescript
// pages/payment/success.tsx
const PaymentSuccess = () => {
  const searchParams = useSearchParams();
  const sessionId = searchParams.get('session_id');
  
  useEffect(() => {
    // Opcional: Verificar status do pagamento
    // O webhook já atualizou o status no backend
  }, [sessionId]);
  
  return (
    <div>
      <h1>Pagamento Realizado com Sucesso!</h1>
      <p>Seu pagamento foi processado. Aguarde a transferência do vendedor.</p>
      <Link href="/transactions">Ver Minhas Transações</Link>
    </div>
  );
};
```

### 3. Página de Cancelamento (`/payment/cancel`)

```typescript
// pages/payment/cancel.tsx
const PaymentCancel = () => {
  return (
    <div>
      <h1>Pagamento Cancelado</h1>
      <p>Você cancelou o pagamento. Nenhuma cobrança foi realizada.</p>
      <Link href="/marketplace">Voltar ao Marketplace</Link>
    </div>
  );
};
```

---

## Status das Transações

| Status | Descrição | Ação do Frontend |
|--------|-----------|------------------|
| `PENDING` | Aguardando pagamento | Botão "Pagar" |
| `PENDING_TRANSFER` | Pago, aguardando vendedor transferir | Mostrar "Aguardando Transferência" |
| `WAITING_ACCEPTANCE` | Vendedor transferiu, comprador deve verificar | Botão "Verificar Transferência" |
| `COMPLETED` | Transação finalizada | Mostrar "Concluída" |
| `CANCELLED` | Cancelada | Mostrar "Cancelada" |

---

## Chave Pública do Stripe (Opcional)

Se precisar usar Stripe.js no frontend:

```
pk_test_51Sb76kHICDhZgt4X59xPNr0RTDBTemsbdkkFtkc3gJ41TuARtG6hrcJCZuNhFDfIVKIJuWmFBtrsAipPCgNxWpMz00cGHV2TzX
```

> Para o fluxo atual (Checkout Sessions), **não é necessário** usar Stripe.js. O redirecionamento para `checkoutUrl` é suficiente.

---

## Exemplo Completo de Fluxo

1. **Comprador** clica em "Comprar MVP"
2. **Frontend** chama `POST /api/mvp/{id}/purchase`
3. **Backend** cria sessão Stripe e retorna `checkoutUrl`
4. **Frontend** redireciona para `checkoutUrl`
5. **Comprador** paga no Stripe
6. **Stripe** redireciona para `/payment/success`
7. **Stripe** envia webhook para backend
8. **Backend** atualiza status para `PENDING_TRANSFER`
9. **Vendedor** transfere GitHub/Drive
10. **Comprador** verifica e transação fica `COMPLETED`

---

## Dúvidas?

Qualquer dúvida sobre a integração, entre em contato.
