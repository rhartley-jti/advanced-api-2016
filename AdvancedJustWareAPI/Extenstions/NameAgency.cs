using AdvancedJustWareAPI.api;

namespace AdvancedJustWareAPI.Extenstions
{
	public class NameAgency
	{
		public NameAgency(int nameID, AgencyType agencyType)
		{
			NameID = nameID;
			AgencyType = agencyType;
		}

		public int NameID { get; protected set; }

		public AgencyType AgencyType { get; protected set; }
	}
}