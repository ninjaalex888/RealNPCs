using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Rumor
	{
		[DataMember(EmitDefaultValue = false)]
		public string FromId;

		[DataMember(EmitDefaultValue = false)]
		public bool NotByYou;   // a death someone else caused (an execution on the road): no avenger, no grief letter to him

		[DataMember(EmitDefaultValue = false)]
		public string FromName;

		[DataMember(EmitDefaultValue = false)]
		public bool FromWoman;

		[DataMember(EmitDefaultValue = false)]
		public string Text;

		[DataMember(EmitDefaultValue = false)]
		public int Hops = 1;

		[DataMember(EmitDefaultValue = false)]
		public string ToId;

		[DataMember(EmitDefaultValue = false)]
		public string ToRelative;

		[DataMember(EmitDefaultValue = false)]
		public string Relation;

		[DataMember(EmitDefaultValue = false)]
		public string How;

		[DataMember(EmitDefaultValue = false)]
		public long SentAt;

		[DataMember(EmitDefaultValue = false)]
		public long DueAt;

		// word of a death to kin (How "death"; 2026-10-31, Lex: "there was no one around to see the death so the word
		// shouldn't have spread"): nobody saw the killing, or it was self-defence. NotByYou is set too. Delivery holds
		// until the body is found (Found says how) or the long delay. An older save's death rumour has neither: witnessed.
		[DataMember(EmitDefaultValue = false)]
		public bool Unwitnessed;

		[DataMember(EmitDefaultValue = false)]
		public string Found;    // how the body was found: "buried", "a lawman carried the body off"

		[DataMember(EmitDefaultValue = false)]
		public string DeedId;   // the killing's deed, when one was made: someone coming upon the body and seeing him makes it witnessed
	}
}
