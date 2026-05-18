namespace Resgrid.Config
{
	public static class SipTrunkConfig
	{
		public static string SipDomain     = "";
		public static string SipServer     = "";
		public static int    SipPort       = 5060;
		public static string SipUsername   = "";
		public static string SipPassword   = "";
		public static string SipFromNumber = "";

		// ── Provider presets ─────────────────────────────────────────────────────
		// Set matching fields in ResgridConfig.json under keys like "SipTrunkConfig.SipServer".
		//
		// BulkVS  (https://portal.bulkvs.com)
		//   SipTrunkConfig.SipDomain     = "sip.bulkvs.com"
		//   SipTrunkConfig.SipServer     = "sip.bulkvs.com"
		//   SipTrunkConfig.SipPort       = 5060
		//   SipTrunkConfig.SipUsername   = "+1XXXXXXXXXX"    (DID in E.164)
		//   SipTrunkConfig.SipPassword   = "<trunk-password>"
		//   SipTrunkConfig.SipFromNumber = "+1XXXXXXXXXX"
		//
		// Voip.ms  (https://voip.ms — substitute nearest PoP)
		//   PoP options: chicago1, dallas, seattle, toronto, amsterdam, london, …
		//   SipTrunkConfig.SipDomain     = "chicago1.voip.ms"
		//   SipTrunkConfig.SipServer     = "chicago1.voip.ms"
		//   SipTrunkConfig.SipPort       = 5060
		//   SipTrunkConfig.SipUsername   = "<account>_<subaccount>"
		//   SipTrunkConfig.SipPassword   = "<subaccount-password>"
		//   SipTrunkConfig.SipFromNumber = "+1XXXXXXXXXX"
		//
		// Anveo Direct  (https://www.anveo.com/direct-sip.html)
		//   SipTrunkConfig.SipDomain     = "direct-sip.anveo.com"
		//   SipTrunkConfig.SipServer     = "direct-sip.anveo.com"
		//   SipTrunkConfig.SipPort       = 5060
		//   SipTrunkConfig.SipUsername   = "<anveo-account-id>"
		//   SipTrunkConfig.SipPassword   = "<sip-password>"
		//   SipTrunkConfig.SipFromNumber = "+1XXXXXXXXXX"
	}
}
