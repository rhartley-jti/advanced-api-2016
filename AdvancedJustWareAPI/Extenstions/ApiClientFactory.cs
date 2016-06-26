using System;
using System.Net;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.api.extra;

namespace AdvancedJustWareAPI.Extenstions
{
	public static class ApiClientFactory
	{
		public const string TC_USER = @"tc\user";
		public const string TC_USER_PASSWORD = "JustWare5";
		public const string TC_ADMIN = @"tc\admin";
		public const string TC_ADMIN_PASSWORD = "JustWare5";

		private static bool _securitySetup;

		private static void ConfigureSecurity()
		{
			if (_securitySetup) return;
			ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
			_securitySetup = true;
		}

		public static IJustWareApi CreateApiClient(bool admin = false, bool ensureAutoGenerationEnabled = true)
		{
			ConfigureSecurity();
			var client = new JustWareApiClient();
			client.ClientCredentials.UserName.UserName = admin ? TC_ADMIN : TC_USER;
			client.ClientCredentials.UserName.Password = admin ? TC_ADMIN_PASSWORD : TC_USER_PASSWORD;

			if (!ensureAutoGenerationEnabled) return client;

			IDataConversionService dsClient = null;
			try
			{
				dsClient = CreateDataConversionClient();
				if (!dsClient.IsAutoGenerationEnabled())
				{
					dsClient.TriggerAutoGeneration();
				}
			}
			finally
			{
				IDisposable disposable = dsClient as IDisposable;
				disposable?.Dispose();
			}

			return client;
		}

		public static IDataConversionService CreateDataConversionClient(string username = TC_ADMIN, string password = TC_ADMIN_PASSWORD)
		{
			ConfigureSecurity();
			var client = new DataConversionServiceClient();
			client.ClientCredentials.UserName.UserName = username;
			client.ClientCredentials.UserName.Password = password;

			return client;
		}
	}
}