using System;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.api.extra;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

namespace AdvancedJustWareAPI.Modules
{
	[TestClass]
	public class DataConversionService
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private IDataConversionService _dataconversionClient;
		private IJustWareApi _apiClient;

		[TestInitialize]
		public void Initialize()
		{
			_dataconversionClient = ApiClientFactory.CreateDataConversionClient();
			_apiClient = ApiClientFactory.CreateApiClient(ensureAutoGenerationEnabled: false);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_dataconversionClient.Dispose();
			_apiClient.Dispose();
		}

		[TestMethod]
		public void DisableAndEnable()
		{
			//Disable (dcs1)
			
			//Enable (dcs2)
		}

		[TestMethod]
		public void UnableToFindCaseWithoutSummary()
		{
			try
			{
				//Submit case that the client will not find (dcs3)
				
				//API will still find the case (The client has the issue) (dcs4)
				
			}
			finally
			{
				//Remember to enable in a finally (dcs5)
				
			}
		}

		[TestMethod]
		public void TriggerAutoGeneration()
		{
			//Triggering auto generation also enables (now the client will find the case from previous test) (dcs6)
			
		}

		[TestMethod]
		public void InsertingCasesFasterWithAutoGenerationDisabled()
		{
			const int NUMBER_OF_CASES = 100;

			try
			{
				// Time case creation with auto generation enabled (dcs7)
				
				// Time case creation with auto generation disabled (dcs8)
				
				// Measure the difference (dcs9)
				
			}
			finally
			{
				// Remember to trigger in finally (dcs10)
				
			}
		}
	}
}