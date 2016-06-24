﻿using System.Collections.Generic;
using System.Linq;
using AdvancedJustWareAPI.api;

namespace AdvancedJustWareAPI.Extenstions
{
	public class ApiCreateResult
	{
		public ApiCreateResult(IEnumerable<Key> keys, double ellapsedSeconds)
		{
			Keys = keys;
			EllapsedSeconds = ellapsedSeconds;
		}

		public IEnumerable<Key> Keys { get; private set; }

		public double EllapsedSeconds { get; private set; }

		private string _firstCaseID;
		public string FirstCaseID
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_firstCaseID))
				{
					_firstCaseID = Keys.FirstOrDefault(k => k.TypeName.Equals("Case"))?.NewCaseID;
				}
				return _firstCaseID;
			}
		}

		private int _firstNameID;

		public int FirstNameID
		{
			get
			{
				if (_firstNameID == default(int))
				{
					int? firstNameID = GetFirstEntityID<Name>();
					if (firstNameID.HasValue)
					{
						_firstNameID = firstNameID.Value;
					}
				}
				return _firstNameID;
			}
		}

		public int? GetFirstEntityID<T>()
		{
			string typeName = typeof(T).Name;
			return Keys.FirstOrDefault(k => k.TypeName.Equals(typeName))?.NewID;
		}
	}
}