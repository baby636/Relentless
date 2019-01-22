using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Protobuf;
using UnityEngine;
using InstanceId = Loom.ZombieBattleground.Data.InstanceId;
using NotImplementedException = System.NotImplementedException;

namespace Loom.ZombieBattleground.Test
{
    /// <summary>
    /// Initiates gameplay actions on the local client.
    /// </summary>
    public class LocalClientPlayerActionTestProxy : IPlayerActionTestProxy
    {
        private readonly TestHelper _testHelper;
        private readonly IPvPManager _pvpManager;
        private readonly IQueueManager _queueManager;
        private readonly BackendDataControlMediator _backendDataControlMediator;

        private Queue<CardAbilityRequest> _cardAbilityRequestsQueue = new Queue<CardAbilityRequest>();

        public LocalClientPlayerActionTestProxy(TestHelper testHelper)
        {
            _testHelper = testHelper;

            _pvpManager = GameClient.Get<IPvPManager>();
            _queueManager = GameClient.Get<IQueueManager>();
            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();
        }

        public async Task EndTurn()
        {
            await _testHelper.EndTurn();
        }

        public Task LeaveMatch()
        {
            throw new NotImplementedException();
        }

        public Task Mulligan(IEnumerable<InstanceId> cards)
        {
            throw new NotImplementedException();
        }

        public async Task CardPlay(InstanceId card, int position, InstanceId? entryAbilityTarget = null)
        {
            BoardObject entryAbilityTargetBoardObject = null;
            if (entryAbilityTarget != null)
            {
                entryAbilityTargetBoardObject = _testHelper.BattlegroundController.GetBoardObjectById(entryAbilityTarget.Value);
                if (entryAbilityTargetBoardObject == null)
                    throw new Exception($"'Entry ability target with instance ID {entryAbilityTarget.Value}' not found on board");
            }
            WorkingCard workingCard = _testHelper.BattlegroundController.GetWorkingCardById(card);
            await _testHelper.PlayCardFromHandToBoard(workingCard, false, entryAbilityTargetBoardObject);
        }

        public Task RankBuff(InstanceId card, IEnumerable<InstanceId> units)
        {
            throw new InvalidOperationException("Doesn't makes sense for local player - sent automatically by the local player");
        }

        public Task CardAbilityUsed(
            InstanceId card,
            Enumerators.AbilityType abilityType,
            IReadOnlyList<ParametrizedAbilityBoardObject> targets = null)
        {
            _cardAbilityRequestsQueue.Enqueue(new CardAbilityRequest(card, abilityType, targets));
            HandleNextCardAbility();
            return Task.CompletedTask;
        }

        public Task OverlordSkillUsed(SkillId skillId, Enumerators.AffectObjectType affectObjectType, InstanceId targetInstanceId)
        {
            throw new NotImplementedException();
        }

        public Task CardAttack(InstanceId attacker, Enumerators.AffectObjectType type, InstanceId target)
        {
            BoardUnitModel boardUnitModel = _testHelper.GetCardOnBoardByInstanceId(attacker, Enumerators.MatchPlayer.CurrentPlayer).Model;
            boardUnitModel.DoCombat(_testHelper.BattlegroundController.GetTargetById(target, type));

            return Task.CompletedTask;
        }

        public Task CheatDestroyCardsOnBoard(IEnumerable<InstanceId> targets)
        {
            MatchRequestFactory matchRequestFactory = new MatchRequestFactory(_pvpManager.MatchMetadata.Id);
            PlayerActionFactory playerActionFactory = new PlayerActionFactory(_backendDataControlMediator.UserDataModel.UserId);
            PlayerAction action = playerActionFactory.CheatDestroyCardsOnBoard(targets);
            _queueManager.AddAction(matchRequestFactory.CreateAction(action));

            return Task.CompletedTask;
        }

        public Task<bool> GetIsCurrentTurn()
        {
            throw new NotSupportedException();
        }

        private void HandleNextCardAbility()
        {
            AbilityBoardArrow abilityBoardArrow = _testHelper.GetAbilityBoardArrow();
            if (abilityBoardArrow != null)
            {
                Debug.Log("!!!! abilityBoardArrow", abilityBoardArrow);
            }
            if (abilityBoardArrow && _cardAbilityRequestsQueue.Count == 0)
            {
                //throw new Exception($"Unhandled card ability - targeting arrow exists, but no CardAbilityUsed call was queued");
            }
        }

        private class CardAbilityRequest
        {
            public readonly InstanceId Card;
            public readonly Enumerators.AbilityType AbilityType;
            public readonly IReadOnlyList<ParametrizedAbilityBoardObject> Targets;

            public CardAbilityRequest(InstanceId card, Enumerators.AbilityType abilityType, IReadOnlyList<ParametrizedAbilityBoardObject> targets)
            {
                Card = card;
                AbilityType = abilityType;
                Targets = targets;
            }

            public override string ToString()
            {
                return $"({nameof(Card)}: {Card}, {nameof(AbilityType)}: {AbilityType}, {nameof(Targets)}: {Targets})";
            }
        }
    }
}
