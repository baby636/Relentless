using System;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using UnityEngine.TestTools;
using System.Linq;

namespace Loom.ZombieBattleground.Test.MultiplayerTests
{
    public class AirCardsTests : BaseIntegrationTest
    {
        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator MindFlayer()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 2, new DeckCardData[]
                {
                    new DeckCardData("Mind Flayer", 2),
                    new DeckCardData("Pyromaz", 20),
                });
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 2, new DeckCardData[]
                {
                    new DeckCardData("Mind Flayer", 2),
                    new DeckCardData("Wood", 20),
                });

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerPyromazId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Pyromaz", 1);
                InstanceId playerMindFlayerId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Mind Flayer", 1);

                InstanceId opponentWoodId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Wood", 1);
                InstanceId opponentMindFlayerId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Mind Flayer", 1);
                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                   {
                       player => {},
                       opponent => {},
                       player =>
                       {
                           player.CardPlay(playerPyromazId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentWoodId, ItemPosition.Start);
                       },
                       player =>
                       {
                           player.CardPlay(playerMindFlayerId, ItemPosition.Start);
                           player.CardAbilityUsed(playerMindFlayerId, Enumerators.AbilityType.TAKE_CONTROL_ENEMY_UNIT, new List<ParametrizedAbilityInstanceId>()
                           {  
                               new ParametrizedAbilityInstanceId(opponentWoodId)
                           });
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentMindFlayerId, ItemPosition.Start);
                           opponent.CardAbilityUsed(opponentMindFlayerId, Enumerators.AbilityType.TAKE_CONTROL_ENEMY_UNIT, new List<ParametrizedAbilityInstanceId>()
                           {  
                               new ParametrizedAbilityInstanceId(playerPyromazId)
                           });
                       },
                       player => {},
                       opponent => {}
                   };

                Action validateEndState = () =>
                {
                    Assert.NotNull(TestHelper.GetCurrentPlayer().BoardCards.Select(card => card.Model.Card.LibraryCard.MouldId == 251));
                    Assert.NotNull(TestHelper.GetOpponentPlayer().BoardCards.Select(card => card.Model.Card.LibraryCard.MouldId == 10));
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            }, 300);
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Banshee()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Banshee", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Banshee", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Banshee", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Banshee", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start),
                       player => {}
                };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId)).HasFeral);
                    Assert.AreEqual(true, ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId)).HasFeral);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            }, 300);
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zonic()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zonic", 10)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zonic", 10)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerCardId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zonic", 1);
                InstanceId opponentCardId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zonic", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => {},
                       opponent => {},
                       player => player.CardPlay(playerCardId, ItemPosition.Start),
                       opponent => opponent.CardPlay(opponentCardId, ItemPosition.Start),
                       player => player.CardAttack(playerCardId, opponentCardId),
                       opponent => {},
                       player => {}
                };

                Action validateEndState = () =>
                {
                    BoardUnitModel playerUnit = ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerCardId));
                    BoardUnitModel opponentUnit = ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentCardId));
                    Assert.AreEqual(playerUnit.InitialHp, playerUnit.CurrentHp);
                    Assert.AreEqual(opponentUnit.InitialHp, opponentUnit.CurrentHp);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            }, 300);
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator ZeuZ()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("ZeuZ", 1),
                    new DeckCardData("Igloo", 20)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("ZeuZ", 1),
                    new DeckCardData("Igloo", 20)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerZeuzId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "ZeuZ", 1);
                InstanceId playerIglooId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Igloo", 1);
                InstanceId opponentZeuzId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "ZeuZ", 1);
                InstanceId opponentIglooId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Igloo", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => 
                       {
                           player.CardPlay(playerIglooId, ItemPosition.Start);
                       },
                       opponent =>
                       {
                           opponent.CardPlay(opponentIglooId, ItemPosition.Start);
                           opponent.CardPlay(opponentZeuzId, ItemPosition.Start);
                       },
                       player => 
                       {
                           player.CardPlay(playerZeuzId, ItemPosition.Start);
                       },
                       opponent => {},
                       player => {}
                };

                Action validateEndState = () =>
                {
                    BoardUnitModel playerZeuzUnit = ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZeuzId));
                    BoardUnitModel playerIglooUnit = ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerIglooId));
                    BoardUnitModel opponentZeuzUnit = ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZeuzId));
                    BoardUnitModel opponentIglooUnit = ((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentIglooId));
                    Assert.AreEqual(playerZeuzUnit.InitialHp, playerZeuzUnit.CurrentHp);
                    Assert.AreEqual(playerIglooUnit.InitialHp-3, playerIglooUnit.CurrentHp);
                    Assert.AreEqual(opponentZeuzUnit.InitialHp-3, opponentZeuzUnit.CurrentHp);
                    Assert.AreEqual(opponentIglooUnit.InitialHp-3, opponentIglooUnit.CurrentHp);
                    Assert.AreEqual(TestHelper.GetCurrentPlayer().InitialHp-3, TestHelper.GetCurrentPlayer().Defense);
                    Assert.AreEqual(TestHelper.GetOpponentPlayer().InitialHp-3, TestHelper.GetOpponentPlayer().Defense);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState);
            }, 400);
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator MonZoon()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("MonZoon", 3),
                    new DeckCardData("Igloo", 20)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("MonZoon", 3),
                    new DeckCardData("Igloo", 20)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerMonzoon1Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "MonZoon", 1);
                InstanceId playerMonzoon2Id = pvpTestContext.GetCardInstanceIdByName(playerDeck, "MonZoon", 2);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => 
                       {
                           player.CardPlay(playerMonzoon1Id, ItemPosition.Start);
                       },
                       opponent => {},
                       player => {}
                };

                Action validateEndState = () =>
                {
                    Assert.AreEqual(7, TestHelper.BattlegroundController.PlayerHandCards.FindAll(x => x.WorkingCard.InstanceId == playerMonzoon2Id)[0].ManaCost);
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            }, 400);
        }

        [UnityTest]
        [Timeout(int.MaxValue)]
        public IEnumerator Zquall()
        {
            return AsyncTest(async () =>
            {
                Deck playerDeck = PvPTestUtility.GetDeckWithCards("deck 1", 0,
                    new DeckCardData("Zquall", 1),
                    new DeckCardData("Whizpar", 1),
                    new DeckCardData("Banshee", 1),
                    new DeckCardData("Enrager", 20)
                );
                Deck opponentDeck = PvPTestUtility.GetDeckWithCards("deck 2", 0,
                    new DeckCardData("Zquall", 1),
                    new DeckCardData("Whizpar", 1),
                    new DeckCardData("Banshee", 1),
                    new DeckCardData("Enrager", 20)
                );

                PvpTestContext pvpTestContext = new PvpTestContext(playerDeck, opponentDeck)
                {
                    Player1HasFirstTurn = true
                };

                InstanceId playerZquallId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Zquall", 1);
                InstanceId playerWhizparId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Whizpar", 1);
                InstanceId playerBansheeId = pvpTestContext.GetCardInstanceIdByName(playerDeck, "Banshee", 1);
                InstanceId opponentZquallId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Zquall", 1);
                InstanceId opponentWhizparId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Whizpar", 1);
                InstanceId opponentBansheeId = pvpTestContext.GetCardInstanceIdByName(opponentDeck, "Banshee", 1);

                IReadOnlyList<Action<QueueProxyPlayerActionTestProxy>> turns = new Action<QueueProxyPlayerActionTestProxy>[]
                {
                       player => {},
                       opponent => {},
                       player => 
                       {
                           player.CardPlay(playerZquallId, ItemPosition.Start, null, true);
                           player.CardAbilityUsed(playerZquallId, Enumerators.AbilityType.SUMMON_UNIT_FROM_HAND, new List<ParametrizedAbilityInstanceId>()
                           {  
                               new ParametrizedAbilityInstanceId(playerWhizparId),
                               new ParametrizedAbilityInstanceId(playerBansheeId)
                           });
                       },
                       opponent => 
                       {
                           opponent.CardPlay(opponentZquallId, ItemPosition.Start, null, true);
                           opponent.CardAbilityUsed(opponentZquallId, Enumerators.AbilityType.SUMMON_UNIT_FROM_HAND, new List<ParametrizedAbilityInstanceId>()
                           {  
                               new ParametrizedAbilityInstanceId(opponentWhizparId),
                               new ParametrizedAbilityInstanceId(opponentBansheeId)
                           });
                       },
                       player => {}
                };

                Action validateEndState = () =>
                {
                    Assert.NotNull(((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerZquallId))); 
                    Assert.NotNull(((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerWhizparId))); 
                    Assert.NotNull(((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(playerBansheeId))); 
                    Assert.NotNull(((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentZquallId))); 
                    Assert.NotNull(((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentWhizparId))); 
                    Assert.NotNull(((BoardUnitModel)TestHelper.BattlegroundController.GetBoardObjectByInstanceId(opponentBansheeId))); 
                };

                await PvPTestUtility.GenericPvPTest(pvpTestContext, turns, validateEndState, false);
            }, 400);
        }
    }
}
