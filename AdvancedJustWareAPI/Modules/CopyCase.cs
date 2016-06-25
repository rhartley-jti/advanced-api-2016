using System.Collections.Generic;
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
			_client = ApiFactory.CreateApiClient();
			_caseID = _client.SubmitCase().ID;
			_nameID = _client.SubmitName();
		}

		[TestMethod]
		public void MinimumCopy()
		{
			var parameters = new CopyCaseParameters
			{
				CaseID = _caseID,
				NewPipNameID = _nameID
			};

			string resultCaseID = _client.CopyCase(parameters);

			Assert.AreNotEqual(_caseID, resultCaseID, "Different cases");
			Case copiedCase = _client.GetCase(resultCaseID, new List<string> { "CaseInvolvedNames" });
			Assert.IsNotNull(copiedCase, "Could not find case");
			Assert.AreEqual(1, copiedCase.CaseInvolvedNames.Count, "Number of involvements");
			Assert.AreEqual(_nameID, copiedCase.CaseInvolvedNames[0].NameID, "Involvment NameID");
		}

		[TestMethod]
		public void CopyAndRelate()
		{
			InvolveType codefendantInvolveType = _client.GetCode<InvolveType>("MasterCode = 4");
			Assert.IsNotNull(codefendantInvolveType, "Co-Defendant involve type");
			RelatedCaseType relationshipType = _client.GetCode<RelatedCaseType>();
			Assert.IsNotNull(relationshipType, "Case relationship type");

			var parameters = new CopyCaseParameters
			{
				CaseID = _caseID,
				NewPipNameID = _nameID,
				PipInvolveTypeCode = codefendantInvolveType.Code,
				CaseRelationshipTypeCode = relationshipType.Code
			};
			string copiedCaseID = _client.CopyCase(parameters);

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
			double manuallyCreatedSeconds = _client.CreateCases(100);
			var parameters = new CopyCaseParameters
			{
				CaseID = _caseID,
				NewPipNameID = _nameID
			};
			double copyEllapsedSeconds = JustWareApiExtensions.TimeAction(() =>
			{
				for (int i = 0; i < 100; i++)
				{
					_client.CopyCase(parameters);
				}
			});
			_logger.Info("Copying case 100 times took {0} seconds", copyEllapsedSeconds);

			Assert.IsTrue(copyEllapsedSeconds < manuallyCreatedSeconds, "Copy case faster");
		}
	}
}