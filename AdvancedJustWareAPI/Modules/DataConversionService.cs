using System;
using System.Linq;
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
			_apiClient = ApiFactory.CreateApiClient();
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
				ApiCreateResult result = _apiClient.CreateCases(1);
				Assert.IsNotNull(result, "Case was not created");
				string newCaseID = result.GetFirstCaseID();
				Assert.IsNotNull(newCaseID, "No CaseID");
				_logger.Info("Case created: {0}", newCaseID);

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
				ApiCreateResult autoGenerationEnabled = _apiClient.CreateCases(NUMBER_OF_CASES);
				_dataconversionClient.DisableAutoGeneration();
				Assert.IsFalse(_dataconversionClient.IsAutoGenerationEnabled(), "!AutoGenerationEnabled");
				_logger.Info("Creating cases with auto generation disabled");
				ApiCreateResult autoGenerationDisabled = _apiClient.CreateCases(NUMBER_OF_CASES);

				Assert.IsTrue(autoGenerationDisabled.EllapsedSeconds < autoGenerationEnabled.EllapsedSeconds, "Disabling auto generation did not speed things up");
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