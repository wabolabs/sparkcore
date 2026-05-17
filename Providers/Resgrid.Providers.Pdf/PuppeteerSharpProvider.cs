using System.Threading.Tasks;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using Resgrid.Model.Providers;

namespace Resgrid.Providers.PdfProvider
{
	public class PuppeteerSharpProvider : IPdfProvider
	{
		public byte[] ConvertHtmlToPdf(string html)
		{
			return ConvertHtmlToPdfAsync(html).GetAwaiter().GetResult();
		}

		private static async Task<byte[]> ConvertHtmlToPdfAsync(string html)
		{
			var launchOptions = new LaunchOptions
			{
				Headless = true,
				ExecutablePath = GetChromiumPath(),
				Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
			};

			await using var browser = await Puppeteer.LaunchAsync(launchOptions);
			await using var page = await browser.NewPageAsync();
			await page.SetContentAsync(html, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });

			return await page.PdfDataAsync(new PdfOptions
			{
				Format = PaperFormat.Letter,
				PrintBackground = true,
				MarginOptions = new MarginOptions { Top = "0.5in", Bottom = "0.5in", Left = "0.5in", Right = "0.5in" }
			});
		}

		private static string GetChromiumPath()
		{
			var configPath = Config.PrintConfig.ChromiumExecutablePath;
			return string.IsNullOrWhiteSpace(configPath) ? null : configPath;
		}
	}
}
