using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Configuration
{
    /// <summary>
    /// PayPal payment gateway configuration containing REST API credentials and settings.
    /// </summary>
    /// <remarks>
    /// PayPal uses OAuth 2.0 client credentials flow for authentication.
    /// Different credentials are required for Sandbox (testing) vs Live (production).
    /// 
    /// Obtain credentials from PayPal Developer Dashboard (https://developer.paypal.com/dashboard).
    /// NEVER commit client secrets to source control - use environment variables or Azure Key Vault.
    /// </remarks>
    public class PayPalOptions
    {
        /// <summary>
        /// Whether PayPal gateway is enabled for payment processing.
        /// Set to false to disable PayPal and prevent gateway initialization.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// PayPal REST API client ID (from PayPal Developer Dashboard).
        /// Used for OAuth 2.0 authentication to obtain access tokens.
        /// </summary>
        /// <remarks>
        /// Obtain from PayPal Developer Dashboard → My Apps & Credentials.
        /// Different IDs for Sandbox vs Live environments.
        /// 
        /// Environment-specific:
        /// - Sandbox: Starts with random alphanumeric string for testing
        /// - Live: Production client ID for real transactions
        /// 
        /// This is not secret but should still be protected from public exposure.
        /// </remarks>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// PayPal REST API client secret (from PayPal Developer Dashboard).
        /// Used with ClientId for OAuth 2.0 authentication.
        /// MUST BE KEPT SECRET - never expose in frontend code!
        /// </summary>
        /// <remarks>
        /// This secret grants access to process payments and refunds:
        /// - Store in environment variables (not appsettings.json in production)
        /// - Use Azure Key Vault or similar secrets management
        /// - Never log or display this value
        /// - Rotate immediately if compromised
        /// 
        /// Used to obtain OAuth 2.0 Bearer tokens via /v1/oauth2/token endpoint.
        /// Tokens expire after 9 hours (32400 seconds) and are auto-refreshed by gateway.
        /// </remarks>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// PayPal webhook ID for validating webhook signatures.
        /// Obtained after creating a webhook endpoint in PayPal Dashboard.
        /// </summary>
        /// <remarks>
        /// Setup:
        /// 1. Go to PayPal Developer Dashboard → Webhooks
        /// 2. Create webhook endpoint with your server URL (e.g., https://yourdomain.com/api/PaymentWebhooks/paypal)
        /// 3. Select event types to subscribe to (e.g., PAYMENT.CAPTURE.COMPLETED)
        /// 4. Copy the Webhook ID from the dashboard
        /// 
        /// Used by ValidateWebhook to call PayPal's verification API:
        /// POST /v1/notifications/verify-webhook-signature
        /// 
        /// If not configured, webhook validation falls back to simple signature presence check (less secure).
        /// </remarks>
        public string WebhookId { get; set; } = string.Empty;

        /// <summary>
        /// PayPal environment: "Sandbox" or "Live".
        /// Determines which PayPal API base URL is used.
        /// </summary>
        /// <remarks>
        /// Environment URLs:
        /// - Sandbox: https://api-m.sandbox.paypal.com (for testing)
        /// - Live: https://api-m.paypal.com (for production)
        /// 
        /// Defaults to "Sandbox" to prevent accidental live charges.
        /// Must match the environment of your ClientId and ClientSecret.
        /// </remarks>
        public string Environment { get; set; } = "Sandbox";

        /// <summary>
        /// PayPal REST API base URL (optional).
        /// Auto-computed from Environment if not explicitly set.
        /// </summary>
        /// <remarks>
        /// Normally left empty to auto-select based on Environment setting.
        /// Can be overridden for:
        /// - Testing against local PayPal mock server
        /// - Using region-specific endpoints if PayPal introduces them
        /// 
        /// Default URLs:
        /// - Sandbox: https://api-m.sandbox.paypal.com
        /// - Live: https://api-m.paypal.com
        /// </remarks>
        public string? BaseUrl { get; set; }
    }
}
