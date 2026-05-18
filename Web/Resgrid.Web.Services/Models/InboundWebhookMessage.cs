using System.Collections.Generic;

namespace Resgrid.Web.Services.Models
{
	/// <summary>
	/// Generic inbound email webhook payload — matches the Postmark inbound webhook JSON shape
	/// so existing integrations continue to work, without a dependency on the Postmark SDK.
	/// </summary>
	public class InboundWebhookMessage
	{
		public string MessageID { get; set; }
		public string From { get; set; }
		public InboundEmailAddress FromFull { get; set; }
		public string Subject { get; set; }
		public string HtmlBody { get; set; }
		public string TextBody { get; set; }
		public List<InboundEmailAddress> ToFull { get; set; }
		public List<InboundEmailAddress> CcFull { get; set; }
		public List<InboundEmailAddress> BccFull { get; set; }
		public List<InboundEmailHeader> Headers { get; set; }
		public List<InboundEmailAttachment> Attachments { get; set; }
	}

	public class InboundEmailAddress
	{
		public string Name { get; set; }
		public string Email { get; set; }
	}

	public class InboundEmailHeader
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}

	public class InboundEmailAttachment
	{
		public string Name { get; set; }
		public string ContentType { get; set; }
		public string ContentID { get; set; }
		public string ContentLength { get; set; }
		public string Content { get; set; }
	}
}
