using System;
using System.Collections.Generic;
using System.Net;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdvancedJustWareAPI.Modules
{
	[TestClass]
	public class Documents
	{
		private const string DOWNLOAD_DATA = "Download Test";
		private IJustWareApi _client;
		private Case _case;
		private CaseDocument _existingDocument;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiClientFactory.CreateApiClient();
			_existingDocument = new CaseDocument().Initialize(DOWNLOAD_DATA);
			var tmpCase = new Case()
				.Initialize()
				.AddDocument(_existingDocument);
			_case = _client.SubmitCase(tmpCase);
			Assert.IsNotNull(_case, "Case was not created");
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_client.Dispose();
		}

		[TestMethod]
		public void CreateCaseDocument()
		{
			// Create CaseDocument (d1)
			
			// Request upload url (d2)
			
			// Finalize upload (d3)
			
			// Find the document (d4)
			
		}

		[TestMethod]
		public void DownloadCaseDocument()
		{
			// Get existing document (d5)

			// Create a WebClient (d6)
			
		}

		[TestMethod]
		public void ModifyCaseDocument()
		{
			// Change an existing document (d10)
			
			// Request file upload (d11)
			
			// Upload file (d12)
			
			// Finalize upload (d13)
			
			// Check the update (d14)
			
		}
	}
}