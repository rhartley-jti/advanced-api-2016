﻿using System;
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
		private static readonly EventType _eventType;
		private static readonly AddressType _addressType;
		private static readonly int _currentUserNameID;

		static JustWareApiExtensions()
		{
			IJustWareApi client = null;
			try
			{
				client = ApiFactory.CreateApiClient(ensureAutoGenerationEnabled: false);
				_primaryInvolveType = client.GetCode<InvolveType>("MasterCode = 1");
				_statusType = client.GetCode<CaseStatusType>("MasterCode = 1");
				_caseType = client.GetCode<CaseType>();
				_agencyType = client.GetCode<AgencyType>("IsAccountOwner = true");
				_documentType = client.GetCode<DocumentType>();
				_eventType = client.GetCode<EventType>("MasterCode = 1");
				_addressType = client.GetCode<AddressType>();
				_currentUserNameID = client.GetCallerNameID();
			}
			catch (Exception exception)
			{
				_logger.Error(exception);
				throw;
			}
			finally
			{
				IDisposable disposable = client as IDisposable;
				disposable?.Dispose();
			}
		}

		public static T GetCode<T>(this IJustWareApi client, string query = "1=1")
			where T : DataContractBase
		{
			string methodName = $"Find{typeof(T).Name}s";
			string fullQuery = $"({query}).Take(1)";
			MethodInfo methodInfo = client.GetType().GetMethods().FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
			if (methodInfo == null)
			{
				throw new ApplicationException($"Could not find JustWare API method: {methodName}");
			}

			IEnumerable<T> results;
			try
			{
				results = methodInfo.Invoke(client, new object[] { fullQuery, null }) as IEnumerable<T>;
			}
			catch (Exception exception)
			{
				_logger.Error("API call to {0} with query {1} failed. Reason: {2}", methodInfo, query, exception);
				return null;
			}

			T code = results?.FirstOrDefault();
			return code;
		}

		public static double CreateCases(this IJustWareApi client, int numberOfCases)
		{
			double ellapsedSeconds;
			Case cse = new Case().Initialize();
			try
			{
				ellapsedSeconds = TimeAction(() =>
				{
					for (int i = 0; i < numberOfCases; i++)
					{
						client.Submit(cse);
					}
				});
				_logger.Info("Created {1} cases in {0} seconds", ellapsedSeconds, numberOfCases);
			}
			catch (Exception exception)
			{
				_logger.Error(exception);
				throw;
			}
			return ellapsedSeconds;
		}

		public static Name SubmitName(this IJustWareApi client, Name name = null)
		{
			Name actualName = name ?? new Name().Initialize();
			try
			{
				double ellapsedSeconds = TimeAction(() =>
				{
					List<Key> keys = client.Submit(name ?? new Name().Initialize());
					actualName.ID = keys.Single(k => k.TypeName.Equals(nameof(Name))).NewID;
				});
				_logger.Info("Created name('{0}') in {1} seconds", actualName.ID, ellapsedSeconds);
			}
			catch (Exception exception)
			{
				_logger.Error(exception);
			}
			return actualName;
		}

		public static Case SubmitCase(this IJustWareApi client, Case cse = null)
		{
			Case actualCase = cse ?? new Case().Initialize();
			try
			{
				actualCase.ProcessCaseDocuments(doc =>
				{
					doc.Operation = OperationType.None;
				});
				double ellapsedSeconds = TimeAction(() =>
				{
					var keys = client.Submit(actualCase);
					actualCase.ID = keys.First(k => k.TypeName.Equals(nameof(Case))).NewCaseID;
					actualCase.ProcessCaseDocuments(doc =>
					{
						doc.CaseID = actualCase.ID;
						client.UploadDocument(doc);
					});
				});

				_logger.Info("Created case {0} with {1} documents in {2} seconds", 
					actualCase.ID,
					actualCase.CaseDocuments?.Count ?? 0,
					ellapsedSeconds);
			}
			catch (Exception exception)
			{
				_logger.Error(exception);
				throw;
			}
			return actualCase;
		}

		public static CaseDocument UploadDocument(this IJustWareApi client, CaseDocument document)
		{
			document.Operation = OperationType.Insert;
			string uploadUrl = client.RequestFileUpload(document);
			uploadUrl.UploadToApi(document.DocumentData);
			List<Key> docKeys = client.FinalizeFileUpload(document, uploadUrl);

			Key docKey = docKeys.FirstOrDefault(k => k.TypeName.Equals(nameof(CaseDocument)));
			if (docKey == null)
			{
				_logger.Warn("Unable to find document key");
			}
			else
			{
				document.Operation = OperationType.None;
				document.ID = docKey.NewID;
			}
			return document;
		}

		public static NameAgency GetFirstNameInAgency(this IJustWareApi client, int agencyMasterCode)
		{
			try
			{
				NameAgency resultNameAgency = null;

				double ellapsedSeconds = TimeAction(() =>
				{
					var masterCodeAgencies = client.FindAgencyTypes($"MasterCode = {agencyMasterCode}", null);
					foreach (AgencyType agency in masterCodeAgencies)
					{
						ApplicationPerson appPerson = client.FindApplicationPersons($"AgencyCode = \"{agency.Code}\"", null).FirstOrDefault();
						if (appPerson == null) continue;
						resultNameAgency = new NameAgency(appPerson.NameID, agency);
						break;
					}
				});
				if (resultNameAgency == null) return null;

				_logger.Info("Found name({0}) in agency({1}) with master code {2} in {3} seconds",
					resultNameAgency.NameID,
					resultNameAgency.AgencyType.Code,
					agencyMasterCode,
					ellapsedSeconds);
				return resultNameAgency;
			}
			catch (Exception exception)
			{
				_logger.Error(exception);
				throw;
			}
		}

		public static AgencyType GetCallerAgencyType(this IJustWareApi client)
		{
			try
			{
				AgencyType agencyType = null;
				double ellapsedSeconds = TimeAction(() =>
				{
					int nameID = client.GetCallerNameID();
					ApplicationPerson appPerson = client.FindApplicationPersons($"NameID = {nameID}", null).Single();
					agencyType = client.FindAgencyTypes($"Code = \"{appPerson.AgencyCode}\"", null).Single();
				});
				_logger.Info("Looked up caller agency type in {0} seconds", ellapsedSeconds);
				return agencyType;
			}
			catch (Exception exception)
			{
				_logger.Error(exception);
				throw;
			}
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

		public static Case Initialize(this Case cse)
		{
			cse.Operation = OperationType.Insert;
			cse.StatusCode = _statusType.Code;
			cse.TypeCode = _caseType.Code;
			cse.AgencyAddedByCode = _agencyType.Code;
			cse.StatusDate = DateTime.Now;
			cse.ReceivedDate = DateTime.Now;
			cse.CaseInvolvedNames = new List<CaseInvolvedName>
			{
				new CaseInvolvedName
				{
					Operation = OperationType.Insert,
					InvolvementCode = _primaryInvolveType.Code,
					NameID = _currentUserNameID,
				}
			};
			return cse;
		}

		public static Case AddInvolvement(this Case cse, InvolveType involveType, int nameID, Agency agency = null)
		{
			if (involveType == null) throw new ArgumentNullException(nameof(involveType));
			if (nameID == default(int)) throw new ArgumentOutOfRangeException(nameof(nameID), "Missing valid NameID");
			if (cse.CaseInvolvedNames == null)
			{
				cse.CaseInvolvedNames = new List<CaseInvolvedName>();
			}
			cse.CaseInvolvedNames.Add(new CaseInvolvedName
			{
				Operation = OperationType.Insert,
				InvolvementCode = involveType.Code,
				NameID = nameID,
				CaseAgency = agency
			});
			return cse;
		}

		public static Case AddAgency(this Case cse, Agency agency)
		{
			if (agency == null) throw new ArgumentNullException(nameof(agency));
			if (cse.Agencies == null)
			{
				cse.Agencies = new List<Agency>();
			}
			cse.Agencies.Add(agency);
			return cse;
		}

		public static Case AddDocument(this Case cse, CaseDocument document = null)
		{
			if (document == null)
			{
				document = new CaseDocument().Initialize("Test document");
			}

			if (cse.CaseDocuments == null)
			{
				cse.CaseDocuments = new List<CaseDocument>();
			}
			cse.CaseDocuments.Add(document);
			return cse;
		}

		public static Case AddEvent(this Case cse, CaseEvent evnt = null)
		{
			if (evnt == null)
			{
				evnt = new CaseEvent().Initialize(DateTime.Now);
			}

			if (cse.Events == null)
			{
				cse.Events = new List<CaseEvent>();
			}
			cse.Events.Add(evnt);
			return cse;
		}

		public static Agency Initialize(this Agency agency, AgencyType agencyType, NumberType numberType, string number = null)
		{
			agency.Operation = OperationType.Insert;
			agency.AgencyCode = agencyType.Code;
			agency.NumberTypeCode = numberType.Code;
			agency.Number = number;
			return agency;
		}

		public static Name Initialize(this Name name)
		{
			name.Operation = OperationType.Insert;
			name.Last = Guid.NewGuid().ToString();
			return name;
		}

		public static Name AddAddress(this Name name, AddressType addressType = null, string street = "843 S. 100 W", string city = "Logan", string state = "UT", string zip = "84321", string tempID = null)
		{
			if (name.Addresses == null)
			{
				name.Addresses = new List<Address>();
			}
			name.Addresses.Add(new Address
			{
				Operation = OperationType.Insert,
				TypeCode = addressType?.Code ?? _addressType.Code,
				StreetAddress = street,
				City = city,
				StateCode = state,
				Zip = zip,
				TempID = tempID
			});
			return name;
		}

		public static Name AddPhone(this Name name, PhoneType phoneType, string number = "555-55-5555", string tempID = null)
		{
			if (phoneType == null) throw new ArgumentNullException(nameof(phoneType));
			if (name.Phones == null)
			{
				name.Phones = new List<Phone>();
			}
			name.Phones.Add(new Phone
			{
				Operation = OperationType.Insert,
				TypeCode = phoneType.Code,
				Number = number,
				TempID = tempID
			});
			return name;
		}

		public static Name AddEmail(this Name name, EmailType emailType, string emailAddress = "no-reply@journaltech.com", string tempID = null)
		{
			if (emailType == null) throw new ArgumentNullException(nameof(emailType));
			if (name.Emails == null)
			{
				name.Emails = new List<Email>();
			}
			name.Emails.Add(new Email
			{
				Operation = OperationType.Insert,
				TypeCode = emailType.Code,
				Address = emailAddress,
				TempID = tempID
			});
			return name;
		}

		public static CaseDocument Initialize(this CaseDocument document, string documentData, string caseID = null, DocumentType documentType = null, string fileName = null)
		{
			document.Operation = OperationType.Insert;
			document.CaseID = caseID;
			document.FileName = fileName ?? $"{Guid.NewGuid()}.txt";
			document.TypeCode = documentType?.Code ?? _documentType.Code;
			document.DocumentData = documentData;
			return document;
		}

		public static CaseEvent Initialize(this CaseEvent evt, DateTime startDate, EventType eventType = null, string caseID = null, DateTime? endDate = null)
		{
			evt.Operation = OperationType.Insert;
			evt.TypeCode = eventType?.Code ?? _eventType.Code;
			evt.EventDate = startDate;
			evt.EventEndDate = endDate.HasValue && endDate.Value > startDate ? endDate.Value : startDate.AddMinutes(1);
			evt.CaseID = caseID;

			return evt;
		}

		public static double TimeAction(Action action)
		{
			Stopwatch watch = Stopwatch.StartNew();
			action.Invoke();
			watch.Stop();

			return watch.ElapsedMilliseconds / 1000.0;
		}

		private static void ProcessCaseDocuments(this Case cse, Action<CaseDocument> processor)
		{
			if (!(cse.CaseDocuments?.Count > 0)) return;
			foreach (CaseDocument document in cse.CaseDocuments)
			{
				processor(document);
			}
		}
	}
}