using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace Recruiter
{
    class RecruiterBehaviour : CampaignBehaviorBase
    {
		Random rand = new Random();
		List<RecruiterProperties> recruiterProperties = new List<RecruiterProperties>();
		private void OnSessionLaunched(CampaignGameStarter obj)
		{
			//this.trackPatrols();
			try
			{
				this.AddRecruiterMenu(obj);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Something screwed up in adding patrol menu. " + ex.ToString());
			}
			try
			{
				//this.AddPatrolDialog(obj);
			}
			catch (Exception ex2)
			{
				MessageBox.Show("Something screwed up in adding patrol dialog. " + ex2.ToString());
			}
		}

		public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.RecruiterHourlyAi));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyAITick));
            CampaignEvents.OnPartyDisbandedEvent.AddNonSerializedListener(this, new Action<MobileParty>(this.DisbandPatrol));
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.RecruiterDestroyed));
        }

		private void RecruiterDestroyed(MobileParty recruiter, PartyBase arg2)
		{
			if (recruiter != null)
			{
				RecruiterProperties props = recruiterProperties.FirstOrDefault(prop => prop.party == recruiter);
				if (props != null && recruiterProperties.Contains(props))
				{
					InformationManager.DisplayMessage(new InformationMessage("Your recruiter bringing recruits to " + recruiter.Name.ToString().Substring(0, recruiter.Name.ToString().Length - " Recruiter".Length) + " has been killed!", new Color(1f, 0f, 0f)));
					recruiterProperties.Remove(props);
					recruiter.RemoveParty();
				}
			}
		}

		private void DisbandPatrol(MobileParty recruiter)
		{
			if(recruiter != null)
			{
				RecruiterProperties props = recruiterProperties.FirstOrDefault(prop => prop.party == recruiter);
				if (props != null && recruiterProperties.Contains(props))
				{
					recruiterProperties.Remove(props);
					recruiter.RemoveParty();
				}
			}
		}

		private void OnDailyAITick()
		{
			RecruiterHourlyAi();
		}

		private void RecruiterHourlyAi()
		{
			List<RecruiterProperties> toBeDeleted = new List<RecruiterProperties>();

			foreach (RecruiterProperties prop in recruiterProperties)
			{
				MobileParty recruiter = prop.party;
				if (recruiter.HomeSettlement == null)
				{
					toBeDeleted.Add(prop);
					break;
				}
				if (recruiter.PartyTradeGold < 20)
				{
					recruiter.SetMoveGoToSettlement(recruiter.HomeSettlement);
				}

				if (recruiter.CurrentSettlement == recruiter.HomeSettlement)
				{
					if (!(recruiter.HomeSettlement.IsCastle || recruiter.HomeSettlement.IsTown))
					{
						throw new Exception("Recruiter Home Settelment is not a castle nor a town. How could this happen?");
					}

					recruitAll(recruiter, recruiter.CurrentSettlement);
					Settlement nearestWithRecruits = findNearestSettlementWithRecruitableRecruits(recruiter);
					if (nearestWithRecruits != null)
					{
						recruiter.SetMoveGoToSettlement(nearestWithRecruits);
					}

					else
					{
						bool hasGarisson = false;
						foreach (MobileParty party in recruiter.HomeSettlement.Parties)
						{
							if (party.IsGarrison)
							{
								hasGarisson = true;
								break;
							}
						}
						if (!hasGarisson)
							recruiter.HomeSettlement.AddGarrisonParty();
						MobileParty garrision = recruiter.HomeSettlement.Parties.First(party => party.IsGarrison);

						int soldierCount = recruiter.MemberRoster.TotalManCount;
						foreach (TroopRosterElement rosterElement in recruiter.MemberRoster)
						{
							int healthy = rosterElement.Number - rosterElement.WoundedNumber;
							garrision.MemberRoster.AddToCounts(rosterElement.Character, healthy, false, rosterElement.WoundedNumber);
						}
						InformationManager.DisplayMessage(new InformationMessage("Your recruiter brought " + soldierCount + " soldiers to " + recruiter.HomeSettlement + ".", new Color(0f, 1f, 0f)));
						toBeDeleted.Add(prop);
						recruiter.RemoveParty();
						continue;
					}

				}
				else if (recruiter.CurrentSettlement != null)
				{
					recruitAll(recruiter, recruiter.CurrentSettlement);
					Settlement temp = findNearestSettlementWithRecruitableRecruits(recruiter);
					if (temp != null)
						recruiter.SetMoveGoToSettlement(findNearestSettlementWithRecruitableRecruits(recruiter));
				}
				else
				{
					Settlement closestWithRecruits = findNearestSettlementWithRecruitableRecruits(recruiter);
					if (closestWithRecruits == null)
					{
						recruiter.SetMoveGoToSettlement(recruiter.HomeSettlement);
						return;
					}
					recruiter.SetMoveGoToSettlement(findNearestSettlementWithRecruitableRecruits(recruiter));
				}
			}

			foreach (RecruiterProperties prop in toBeDeleted)
			{
				recruiterProperties.Remove(prop);
			}

		}

		private bool hasSettlementRecruits(Settlement settlement, MobileParty recruiter)
		{
			DefaultPartyWageModel wageModel = new DefaultPartyWageModel();
			if (settlement.IsRaided)
			{
				return false;
			}
			foreach (Hero notable in settlement.Notables)
			{
				for (int i = 0; i < notable.VolunteerTypes.Length; i++)
				{
					CharacterObject character = notable.VolunteerTypes[i];
					if (character == null)
					{
						continue;
					}

					if (wageModel.GetTroopRecruitmentCost(character, Hero.MainHero) > recruiter.PartyTradeGold || !hasSufficientRelationsship(notable, i))
					{
						continue;
					}

					return true;
				}
			}
			return false;
		}

		private Settlement findNearestSettlementWithRecruitableRecruits(MobileParty recruiter)
		{
			RecruiterProperties props = recruiterProperties.First(prop => prop.party == recruiter);
			CultureObject onlyCulture = props.SearchCulture;
			IEnumerable<Settlement> settlementsWithRecruits;
			if (onlyCulture != null)
			{
				settlementsWithRecruits = Settlement.All.
					Where(settlement => settlement.GetNumberOfAvailableRecruits() > 0 && 
					!settlement.OwnerClan.IsAtWarWith(Hero.MainHero.Clan) && 
					settlement.Culture.Name.ToString().Equals(onlyCulture.Name.ToString()));
			}
			else
			{
				settlementsWithRecruits = Settlement.All.
					Where(settlement => settlement.GetNumberOfAvailableRecruits() > 0 
					&& !settlement.OwnerClan.IsAtWarWith(Hero.MainHero.Clan));
			}

			DefaultPartyWageModel wageModel = new DefaultPartyWageModel();

			Settlement nearest = null;
			float shortestDistance = float.MaxValue; 
			
			foreach (Settlement settlement in settlementsWithRecruits)
			{

				if(!hasSettlementRecruits(settlement, recruiter))
				{
					continue;
				}

				Vec2 position = settlement.GatePosition;
				float distance = position.Distance(recruiter.GetPosition2D);
				if (distance < shortestDistance)
				{
					shortestDistance = distance;
					nearest = settlement;
				}
			}

			return nearest;
		}

		private void recruitAll(MobileParty recruiter, Settlement settlement)
		{
			DefaultPartyWageModel wageModel = new DefaultPartyWageModel();
			foreach (Hero notable in settlement.Notables)
			{
				List<CharacterObject> recruitables = HeroHelper.GetVolunteerTroopsOfHeroForRecruitment(notable);
				for (int recruitableIndex = 0; recruitableIndex < recruitables.Count; recruitableIndex++)
				{
					CharacterObject recruitable = recruitables[recruitableIndex];
					if (recruitable != null && hasSufficientRelationsship(notable, recruitableIndex))
					{

						int recruitCost = wageModel.GetTroopRecruitmentCost(recruitable, Hero.MainHero);

						if(recruitCost > recruiter.PartyTradeGold)
						{
							continue;
						}

						recruiter.MemberRoster.AddToCounts(recruitable, 1);
						GiveGoldAction.ApplyForPartyToSettlement(recruiter.Party, settlement, recruitCost);
						for (int i = 0; i < notable.VolunteerTypes.Length; i++)
						{
							if (recruitable == notable.VolunteerTypes[i])
							{
								notable.VolunteerTypes[i] = null;
								break;
							}
						}
					}
				} 
			}
		}

		private bool hasSufficientRelationsship(Hero notable, int index)
		{
			switch (index)
			{
				case 0: return true; //TODO: Check
				case 1:
					return notable.GetRelationWithPlayer() >= 0;
				case 2:
					return notable.GetRelationWithPlayer() >= 0;
				case 3:
					return notable.GetRelationWithPlayer() >= 5;
				case 4:
					return notable.GetRelationWithPlayer() >= 10;
				case 5:
					return notable.GetRelationWithPlayer() >= 20;
				default:
					return false;
			}
		}

		public override void SyncData(IDataStore dataStore)
        {
			//List<MobileParty> allRecruitersLegacy = new List<MobileParty>();
			//dataStore.SyncData<List<MobileParty>>("allRecruiters", ref allRecruitersLegacy);
			//dataStore.SyncData<Dictionary<MobileParty, RecruiterProperties>>("allRecruitersToProperties", ref allRecruitersToProperties);

			//foreach (MobileParty recruiter in allRecruitersLegacy)
			//{
			//	if(!allRecruitersToProperties.ContainsKey(recruiter))
			//	{
			//		allRecruitersToProperties.Add(recruiter, new RecruiterProperties());
			//	}
			//}
			dataStore.SyncData<List<RecruiterProperties>>("recruiterProperties", ref recruiterProperties);
        }

		public List<CultureObject> getPossibleCultures()
		{
			IEnumerable<Settlement> settlements = Settlement.All;

			List<CultureObject> returnList = new List<CultureObject>();
			foreach (Settlement settlement in settlements)
			{
				if(!returnList.Contains(settlement.Culture))
				{
					returnList.Add(settlement.Culture);
				}
			}

			return returnList;
		}

		public void AddRecruiterMenu(CampaignGameStarter obj)
		{
			GameMenuOption.OnConditionDelegate hireRecruiterDelegate = delegate (MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
				return Settlement.CurrentSettlement.OwnerClan == Clan.PlayerClan;
			};
			GameMenuOption.OnConsequenceDelegate hireRecruiterConsequenceDelegate = delegate (MenuCallbackArgs args)
			{
				GameMenu.SwitchToMenu("recruiter_culture_menu");
			};

			obj.AddGameMenu("recruiter_culture_menu", "The Chamberlain asks you what culture your recruits should be.", null, GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.none, null);

			RecruiterProperties props = new RecruiterProperties();
			foreach (CultureObject culture in getPossibleCultures())
			{
				obj.AddGameMenuOption("recruiter_culture_menu", "recruiter_" + culture.GetName().ToString(), culture.GetName().ToString(),
					delegate (MenuCallbackArgs args)
				{
					return Settlement.All.Count(settlement => settlement.Culture == culture && settlement.OwnerClan != null && !settlement.OwnerClan.IsAtWarWith(Hero.MainHero.Clan)) > 0;
				},
				delegate (MenuCallbackArgs args)
				{
					props = new RecruiterProperties();
					props.SearchCulture = culture;
					GameMenu.SwitchToMenu("recruiter_pay_menu");
				});

			}

			obj.AddGameMenuOption("town_keep", "recruiter_buy_recruiter", "Hire a Recruiter", hireRecruiterDelegate, hireRecruiterConsequenceDelegate, false, 4, false);
			obj.AddGameMenuOption("castle", "recruiter_buy_recruiter", "Hire a Recruiter", hireRecruiterDelegate, hireRecruiterConsequenceDelegate, false, 4, false);

			obj.AddGameMenu("recruiter_pay_menu", "The Chamberlain asks you for how many denars he should buy recruits.", null, GameOverlays.MenuOverlayType.None, GameMenu.MenuFlags.none, null);
			
			obj.AddGameMenuOption("recruiter_pay_menu", "recruiter_pay_small", "Pay 500.", delegate (MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
				string stringId = Settlement.CurrentSettlement.StringId;
				int cost = 500;
				bool flag = cost >= Hero.MainHero.Gold;
				return !flag;
			}, delegate (MenuCallbackArgs args)
			{
				string stringId = Settlement.CurrentSettlement.StringId;
				int cost = 500;
				bool flag = cost <= Hero.MainHero.Gold;
				if (flag)
				{
					GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost, false);
					MobileParty item = this.spawnRecruiter(Settlement.CurrentSettlement, cost, props);
				}
				GameMenu.SwitchToMenu("castle");
			}, false, -1, false);
			obj.AddGameMenuOption("recruiter_pay_menu", "recruiter_pay_medium", "Pay 2500.", delegate (MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
				string stringId = Settlement.CurrentSettlement.StringId;
				int cost = 2500;
				bool flag = cost >= Hero.MainHero.Gold;
				return !flag;
			}, delegate (MenuCallbackArgs args)
			{
				string stringId = Settlement.CurrentSettlement.StringId;
				int cost = 2500;
				bool flag = cost <= Hero.MainHero.Gold;
				if (flag)
				{
					GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost, false);
					MobileParty item = this.spawnRecruiter(Settlement.CurrentSettlement, cost, props);
				}
				GameMenu.SwitchToMenu("castle");
			}, false, -1, false);
			obj.AddGameMenuOption("recruiter_pay_menu", "recruiter_pay_large", "Pay 5000.", delegate (MenuCallbackArgs args)
			{
				args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
				string stringId = Settlement.CurrentSettlement.StringId;
				int cost = 5000;
				bool flag = cost >= Hero.MainHero.Gold;
				return !flag;
			}, delegate (MenuCallbackArgs args)
			{
				string stringId = Settlement.CurrentSettlement.StringId;
				int cost = 5000;
				bool flag = cost <= Hero.MainHero.Gold;
				if (flag)
				{
					GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost, false);
					MobileParty item = this.spawnRecruiter(Settlement.CurrentSettlement, cost, props);
				}
				GameMenu.SwitchToMenu("castle");
			}, false, -1, false);
			obj.AddGameMenuOption("recruiter_pay_menu", "recruiter_leave", "Leave", new GameMenuOption.OnConditionDelegate(this.game_menu_just_add_leave_conditional), new GameMenuOption.OnConsequenceDelegate(this.game_menu_switch_to_village_menu), false, -1, false);
		}

		private void game_menu_switch_to_village_menu(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("castle");
		}

		public MobileParty spawnRecruiter(Settlement settlement, int cash, RecruiterProperties props)
		{
			PartyTemplateObject defaultPartyTemplate = settlement.Culture.DefaultPartyTemplate;
			int numberOfCreated = defaultPartyTemplate.NumberOfCreated;
			defaultPartyTemplate.IncrementNumberOfCreated();
			MobileParty mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(settlement.OwnerClan.StringId + "_" + numberOfCreated);
			TextObject textObject = new TextObject("{RECRUITER_SETTLEMENT_NAME} Recruiter", null);
			textObject.SetTextVariable("RECRUITER_SETTLEMENT_NAME", settlement.Name);
			mobileParty.InitializeMobileParty(textObject, defaultPartyTemplate, settlement.GatePosition, 0f, 0f, MobileParty.PartyTypeEnum.Default, 1);
			mobileParty.PartyTradeGold = cash;
			this.InitRecruiterParty(mobileParty, textObject, settlement.OwnerClan, settlement);
			mobileParty.Aggressiveness = 0f;
			props.party = mobileParty;
			recruiterProperties.Add(props);
			mobileParty.SetMoveGoToSettlement(findNearestSettlementWithRecruitableRecruits(mobileParty));
			return mobileParty;
		}

		public void InitRecruiterParty(MobileParty patrolParty, TextObject name, Clan faction, Settlement homeSettlement)
		{
			patrolParty.Name = name;
			patrolParty.IsMilitia = true;
			patrolParty.HomeSettlement = homeSettlement;
			patrolParty.Party.Owner = faction.Leader;
			patrolParty.SetInititave(0f, 0f, 1E+08f);
			patrolParty.Party.Visuals.SetMapIconAsDirty();
			generateFood(patrolParty);
		}
		public void generateFood(MobileParty patrolParty)
		{
			foreach (ItemObject itemObject in ItemObject.All)
			{
				bool isFood = itemObject.IsFood;
				if (isFood)
				{
					int num = MBRandom.RoundRandomized((float)patrolParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * 1f * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
					bool flag = num > 0;
					if (flag)
					{
						patrolParty.ItemRoster.AddToCounts(itemObject, num, true);
					}
				}
			}
		}
		private bool game_menu_just_add_leave_conditional(MenuCallbackArgs args)
		{
			args.optionLeaveType = GameMenuOption.LeaveType.Leave;
			return true;
		}

		public class BannerlordRecruiterSaveDefiner : SaveableTypeDefiner
		{
			// Token: 0x06000043 RID: 67 RVA: 0x000034F1 File Offset: 0x000016F1
			public BannerlordRecruiterSaveDefiner() : base(91215129)
			{
			}

			// Token: 0x06000044 RID: 68 RVA: 0x00003500 File Offset: 0x00001700
			protected override void DefineClassTypes()
			{
				base.AddClassDefinition(typeof(RecruiterProperties), 1);
			}

			// Token: 0x06000045 RID: 69 RVA: 0x00003515 File Offset: 0x00001715
			protected override void DefineContainerDefinitions()
			{
				base.ConstructContainerDefinition(typeof(List<RecruiterProperties>));
			}
		}
	}
}
