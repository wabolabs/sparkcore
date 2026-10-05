#pragma warning disable S2223 // Non-constant static fields should not be visible
#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable S1104 // Fields should not have public accessibility

namespace Resgrid.Config
{
	public static class OidcConfig
	{
		/// <summary>
		/// The underlying database engine for the OIDC database (Does not support Mongo)
		/// </summary>
		public static DatabaseTypes DatabaseType = DatabaseTypes.SqlServer;

		public static string Key = "";

		public static string ConnectionString = "";

		public static int AccessTokenExpiryMinutes = 1440;

		public static int RefreshTokenExpiryDays = 365;

		public static int NonMobileRefreshTokenExpiryDays = 2;

		/// <summary>
		/// Comma-separated, registered client IDs allowed to receive the longer mobile
		/// refresh-token lifetime. Anonymous requests and caller-supplied scopes never qualify.
		/// </summary>
		public static string TrustedLongLivedClientIds = "";

		public static string EncryptionCert = "";

		public static string SigningCert = "";

		/// <summary>
		/// Pins the token issuer (SparkOps fork). Empty keeps upstream behaviour: the
		/// issuer follows the host each request arrived on (forced to https by the API's
		/// scheme middleware), so a token minted through one address is reported
		/// inactive when introspected through another. Set this when clients reach the
		/// API by more than one address (loopback, a tailnet IP, a DNS name). The events
		/// service then validates against this issuer with a static configuration whose
		/// introspection endpoint is SystemBehaviorConfig.ResgridApiBaseUrl, instead of
		/// discovering one (discovery advertises https endpoints even on a plain-HTTP API).
		/// </summary>
		public static string Issuer = "";
	}
}

#pragma warning restore CA2211 // Non-constant fields should not be visible
#pragma warning restore S2223 // Non-constant static fields should not be visible
#pragma warning restore S1104 // Fields should not have public accessibility
