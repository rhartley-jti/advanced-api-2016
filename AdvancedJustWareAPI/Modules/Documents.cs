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
		private string _caseID;
		private CaseDocument _existingDocument;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiFactory.CreateApiClient();
			ApiCreateResult caseCreationResult = _client.CreateCases(1);
			_caseID = caseCreationResult.FirstCaseID;
			Assert.IsNotNull(_caseID, "Test case was not created");

			ApiCreateResult documentResult = _client.AddDocumentToCase(_caseID, DOWNLOAD_DATA, "download.txt");
			int? documentID = documentResult.GetFirstEntityID<CaseDocument>();
			Assert.IsTrue(documentID.HasValue, "No document");
			_existingDocument = _client.GetCaseDocument(documentID.Value, null);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			IDisposable disposable = _client as IDisposable;
			disposable?.Dispose();
		}

		[TestMethod]
		public void CreateCaseDocument()
		{
			DocumentType documentType = _client.GetCode<DocumentType>();
			
			var document = new CaseDocument
			{
				Operation = OperationType.Insert,
				FileName = "uploaded.txt",
				CaseID = _caseID,
				TypeCode = documentType.Code,
				Notes = "Came from API"
			};

			string uploadUrl = _client.RequestFileUpload(document);
			uploadUrl.UploadToApi("This is a test document sent through the API");

			_client.FinalizeFileUpload(document, uploadUrl);

			string query = $"CaseID = \"{_caseID}\" && FileName = \"uploaded.txt\"";
			List<CaseDocument> documents = _client.FindCaseDocuments(query, null);
			Assert.AreEqual(1, documents.Count, "Expected documents");
		}

		[TestMethod]
		public void DownloadCaseDocument()
		{
			// 1. Get the document you want to download (extracted to initialize)
			

			// 2. Create a new WebClient
			using (WebClient webClient = new WebClient())
			{
				// 3. Setup Credentials
				NetworkCredential networkCredential = new NetworkCredential
				{
					UserName = ApiFactory.TC_USER,
					Password = ApiFactory.TC_USER_PASSWORD
				};
				webClient.Credentials = networkCredential;

				// 4. Download the file
				string downloadUrl = _client.RequestFileDownload(_existingDocument);
				string fileResult = webClient.DownloadString(downloadUrl);

				Assert.AreEqual(DOWNLOAD_DATA, fileResult, "Documents do not match");
			}
		}

		[TestMethod]
		public void ModifyCaseDocument()
		{
			const string UPDATED_DATA = "Updated the document";

			_existingDocument.Operation = OperationType.Update;

			string uploadUrl = _client.RequestFileUpload(_existingDocument);

			uploadUrl.UploadToApi(UPDATED_DATA);
			_client.FinalizeFileUpload(_existingDocument, uploadUrl);

			Assert.AreEqual(UPDATED_DATA, _client.DownloadFromApi(_existingDocument), "Did not update document");
		}
	}
}