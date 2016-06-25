using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.api.extra;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdvancedJustWareAPI
{
	[TestClass]
	public class Environment
	{
		private IJustWareApi _apiClient;

		[TestInitialize]
		public void Initialize()
		{
			_apiClient = ApiFactory.CreateApiClient();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			IDisposable disposable = _apiClient as IDisposable;
			disposable?.Dispose();
		}

		[TestMethod]
		public void CurrentUserNameID()
		{
			int currentNameID = _apiClient.GetCallerNameID();
			Assert.IsTrue(currentNameID > 0, "NameID was not greater than zero");
		}

		[TestMethod]
		public void CurrentUserNameEntity()
		{
			Name currentName = _apiClient.GetName(_apiClient.GetCallerNameID(), null);
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
			List<Key> keys = _apiClient.Submit(newName);

			Assert.AreEqual(1, keys.Count, "Expected one key");
			Assert.AreEqual("Name", keys[0].TypeName, "Key TypeName");
			Assert.IsTrue(keys[0].NewID > 0, "Key NewID not greater than zero");
		}

		[TestMethod]
		public void FindName()
		{
			List<Name> results = _apiClient.FindNames("Last = \"JustWare User\"", null);
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
			Case actualCase = _apiClient.SubmitCase(cse);
			Assert.IsNotNull(actualCase.ID, "No Case");

			string actualDocumentContents = _apiClient.DownloadFromApi(document);

			Assert.AreEqual(TEST_DOCUMENT, actualDocumentContents, "Document contents");
		}

		[TestMethod]
		public void IsAutoGenerationEnabled()
		{
			IDataConversionService dataConvertionClient = ApiFactory.CreateDataConversionClient();
			bool result = dataConvertionClient.IsAutoGenerationEnabled();
			Assert.IsTrue(result, "AutoGeneration is not enabled");
		}
	}
}