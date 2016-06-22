using System;
using System.Collections.Generic;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.api.extra;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdvancedJustWareAPI
{
	[TestClass]
	public class Environment
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
		public void CurrentUserNameID()
		{
			int currentNameID = _client.GetCallerNameID();
			Assert.IsTrue(currentNameID > 0, "NameID was not greater than zero");
		}

		[TestMethod]
		public void CurrentUserNameEntity()
		{
			Name currentName = _client.GetName(_client.GetCallerNameID(), null);
			Assert.IsNotNull(currentName, "Name entity for current user not found");
			Assert.AreEqual("JustWare User", currentName.FullName, "Full Name");
		}

		[TestMethod]
		public void CreateNewName()
		{
			string lastName = Guid.NewGuid().ToString();
			Name newName = new Name
			{
				Operation = OperationType.Insert,
				Last = lastName
			};
			List<Key> keys = _client.Submit(newName);

			Assert.AreEqual(1, keys.Count, "Expected one key");
			Assert.AreEqual("Name", keys[0].TypeName, "Key TypeName");
			Assert.IsTrue(keys[0].NewID > 0, "Key NewID not greater than zero");
		}

		[TestMethod]
		public void FindName()
		{
			List<Name> results = _client.FindNames("Last = \"JustWare User\"", null);
			Assert.AreEqual(1, results.Count, "Did not find name");
		}

		[TestMethod]
		public void CreateNewCaseWithDocument()
		{
			const string TEST_DOCUMENT = "Test Document";
			CaseDocument document = new CaseDocument().Initialize(TEST_DOCUMENT);
			Case cse = new Case()
				.Initialize()
				.AddDocument(document);
			Case actualCase = _client.SubmitCase(cse);
			Assert.IsNotNull(actualCase.ID, "No Case");

			string actualDocumentContents = _client.DownloadFromApi(document);

			Assert.AreEqual(TEST_DOCUMENT, actualDocumentContents, "Document contents");
		}

		[TestMethod]
		public void IsAutoGenerationEnabled()
		{
			IDataConversionService client = null;
			try
			{
				client = ApiClientFactory.CreateDataConversionClient();
				bool result = client.IsAutoGenerationEnabled();
				Assert.IsTrue(result, "AutoGeneration is not enabled");
			}
			finally
			{
				client.Dispose();
			}
		}
	}
}