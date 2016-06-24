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
		private NumberType _numberType;
		private Case _newCase;
		private InvolveType _lawInvolveType;
		private NameAgency _officerData;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiFactory.CreateApiClient();
			_numberType = _client.GetCode<NumberType>("MasterCode = 3");
			Assert.IsNotNull(_numberType, "Number type with master code 3 not found");
			_lawInvolveType = _client.GetCode<InvolveType>("MasterCode = 5");
			Assert.IsNotNull(_lawInvolveType, "Law involvment type not found");
			
			_newCase = new Case().Initialize();

			_officerData = _client.GetFirstNameInAgency(agencyMasterCode: 3);
			Assert.IsNotNull(_officerData, "Could not find name in agency");
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

			Agency agency = new Agency
			{
				Operation = OperationType.Insert,
				AgencyCode = agencyType.Code,
				NumberTypeCode = _numberType.Code,
				Number = "L16-123",
				Lead = true,
				Active = true
			};
			
			_newCase.AddAgency(agency);

			string caseID = _client.SubmitCase(_newCase);
			Case actualCase = _client.GetCase(caseID, new List<string> { "Agencies" });
			Assert.IsNotNull(actualCase, $"Case({caseID}) not found");
			Assert.AreEqual(1, actualCase.Agencies.Count, "No agencies");
			Assert.AreEqual(agency.Number, actualCase.Agencies[0].Number, "Number");
		}

		[TestMethod]
		public void AgencyAlsoUsedOnCaseInvolvement()
		{
			Agency lawAgency = new Agency().Initialize(_officerData.AgencyType, _numberType);

			_newCase
				.AddAgency(lawAgency)
				.AddInvolvement(_lawInvolveType, _officerData.NameID, lawAgency);

			string caseID = _client.SubmitCase(_newCase);

			Case actualCase = _client.GetCase(caseID, new List<string> { "CaseInvolvedNames" });
			Assert.IsNotNull(actualCase, "Case not found");
			Assert.AreEqual(2, actualCase.CaseInvolvedNames.Count, "Case involvements");
			CaseInvolvedName officerInvolvement = actualCase.CaseInvolvedNames.FirstOrDefault(i => i.NameID == _officerData.NameID);
			Assert.IsNotNull(officerInvolvement, "Could not find officer involvement");
			Assert.AreEqual(_officerData.AgencyType.Code, officerInvolvement.AgencyCode, "Officer involvement agency code");
			Assert.AreEqual(_officerData.AgencyType.MasterCode, officerInvolvement.InvolvementCodeType, "Agency type lines up with invovlement type");
		}

		[TestMethod]
		public void AssigningAgencyToInvolvmentThatDoesNotMatch()
		{
			AgencyType callerAgencyType = _client.GetCallerAgencyType();

			Agency agency = new Agency().Initialize(callerAgencyType, _numberType);

			_newCase
				.AddAgency(agency)
				.AddInvolvement(_lawInvolveType, _officerData.NameID, agency);

			string caseID = _client.SubmitCase(_newCase);

			Case actualCase = _client.GetCase(caseID, new List<string> { "CaseInvolvedNames" });
			Assert.IsNotNull(actualCase, "Case not found");
			Assert.AreEqual(2, actualCase.CaseInvolvedNames.Count, "Case involvements");
			CaseInvolvedName officerInvolvement = actualCase.CaseInvolvedNames.FirstOrDefault(i => i.NameID == _officerData.NameID);
			Assert.IsNotNull(officerInvolvement, "Could not find officer involvement");
			Assert.AreEqual(callerAgencyType.Code, officerInvolvement.AgencyCode, "Officer involvement agency code");
			Assert.AreNotEqual(callerAgencyType.MasterCode, officerInvolvement.InvolvementCodeType, "Expected agency and involvment codes to be different");
		}
	}
}