using System;
using System.Net.Mail;
using System.Threading.Tasks;
using MailKit.Security;
using MimeKit;
using Resgrid.Config;
using Resgrid.Framework;
using Resgrid.Model;
using Resgrid.Model.Providers;

namespace Resgrid.Providers.EmailProvider
{
	public class SmtpEmailSender : IEmailSender
	{
		public async Task<bool> Send(Email email)
		{
			try
			{
				var message = new MimeMessage();
				message.From.Add(new MailboxAddress(string.Empty, email.From));

				foreach (var to in email.To)
					message.To.Add(MailboxAddress.Parse(to));
				foreach (var cc in email.CC)
					message.Cc.Add(MailboxAddress.Parse(cc));
				foreach (var bcc in email.Bcc)
					message.Bcc.Add(MailboxAddress.Parse(bcc));

				message.Subject = email.Subject;

				var builder = new BodyBuilder();
				if (!string.IsNullOrEmpty(email.HtmlBody))
					builder.HtmlBody = email.HtmlBody;
				if (!string.IsNullOrEmpty(email.TextBody))
					builder.TextBody = email.TextBody;
				if (!string.IsNullOrEmpty(email.AttachmentName) && email.AttachmentData?.Length > 0)
					builder.Attachments.Add(email.AttachmentName, email.AttachmentData,
						ContentType.Parse(email.AttachmentContentType ?? "application/octet-stream"));

				message.Body = builder.ToMessageBody();

				await SendMimeMessageAsync(message);
				return true;
			}
			catch (Exception ex)
			{
				Logging.LogException(ex);
				return false;
			}
		}

		public async Task<bool> SendEmail(MailMessage email)
		{
			try
			{
				var message = new MimeMessage();
				message.From.Add(MailboxAddress.Parse(email.From.Address));
				foreach (MailAddress to in email.To)
					message.To.Add(MailboxAddress.Parse(to.Address));

				message.Subject = email.Subject;

				var builder = new BodyBuilder();
				if (email.IsBodyHtml)
					builder.HtmlBody = email.Body;
				else
					builder.TextBody = email.Body;
				message.Body = builder.ToMessageBody();

				await SendMimeMessageAsync(message);
				return true;
			}
			catch (Exception ex)
			{
				Logging.LogException(ex);
				return false;
			}
		}

		public MailMessage CreateMailMessageFromEmail(Email email) => null;

		private static async Task SendMimeMessageAsync(MimeMessage message)
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
		}
	}
}
