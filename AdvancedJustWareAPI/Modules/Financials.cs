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
				client.Dispose();
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
			Name name = _client.SubmitName();
			Payment payment = new Payment
			{
				Operation = OperationType.Insert,
				TypeCode = _paymentType.Code,
				AgencyCode = _agencyType.Code,
				NameID = name.ID,
				Payer = name.ID,
				Amount = 50.00m,
				ReceivedBy = _agencyNameID, //must be member of agency
				ReceivedDate = DateTime.Now.AddMonths(1),
				ReferenceNumber = "API-1"  //data of your choice
			};

			List<Key> keys = _client.Submit(payment);

			Assert.AreEqual(1, keys.Count(k => k.TypeName.Equals(nameof(Payment))), "Payment key");
			Name actualName = _client.GetName(name.ID, new List<string> { "Payments" });
			Assert.AreEqual(1, actualName.Payments.Count, "Payments associated with name");
			Payment actualPayment = actualName.Payments[0];
			Assert.AreEqual(payment.Amount, actualPayment.Amount, "Amount");
			Assert.AreEqual(payment.ReferenceNumber, actualPayment.ReferenceNumber, "ReferenceNumber");
			Assert.IsNotNull(actualPayment.ReceiptNumber, "ReceiptNumber");
			_logger.Info("Payment reciept number: {0}", actualPayment.ReceiptNumber);
		}

		[TestMethod]
		public void Obligation()
		{
			Name primaryName = _client.SubmitName(new Name().Initialize());
			Charge charge = new Charge().Initialize(number: 1);
			Case cse = _client.SubmitCase(new Case()
				.Initialize(primaryName)
				.AddCharge(charge));

			//Need ChargeInvolvedNames
			Assert.AreEqual(1, charge.ChargeInvolvedNames.Count, "ChargeInvolvedNames");
			
			var obligation = new Obligation
			{
				Operation = OperationType.Insert,
				TypeCode = _obligationType.Code,
				CaseID = cse.ID,
				DateDue = DateTime.Now.AddDays(15),
				Amount = 100.00m,
				Payee = ApiExtensions.CallerNameID
			};
			var keys = _client.Submit(obligation);

			Assert.AreEqual(1, keys.Count(k => k.TypeName.Equals(nameof(Obligation))), "Obligation key count");
			
		}
	}
}