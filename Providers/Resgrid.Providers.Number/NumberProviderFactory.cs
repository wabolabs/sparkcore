using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Resgrid.Model;
using Resgrid.Model.Providers;

namespace Resgrid.Providers.NumberProvider
{
	public class NumberProviderFactory : INumberProvider
	{
		private readonly TwilioProvider _twilioProvider;
		private readonly NexmoProvider _nexmoProvider;
		private readonly BulkVSNumberProvider _bulkVsProvider;

		public NumberProviderFactory()
		{
			_twilioProvider = new TwilioProvider();
			_nexmoProvider = new NexmoProvider();
			_bulkVsProvider = new BulkVSNumberProvider();
		}

		public async Task<List<TextNumber>> GetAvailableNumbers(string country, string areaCode)
		{
			if (Config.SystemBehaviorConfig.SmsProviderType == Config.SmsProviderTypes.BulkVS)
				return await _bulkVsProvider.GetAvailableNumbers(country, areaCode);

			return await _twilioProvider.GetAvailableNumbers(country, areaCode);
		}

		public async Task<bool> ProvisionNumber(string country, string number)
		{
			if (Config.SystemBehaviorConfig.SmsProviderType == Config.SmsProviderTypes.BulkVS)
				return await _bulkVsProvider.ProvisionNumber(country, number);

			return await _twilioProvider.ProvisionNumber(country, number);
		}

		public string ConvertCountryToCode(string country)
		{
			if (Config.SystemBehaviorConfig.SmsProviderType == Config.SmsProviderTypes.BulkVS)
				return _bulkVsProvider.ConvertCountryToCode(country);

			return country switch
			{
				"United States" => "US",
				"United Kingdom" => "GB",
				"Australia" => "AU",
				"Canada" => "CA",
				"Ireland" => "IE",
				"New Zealand" => "NZ",
				_ => throw new Exception("Not supported country code for Twilio numbers.")
			};
		}
	}
}
