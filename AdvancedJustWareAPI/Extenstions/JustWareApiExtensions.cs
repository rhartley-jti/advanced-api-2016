using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
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
		private static readonly DocumentType _documentType;
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
				_documentType = client.GetCode<DocumentType>();
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

		public static ApiCreateResult CreateCases(this IJustWareApi client, int numberOfCases)
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
				ellapsedSeconds = TimeAction(() =>
				{
					for (int i = 0; i < numberOfCases; i++)
					{
						keys.AddRange(client.Submit(cse));
					}
				});
				_logger.Info("Created {1} cases in {0} seconds", ellapsedSeconds, numberOfCases);
			}
			catch (Exception exception)
			{
				_logger.Error(exception);
				throw;
			}
			return new ApiCreateResult(keys.Where(k => k.TypeName.Equals("Case", StringComparison.OrdinalIgnoreCase)), ellapsedSeconds);
		}

		public static ApiCreateResult CreateName(this IJustWareApi client)
		{
			var newName = new Name
			{
				Operation = OperationType.Insert,
				Last = Guid.NewGuid().ToString()
			};
			List<Key> keys = null;
			double ellapsedSeconds = TimeAction(() =>
			{
				keys = client.Submit(newName);
			});
			_logger.Info("Created name('{0}') in {1} seconds", newName.Last, ellapsedSeconds);

			return new ApiCreateResult(keys, ellapsedSeconds);
		}

		public static ApiCreateResult AddDocumentToCase(this IJustWareApi client, string caseID, string data, string fileName = null)
		{
			var document = new CaseDocument
			{
				Operation = OperationType.Insert,
				FileName = fileName ?? Guid.NewGuid().ToString(),
				CaseID = caseID,
				TypeCode = _documentType.Code
			};

			List<Key> keys = new List<Key>();
			double ellapsedSeconds = TimeAction(() =>
			{
				string uploadUrl = client.RequestFileUpload(document);
				uploadUrl.UploadToApi(data);
				keys.AddRange(client.FinalizeFileUpload(document, uploadUrl));
			});
			_logger.Info("Uploaded document({0}) to case '{1}' in {2} seconds", document.FileName, caseID, ellapsedSeconds);

			return new ApiCreateResult(keys, ellapsedSeconds);
		}

		public static void UploadToApi(this string url, string data)
		{
			HttpWebRequest headRequest = (HttpWebRequest)WebRequest.Create(url);
			headRequest.Credentials = CredentialCache.DefaultCredentials; // This works for integrated security, use NetworkCredential for basic authentication
			headRequest.PreAuthenticate = true; // In order to stream a large file PreAuthenticate must be set to true, and an initial call is used to authenticate.
			headRequest.UnsafeAuthenticatedConnectionSharing = true;
			headRequest.Method = "HEAD";
			headRequest.GetResponse();

			HttpWebRequest uploadRequest = (HttpWebRequest)WebRequest.Create(url);
			uploadRequest.Credentials = headRequest.Credentials;
			uploadRequest.Method = "POST";
			uploadRequest.AllowWriteStreamBuffering = false; // Setting AllowWriteStreamBuffering instructs the web request to not load the file into memory.
			uploadRequest.SendChunked = true; // SendChunked allows a large file to be sent in smaller pieces.  When sending chunked, the content length should not be set.
			uploadRequest.PreAuthenticate = true;
			uploadRequest.Timeout = -1; // Infinite timeout
			uploadRequest.UnsafeAuthenticatedConnectionSharing = true;

			const int bufferSize = 65536;
			byte[] buffer = new byte[bufferSize];

			using (var uploadStream = uploadRequest.GetRequestStream())
			using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
			{
				int bytesRead;
				while ((bytesRead = memoryStream.Read(buffer, 0, bufferSize)) > 0)
				{
					uploadStream.Write(buffer, 0, bytesRead);
				}
				uploadStream.Flush();
			}
			uploadRequest.GetResponse();
		}

		public static string DownloadFromApi(this IJustWareApi client, CaseDocument document, string username = ApiFactory.TC_USER, string password = ApiFactory.TC_USER_PASSWORD)
		{
			using (WebClient webClient = new WebClient())
			{
				NetworkCredential networkCredential = new NetworkCredential
				{
					UserName = username,
					Password = password
				};
				webClient.Credentials = networkCredential;

				string downloadUrl = client.RequestFileDownload(document);
				return webClient.DownloadString(downloadUrl);
			}
		}

		public static double TimeAction(Action action)
		{
			Stopwatch watch = Stopwatch.StartNew();
			action.Invoke();
			watch.Stop();

			return watch.ElapsedMilliseconds / 1000.0;
		}
	}
}