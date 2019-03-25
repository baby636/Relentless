using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class ReanimateAbility : AbilityBase
    {
        private IGameplayManager _gameplayManager;

        private AbilitiesController _abilitiesController;

        private BoardUnitView _reanimatedUnit;

        public ReanimateAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _abilitiesController = _gameplayManager.GetController<AbilitiesController>();
        }

        public override void Activate()
        {
            base.Activate();

            if (!AbilityUnitOwner.IsReanimated)
            {
                InvokeUseAbilityEvent();

                AbilityUnitOwner.AddGameMechanicDescriptionOnUnit(Enumerators.GameMechanicDescription.Reanimate);
            }
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            if (PvPManager.UseBackendGameLogic)
                return;

            if (AbilityUnitOwner.IsReanimated)
                return;

            Player owner = AbilityUnitOwner.OwnerPlayer;

            owner.PlayerCardsController.RemoveCardFromGraveyard(AbilityUnitOwner);

            AbilityUnitOwner.ResetToInitial();
            _reanimatedUnit = CreateBoardUnit(AbilityUnitOwner, owner);
            AbilityUnitOwner.IsReanimated = true;

            if (_reanimatedUnit != null)
            {
                _reanimatedUnit.Model.RemoveGameMechanicDescriptionFromUnit(Enumerators.GameMechanicDescription.Reanimate);
            }

            if (PlayerCallerOfAbility.IsLocalPlayer)
            {
                BattlegroundController.RegisterBoardUnitView(_reanimatedUnit, GameplayManager.CurrentPlayer);
                _abilitiesController.ActivateAbilitiesOnCard(_reanimatedUnit.Model, AbilityUnitOwner, AbilityUnitOwner.Owner);
            }
            else
            {
                BattlegroundController.RegisterBoardUnitView(_reanimatedUnit, GameplayManager.OpponentPlayer);
                if (_gameplayManager.IsLocalPlayerTurn()) {
                    _abilitiesController.ActivateAbilitiesOnCard(_reanimatedUnit.Model, AbilityUnitOwner, AbilityUnitOwner.Owner);
                }
            }

            AbilityUnitOwner.Owner.PlayerCardsController.AddCardToBoard(AbilityUnitOwner, ItemPosition.End);

            InvokeActionTriggered(_reanimatedUnit);
        }

        protected override void UnitHpChangedHandler(int oldValue, int newValue)
        {
            base.UnitHpChangedHandler(oldValue, newValue);

            if (AbilityUnitOwner.CurrentDefense == 0 && !AbilityUnitOwner.IsReanimated)
            {
                AbilityProcessingAction = ActionsQueueController.AddNewActionInToQueue(null, Enumerators.QueueActionType.AbilityUsageBlocker, blockQueue: true);
            }
        }

        protected override void UnitDiedHandler()
        {
            Action();
        }

        protected override void VFXAnimationEndedHandler()
        {
            base.VFXAnimationEndedHandler();

            AbilityProcessingAction?.ForceActionDone();

            base.UnitDiedHandler();

            _gameplayManager.CanDoDragActions = true;
        }

        private BoardUnitView CreateBoardUnit(BoardUnitModel boardUnitModel, Player owner)
        {
            BoardUnitView boardUnitView = BattlegroundController.CreateBoardUnit(owner, boardUnitModel);

            if (!owner.Equals(GameplayManager.CurrentTurnPlayer))
            {
                boardUnitView.Model.IsPlayable = true;
            }

            boardUnitView.StopSleepingParticles();

            GameplayManager.CanDoDragActions = true;

            return boardUnitView;
        }
    }
}
