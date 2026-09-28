using System;
using FluentAssertions;
using NUnit.Framework;
using Resgrid.Web.Services.Controllers.v4;

namespace Resgrid.Tests.CallNotes
{
	/// <summary>
	/// The v4 SaveCallNote endpoint stamped every note with the server's current
	/// time unless the caller's own time is honoured. These pin the parsing
	/// rules, including the one that matters most: a value with no offset must
	/// be read as UTC, never shifted into the server's local zone.
	/// </summary>
	[TestFixture, NonParallelizable]
	public class CallNoteTimestampTests
	{
		[Test]
		public void Omitted_timestamp_falls_back_to_now()
		{
			var before = DateTime.UtcNow;
			var result = CallNotesController.ResolveNoteTimestamp(null);
			var after = DateTime.UtcNow;

			result.Should().BeOnOrAfter(before.AddSeconds(-1)).And.BeOnOrBefore(after.AddSeconds(1));
			result.Kind.Should().Be(DateTimeKind.Utc);
		}

		[TestCase("")]
		[TestCase("   ")]
		public void Empty_or_whitespace_falls_back_to_now(string supplied)
		{
			CallNotesController.ResolveNoteTimestamp(supplied)
				.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
		}

		[Test]
		public void Roundtrip_utc_value_is_kept_verbatim()
		{
			CallNotesController.ResolveNoteTimestamp("2026-09-28T16:49:15Z")
				.Should().Be(new DateTime(2026, 9, 28, 16, 49, 15, DateTimeKind.Utc));
		}

		[Test]
		public void Offset_bearing_value_is_converted_to_utc()
		{
			// 12:44 EDT is 16:44 UTC. This is the seven-hour shift, in one line.
			CallNotesController.ResolveNoteTimestamp("2026-09-28T12:44:00-04:00")
				.Should().Be(new DateTime(2026, 9, 28, 16, 44, 0, DateTimeKind.Utc));
		}

		[Test]
		public void Naive_value_is_read_as_utc_and_not_shifted()
		{
			// A value with no offset must land on the same wall clock it names.
			// Reading it as local time would move it by the server's offset,
			// which is exactly the bug being fixed.
			CallNotesController.ResolveNoteTimestamp("2026-09-28T16:49:15")
				.Should().Be(new DateTime(2026, 9, 28, 16, 49, 15, DateTimeKind.Utc));
		}

		[Test]
		public void Unparseable_value_falls_back_to_now_rather_than_failing()
		{
			CallNotesController.ResolveNoteTimestamp("yesterday afternoon")
				.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
		}

		[Test]
		public void Future_value_is_clamped_to_now()
		{
			// Otherwise a note dated in the future sorts above every live note
			// forever, and the chronology is lost.
			CallNotesController.ResolveNoteTimestamp(DateTime.UtcNow.AddDays(30).ToString("o"))
				.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
		}
	}
}
