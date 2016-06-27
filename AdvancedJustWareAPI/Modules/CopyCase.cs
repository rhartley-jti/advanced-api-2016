﻿using System.Collections.Generic;
using System.Linq;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.Extenstions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;

namespace AdvancedJustWareAPI.Modules
{
	[TestClass]
	public class CopyCase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private IJustWareApi _client;
		private string _caseID;
		private int _nameID;

		[TestInitialize]
		public void Initialize()
		{
			_client = ApiClientFactory.CreateApiClient();
			_caseID = _client.SubmitCase().ID;
			_nameID = _client.SubmitName().ID;
		}

		[TestCleanup]
		public void TestCleanup()
		{
			_client.Dispose();
		}

		[TestMethod]
		public void MinimumCopy()
		{
			//Setup parameters and copy case
			var parameters = new CopyCaseParameters
			{
				CaseID = _caseID,
				NewPipNameID = _nameID
			};
			string resultCaseID = _client.CopyCase(parameters);
			//Check results
			Assert.AreNotEqual(_caseID, resultCaseID, "Different cases");
			Case copiedCase = _client.GetCase(resultCaseID, new List<string> { "CaseInvolvedNames" });
			Assert.IsNotNull(copiedCase, "Could not find case");
			Assert.AreEqual(1, copiedCase.CaseInvolvedNames.Count, "Number of involvements");
			Assert.AreEqual(_nameID, copiedCase.CaseInvolvedNames[0].NameID, "Involvment NameID");
		}

		[TestMethod]
		public void CopyAndRelate()
		{
			//Find codes needed to relate
			InvolveType codefendantInvolveType = _client.GetCode<InvolveType>("MasterCode = 4");
			Assert.IsNotNull(codefendantInvolveType, "Co-Defendant involve type");
			RelatedCaseType relationshipType = _client.GetCode<RelatedCaseType>();
			Assert.IsNotNull(relationshipType, "Case relationship type");
			//Setup parameters and copy case
			var parameters = new CopyCaseParameters
			{
				CaseID = _caseID,
				NewPipNameID = _nameID,
				PipInvolveTypeCode = codefendantInvolveType.Code,
				CaseRelationshipTypeCode = relationshipType.Code
			};
			string copiedCaseID = _client.CopyCase(parameters);
			//Check results
			Case copiedCase = _client.GetCase(copiedCaseID, new List<string> { "CaseInvolvedNames", "CaseRelationships" });
			Assert.AreEqual(2, copiedCase.CaseInvolvedNames.Count, "Involvements");
			Assert.AreEqual(1, copiedCase.CaseRelationships.Count, "Relationships");
			string involvement = copiedCase.CaseInvolvedNames
				.Where(i => i.NameID != _nameID)
				.Select(i => i.InvolvementCode).FirstOrDefault();
			Assert.AreEqual(codefendantInvolveType.Code, involvement, "Previous involvement type");
			Assert.AreEqual(relationshipType.Code, copiedCase.CaseRelationships[0].RelationshipCode, "Relationship type");
		}

		[TestMethod]
		public void CopyCaseHasBetterPerformance()
		{
			// Create 100 cases and measure time
			double manuallyCreatedSeconds = _client.CreateCases(100);
			// Copy 100 cases and measure time
			var parameters = new CopyCaseParameters
			{
				CaseID = _caseID,
				NewPipNameID = _nameID
			};
			double copyEllapsedSeconds = ApiExtensions.TimeAction(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					_client.CopyCase(parameters);
				}
			});
			_logger.Info("Copying case 100 times took {0} seconds", copyEllapsedSeconds);
			// Compare speeds
			Assert.IsTrue(copyEllapsedSeconds < manuallyCreatedSeconds, "Copy case faster");
		}
	}
}