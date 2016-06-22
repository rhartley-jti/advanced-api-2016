using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdvancedJustWareAPI.Modules
{
	[TestClass]
	public class Keys
	{
		private IJustWareApi _client;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiClientFactory.CreateApiClient();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_client.Dispose();
		}

		[TestMethod]
		public void AllEntitiesButCaseUseNewID()
		{
			// Create name (k1)
			
			// Explore keys collection (k2)
			
		}

		[TestMethod]
		public void AllEntitesCreatedDuringSubmitWillHaveAKeyReturned()
		{
			// Create a name with metadata (k3)
			
			// Explore the keys collection (k4)
			
		}

		[TestMethod]
		public void CaseEntitiesUseNewCaseID()
		{
			// Create a case (k5)
			
			// Explore the keys collection (k6)
			
		}

		[TestMethod]
		public void TempIDIsForYou()
		{
			// Create name with metadata and tempid (k7)
			
			// Explore the keys collection (k8)
			
		}
	}
}