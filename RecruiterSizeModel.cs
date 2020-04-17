using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Party;

namespace Recruiter
{
	public class RecruiterSizeModel : DefaultPartySizeLimitModel
	{
		// Token: 0x0600003C RID: 60 RVA: 0x000033F8 File Offset: 0x000015F8
		public override int GetPartyMemberSizeLimit(PartyBase party, StatExplainer explanation = null)
		{
			if(party.Owner != null && party.Owner.IsHumanPlayerCharacter)
			{
				if (party.Name.ToString().EndsWith("Recruiter"))
				{
					return 200;
				}
			}
			return base.GetPartyMemberSizeLimit(party, explanation);

		}

		public override int GetPartyPrisonerSizeLimit(PartyBase party, StatExplainer explanation = null)
		{
			if (party.Owner != null && party.Owner.IsHumanPlayerCharacter)
			{
				if (party.Name.ToString().EndsWith("Recruiter"))
				{
					return 200;
				}
			}
				return base.GetPartyPrisonerSizeLimit(party, explanation);
			
		}
	}
}
