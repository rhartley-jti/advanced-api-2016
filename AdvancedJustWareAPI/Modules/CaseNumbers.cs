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
		private ApplicationPerson _officer;
		private AgencyType _lawAgencyType;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiClientFactory.CreateApiClient();
			_numberType = _client.GetCode<NumberType>("MasterCode = 3");
			Assert.IsNotNull(_numberType, "Number type with master code 3 not found");
			_lawInvolveType = _client.GetCode<InvolveType>("MasterCode = 5");
			Assert.IsNotNull(_lawInvolveType, "Law involvment type not found");
			
			_newCase = new Case().Initialize();

			_officer = _client.GetFirstNameInAgency(agencyMasterCode: 3);
			Assert.IsNotNull(_officer, "Could not find name in agency");

			_lawAgencyType = _client.GetCode<AgencyType>($"Code = \"{_officer.AgencyCode}\"");
			Assert.IsNotNull(_lawAgencyType, $"Agency '{_officer.AgencyCode}' type was not found");
			
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_client.Dispose();
		}

		[TestMethod]
		public void AddNumberToCase()
		{
			//Create agency entity
			Agency agency = new Agency
			{
				Operation = OperationType.Insert,
				AgencyCode = _lawAgencyType.Code,
				NumberTypeCode = _numberType.Code,
				Number = "L16-123",
				Lead = true,
				Active = true
			};
			//Add agency to case and submit.  Using extension to simplify
			_newCase.AddAgency(agency);
			string caseID = _client.SubmitCase(_newCase).ID;
			//Load case and make sure entity was added
			Case actualCase = _client.GetCase(caseID, new List<string> { "Agencies" });
			Assert.IsNotNull(actualCase, $"Case({caseID}) not found");
			Assert.AreEqual(1, actualCase.Agencies.Count, "No agencies");
			Assert.AreEqual(agency.Number, actualCase.Agencies[0].Number, "Number");
		}

		[TestMethod]
		public void AgencyAlsoUsedOnCaseInvolvement()
		{
			//Create agency and officer involvement
			var lawAgency = new Agency().Initialize(_lawAgencyType, _numberType);
			var involvement = new CaseInvolvedName
			{
				Operation = OperationType.Insert,
				InvolvementCode = _lawInvolveType.Code,
				NameID = _officer.NameID,
				CaseAgency = lawAgency
			};
			//Add agency and involvment to case and submit
			_newCase
				.AddAgency(lawAgency)
				.AddInvolvement(involvement);
			string caseID = _client.SubmitCase(_newCase).ID;
			//Get the case and check for expected result
			Case actualCase = _client.GetCase(caseID, new List<string> { "CaseInvolvedNames" });
			Assert.IsNotNull(actualCase, "Case not found");
			Assert.AreEqual(2, actualCase.CaseInvolvedNames.Count, "Case involvements");
			CaseInvolvedName officerInvolvement = actualCase.CaseInvolvedNames.FirstOrDefault(i => i.NameID == _officer.NameID);
			Assert.IsNotNull(officerInvolvement, "Could not find officer involvement");
			Assert.AreEqual(_lawAgencyType.Code, officerInvolvement.AgencyCode, "Officer involvement agency code");
			Assert.AreEqual(_lawAgencyType.MasterCode, officerInvolvement.InvolvementCodeType, "Agency type lines up with invovlement type");
		}

		[TestMethod]
		public void AssigningAgencyToInvolvmentThatDoesNotMatch()
		{
			//Create agency(not law) and officer involvement
			AgencyType callerAgencyType = _client.GetCallerAgencyType();
			Agency otherAgency = new Agency().Initialize(callerAgencyType, _numberType);
			var involvement = new CaseInvolvedName
			{
				Operation = OperationType.Insert,
				InvolvementCode = _lawInvolveType.Code,
				NameID = _officer.NameID,
				CaseAgency = otherAgency
			};
			//Add agency and involvement to case and submit
			_newCase
				.AddAgency(otherAgency)
				.AddInvolvement(involvement);
			string caseID = _client.SubmitCase(_newCase).ID;
			//Get the case and check for expected result
			Case actualCase = _client.GetCase(caseID, new List<string> { "CaseInvolvedNames" });
			Assert.IsNotNull(actualCase, "Case not found");
			Assert.AreEqual(2, actualCase.CaseInvolvedNames.Count, "Case involvements");
			CaseInvolvedName officerInvolvement = actualCase.CaseInvolvedNames.FirstOrDefault(i => i.NameID == _officer.NameID);
			Assert.IsNotNull(officerInvolvement, "Could not find officer involvement");
			Assert.AreEqual(callerAgencyType.Code, officerInvolvement.AgencyCode, "Officer involvement agency code");
			Assert.AreNotEqual(callerAgencyType.MasterCode, officerInvolvement.InvolvementCodeType, "Expected agency and involvment codes to be different");
		}
	}
}