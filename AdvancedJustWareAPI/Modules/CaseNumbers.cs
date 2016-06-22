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
			//Create agency entity (cn1)
			
			//Add agency to case and submit.  Using extension to simplify (cn2)
			
			//Load case and make sure entity was added (cn3)			
		}

		[TestMethod]
		public void AgencyAlsoUsedOnCaseInvolvement()
		{
			//Create agency and officer involvement (cn4)

			//Add agency and involvment to case and submit (cn5)

			//Get the case and check for expected result (cn6)
		}

		[TestMethod]
		public void AssigningAgencyToInvolvmentThatDoesNotMatch()
		{
			//Create agency(not law) and officer involvement (cn7)
			
			//Add agency and involvement to case and submit (cn8)
			
			//Get the case and check for expected result (cn9)
		}
	}
}