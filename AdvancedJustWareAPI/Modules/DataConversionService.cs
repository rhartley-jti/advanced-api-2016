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
			//Disable
			_dataconversionClient.DisableAutoGeneration();
			Assert.IsFalse(_dataconversionClient.IsAutoGenerationEnabled(), "Autogeneration should be disabled");
			//Enable
			_dataconversionClient.EnableAutoGeneration();
			Assert.IsTrue(_dataconversionClient.IsAutoGenerationEnabled(), "Autogeneration should be enabled");
		}

		[TestMethod]
		public void UnableToFindCaseWithoutSummary()
		{
			try
			{
				//Submit case that the client will not find
				_dataconversionClient.DisableAutoGeneration();
				string newCaseID = _apiClient.SubmitCase().ID;
				Assert.IsNotNull(newCaseID, "No CaseID");
				//API will still find the case (The client has the issue)
				var cse = _apiClient.GetCase(newCaseID, null);
				Assert.IsNotNull(cse, $"Found case {newCaseID}");

				var findResults = _apiClient.FindCases($"ID = \"{newCaseID}\"", null);
				Assert.AreEqual(1, findResults.Count, $"Found case {newCaseID}");
			}
			finally
			{
				//Remember to enable in a finally
				_dataconversionClient.EnableAutoGeneration();
			}
		}

		[TestMethod]
		public void TriggerAutoGeneration()
		{
			//Triggering auto generation also enables (now the client will find the case from previous test)
			_dataconversionClient.TriggerAutoGeneration();
			Assert.IsTrue(_dataconversionClient.IsAutoGenerationEnabled(), "AutoGeneration not enabled");
		}

		[TestMethod]
		public void InsertingCasesFasterWithAutoGenerationDisabled()
		{
			const int NUMBER_OF_CASES = 100;

			try
			{
				// Time case creation with auto generation enabled
				Assert.IsTrue(_dataconversionClient.IsAutoGenerationEnabled(), "AutoGenerationEnabled");
				_logger.Info("Creating cases with auto generation enabled");
				double autoGenerationEnabledSeconds = _apiClient.CreateCases(NUMBER_OF_CASES);
				// Time case creation with auto generation disabled
				_dataconversionClient.DisableAutoGeneration();
				Assert.IsFalse(_dataconversionClient.IsAutoGenerationEnabled(), "!AutoGenerationEnabled");
				_logger.Info("Creating cases with auto generation disabled");
				double autoGenerationDisabledSeconds = _apiClient.CreateCases(NUMBER_OF_CASES);
				// Measure the difference
				Assert.IsTrue(autoGenerationDisabledSeconds < autoGenerationEnabledSeconds, "Disabling auto generation did not speed things up");
			}
			finally
			{
				// Remember to trigger in finally
				if (!_dataconversionClient.IsAutoGenerationEnabled())
				{
					_dataconversionClient.TriggerAutoGeneration();
				}
			}
		}
	}
}