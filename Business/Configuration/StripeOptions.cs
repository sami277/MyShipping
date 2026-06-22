using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Configuration
{
    /// <summary>
    /// Stripe payment gateway configuration containing API credentials and settings.
    /// </summary>
    /// <remarks>
    /// Stripe requires different keys for test vs production environments:
    /// - Test keys: pk_test_..., sk_test_..., whsec_test_...
    /// - Live keys: pk_live_..., sk_live_..., whsec_live_...
    /// 
    /// Obtain keys from Stripe Dashboard (https://dashboard.stripe.com/apikeys).
    /// NEVER commit secret keys to source control - use environment variables or Azure Key Vault.
    /// </remarks>
    public class StripeOptions
    {
        /// <summary>
        /// Whether Stripe gateway is enabled for payment processing.
        /// Set to false to disable Stripe and prevent gateway initialization.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Stripe publishable key (starts with pk_test_ or pk_live_).
        /// Used in frontend JavaScript for Stripe.js initialization.
        /// Safe to expose publicly in HTML/JavaScript.
        /// </summary>
        /// <remarks>
        /// This key is used client-side to:
        /// - Tokenize credit card data securely without touching your server
        /// - Create payment methods through Stripe.js
        /// - Display Stripe UI components (card element, payment element)
        /// 
        /// Environment-specific:
        /// - Test: pk_test_51ABC...
        /// - Live: pk_live_51ABC...
        /// </remarks>
        public string PublishableKey { get; set; } = string.Empty;
                     
        /// <summary>
        /// Stripe secret key (starts with sk_test_ or sk_live_).
        /// Used in backend API calls to create charges, process refunds, etc.
        /// MUST BE KEPT SECRET - never expose in frontend code!
        /// </summary>
        /// <remarks>
        /// This key grants full access to your Stripe account and must be protected:
        /// - Store in environment variables (not appsettings.json in production)
        /// - Use Azure Key Vault or similar secrets management
        /// - Never log or display this value
        /// - Rotate immediately if compromised
        /// 
        /// Environment-specific:
        /// - Test: sk_test_51ABC...
        /// - Live: sk_live_51ABC...
        /// </remarks>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Webhook signing secret for validating webhook signatures (starts with whsec_).
        /// Used to verify webhooks originated from Stripe and prevent forgery attacks.
        /// </summary>
        /// <remarks>
        /// Obtained from Stripe Dashboard → Webhooks → Add Endpoint.
        /// Each webhook endpoint has its own signing secret.
        /// 
        /// Validation flow:
        /// 1. Extract Stripe-Signature header from webhook request
        /// 2. Use this secret with Stripe SDK to verify HMAC signature
        /// 3. Reject webhook if signature invalid
        /// 
        /// Environment-specific:
        /// - Test: whsec_test_...
        /// - Live: whsec_live_...
        /// </remarks>
        public string WebhookSecret { get; set; } = string.Empty;

        /// <summary>
        /// Environment identifier: "Test" or "Production".
        /// Used for logging and monitoring to distinguish test vs live transactions.
        /// </summary>
        /// <remarks>
        /// Defaults to "Test" to prevent accidental live charges during development.
        /// Must match the environment of your API keys (test keys for Test, live keys for Production).
        /// </remarks>
        public string Environment { get; set; } = "Test";

        /// <summary>
        /// Stripe API version to use (e.g., "2023-10-16").
        /// Optional - uses Stripe's latest API version if not specified.
        /// </summary>
        /// <remarks>
        /// Stripe periodically releases new API versions with breaking changes.
        /// Pinning a version ensures consistent behavior.
        /// See: https://stripe.com/docs/api/versioning
        /// </remarks>
        public string? ApiVersion { get; set; }
    }
}
