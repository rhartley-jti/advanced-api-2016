using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdvancedJustWareAPI.Modules
{
	[TestClass]
	public class CaseNumbers
	{
		private IJustWareApi _client;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiFactory.CreateApiClient();
		}

		[TestCleanup]
		public void TestCleanup()
		{
			IDisposable disposable = _client as IDisposable;
			disposable?.Dispose();
		}

		[TestMethod]
		public void AddNumberToCase()
		{
			AgencyType agencyType = _client.GetCode<AgencyType>("MasterCode = 3");
			Assert.IsNotNull(agencyType, "Agency type not found");
			NumberType numberType = _client.GetCode<NumberType>("MasterCode = 3");
			Assert.IsNotNull(numberType, "Number type not found");

			Agency agency = new Agency
			{
				Operation = OperationType.Insert,
				AgencyCode = agencyType.Code,
				NumberTypeCode = numberType.Code,
				Number = "L16-123",
				Lead = true,
				Active = true
			};
			Case newCase = new Case();
			newCase.Initialize();
			newCase.Agencies = new List<Agency>
			{
				agency
			};

			string caseID = _client.SubmitCase(newCase);
			Case actualCase = _client.GetCase(caseID, new List<string> { "Agencies" });
			Assert.IsNotNull(actualCase, $"Case({caseID}) not found");
			Assert.AreEqual(1, actualCase.Agencies.Count, "No agencies");
			Assert.AreEqual(agency.Number, actualCase.Agencies[0].Number, "Number");
		}

		[TestMethod]
		public void AgencyAlsoUsedOnCaseInvolvement()
		{
			NameAgency officer = _client.GetFirstNameInAgency(agencyMasterCode: 3);
			Assert.IsNotNull(officer, "Could not find name in agency");
			NumberType numberType = _client.GetCode<NumberType>("MasterCode = 3");
			Assert.IsNotNull(numberType, "Number type not found");
			InvolveType lawInvolveType = _client.GetCode<InvolveType>("MasterCode = 5");

			Agency lawAgency = new Agency
			{
				Operation = OperationType.Insert,
				AgencyCode = officer.AgencyCode,
				NumberTypeCode = numberType.Code,
				Lead = true,
				Active = true
			};

			Case newCase = new Case();
			newCase.Initialize();
			newCase.CaseInvolvedNames.Add(new CaseInvolvedName
			{
				Operation = OperationType.Insert,
				InvolvementCode = lawInvolveType.Code,
				NameID = officer.NameID,
				CaseAgency = lawAgency  //Do not set the AgencyCode property.  Will not work
			});
			newCase.Agencies = new List<Agency>
			{
				lawAgency
			};

			string caseID = _client.SubmitCase(newCase);

			Case actualCase = _client.GetCase(caseID, new List<string> { "CaseInvolvedNames" });
			Assert.IsNotNull(actualCase, "Case not found");
			Assert.AreEqual(2, actualCase.CaseInvolvedNames.Count, "Case involvements");
			CaseInvolvedName officerInvolvement = actualCase.CaseInvolvedNames.FirstOrDefault(i => i.NameID == officer.NameID);
			Assert.IsNotNull(officerInvolvement, "Could not find officer involvement");
			Assert.AreEqual(officer.AgencyCode, officerInvolvement.AgencyCode, "Officer involvement agency code");
		}
	}
}