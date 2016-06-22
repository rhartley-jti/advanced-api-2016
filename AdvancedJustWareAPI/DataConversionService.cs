using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

namespace AdvancedJustWareAPI
{
	[TestClass]
	public class DataConversionService
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[TestMethod]
		public void InsertingCasesFasterWithAutoGenerationDisabled()
		{
			const int NUMBER_OF_CASES = 100;
			var dataconversionClient = ApiFactory.CreateDataConversionClient();
			var apiClient = ApiFactory.CreateApiClient();

			try
			{
				Assert.IsTrue(dataconversionClient.IsAutoGenerationEnabled(), "AutoGenerationEnabled");
				_logger.Info("Creating cases with auto generation enabled...");
				CreationResult autoGenerationEnabled = apiClient.CreateCases(NUMBER_OF_CASES);
				dataconversionClient.DisableAutoGeneration();
				Assert.IsFalse(dataconversionClient.IsAutoGenerationEnabled(), "!AutoGenerationEnabled");
				_logger.Info("Creating cases wit auto generation disabled...");
				CreationResult autoGenerationDisabled = apiClient.CreateCases(NUMBER_OF_CASES);

				Assert.IsTrue(autoGenerationDisabled.EllapsedSeconds < autoGenerationEnabled.EllapsedSeconds, "Disabling auto generation did not speed things up");
			}
			finally
			{
				if (!dataconversionClient.IsAutoGenerationEnabled())
				{
					dataconversionClient.TriggerAutoGeneration();
				}
			}
		}
	}
}