using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

namespace AdvancedJustWareAPI.Modules
{
	[TestClass]
	public class Financials
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private IJustWareApi _client;
		private static PaymentType _paymentType;
		private static AgencyType _agencyType;
		private static int _agencyNameID;
		private static ObligationType _obligationType;

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			IJustWareApi client = null;
			try
			{
				client = ApiClientFactory.CreateApiClient();
				_paymentType = client.GetCode<PaymentType>();
				Assert.IsNotNull(_paymentType, "PaymentType not found");

				_agencyType = client.GetCode<AgencyType>("IsAccountOwner = true");
				Assert.IsNotNull(_agencyType, "AgencyType with account owner not found");

				List<ApplicationPerson> agencyNames = client.FindApplicationPersons($"(AgencyCode = \"{_agencyType.Code}\").Take(1)", null);
				Assert.AreEqual(1, agencyNames.Count, $"Expected name in agency '{_agencyType.Code}'");
				ApplicationPerson agencyAppPerson = agencyNames.First();
				Assert.IsNotNull(agencyAppPerson, $"Name in agency '{_agencyType.Code}' not found");
				_agencyNameID = agencyAppPerson.NameID;

				_obligationType = client.GetCode<ObligationType>();
				Assert.IsNotNull(_obligationType, "ObligationType not found");
			}
			finally
			{
				client?.Dispose();
			}
		}

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
		public void Payment()
		{
			// Need a name (f1)
			
			// Payment entity (f2)
			
			// Submit (f3)
			
			// Check the payment (f4)
			
			// Reciept # can be used with report (f5)
			
		}

		[TestMethod]
		public void Obligation()
		{
			// Need a case with a charge (f6)
			
			// Obligation requries the charge involved record ID, will need to load (f7)
			
			// Need agency liability account for ToAccountID (may have to create) (f8)
			
			// Ready to create obligation and submit (f9)
			
			// Make sure we got a key for the obligation (f11)
			
		}
	}
}