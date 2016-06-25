using System;
using System.Net;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.api.extra;

namespace AdvancedJustWareAPI.Extenstions
{
	public static class ApiFactory
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

		public static IJustWareApi CreateApiClient(string username = TC_USER, string password = TC_USER_PASSWORD, bool ensureAutoGenerationEnabled = true)
		{
			ConfigureSecurity();
			var client = new JustWareApiClient();
			client.ClientCredentials.UserName.UserName = username;
			client.ClientCredentials.UserName.Password = password;

			if (!ensureAutoGenerationEnabled) return client;

			IDataConversionService dsClient = null;
			try
			{
				dsClient = CreateDataConversionClient();
				dsClient.EnableAutoGeneration();
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