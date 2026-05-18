using System;
using System.Threading.Tasks;
using MailKit.Security;
using MimeKit;
using Resgrid.Config;
using Resgrid.Framework;
using Resgrid.Model.Providers;

namespace Resgrid.Providers.EmailProvider
{
	public class AmazonEmailSender : IAmazonEmailSender
	{
		public async Task<bool> SendDistributionListEmail(MimeMessage message)
		{
			try
			{
				using var client = new MailKit.Net.Smtp.SmtpClient();

				var ssl = OutboundEmailServerConfig.EnableSsl
					? SecureSocketOptions.SslOnConnect
					: SecureSocketOptions.StartTlsWhenAvailable;

				await client.ConnectAsync(OutboundEmailServerConfig.Host, OutboundEmailServerConfig.Port, ssl);

				if (!string.IsNullOrWhiteSpace(OutboundEmailServerConfig.UserName) &&
				    !string.IsNullOrWhiteSpace(OutboundEmailServerConfig.Password))
					await client.AuthenticateAsync(OutboundEmailServerConfig.UserName, OutboundEmailServerConfig.Password);

				await client.SendAsync(message);
				await client.DisconnectAsync(true);

				return true;
			}
			catch (Exception ex)
			{
				Logging.LogException(ex);
				return false;
			}
		}
	}
}
