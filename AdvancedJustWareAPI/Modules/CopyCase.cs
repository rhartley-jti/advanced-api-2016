using System.Collections.Generic;
using System.Linq;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

namespace AdvancedJustWareAPI.Modules
{
	[TestClass]
	public class CopyCase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private IJustWareApi _client;
		private string _caseID;
		private int _nameID;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiClientFactory.CreateApiClient();
			_caseID = _client.SubmitCase().ID;
			_nameID = _client.SubmitName().ID;
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_client.Dispose();
		}

		[TestMethod]
		public void MinimumCopy()
		{
			//Setup parameters and copy case (cc1)

			//Check results (cc2)
		}

		[TestMethod]
		public void CopyAndRelate()
		{
			//Find codes needed to relate (cc3)

			//Setup parameters and copy case (cc4)
			
			//Check results (cc5)
		}

		[TestMethod]
		public void CopyCaseHasBetterPerformance()
		{
			// Create 100 cases and measure time (cc6)
			
			// Copy 100 cases and measure time (cc7)
			
			// Compare speeds (cc8)
		}
	}
}