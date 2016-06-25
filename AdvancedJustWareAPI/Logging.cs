using System;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AdvancedJustWareAPI
{
	[TestClass]
	public class Logging
	{
		[TestMethod]
		public void ExampleLogging()
		{
			IJustWareApi client = ApiFactory.CreateApiClient(ensureAutoGenerationEnabled: false);
			Name name = client.SubmitName(new Name().Initialize());
			client.SubmitCase(new Case().Initialize());
			try
			{
				client.FindNames("BadQuery = 1", null);
			}
			catch {}
			client.FindNames($"ID = {name.ID}", null);
		}

	}
}