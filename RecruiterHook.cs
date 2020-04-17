using SandBox.GauntletUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.Engine.Screens;
using TaleWorlds.GauntletUI;

namespace Recruiter
{
    public class RecruiterHook : Widget
    {
        public RecruiterHook(UIContext context) : base(context)
        {
            if (ScreenManager.TopScreen is GauntletClanScreen)
            {
                GauntletClanScreen screen = (GauntletClanScreen)ScreenManager.TopScreen;
                ClanVM clanVM = (ClanVM)screen.GetField("_dataSource");

                foreach (PartyBase recruiter in Hero.MainHero.OwnedParties.Where(party => party.Name.ToString().EndsWith("Recruiter")))
                {
                    ClanPartyItemVM partyItem = new ClanPartyItemVM(recruiter, null, null);
                    partyItem.PartyLocationText = "Travelling to " + recruiter.MobileParty.TargetSettlement;
                    partyItem.PartyMoraleText = recruiter.MobileParty.PartyTradeGold + "G";
                    clanVM.ClanParties.Garrisons.Add(partyItem);
                }

            }
        }

        protected override void OnUpdate(float dt)
        {
            ListPanel parentList = (ListPanel)this.ParentWidget;
            foreach (Widget widget in parentList.Children)
            {
                if(widget.Id == "GarrisonsList")
                {
                    ListPanel garissonsList = (ListPanel)widget;
                    foreach (ButtonWidget item in garissonsList.Children)
                    {
                        foreach (Widget child in item.Children[0].Children)
                        {
                            if (child is RichTextWidget)
                            {
                                RichTextWidget textWidget = (RichTextWidget)child;
                                if(textWidget.Text.EndsWith("Recruiter"))
                                {
                                    item.IsEnabled = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
