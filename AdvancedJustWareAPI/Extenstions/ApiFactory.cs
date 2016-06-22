using System.Net;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.api.extra;

namespace AdvancedJustWareAPI.Extenstions
{
	public static class ApiFactory
	{
		private static bool _securitySetup;

		private static void ConfigureSecurity()
		{
			ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
			_securitySetup = true;
		}

		public static IJustWareApi CreateApiClient(string username = @"dev\user", string password = "Training2016")
		{
			ConfigureSecurity();
			var client = new JustWareApiClient();
			client.ClientCredentials.UserName.UserName = username;
			client.ClientCredentials.UserName.Password = password;

			return client;
		}

		public static IDataConversionService CreateDataConversionClient(string username = @"dev\admin", string password = "Training2016")
		{
			ConfigureSecurity();
			var client = new DataConversionServiceClient();
			client.ClientCredentials.UserName.UserName = username;
			client.ClientCredentials.UserName.Password = password;

			return client;
		}
	}
}