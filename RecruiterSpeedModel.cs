using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;

namespace Recruiter
{
	public class RecruiterSpeedModel : DefaultPartySpeedCalculatingModel
	{
		// Token: 0x0600003C RID: 60 RVA: 0x000033F8 File Offset: 0x000015F8
		public override float CalculateFinalSpeed(MobileParty mobileParty, float baseSpeed, StatExplainer explanation)
		{
			float num = base.CalculateFinalSpeed(mobileParty, baseSpeed, explanation);
			bool flag = mobileParty.Name.Contains("Recruiter");
			if (flag)
			{
				num += 10f;
			}
			return num;
		}

		public override float CalculatePureSpeed(MobileParty mobileParty, StatExplainer explanation, int additionalTroopOnFootCount = 0, int additionalTroopOnHorseCount = 0)
		{
			float num = base.CalculatePureSpeed(mobileParty, explanation, additionalTroopOnFootCount, additionalTroopOnHorseCount);
			bool flag = mobileParty.Name.Contains("Recruiter");
			if (flag)
			{
				num += 10f;
			}
			return num;
		}
	}
}
