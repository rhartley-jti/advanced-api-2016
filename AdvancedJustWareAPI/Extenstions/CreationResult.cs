using System.Collections.Generic;
using AdvancedJustWareAPI.api;

namespace AdvancedJustWareAPI.Extenstions
{
	public class CreationResult
	{
		public CreationResult(IEnumerable<Key> keys, double ellapsedSeconds)
		{
			Keys = keys;
			EllapsedSeconds = ellapsedSeconds;
		}

		public IEnumerable<Key> Keys { get; private set; }

		public double EllapsedSeconds { get; private set; }
	}
}