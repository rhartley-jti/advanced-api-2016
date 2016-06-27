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
			// Create name
			var name = new Name().Initialize();
			List<Key> keys = _client.Submit(name);
			// Explore keys collection
			Assert.AreEqual(1, keys.Count, "Number of keys returned");
			Assert.AreEqual(nameof(Name), keys[0].TypeName, "Type name");
			Assert.IsTrue(keys[0].NewID > 0, "NewID > 0");
			Assert.IsNull(keys[0].NewCaseID, "NewCaseID");
		}

		[TestMethod]
		public void AllEntitesCreatedDuringSubmitWillHaveAKeyReturned()
		{
			// Create a name with metadata
			var name = new Name()
				.Initialize()
				.AddAddress()
				.AddEmail()
				.AddEmail("api@journaltech.com")
				.AddPhone();
			List<Key> keys = _client.Submit(name);
			// Explore the keys collection
			Assert.AreEqual(5, keys.Count, "Number of keys");
			Assert.AreEqual(1, keys.Count(k => k.TypeName.Equals(nameof(Name))), "Number of name keys");
			Assert.AreEqual(1, keys.Count(k => k.TypeName.Equals(nameof(Phone))), "Number of phone keys");
			Assert.AreEqual(2, keys.Count(k => k.TypeName.Equals(nameof(Email))), "Number of email keys");
			Assert.AreEqual(1, keys.Count(k => k.TypeName.Equals(nameof(Address))), "Number of address keys");
		}

		[TestMethod]
		public void CaseEntitiesUseNewCaseID()
		{
			// Create a case
			var cse = new Case().Initialize();
			List<Key> keys = _client.Submit(cse);
			// Explore the keys collection
			Assert.AreEqual(2, keys.Count, "Number of keys returned");
			Key caseKey = keys.FirstOrDefault(k => k.TypeName.Equals(nameof(Case)));
			Assert.IsNotNull(caseKey, "No case key");
			Assert.AreEqual(default(int), caseKey.NewID, "NewID");
			Assert.IsNotNull(caseKey.NewCaseID, "NewCaseID");
		}

		[TestMethod]
		public void TempIDIsForYou()
		{
			// Create name with metadata and tempid
			const string MY_TEMP_ID = "TMP-1";
			var name = new Name()
				.Initialize()
				.AddEmail()
				.AddEmail("api@journaltech.com", tempID: MY_TEMP_ID)
				.AddEmail()
				.AddPhone()
				.AddPhone("877-587-8927", tempID: MY_TEMP_ID)
				.AddPhone();
			List<Key> keys = _client.Submit(name);
			// Explore the keys collection
			Assert.AreEqual(7, keys.Count, "Total number of keys returned");
			Assert.AreEqual(2, keys.Count(k => k.TempID != null && k.TempID.Equals(MY_TEMP_ID)), "Number of keys with my temp id");
		}
	}
}