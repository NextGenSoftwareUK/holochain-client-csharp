# Solana Wallet Creation Guide

**Last Updated:** 2026-01-09  
**Status:** ✅ **WORKING SOLUTION**

---

## ⚠️ CRITICAL: Correct Order for Solana Wallet Creation

For **SolanaOASIS** and other non-Bitcoin providers, you **MUST** follow this order:

1. **Link Public Key FIRST** (creates wallet with correct address)
2. **Link Private Key SECOND** (completes wallet using wallet ID from step 1)

### Why This Order?

The `LinkProviderPrivateKeyToAvatar` method calls `WalletAddressHelper.PrivateKeyToAddress()` which:
- Only works for Bitcoin format (base58 WIF)
- Fails with "Invalid base58 data" for Solana if private keys are not properly formatted
- Cannot derive Solana addresses from private keys

By linking the public key first:
- The wallet is created with the correct address from keypair generation
- The private key is then linked to that existing wallet
- This avoids the address derivation issue entirely

---

## Complete Workflow

### Prerequisites

1. **Register Solana Provider:**
   ```bash
   POST /api/provider/register-provider-type/3
   # or
   POST /api/provider/register-provider-type/SolanaOASIS
   ```

2. **Activate Solana Provider:**
   ```bash
   POST /api/provider/activate-provider/3
   # or
   POST /api/provider/activate-provider/SolanaOASIS
   ```

### Step 1: Generate Solana Keypair

**Endpoint:** `POST /api/keys/generate_keypair_with_wallet_address_for_provider/SolanaOASIS`

**Response:**
```json
{
  "isError": false,
  "result": {
    "privateKey": "wq+ufyyXgTXZxc6f7MONxQgrpJ10AawLEO6IHwmoZtlhqObAQOhnBCdkc5saz2oMQ6EyIQW2C/pPCMdxdXtE4w==",
    "publicKey": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",
    "walletAddressLegacy": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q"
  }
}
```

**Note:** 
- `privateKey` is base64 encoded (Solana format)
- `publicKey` and `walletAddressLegacy` are base58 (valid Solana addresses, 32-44 chars)

### Step 2: Link Public Key FIRST (Creates Wallet)

**Endpoint:** `POST /api/keys/link_provider_public_key_to_avatar_by_id`

**Request:**
```json
{
  "AvatarID": "d42b8448-52a9-4579-a6b1-b7c624616459",
  "ProviderType": "SolanaOASIS",
  "ProviderKey": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",
  "WalletAddress": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q"
}
```

**Note:** Omit `WalletId` to create a new wallet.

**Response:**
```json
{
  "isError": false,
  "result": {
    "id": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "walletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "walletAddress": "7aDvb3FFPm2XZ3x5mgtdT5qKnjyfpCnKDQ9wGAAqJi1Q",
    "providerType": 3
  }
}
```

**Extract:** `walletId` or `id` for next step.

### Step 3: Link Private Key SECOND (Completes Wallet)

**Endpoint:** `POST /api/keys/link_provider_private_key_to_avatar_by_id`

**Request:**
```json
{
  "WalletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
  "AvatarID": "d42b8448-52a9-4579-a6b1-b7c624616459",
  "ProviderType": "SolanaOASIS",
  "ProviderKey": "wq+ufyyXgTXZxc6f7MONxQgrpJ10AawLEO6IHwmoZtlhqObAQOhnBCdkc5saz2oMQ6EyIQW2C/pPCMdxdXtE4w=="
}
```

**Note:** `WalletId` is **REQUIRED** - use the wallet ID from step 2.

**Response:**
```json
{
  "isError": false,
  "result": {
    "id": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "walletId": "e743527b-4011-406c-8d7d-af38fd1fcad8",
    "providerType": 3
  }
}
```

---

## Using the Helper Method

The `linkKeys()` method in `keysApi.ts` now automatically handles the correct order:

```typescript
import { keysAPI } from './lib/keysApi';

const result = await keysAPI.linkKeys({
  avatarId: "d42b8448-52a9-4579-a6b1-b7c624616459",
  providerType: "SolanaOASIS",
  privateKey: "...",
  publicKey: "...",
  walletAddress: "..."
});

if (result.isError) {
  console.error("Failed:", result.message);
} else {
  console.log("Wallet created:", result.result.walletId);
}
```

The helper method:
- Detects Solana provider automatically
- Links public key first
- Links private key second
- Returns the wallet ID

---

## Provider Registration & Activation

### Registration

**Endpoint:** `POST /api/provider/register-provider-type/{providerType}`

- Route parameter: `3` or `SolanaOASIS`
- This instantiates the provider from DNA config and registers it

### Activation

**Endpoint:** `POST /api/provider/activate-provider/{providerType}`

- Route parameter: `3` or `SolanaOASIS`
- This activates the registered provider

**Note:** Both endpoints use route parameters, NOT JSON body.

---

## Common Errors & Solutions

### Error: "Solana provider is not activated"
**Solution:** Register and activate the provider first (see Prerequisites above)

### Error: "Invalid base58 data" when linking private key
**Solution:** You're linking private key first. Use the correct order: public key first, then private key.

### Error: "providerType Default"
**Solution:** Make sure you're passing `"SolanaOASIS"` as a string, not a number, in the JSON body.

### Error: "WalletId is required"
**Solution:** When linking private key, you must provide the `WalletId` from the public key linking step.

---

## Testing

Use the provided test script:
```bash
./test-solana-wallet.sh
```

Or test manually:
```bash
# 1. Register provider
curl -k -X POST "https://127.0.0.1:5004/api/provider/register-provider-type/3" \
  -H "Authorization: Bearer $TOKEN"

# 2. Activate provider
curl -k -X POST "https://127.0.0.1:5004/api/provider/activate-provider/3" \
  -H "Authorization: Bearer $TOKEN"

# 3. Generate keypair
curl -k -X POST "https://127.0.0.1:5004/api/keys/generate_keypair_with_wallet_address_for_provider/SolanaOASIS" \
  -H "Authorization: Bearer $TOKEN"

# 4. Link public key FIRST
curl -k -X POST "https://127.0.0.1:5004/api/keys/link_provider_public_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"AvatarID": "...", "ProviderType": "SolanaOASIS", "ProviderKey": "...", "WalletAddress": "..."}'

# 5. Link private key SECOND (with WalletId from step 4)
curl -k -X POST "https://127.0.0.1:5004/api/keys/link_provider_private_key_to_avatar_by_id" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"WalletId": "...", "AvatarID": "...", "ProviderType": "SolanaOASIS", "ProviderKey": "..."}'
```

---

## Summary

✅ **Always use this order for Solana:**
1. Register provider
2. Activate provider  
3. Generate keypair
4. Link **public key FIRST** (creates wallet)
5. Link **private key SECOND** (completes wallet)

✅ **Use the `linkKeys()` helper method** - it handles the correct order automatically

✅ **Provider activation uses route parameters**, not JSON body

---

**Related Documentation:**
- `WALLET_CREATION_VIA_KEYS_API_SOLUTION.md` - General Keys API guide
- `test-solana-wallet.sh` - Test script with correct order
