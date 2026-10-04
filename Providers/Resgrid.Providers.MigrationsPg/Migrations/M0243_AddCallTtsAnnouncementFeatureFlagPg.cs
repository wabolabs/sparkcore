using FluentMigrator;

namespace Resgrid.Providers.MigrationsPg.Migrations
{
	[Migration(243)]
	public class M0243_AddCallTtsAnnouncementFeatureFlagPg : Migration
	{
		// Keep FlagKey in sync with Resgrid.Model.FeatureFlagKeys.CallTtsAnnouncement.
		private const string FlagKey = "Calls.TtsAnnouncement";

		public override void Up()
		{
			// Seeded OFF (isenabledglobally = false). The spoken call alert (A7) synthesizes the
			// dispatch announcement and attaches it to the call; it stays off until a department
			// opts in with a per-department override. flagtype, isarchived, ispermanent and
			// createdon fall back to their table defaults; the identity PK is omitted so
			// Postgres assigns it.
			// Guarded with WHERE NOT EXISTS so re-running the migration does not violate the unique
			// flagkey index.
			Execute.Sql(
				"INSERT INTO featureflags (flagkey, name, description, category, isenabledglobally) " +
				"SELECT '" + FlagKey + "', " +
				"'Spoken Call Alert', " +
				"'When enabled, the dispatch announcement (nature, address, units) is synthesized and attached to each new call as audio.', " +
				"'Calls', false " +
				"WHERE NOT EXISTS (SELECT 1 FROM featureflags WHERE flagkey = '" + FlagKey + "');");
		}

		public override void Down()
		{
			Delete.FromTable("FeatureFlags".ToLower()).Row(new { flagkey = FlagKey });
		}
	}
}
