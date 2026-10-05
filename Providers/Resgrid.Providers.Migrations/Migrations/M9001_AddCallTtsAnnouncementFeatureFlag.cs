using FluentMigrator;

namespace Resgrid.Providers.Migrations.Migrations
{
	[Migration(9001)]
	public class M9001_AddCallTtsAnnouncementFeatureFlag : Migration
	{
		// Keep FlagKey in sync with Resgrid.Model.FeatureFlagKeys.CallTtsAnnouncement.
		private const string FlagKey = "Calls.TtsAnnouncement";

		public override void Up()
		{
			// Seeded OFF (IsEnabledGlobally = 0). The spoken call alert (A7) synthesizes the dispatch
			// announcement and attaches it to the call; it stays off until a department opts in with a
			// per-department override. FlagType, IsArchived, IsPermanent and CreatedOn fall back to
			// their table defaults.
			// Guarded with IF NOT EXISTS so re-running the migration does not violate the unique
			// FlagKey index.
			Execute.Sql(
				"IF NOT EXISTS (SELECT 1 FROM [FeatureFlags] WHERE [FlagKey] = '" + FlagKey + "') " +
				"INSERT INTO [FeatureFlags] ([FlagKey], [Name], [Description], [Category], [IsEnabledGlobally]) " +
				"VALUES ('" + FlagKey + "', " +
				"'Spoken Call Alert', " +
				"'When enabled, the dispatch announcement (nature, address, units) is synthesized and attached to each new call as audio.', " +
				"'Calls', 0);");
		}

		public override void Down()
		{
			Delete.FromTable("FeatureFlags").Row(new { FlagKey = FlagKey });
		}
	}
}
