﻿using System;
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
		private AddressType _addressType;
		private EmailType _emailType;
		private PhoneType _phoneType;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiFactory.CreateApiClient();
			_addressType = _client.GetCode<AddressType>();
			Assert.IsNotNull(_addressType, "No address types");
			_emailType = _client.GetCode<EmailType>();
			Assert.IsNotNull(_emailType, "No email types");
			_phoneType = _client.GetCode<PhoneType>();
			Assert.IsNotNull(_phoneType, "No phone types");
		}

		[TestCleanup]
		public void TestCleanup()
		{
			IDisposable disposable = _client as IDisposable;
			disposable?.Dispose();
		}

		[TestMethod]
		public void AllEntitiesButCaseUseNewID()
		{
			var name = new Name().Initialize();

			List<Key> keys =_client.Submit(name);

			Assert.AreEqual(1, keys.Count, "Number of keys returned");
			Assert.AreEqual(nameof(Name), keys[0].TypeName, "Type name");
			Assert.IsTrue(keys[0].NewID > 0, "NewID > 0");
			Assert.IsNull(keys[0].NewCaseID, "NewCaseID");
		}

		[TestMethod]
		public void AllEntitesCreatedDuringSubmitWillHaveAKeyReturned()
		{
			var name = new Name()
				.Initialize()
				.AddAddress(_addressType)
				.AddEmail(_emailType)
				.AddEmail(_emailType, "api@journaltech.com")
				.AddPhone(_phoneType);

			List<Key> keys = _client.Submit(name);

			Assert.AreEqual(5, keys.Count, "Number of keys");
			Assert.AreEqual(1, keys.Count(k => k.TypeName.Equals(nameof(Name))), "Number of name keys");
			Assert.AreEqual(1, keys.Count(k => k.TypeName.Equals(nameof(Phone))), "Number of phone keys");
			Assert.AreEqual(2, keys.Count(k => k.TypeName.Equals(nameof(Email))), "Number of email keys");
			Assert.AreEqual(1, keys.Count(k => k.TypeName.Equals(nameof(Address))), "Number of address keys");
		}

		[TestMethod]
		public void CaseEntitiesUseNewCaseID()
		{
			var cse = new Case().Initialize();

			List<Key> keys = _client.Submit(cse);

			Assert.AreEqual(2, keys.Count, "Number of keys returned");
			Key caseKey = keys.FirstOrDefault(k => k.TypeName.Equals(nameof(Case)));
			Assert.IsNotNull(caseKey, "No case key");
			Assert.AreEqual(default(int), caseKey.NewID, "NewID");
			Assert.IsNotNull(caseKey.NewCaseID, "NewCaseID");
		}

		[TestMethod]
		public void TempIDIsForYou()
		{
			const string MY_TEMP_ID = "TMP-1";
			var name = new Name()
				.Initialize()
				.AddEmail(_emailType)
				.AddEmail(_emailType, "api@journaltech.com", tempID: MY_TEMP_ID)
				.AddEmail(_emailType)
				.AddPhone(_phoneType)
				.AddPhone(_phoneType, "877-587-8927", tempID: MY_TEMP_ID)
				.AddPhone(_phoneType);

			List<Key> keys = _client.Submit(name);

			Assert.AreEqual(7, keys.Count, "Total number of keys returned");
			Assert.AreEqual(2, keys.Count(k => k.TempID != null && k.TempID.Equals(MY_TEMP_ID)), "Number of keys with my temp id");
		}
	}
}