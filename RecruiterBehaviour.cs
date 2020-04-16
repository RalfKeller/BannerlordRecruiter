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

namespace Recruiter
{
    class RecruiterBehaviour : CampaignBehaviorBase
    {
		Random rand = new Random();
		List<MobileParty> allRecruiters = new List<MobileParty>();
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
        }

		private void DisbandPatrol(MobileParty recruiter)
		{
			if(recruiter != null)
			{
				if (allRecruiters.Contains(recruiter))
				{
					allRecruiters.Remove(recruiter);
					recruiter.RemoveParty();
				}

				else if(recruiter.Name.ToString().EndsWith("Recruiter"))
				{
					InformationManager.DisplayMessage(new InformationMessage("Your recruiter got killed!"));
				}
			}
		}

		private void OnDailyAITick()
		{
			RecruiterHourlyAi();
		}

		private void RecruiterHourlyAi()
		{
			List<MobileParty> toBeDeleted = new List<MobileParty>();
			foreach(MobileParty recruiter in allRecruiters)
			{
				if(recruiter.HomeSettlement == null)
				{
					toBeDeleted.Add(recruiter);
					break;
				}
				if(recruiter.PartyTradeGold < 20)
				{
					recruiter.SetMoveGoToSettlement(recruiter.HomeSettlement);
				}

				if(recruiter.CurrentSettlement == recruiter.HomeSettlement)
				{
					if(!(recruiter.HomeSettlement.IsCastle || recruiter.HomeSettlement.IsTown))
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
						InformationManager.DisplayMessage(new InformationMessage("Your recruiter brought " + soldierCount + " soldiers to " + recruiter.HomeSettlement + "."));
						toBeDeleted.Add(recruiter);
						recruiter.RemoveParty();
						continue;
					}
					
				}
				else if(recruiter.CurrentSettlement != null)
				{
					recruitAll(recruiter, recruiter.CurrentSettlement);
					Settlement temp = findNearestSettlementWithRecruitableRecruits(recruiter);
					if(temp != null)
						recruiter.SetMoveGoToSettlement(findNearestSettlementWithRecruitableRecruits(recruiter));
				}
				else
				{
					Settlement closestWithRecruits = findNearestSettlementWithRecruitableRecruits(recruiter);
					if(closestWithRecruits == null)
					{
						recruiter.SetMoveGoToSettlement(recruiter.HomeSettlement);
						return;
					}
					recruiter.SetMoveGoToSettlement(findNearestSettlementWithRecruitableRecruits(recruiter));
				}
			}

			foreach (MobileParty deletedRecruiter in toBeDeleted)
			{
				allRecruiters.Remove(deletedRecruiter);
			}

		}

		private bool hasSettlementRecruits(Settlement settlement, MobileParty recruiter)
		{
			DefaultPartyWageModel wageModel = new DefaultPartyWageModel();
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
			IEnumerable<Settlement> settlementsWithRecruits = Settlement.All.Where(settlement => settlement.GetNumberOfAvailableRecruits() > 0 && !settlement.OwnerClan.IsAtWarWith(Hero.MainHero.Clan));
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
            dataStore.SyncData<List<MobileParty>>("allRecruiters", ref this.allRecruiters);
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
				GameMenu.SwitchToMenu("recruiter_pay_menu");
			};

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
					MobileParty item = this.spawnRecruiter(Settlement.CurrentSettlement, cost);
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
					MobileParty item = this.spawnRecruiter(Settlement.CurrentSettlement, cost);
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
					MobileParty item = this.spawnRecruiter(Settlement.CurrentSettlement, cost);
				}
				GameMenu.SwitchToMenu("castle");
			}, false, -1, false);
			obj.AddGameMenuOption("recruiter_pay_menu", "recruiter_leave", "Leave", new GameMenuOption.OnConditionDelegate(this.game_menu_just_add_leave_conditional), new GameMenuOption.OnConsequenceDelegate(this.game_menu_switch_to_village_menu), false, -1, false);
		}

		private void game_menu_switch_to_village_menu(MenuCallbackArgs args)
		{
			GameMenu.SwitchToMenu("castle");
		}

		public MobileParty spawnRecruiter(Settlement settlement, int cash)
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
			mobileParty.SetMoveGoToSettlement(findNearestSettlementWithRecruitableRecruits(mobileParty));
			allRecruiters.Add(mobileParty);
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
	}
}
