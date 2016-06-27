using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using AdvancedJustWareAPI.api;
using AdvancedJustWareAPI.api.extra;
using NLog;

namespace AdvancedJustWareAPI.Extenstions
{
	public static class ApiExtensions
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private static readonly InvolveType _primaryInvolveType;
		private static readonly CaseStatusType _statusType;
		private static readonly CaseType _caseType;
		private static readonly AgencyType _agencyType;
		private static readonly DocumentType _documentType;
		private static readonly EventType _eventType;
		private static readonly AddressType _addressType;
		private static readonly EmailType _emailType;
		private static readonly PhoneType _phoneType;
		private static readonly Statute _statute;
		private static readonly int _callerNameID;

		static ApiExtensions()
		{
			IJustWareApi client = null;
			try
			{
				client = ApiClientFactory.CreateApiClient(ensureAutoGenerationEnabled: false);
				_primaryInvolveType = client.GetCode<InvolveType>("MasterCode = 1");
				_statusType = client.GetCode<CaseStatusType>("MasterCode = 1");
				_caseType = client.GetCode<CaseType>();
				_agencyType = client.GetCode<AgencyType>("IsAccountOwner = true");
				_documentType = client.GetCode<DocumentType>();
				_eventType = client.GetCode<EventType>("MasterCode = 1");
				_addressType = client.GetCode<AddressType>();
				_emailType = client.GetCode<EmailType>();
				_phoneType = client.GetCode<PhoneType>();
				_statute = client.GetStatute();
				_callerNameID = client.GetCallerNameID();
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

		public static int CallerNameID => _callerNameID;

		public static void Dispose(this IDataConversionService client)
		{
			IDisposable disposable = client as IDisposable;
			disposable?.Dispose();
		}

		public static void Dispose(this IJustWareApi client)
		{
			IDisposable disposable = client as IDisposable;
			disposable?.Dispose();
		}

		public static T GetCode<T>(this IJustWareApi client, string query = "1=1")
			where T : DataContractBase
		{
			string typeName = typeof(T).Name;
			string suffix = typeName.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? "es" : "s";
			string methodName = $"Find{typeName}{suffix}";
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
					actualName.Operation = OperationType.None;
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
				actualCase.ProcessCollectionOfType<CaseDocument>(doc =>
				{
					doc.Operation = OperationType.None;
				});
				actualCase.ProcessCollectionOfType<Charge>(charge =>
				{
					charge.TempID = Guid.NewGuid().ToString();
				});
				double ellapsedSeconds = TimeAction(() =>
				{
					var keys = client.Submit(actualCase);
					actualCase.ID = keys.First(k => k.TypeName.Equals(nameof(Case))).NewCaseID;
					actualCase.Operation = OperationType.None;
					actualCase.ProcessCollectionOfType<CaseDocument>(doc =>
					{
						doc.CaseID = actualCase.ID;
						client.UploadDocument(doc);
					});
					actualCase.ProcessCollectionOfType<Charge>(charge =>
					{
						Key chargeKey = keys.FirstOrDefault(k => k.TempID != null && k.TempID.Equals(charge.TempID));
						if (chargeKey == null)
						{
							_logger.Warn("Charge with TempID '{0}' was not found", charge.TempID);
							return;
						}
						charge.ID = chargeKey.NewID;
						charge.ChargeInvolvedNames = client.FindChargeInvolvedNames($"ChargeID = {charge.ID}", null);
						charge.Operation = OperationType.None;
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

		public static ApplicationPerson GetFirstNameInAgency(this IJustWareApi client, int agencyMasterCode)
		{
			try
			{
				ApplicationPerson appPerson = null;

				double ellapsedSeconds = TimeAction(() =>
				{
					var masterCodeAgencies = client.FindAgencyTypes($"MasterCode = {agencyMasterCode}", null);
					foreach (AgencyType agency in masterCodeAgencies)
					{
						appPerson = client.FindApplicationPersons($"AgencyCode = \"{agency.Code}\"", null).FirstOrDefault();
						if (appPerson != null) break;
					}
				});
				if (appPerson == null) return null;

				_logger.Info("Found name({0}) in agency({1}) with master code {2} in {3} seconds",
					appPerson.NameID,
					appPerson.AgencyCode,
					agencyMasterCode,
					ellapsedSeconds);
				return appPerson;
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

		public static string DownloadFromApi(this IJustWareApi client, CaseDocument document, string username = ApiClientFactory.TC_USER, string password = ApiClientFactory.TC_USER_PASSWORD)
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

		public static Case Initialize(this Case cse, Name primaryName = null)
		{
			CaseInvolvedName involvement = new CaseInvolvedName
			{
				Operation = OperationType.Insert,
				InvolvementCode = _primaryInvolveType.Code,
			};
			if (primaryName != null)
			{
				involvement.Name = primaryName;
			}
			else
			{
				involvement.NameID = _callerNameID;
			}
			cse.Operation = OperationType.Insert;
			cse.StatusCode = _statusType.Code;
			cse.TypeCode = _caseType.Code;
			cse.AgencyAddedByCode = _agencyType.Code;
			cse.StatusDate = DateTime.Now;
			cse.ReceivedDate = DateTime.Now;
			cse.CaseInvolvedNames = new List<CaseInvolvedName> { involvement };
			return cse;
		}

		public static Case AddInvolvement(this Case cse, CaseInvolvedName caseInvolvedName)
		{
			if (cse.CaseInvolvedNames == null)
			{
				cse.CaseInvolvedNames = new List<CaseInvolvedName>();
			}
			cse.CaseInvolvedNames.Add(caseInvolvedName);
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

		public static Case AddCharge(this Case cse, Charge charge = null)
		{
			if (charge == null)
			{
				charge = new Charge().Initialize(number: 1);
			}

			if (cse.Charges == null)
			{
				cse.Charges = new List<Charge>();
			}
			cse.Charges.Add(charge);
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

		public static Name AddPhone(this Name name, string number = "555-55-5555", PhoneType phoneType = null, string tempID = null)
		{
			if (name.Phones == null)
			{
				name.Phones = new List<Phone>();
			}
			name.Phones.Add(new Phone
			{
				Operation = OperationType.Insert,
				TypeCode = phoneType?.Code ?? _phoneType.Code,
				Number = number,
				TempID = tempID
			});
			return name;
		}

		public static Name AddEmail(this Name name, string emailAddress = "no-reply@journaltech.com", EmailType emailType = null, string tempID = null)
		{
			if (name.Emails == null)
			{
				name.Emails = new List<Email>();
			}
			name.Emails.Add(new Email
			{
				Operation = OperationType.Insert,
				TypeCode = emailType?.Code ?? _emailType.Code,
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

		public static Charge Initialize(this Charge charge, short number, string caseID = null)
		{
			charge.Operation = OperationType.Insert;
			charge.ChargeNumber = number;
			charge.StatuteID = _statute.ID;
			charge.CaseID = caseID;
			return charge;
		}

		public static Statute Initialize(this Statute statute, string code = "JTI-1", string description = "Test Statute")
		{
			statute.Operation = OperationType.Insert;
			statute.Code = code;
			statute.Description = description;
			return statute;
		}

		public static Statute GetStatute(this IJustWareApi client)
		{
			Statute statute = client.GetCode<Statute>();

			if (statute != null) return statute;

			IJustWareApi adminClient = null;
			try
			{
				//Statutes require Admin level to insert, modify, or delete
				adminClient = ApiClientFactory.CreateApiClient(admin: true);
				statute = new Statute().Initialize();
				List<Key> keys = adminClient.Submit(statute);
				statute.ID = keys.Single(k => k.TypeName.Equals(nameof(Statute))).NewID;
				statute.Operation = OperationType.None;
				return statute;
			}
			finally
			{
				adminClient.Dispose();
			}
		}

		public static Account GetLiabilityAccount(this IJustWareApi client, AgencyType agency, int nameID)
		{
			Account account = client.GetCode<Account>($"AgencyCode = \"{agency.Code}\" && TypeCode = \"LBLTY\"");
			if (account != null) return account;
			IJustWareApi adminClient = null;
			try
			{
				adminClient = ApiClientFactory.CreateApiClient(admin: true);
				AccountType liabilityType = client.GetCode<AccountType>("MasterCode = 2");
				AccountStatus openStatus = client.GetCode<AccountStatus>("MasterCode = 1");
				var liabilityAccount = new Account
				{
					Operation = OperationType.Insert,
					TypeCode = liabilityType.Code,
					StatusCode = openStatus.Code,
					AgencyCode = _agencyType.Code,
					OwnerAgencyCode = _agencyType.Code,
					Name = $"{liabilityType.Code}-{_agencyType.Description}",
					Number = "DL-2",
					NameID = nameID
				};
				var keys = adminClient.Submit(liabilityAccount);
				account = client.GetAccount(keys.Single(k => k.TypeName.Equals(nameof(Account))).NewID, null);
			}
			finally
			{
				adminClient?.Dispose();
			}
			return account;
		}

		public static double TimeAction(Action action)
		{
			Stopwatch watch = Stopwatch.StartNew();
			action.Invoke();
			watch.Stop();

			return watch.ElapsedMilliseconds / 1000.0;
		}

		private static void ProcessCollectionOfType<T>(this Case cse, Action<T> processor)
			where T : DataContractBase
		{
			PropertyInfo property = typeof(Case).GetProperties().SingleOrDefault(p => p.PropertyType.IsAssignableFrom(typeof(List<T>)));
			List<T> collection = property?.GetValue(cse) as List<T>;
			if (collection == null) return;

			if (!collection.Any()) return;
			foreach (T item in collection)
			{
				processor(item);
			}
		}
	}
}