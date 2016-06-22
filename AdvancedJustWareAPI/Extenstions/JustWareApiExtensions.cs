using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using AdvancedJustWareAPI.api;
using NLog;

namespace AdvancedJustWareAPI.Extenstions
{
	public static class JustWareApiExtensions
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private static readonly InvolveType _primaryInvolveType;
		private static readonly CaseStatusType _statusType;
		private static readonly CaseType _caseType;
		private static readonly AgencyType _agencyType;
		private static readonly int _currentUserNameID;

		static JustWareApiExtensions()
		{
			try
			{
				IJustWareApi client = ApiFactory.CreateApiClient();
				_primaryInvolveType = client.GetCode<InvolveType>("MasterCode = 1");
				_statusType = client.GetCode<CaseStatusType>("MasterCode = 1");
				_caseType = client.GetCode<CaseType>();
				_agencyType = client.GetCode<AgencyType>("IsAccountOwner = true");
				_currentUserNameID = client.GetCallerNameID();
			}
			catch (Exception exception)
			{
				_logger.Error(exception);
				throw;
			}
		}

		public static T GetCode<T>(this IJustWareApi client, string query = "1=1")
		{
			string methodName = $"Find{typeof(T).Name}s";
			string fullQuery = $"({query}).Take(1)";
			MethodInfo methodInfo = client.GetType().GetMethods().FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
			if (methodInfo == null)
			{
				throw new ApplicationException($"Could not find JustWare API method: {methodName}");
			}

			IEnumerable<T> results = methodInfo.Invoke(client, new object[] { fullQuery, null }) as IEnumerable<T>;
			if (results == null)
			{
				throw new ApplicationException($"No codes found with query: '{query}' using method: {methodName}");
			}

			T code = results.FirstOrDefault();
			if (code == null)
			{
				throw new ApplicationException($"Code was not found with query: '{query}' using method: {methodName}");
			}

			return code;
		}

		public static CreationResult CreateCases(this IJustWareApi client, int numberOfCases)
		{
			var keys = new List<Key>();
			double ellapsedSeconds;
			var cse = new Case
			{
				Operation = OperationType.Insert,
				StatusCode = _statusType.Code,
				TypeCode = _caseType.Code,
				AgencyAddedByCode = _agencyType.Code,
				StatusDate = DateTime.Now,
				ReceivedDate = DateTime.Now,
				CaseInvolvedNames = new List<CaseInvolvedName>
							{
								new CaseInvolvedName
								{
									Operation = OperationType.Insert,
									InvolvementCode = _primaryInvolveType.Code,
									NameID = _currentUserNameID,
								}
							}
			};
			try
			{
				Stopwatch watch = Stopwatch.StartNew();
				for (int i = 0; i < numberOfCases; i++)
				{
					keys.AddRange(client.Submit(cse));
				}
				watch.Stop();
				ellapsedSeconds = watch.ElapsedMilliseconds / 1000.0;
				_logger.Info("Created {1} cases in {0} seconds", ellapsedSeconds, numberOfCases);
			}
			catch (Exception exception)
			{
				_logger.Error(exception);
				throw;
			}
			return new CreationResult(keys.Where(k => k.TypeName.Equals("Case", StringComparison.OrdinalIgnoreCase)), ellapsedSeconds);
		}
	}
}