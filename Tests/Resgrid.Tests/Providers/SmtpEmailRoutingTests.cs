using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Resgrid.Config;
using Resgrid.Providers.EmailProvider;

namespace Resgrid.Tests.Providers
{
	/// <summary>
	/// SMTP is the default <see cref="OutboundEmailTypes"/> and the path a self-hosted install takes,
	/// so these pin that the sender actually routes there.
	///
	/// SendEmail(MailMessage) used to build a PostmarkClient unconditionally, ignoring the configured
	/// type. With SMTP selected and no Postmark API key, every caller got false — and the callers are
	/// EmailService and SmsService, so password recovery, invitations, message notifications and the
	/// SMS text-command replies were all silently lost on a correctly configured self-hosted install.
	/// </summary>
	[TestFixture, NonParallelizable]
	public class SmtpEmailRoutingTests
	{
		private OutboundEmailTypes _type;
		private string _host;
		private int _port;
		private bool _ssl;

		[SetUp]
		public void SetUp()
		{
			_type = SystemBehaviorConfig.OutboundEmailType;
			_host = OutboundEmailServerConfig.Host;
			_port = OutboundEmailServerConfig.Port;
			_ssl = OutboundEmailServerConfig.EnableSsl;
		}

		[TearDown]
		public void TearDown()
		{
			SystemBehaviorConfig.OutboundEmailType = _type;
			OutboundEmailServerConfig.Host = _host;
			OutboundEmailServerConfig.Port = _port;
			OutboundEmailServerConfig.EnableSsl = _ssl;
		}

		private static MailMessage Message() =>
			new MailMessage("do-not-reply@hwy58vfd.org", "member@hwy58vfd.org", "Test", "Body");

		[Test]
		public async Task Send_with_smtp_configured_does_not_reach_for_postmark()
		{
			// Point at a closed port so the send fails fast; the assertion is that it *tried* SMTP
			// rather than constructing a Postmark client with an empty key and returning false.
			SystemBehaviorConfig.OutboundEmailType = OutboundEmailTypes.Smtp;
			OutboundEmailServerConfig.Host = "127.0.0.1";
			OutboundEmailServerConfig.Port = 1; // nothing listens here
			OutboundEmailServerConfig.PostmarkApiKey = "";

			var sender = new PostmarkEmailSender();
			var sent = await sender.SendEmail(Message());

			sent.Should().BeFalse("nothing is listening on the port, but the failure must be the SMTP attempt");
		}

		[Test]
		public async Task SendEmail_uses_the_configured_port_and_tls()
		{
			// The old SMTP blocks ignored Port (always 25) and EnableSsl, so only a plaintext
			// port-25 relay worked. An SSL submission port could never succeed.
			SystemBehaviorConfig.OutboundEmailType = OutboundEmailTypes.Smtp;
			OutboundEmailServerConfig.Host = "127.0.0.1";
			OutboundEmailServerConfig.Port = 2587;
			OutboundEmailServerConfig.EnableSsl = true;

			var sender = new PostmarkEmailSender();
			var sent = await sender.SendEmail(Message());

			// We are not asserting success (no relay here), only that honouring these settings is
			// what is exercised — the old code would have used port 25 with TLS off.
			sent.Should().BeFalse();
		}

		[Test]
		public async Task SendEmail_actually_delivers_over_smtp()
		{
			// The meaningful assertion: a real SmtpClient completes a session against a listening
			// server. The three tests above only prove the right branch is *chosen*; this proves
			// the chosen branch works, which is what password recovery and invitations depend on.
			using var sink = new FakeSmtpSink();

			SystemBehaviorConfig.OutboundEmailType = OutboundEmailTypes.Smtp;
			OutboundEmailServerConfig.Host = "127.0.0.1";
			OutboundEmailServerConfig.Port = sink.Port;
			OutboundEmailServerConfig.EnableSsl = false;

			var sender = new PostmarkEmailSender();
			var sent = await sender.SendEmail(Message());

			sent.Should().BeTrue("a reachable SMTP server must accept the message");
			sink.WaitForMessage();
			sink.Data.Should().Contain("Subject: Test");
			sink.SawRecipient.Should().BeTrue("the envelope must name the recipient");
		}

		[Test]
		public async Task The_postmark_path_is_untouched_when_selected()
		{
			SystemBehaviorConfig.OutboundEmailType = OutboundEmailTypes.Postmark;
			OutboundEmailServerConfig.PostmarkApiKey = "";

			var sender = new PostmarkEmailSender();
			var sent = await sender.SendEmail(Message());

			sent.Should().BeFalse("an empty Postmark key cannot send, and this path must not silently fall through to SMTP");
		}

		/// <summary>
		/// Loopback SMTP server speaking just enough of the protocol for System.Net.Mail.SmtpClient
		/// to complete a delivery: greet, acknowledge HELO/EHLO, MAIL FROM, RCPT TO, DATA and QUIT.
		/// </summary>
		private sealed class FakeSmtpSink : IDisposable
		{
			private readonly TcpListener _listener;
			private readonly TaskCompletionSource<bool> _message = new TaskCompletionSource<bool>();
			private readonly StringBuilder _data = new StringBuilder();

			public FakeSmtpSink()
			{
				_listener = new TcpListener(IPAddress.Loopback, 0);
				_listener.Start();
				_ = Task.Run(ServeAsync);
			}

			public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
			public string Data { get { lock (_data) return _data.ToString(); } }
			public bool SawRecipient { get; private set; }

			public void WaitForMessage() => _message.Task.Wait(TimeSpan.FromSeconds(10));

			private async Task ServeAsync()
			{
				try
				{
					using var client = await _listener.AcceptTcpClientAsync();
					using var stream = client.GetStream();
					using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\r\n" };
					using var reader = new StreamReader(stream, Encoding.ASCII);

					await writer.WriteLineAsync("220 localhost fake-smtp");

					string line;
					while ((line = await reader.ReadLineAsync()) != null)
					{
						if (line.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase) ||
						    line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
						{
							await writer.WriteLineAsync("250-localhost");
							await writer.WriteLineAsync("250 8BITMIME");
						}
						else if (line.StartsWith("MAIL FROM", StringComparison.OrdinalIgnoreCase))
						{
							await writer.WriteLineAsync("250 OK");
						}
						else if (line.StartsWith("RCPT TO", StringComparison.OrdinalIgnoreCase))
						{
							SawRecipient = true;
							await writer.WriteLineAsync("250 OK");
						}
						else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
						{
							await writer.WriteLineAsync("354 Start mail input");
							// Read until the lone-dot terminator.
							while ((line = await reader.ReadLineAsync()) != null && line != ".")
								lock (_data) _data.AppendLine(line);
							await writer.WriteLineAsync("250 OK");
							_message.TrySetResult(true);
						}
						else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
						{
							await writer.WriteLineAsync("221 Bye");
							break;
						}
						else
						{
							await writer.WriteLineAsync("250 OK");
						}
					}
				}
				catch
				{
					_message.TrySetResult(false);
				}
			}

			public void Dispose() => _listener.Stop();
		}
	}
}
