using System.Collections.Generic;
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

		public string GetFirstCaseID()
		{
			return Keys.FirstOrDefault(k => k.TypeName.Equals("Case"))?.NewCaseID;
		}

		public int? GetFirstEntityID<T>()
		{
			string typeName = typeof(T).Name;
			return Keys.FirstOrDefault(k => k.TypeName.Equals(typeName))?.NewID;
		}
	}
}