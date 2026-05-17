namespace Resgrid.Config
{
	/// <summary>
	/// Configurations for managing printers and print services
	/// </summary>
	public static class PrintConfig
	{
		/// <summary>
		/// PrintNode (https://www.printnode.com/en/docs/api/curl) api url
		/// </summary>
		public static string PrintNodeBaseUrl = "https://api.printnode.com";

		/// <summary>
		/// Path to the Chromium/Chrome executable for PuppeteerSharp. Leave empty to use PuppeteerSharp's bundled revision.
		/// In Docker, set to /usr/bin/chromium or /usr/bin/google-chrome.
		/// </summary>
		public static string ChromiumExecutablePath = "";
	}
}
