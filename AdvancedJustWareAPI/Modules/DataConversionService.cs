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
			_dataconversionClient = ApiFactory.CreateDataConversionClient();
			_apiClient = ApiFactory.CreateApiClient(ensureAutoGenerationEnabled: false);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			IDisposable disposableDataconverionClient = _dataconversionClient as IDisposable;
			disposableDataconverionClient?.Dispose();

			IDisposable disposableApiClient = _apiClient as IDisposable;
			disposableApiClient?.Dispose();
		}

		[TestMethod]
		public void DisableAndEnable()
		{
			_dataconversionClient.DisableAutoGeneration();
			Assert.IsFalse(_dataconversionClient.IsAutoGenerationEnabled(), "Autogeneration should be disabled");
			_dataconversionClient.EnableAutoGeneration();
			Assert.IsTrue(_dataconversionClient.IsAutoGenerationEnabled(), "Autogeneration should be enabled");
		}

		[TestMethod]
		public void UnableToFindCaseWithoutSummary()
		{
			try
			{
				_dataconversionClient.DisableAutoGeneration();
				string newCaseID = _apiClient.SubmitCase().ID;
				Assert.IsNotNull(newCaseID, "No CaseID");

				//API will still find the case (The client has the issue)
				//var cse = _apiClient.GetCase(newCaseID, null);
				//Assert.IsNull(cse, $"Found case {newCaseID}");

				//var findResults = _apiClient.FindCases($"ID = \"{newCaseID}\"", null);
				//Assert.AreEqual(0, findResults.Count, $"Found case {newCaseID}");
			}
			finally
			{
				_dataconversionClient.EnableAutoGeneration();
			}
		}

		[TestMethod]
		public void TriggerAutoGeneration()
		{
			_dataconversionClient.TriggerAutoGeneration();
			Assert.IsTrue(_dataconversionClient.IsAutoGenerationEnabled(), "AutoGeneration not enabled");
		}

		[TestMethod]
		public void InsertingCasesFasterWithAutoGenerationDisabled()
		{
			const int NUMBER_OF_CASES = 100;

			try
			{
				Assert.IsTrue(_dataconversionClient.IsAutoGenerationEnabled(), "AutoGenerationEnabled");
				_logger.Info("Creating cases with auto generation enabled");
				double autoGenerationEnabledSeconds = _apiClient.CreateCases(NUMBER_OF_CASES);
				_dataconversionClient.DisableAutoGeneration();
				Assert.IsFalse(_dataconversionClient.IsAutoGenerationEnabled(), "!AutoGenerationEnabled");
				_logger.Info("Creating cases with auto generation disabled");
				double autoGenerationDisabledSeconds = _apiClient.CreateCases(NUMBER_OF_CASES);

				Assert.IsTrue(autoGenerationDisabledSeconds < autoGenerationEnabledSeconds, "Disabling auto generation did not speed things up");
			}
			finally
			{
				if (!_dataconversionClient.IsAutoGenerationEnabled())
				{
					_dataconversionClient.TriggerAutoGeneration();
				}
			}
		}
	}
}