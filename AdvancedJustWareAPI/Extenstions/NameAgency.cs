namespace AdvancedJustWareAPI.Extenstions
{
	public class NameAgency
	{
		public NameAgency(int nameID, string agencyCode)
		{
			NameID = nameID;
			AgencyCode = agencyCode;
		}

		public int NameID { get; protected set; }

		public string AgencyCode { get; protected set; }
	}
}