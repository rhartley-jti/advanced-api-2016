using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdvancedJustWareAPI.Modules
{
	[TestClass]
	public class Queries
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
		public void StringVsCharacter()
		{
			try
			{
				_client.FindAgencyTypes("Code = '1'", null);
				Assert.Fail("Expected exception");
			}
			catch (FaultException exception)
			{
				Assert.AreEqual("InvalidWhereClause", exception.Code.Name, "Code name");
			}

			var results = _client.FindAgencyTypes("Code = \"1\"", null);
			Assert.AreEqual(1, results.Count, "Find result");
		}

		[TestMethod]
		public void DatesAndTimes()
		{
			int yr = DateTime.Now.Year;
			int month = DateTime.Now.Month;
			int day = DateTime.Now.Day;
			int hour = DateTime.Now.Hour;
			int min = DateTime.Now.Minute;
			int sec = DateTime.Now.Second;
			Case cse = _client.SubmitCase(new Case()
				.Initialize()
				.AddEvent(new CaseEvent().Initialize(DateTime.Now.AddDays(-5)))
				.AddEvent(new CaseEvent().Initialize(DateTime.Now.AddDays(5)))
				.AddEvent(new CaseEvent().Initialize(DateTime.Now.AddMinutes(15)))
				.AddEvent(new CaseEvent().Initialize(DateTime.Now.AddHours(2)))
				.AddEvent(new CaseEvent().Initialize(DateTime.Now.AddMinutes(-20))));

			//Find all events in the future
			string futureQuery = $"CaseID = \"{cse.ID}\" && EventDate > DateTime({yr},{month},{day})";
			List<CaseEvent> futureEvents = _client.FindCaseEvents(futureQuery, null);
			Assert.AreEqual(4, futureEvents.Count, "Future events");

			//Find all past events
			//Notice you can use both && or and for logic, = or == for equality
			string pastQuery = $"CaseID == \"{cse.ID}\" and EventDate < DateTime({yr},{month},{day})";
			List<CaseEvent> pastEvents = _client.FindCaseEvents(pastQuery, null);
			Assert.AreEqual(1, pastEvents.Count, "Past events");

			//Find events for today
			string todayQuery 
				= $"CaseID = \"{cse.ID}\"" + 
				//When using time you must specify all three (hour, minute, and second)
				$" && EventDate > DateTime({yr},{month},{day},{hour},{min},{sec})" +
				$" && EventDate < DateTime({yr},{month},{day + 1})";
			List<CaseEvent> todayEvents = _client.FindCaseEvents(todayQuery, null);
			Assert.AreEqual(2, todayEvents.Count, "Today events");
		}

		[TestMethod]
		public void QueryingCollections()
		{
			Name name1 = _client.SubmitName(new Name().Initialize());
			Name name2 = _client.SubmitName(new Name()
				.Initialize()
				.AddAddress(city: "Los Angeles", state: "CA")
				.AddAddress(city: "Logan", state: "UT"));
			Name name3 = _client.SubmitName(new Name()
				.Initialize()
				.AddAddress(city: "Logan", state: "UT")
				.AddAddress(city: "Phoneix", state: "AZ"));
			Name name4 = _client.SubmitName(new Name()
				.Initialize()
				.AddAddress(city: "Logan", state: "UT")
				.AddAddress(city: "Salt Lake City", state: "UT"));

			string testNames = $"(ID = {name1.ID} || ID = {name2.ID} || ID = {name3.ID} || ID = {name4.ID})";

			//Find all names that have an address in Utah
			string anyUtahQuery = $"{testNames} && Addresses.Any(StateCode = \"UT\")";
			var utahNames = _client.FindNames(anyUtahQuery, null);
			Assert.AreEqual(3, utahNames.Count, "Any Utah addresses");

			//Find all names that have addresses only in Utah
			string allUtahQuery = $"{testNames} && Addresses.All(StateCode = \"UT\")";
			var allUtahNames = _client.FindNames(allUtahQuery, null);
			// Empty collections will return true when using All
			Assert.AreEqual(2, allUtahNames.Count, "All Utah addresses");

			//Find all names that have addresses only in Utah for real
			string onlyUtahQuery = $"{testNames} && Addresses.Count() >= 1 && Addresses.All(StateCode = \"UT\")";
			var onlyUtahNames = _client.FindNames(onlyUtahQuery, null);
			Assert.AreEqual(1, onlyUtahNames.Count, "Only Utah addresses");
		}

		[TestMethod]
		public void OrderBy()
		{
			Name peter = _client.SubmitName(new Name {First = "Peter"}.Initialize());
			Name abigail = _client.SubmitName(new Name {First = "Abigail"}.Initialize());

			string myNames = $"ID = {peter.ID} || ID = {abigail.ID}";
			List<Name> insertOrder = _client.FindNames(myNames, null);
			Assert.AreEqual(2, insertOrder.Count, "Insert order");
			Assert.AreEqual(peter.First, insertOrder.First().First, "First name");

			string orderByFirstName = $"({myNames}).OrderBy(First)";
			List<Name> firstOrder = _client.FindNames(orderByFirstName, null);
			Assert.AreEqual(2, firstOrder.Count, "First order");
			Assert.AreEqual(abigail.First, firstOrder.First().First, "First name");

			string descendingOrderByFirstName = $"({myNames}).OrderBy(First desc)";
			List<Name> descendingOrder = _client.FindNames(descendingOrderByFirstName, null);
			Assert.AreEqual(2, descendingOrder.Count, "First order");
			Assert.AreEqual(peter.First, descendingOrder.First().First, "First name");
		}

		[TestMethod]
		public void SkipAndTake()
		{
			Name name1 = _client.SubmitName(new Name().Initialize());
			Name name2 = _client.SubmitName(new Name().Initialize());
			Name name3 = _client.SubmitName(new Name().Initialize());
			Name name4 = _client.SubmitName(new Name().Initialize());
			Name name5 = _client.SubmitName(new Name().Initialize());

			string myNames = $"ID = {name1.ID} || ID = {name2.ID} || ID = {name3.ID} || ID = {name4.ID} || ID = {name5.ID}";

			//Get first two names
			List<Name> firstTwo = _client.FindNames($"({myNames}).Take(2)", null);
			Assert.AreEqual(2, firstTwo.Count, "First two names");
			Assert.AreEqual(name1.ID, firstTwo.First().ID, "First name");
			Assert.AreEqual(name2.ID, firstTwo.Last().ID, "Second name");

			//Get next two names (Notice the OrderBy. When using Skip and Take together query will fail without OrderBy)
			List<Name> nextTwo = _client.FindNames($"({myNames}).OrderBy(ID).Skip(2).Take(2)", null);
			Assert.AreEqual(2, nextTwo.Count, "Next two names");
			Assert.AreEqual(name3.ID, nextTwo.First().ID, "Third name");
			Assert.AreEqual(name4.ID, nextTwo.Last().ID, "Fourth name");

			//Get last two(really one) names
			List<Name> lastTwo = _client.FindNames($"({myNames}).OrderBy(ID).Skip(4).Take(2)", null);
			Assert.AreEqual(1, lastTwo.Count, "Last two names");
			Assert.AreEqual(name5.ID, lastTwo.First().ID, "Fifth name");
		}
	}
}