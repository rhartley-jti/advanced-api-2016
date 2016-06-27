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
		private int _yr;
		private int _month;
		private int _day;
		private int _hour;
		private int _min;
		private int _sec;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiClientFactory.CreateApiClient();
			_yr = DateTime.Now.Year;
			_month = DateTime.Now.Month;
			_day = DateTime.Now.Day;
			_hour = DateTime.Now.Hour;
			_min = DateTime.Now.Minute;
			_sec = DateTime.Now.Second;
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_client.Dispose();
		}

		[TestMethod]
		public void StringVsCharacter()
		{
			// Using single quotes to qualify strings will fail (q1)
			
			// Always use double quotes event though it is a pain to escape (q2)
			
		}

		[TestMethod]
		public void DatesAndTimes()
		{
			// Setup a case with several events (q3)
			
			// Find all events in the future (q4)
			
			//Find all past events (q5)
			
			//Find events for today (q6)
			
		}

		[TestMethod]
		public void QueryingCollections()
		{
			// Setup names to query (q7)
			
			// Setup reusable query to restrict to names created above (q8)
			
			// Find all names that have an address in Utah (q9)
			
			// Find all names that have addresses only in Utah (q10)
			
			// Find all names that have addresses only in Utah for real (q11)
			
		}

		[TestMethod]
		public void OrderBy()
		{
			// Setup names (q12)
			
			// Setup reusable query to restrict to names created above (q13)
			
			// Query with no order by specified.  Gives back insert order (q14)
			
			// Query with order by first name (defaults to ascending) (q15)
			
			// Query with order by first name descending (q16)
			
		}

		[TestMethod]
		public void SkipAndTake()
		{
			// Setup names (q17)
			
			// Setup reusable query to restrict to names created above (q18)
			
			// Get first two names (q19)

			// Get next two names (Notice the OrderBy. When using Skip and Take together query will fail without OrderBy) (q20)
			
			// Get last two(really one) names (q21)
			
		}
	}
}