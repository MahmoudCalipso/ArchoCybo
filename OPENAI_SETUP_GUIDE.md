# Getting Your OpenAI API Key

## Why You Need Your Own API Key

I cannot provide an API key from my account for security and billing reasons. OpenAI API keys are tied to individual accounts and usage is billed to the account owner. You'll need to create your own account and generate an API key.

## Steps to Get Your OpenAI API Key

### 1. Create an OpenAI Account
- Go to: https://platform.openai.com/signup
- Sign up with your email or Google/Microsoft account
- Verify your email address

### 2. Add Payment Method (Required for API Access)
- Go to: https://platform.openai.com/account/billing
- Click "Add payment method"
- Add a credit/debit card
- **Note**: OpenAI requires a payment method, but they offer $5 free credits for new accounts

### 3. Generate API Key
- Go to: https://platform.openai.com/api-keys
- Click "Create new secret key"
- Give it a name (e.g., "ArchoCybo Development")
- **IMPORTANT**: Copy the key immediately - you won't be able to see it again!
- It will look like: `sk-proj-xxxxxxxxxxxxxxxxxxxxx`

### 4. Add to Your Project
Open `ArchoCybo.WebApi/appsettings.json` and replace the empty string:

```json
{
  "OpenAI": {
    "ApiKey": "sk-proj-YOUR_ACTUAL_KEY_HERE",
    "Model": "gpt-4"
  }
}
```

### 5. Secure Your API Key
**⚠️ SECURITY WARNING**:
- **NEVER** commit your API key to Git
- Add `appsettings.json` to `.gitignore` if sharing code
- For production, use environment variables or Azure Key Vault

## Cost Considerations

### Pricing (as of 2024)
- **GPT-4**: ~$0.03 per 1K input tokens, ~$0.06 per 1K output tokens
- **GPT-3.5-Turbo**: ~$0.001 per 1K tokens (20x cheaper)

### Recommendations
1. **Start with GPT-3.5-Turbo** for development:
   ```json
   "Model": "gpt-3.5-turbo"
   ```
2. **Set usage limits** in OpenAI dashboard
3. **Monitor usage** at: https://platform.openai.com/usage

## Alternative: Use GPT-3.5-Turbo

If you want to minimize costs during development, change the model in `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-key-here",
    "Model": "gpt-3.5-turbo"  // Much cheaper!
  }
}
```

GPT-3.5-Turbo is 20x cheaper and still provides good results for most code generation tasks.

## Troubleshooting

### "Insufficient quota" error
- You've exceeded your free credits
- Add a payment method or wait for monthly reset

### "Invalid API key" error
- Double-check the key (starts with `sk-`)
- Ensure no extra spaces in appsettings.json
- Generate a new key if needed

### Want to test without OpenAI?
For now, the AI features are optional. The core code generation still works without an API key - you just won't have AI suggestions.

## Next Steps After Adding Key

1. Restart your ArchoCybo.WebApi project
2. The floating AI assistant button will appear (bottom-right)
3. Try typing: "Create a blog system with posts and comments"
4. Watch AI generate entity suggestions!

---

**Questions?** Feel free to ask if you need help with any step!
