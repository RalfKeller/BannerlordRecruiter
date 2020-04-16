using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using System.Linq;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using System.Windows.Forms;

namespace Recruiter
{
    public class RecruiterSubModule : MBSubModuleBase
    {
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
			if (game.GameType is Campaign)
			{
				CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;
				try
				{
					campaignGameStarter.AddBehavior(new RecruiterBehaviour());
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString());
				}
			}
		}
    }
}
