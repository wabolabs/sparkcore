using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Resgrid.Framework;
using Resgrid.Model;
using Resgrid.Model.Providers;
using RestSharp;
using RestSharp.Authenticators;

namespace Resgrid.Providers.NumberProvider
{
	public class BulkVSNumberProvider : INumberProvider
	{
		private RestClient MakeClient() =>
			new(new RestClientOptions(Config.NumberProviderConfig.BulkVSBaseUrl)
			{
				Authenticator = new HttpBasicAuthenticator(
					Config.NumberProviderConfig.BulkVSApiUsername,
					Config.NumberProviderConfig.BulkVSApiPassword)
			});

		public async Task<List<TextNumber>> GetAvailableNumbers(string country, string areaCode)
		{
			try
			{
				var request = new RestRequest("/did/search");
				request.AddQueryParameter("npa", areaCode);
				request.AddQueryParameter("quantity", "20");
				var response = await MakeClient().ExecuteAsync<List<BulkVSDidResult>>(request);
				return response.Data?.Select(d => new TextNumber { Number = d.Did }).ToList()
				       ?? new List<TextNumber>();
			}
			catch (Exception ex)
			{
				Logging.LogException(ex);
				return new List<TextNumber>();
			}
		}

		public async Task<bool> ProvisionNumber(string country, string number)
		{
			try
			{
				var request = new RestRequest("/did/purchaseDid", Method.Post);
				request.AddJsonBody(new { Did = number });
				var response = await MakeClient().ExecuteAsync(request);
				return response.IsSuccessful;
			}
			catch (Exception ex)
			{
				Logging.LogException(ex);
				return false;
			}
		}

		public string ConvertCountryToCode(string country) =>
			country?.ToLower() switch
			{
				"canada" => "CA",
				_ => "US"
			};
	}

	internal class BulkVSDidResult
	{
		public string Did { get; set; }
	}
}
