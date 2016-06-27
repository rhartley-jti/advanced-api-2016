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
			// Create CaseDocument
			DocumentType documentType = _client.GetCode<DocumentType>();
			var document = new CaseDocument
			{
				Operation = OperationType.Insert,
				FileName = "uploaded.txt",
				CaseID = _case.ID,
				TypeCode = documentType.Code,
				Notes = "Came from API"
			};
			// Request upload url
			string uploadUrl = _client.RequestFileUpload(document);
			uploadUrl.UploadToApi("This is a test document sent through the API");
			// Finalize upload
			_client.FinalizeFileUpload(document, uploadUrl);
			// Find the document
			string query = $"CaseID = \"{_case.ID}\" && FileName = \"uploaded.txt\"";
			List<CaseDocument> documents = _client.FindCaseDocuments(query, null);
			Assert.AreEqual(1, documents.Count, "Expected documents");
		}

		[TestMethod]
		public void DownloadCaseDocument()
		{
			// Get existing document
			CaseDocument document = _client.GetCaseDocument(_existingDocument.ID, null);
			// Create a WebClient
			using (WebClient webClient = new WebClient())
			{
				// Setup Credentials
				NetworkCredential networkCredential = new NetworkCredential
				{
					UserName = ApiClientFactory.TC_USER,
					Password = ApiClientFactory.TC_USER_PASSWORD
				};
				webClient.Credentials = networkCredential;
				// Download the file
				string downloadUrl = _client.RequestFileDownload(document);
				string fileResult = webClient.DownloadString(downloadUrl);
				// Check the document
				Assert.AreEqual(DOWNLOAD_DATA, fileResult, "Documents do not match");
			}
		}

		[TestMethod]
		public void ModifyCaseDocument()
		{
			// Change an existing document
			const string UPDATED_DATA = "Updated the document";
			_existingDocument.Operation = OperationType.Update;
			// Request file upload
			string uploadUrl = _client.RequestFileUpload(_existingDocument);
			// Upload file
			uploadUrl.UploadToApi(UPDATED_DATA);
			// Finalize upload
			_client.FinalizeFileUpload(_existingDocument, uploadUrl);
			// Check the update
			Assert.AreEqual(UPDATED_DATA, _client.DownloadFromApi(_existingDocument), "Did not update document");
		}
	}
}