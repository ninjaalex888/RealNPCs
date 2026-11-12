using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Hearsay
	{
		[DataMember(EmitDefaultValue = false)]
		public string FromId;

		[DataMember(EmitDefaultValue = false)]
		public string FromName;

		[DataMember(EmitDefaultValue = false)]
		public string Relation;

		[DataMember(EmitDefaultValue = false)]
		public bool FromWoman;

		[DataMember(EmitDefaultValue = false)]
		public string Text;

		[DataMember(EmitDefaultValue = false)]
		public int Hops;

		[DataMember(EmitDefaultValue = false)]
		public long At;

		[DataMember(EmitDefaultValue = false)]
		public bool Told;

		[DataMember(EmitDefaultValue = false)]
		public bool Thought;

		public string Key => FromId + "|" + Text;

		public const int RelativeKept = 12;

		/// A relative's hearsay kept to the newest RelativeKept, the oldest let go first, but never a death (SWEEP item 1's second sweep,
		/// and the halfway review: a "died" is the relative's own lasting fact: the kin who know you killed him, one notice a death;
		/// Forget keeps it too).
		public static void CapRelative(System.Collections.Generic.List<Hearsay> heard)
		{
			if (heard == null)
				return;
			for (int i = 0; heard.Count > RelativeKept && i < heard.Count;)
			{
				if (heard[i] != null && heard[i].Text == "died")
					i++;
				else
					heard.RemoveAt(i);
			}
		}
	}
}
