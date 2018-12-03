using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Protobuf;

namespace Loom.ZombieBattleground.BackendCommunication
{
    public class ActionCollectorUploader : IService
    {
        private IGameplayManager _gameplayManager;

        private IAnalyticsManager _analyticsManager;

        private BackendFacade _backendFacade;

        private BackendDataControlMediator _backendDataControlMediator;

        private PlayerEventListener _playerEventListener;

        private PlayerEventListener _opponentEventListener;

        public void Init()
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _analyticsManager = GameClient.Get<IAnalyticsManager>();
            _backendFacade = GameClient.Get<BackendFacade>();
            _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();

            _gameplayManager.GameInitialized += GameplayManagerGameInitialized;
            _gameplayManager.GameEnded += GameplayManagerGameEnded;
        }

        public void Update()
        {
        }

        public void Dispose()
        {
            _playerEventListener?.Dispose();
            _opponentEventListener?.Dispose();
        }

        private void GameplayManagerGameEnded(Enumerators.EndGameType obj)
        {
            _playerEventListener?.Dispose();
            _opponentEventListener?.Dispose();

            _analyticsManager.NotifyFinishedMatch(obj);
            _analyticsManager.SetEvent(AnalyticsManager.EventEndedMatch);
        }

        private void GameplayManagerGameInitialized()
        {
            _playerEventListener?.Dispose();
            _opponentEventListener?.Dispose();

            _playerEventListener = new PlayerEventListener(_gameplayManager.CurrentPlayer, false);
            _opponentEventListener = new PlayerEventListener(_gameplayManager.OpponentPlayer, true);

            _analyticsManager.NotifyStartedMatch();
            _analyticsManager.SetEvent(AnalyticsManager.EventStartedMatch);
        }

        private class PlayerEventListener : IDisposable
        {
            private readonly BackendFacade _backendFacade;

            private readonly IQueueManager _queueManager;

            private readonly BackendDataControlMediator _backendDataControlMediator;

            private readonly BattlegroundController _battlegroundController;

            private readonly IPvPManager _pvpManager;
            
            private readonly SkillsController _skillsController;

            private readonly AbilitiesController _abilitiesController;

            private readonly RanksController _ranksController;

            public PlayerEventListener(Player player, bool isOpponent)
            {
                _backendFacade = GameClient.Get<BackendFacade>();
                _queueManager = GameClient.Get<IQueueManager>();
                _backendDataControlMediator = GameClient.Get<BackendDataControlMediator>();
                _pvpManager = GameClient.Get<IPvPManager>();
                _battlegroundController = GameClient.Get<IGameplayManager>().GetController<BattlegroundController>();
                _abilitiesController = GameClient.Get<IGameplayManager>().GetController<AbilitiesController>();
                _skillsController = GameClient.Get<IGameplayManager>().GetController<SkillsController>();
                _ranksController = GameClient.Get<IGameplayManager>().GetController<RanksController>();

                Player = player;
                IsOpponent = isOpponent;

                if (!_backendFacade.IsConnected)
                    return;

                IMatchManager matchManager = GameClient.Get<IMatchManager>();
                if (matchManager.MatchType == Enumerators.MatchType.LOCAL ||
                    matchManager.MatchType == Enumerators.MatchType.PVE ||
                    _pvpManager.InitialGameState == null)
                    return;

                if (!isOpponent)
                {
                    _battlegroundController.TurnEnded += TurnEndedHandler;

                    _abilitiesController.AbilityUsed += AbilityUsedHandler;

                    Player.DrawCard += DrawCardHandler;
                    Player.CardPlayed += CardPlayedHandler;
                    Player.CardAttacked += CardAttackedHandler;
                    Player.LeaveMatch += LeaveMatchHandler;

                    if (_skillsController.PlayerPrimarySkill != null)
                    {
                        _skillsController.PlayerPrimarySkill.SkillUsed += SkillUsedHandler;
                    }

                    if (_skillsController.PlayerSecondarySkill != null)
                    {
                        _skillsController.PlayerSecondarySkill.SkillUsed += SkillUsedHandler;
                    }

                    _ranksController.RanksUpdated += RanksUpdatedHandler;
                }
            }

            private void DrawCardHandler(WorkingCard card)
            {
                /*string playerId = _backendDataControlMediator.UserDataModel.UserId;
                PlayerAction playerAction = new PlayerAction
                {
                    ActionType = PlayerActionType.Types.Enum.DrawCard,
                    PlayerId = playerId,
                    DrawCard = new PlayerActionDrawCard
                    {
                        CardInstance = new CardInstance
                        {
                            InstanceId = card.Id,
                            Prototype = ToProtobufExtensions.GetCardPrototype(card),
                            Defense = card.Health,
                            Attack = card.Damage
                        }
                    }
                };

                _backendFacade.AddAction(_pvpManager.MatchMetadata.Id, playerAction);*/
            }

            public Player Player { get; }

            public bool IsOpponent { get; }

            public void Dispose()
            {
                UnsubscribeFromPlayerEvents();
            }

            private void UnsubscribeFromPlayerEvents()
            {
                if (!IsOpponent)
                {
                    _battlegroundController.TurnEnded -= TurnEndedHandler;

                    _abilitiesController.AbilityUsed -= AbilityUsedHandler;

                    Player.DrawCard -= DrawCardHandler;
                    Player.CardPlayed -= CardPlayedHandler;
                    Player.CardAttacked -= CardAttackedHandler;
                    Player.LeaveMatch -= LeaveMatchHandler;

                    if (_skillsController.PlayerPrimarySkill != null)
                    {
                        _skillsController.PlayerPrimarySkill.SkillUsed -= SkillUsedHandler;
                    }

                    if (_skillsController.PlayerSecondarySkill != null)
                    {
                        _skillsController.PlayerSecondarySkill.SkillUsed -= SkillUsedHandler;
                    }

                    _ranksController.RanksUpdated -= RanksUpdatedHandler;
                }
            }

            private void CardPlayedHandler(WorkingCard card, int position)
            {
                string playerId = _backendDataControlMediator.UserDataModel.UserId;
                PlayerAction playerAction = new PlayerAction
                {
                    ActionType = PlayerActionType.Types.Enum.CardPlay,
                    PlayerId = playerId,
                    CardPlay = new PlayerActionCardPlay
                    {
                        Card = card.ToProtobuf(),
                        Position = position
                    }
                };

                _queueManager.AddAction(RequestFactory.CreateAction(_pvpManager.MatchMetadata.Id, playerAction));
            }

            private void TurnEndedHandler()
            {
                string playerId = _backendDataControlMediator.UserDataModel.UserId;
                PlayerAction playerAction = new PlayerAction
                {
                    ActionType = PlayerActionType.Types.Enum.EndTurn,
                    PlayerId = playerId,
                    EndTurn = new PlayerActionEndTurn()
                };

                _queueManager.AddAction(RequestFactory.CreateAction(_pvpManager.MatchMetadata.Id, playerAction));
            }

            private void LeaveMatchHandler()
            {
                string playerId = _backendDataControlMediator.UserDataModel.UserId;
                PlayerAction playerAction = new PlayerAction
                {
                    ActionType = PlayerActionType.Types.Enum.LeaveMatch,
                    PlayerId = playerId,
                    LeaveMatch = new PlayerActionLeaveMatch()
                };

                _queueManager.AddAction(RequestFactory.CreateAction(_pvpManager.MatchMetadata.Id, playerAction));
            }

            private void CardAttackedHandler(WorkingCard attacker, Enumerators.AffectObjectType type, int instanceId)
            {
                string playerId = _backendDataControlMediator.UserDataModel.UserId;
                PlayerAction playerAction = new PlayerAction
                {
                    ActionType = PlayerActionType.Types.Enum.CardAttack,
                    PlayerId = playerId,
                    CardAttack = new PlayerActionCardAttack
                    {
                        Attacker = attacker.ToProtobuf(),
                        AffectObjectType = (Protobuf.AffectObjectType.Types.Enum) type,
                        Target = new Protobuf.Unit
                        {
                            InstanceId = instanceId,
                            Parameter = new Parameter() { }
                        }
                    }
                };

                _queueManager.AddAction(RequestFactory.CreateAction(_pvpManager.MatchMetadata.Id, playerAction));
            }

            private void AbilityUsedHandler(
                WorkingCard card,
                Enumerators.AbilityType abilityType,
                Enumerators.CardKind cardKind,
                Enumerators.AffectObjectType affectObjectType,
                List<ParametrizedAbilityBoardObject> targets = null,
                List<WorkingCard> cards = null)
            {
                PlayerActionCardAbilityUsed cardAbilityUsed = new PlayerActionCardAbilityUsed()
                {
                    CardKind = (CardKind.Types.Enum) cardKind,
                    AbilityType = abilityType.ToString(),
                    Card = card.ToProtobuf()
                };

                Protobuf.Unit targetUnit;
                if (targets != null)
                {
                    foreach(ParametrizedAbilityBoardObject parametrizedAbility in targets)
                    {
                        if (parametrizedAbility.BoardObject == null)
                            continue;
                            
                        targetUnit = new Protobuf.Unit();

                        if (parametrizedAbility.BoardObject is BoardUnitModel model)
                        {
                            targetUnit = new Protobuf.Unit
                            {
                                InstanceId = model.Card.InstanceId,
                                AffectObjectType =  AffectObjectType.Types.Enum.Character,
                                Parameter = new Parameter()
                                {
                                    Attack = parametrizedAbility.Parameters.Attack,
                                    Defense = parametrizedAbility.Parameters.Defense,
                                    CardName = parametrizedAbility.Parameters.CardName
                                }
                            };
                        }
                        else if (parametrizedAbility.BoardObject is Player player)
                        {
                            targetUnit = new Protobuf.Unit
                            {
                                InstanceId = player.Id == 0 ? 1 : 0,
                                AffectObjectType = AffectObjectType.Types.Enum.Player,
                                Parameter = new Parameter() { }
                            };
                        }
                        else if(parametrizedAbility.BoardObject is HandBoardCard handCard)
                        {
                            targetUnit = new Protobuf.Unit
                            {
                                InstanceId = handCard.CardView.WorkingCard.InstanceId,
                                AffectObjectType = AffectObjectType.Types.Enum.Card,
                                Parameter = new Parameter()
                                {
                                    Attack = parametrizedAbility.Parameters.Attack,
                                    Defense = parametrizedAbility.Parameters.Defense,
                                    CardName = parametrizedAbility.Parameters.CardName
                                }
                            };
                        }

                        cardAbilityUsed.Targets.Add(targetUnit);
                    }
                }

                if(cards != null)
                {
                    foreach (WorkingCard workingCard in cards)
                    {
                        targetUnit = new Protobuf.Unit
                        {
                            InstanceId = workingCard.InstanceId,
                            AffectObjectType = AffectObjectType.Types.Enum.Card,
                            Parameter = new Parameter() { }
                        };

                        cardAbilityUsed.Targets.Add(targetUnit);
                    }
                }

                string playerId = _backendDataControlMediator.UserDataModel.UserId;
                PlayerAction playerAction = new PlayerAction
                {
                    ActionType = PlayerActionType.Types.Enum.CardAbilityUsed,
                    PlayerId = playerId,
                    CardAbilityUsed = cardAbilityUsed

                };

                 UnityEngine.Debug.LogWarning("Action json send = " + Newtonsoft.Json.JsonConvert.SerializeObject(playerAction));

                _queueManager.AddAction(RequestFactory.CreateAction(_pvpManager.MatchMetadata.Id, playerAction));
            }

            private void MulliganHandler(List<WorkingCard> cards)
            {
                string playerId = _backendDataControlMediator.UserDataModel.UserId;
                PlayerAction playerAction = new PlayerAction
                {
                    ActionType = PlayerActionType.Types.Enum.Mulligan,
                    PlayerId = playerId,
                    Mulligan = new PlayerActionMulligan
                    {
                        // TODO : cant able to set the mulligan cards, no setter in zb protobuf
                        //MulliganedCards = GetMulliganCards(cards)
                    }
                };

                _queueManager.AddAction(RequestFactory.CreateAction(_pvpManager.MatchMetadata.Id, playerAction));
            }


            private void SkillUsedHandler(BoardSkill skill, BoardObject target)
            {
                string playerId = _backendDataControlMediator.UserDataModel.UserId;
                AffectObjectType.Types.Enum affectObjectType =
                    target is Player ?
                        AffectObjectType.Types.Enum.Player :
                        AffectObjectType.Types.Enum.Character;
                Protobuf.Unit targetUnit = null;

                switch (target)
                {
                    case BoardUnitModel unit:
                        targetUnit = new Protobuf.Unit
                        {
                            InstanceId = unit.Card.InstanceId,
                            Parameter = new Parameter() { }
                        };
                        break;
                    case Player player:
                        targetUnit = new Protobuf.Unit
                        {
                            InstanceId = player.Id == 0 ? 1 : 0,
                            Parameter = new Parameter() { }
                        };
                        break;
                }

                PlayerAction playerAction = new PlayerAction
                {
                    ActionType = PlayerActionType.Types.Enum.OverlordSkillUsed,
                    PlayerId = playerId,
                    OverlordSkillUsed = new PlayerActionOverlordSkillUsed
                    {
                        SkillId = skill.Id,
                        AffectObjectType = affectObjectType,     
                        Target = targetUnit
                    }
                };

                _queueManager.AddAction(RequestFactory.CreateAction(_pvpManager.MatchMetadata.Id, playerAction));
            }

            private void RanksUpdatedHandler(WorkingCard card, List<BoardUnitView> units)
            {
                string playerId = _backendDataControlMediator.UserDataModel.UserId;

                PlayerActionRankBuff rankBuff = new PlayerActionRankBuff
                {
                    Card = card.ToProtobuf()
                };

                Protobuf.Unit unit;
                foreach (BoardUnitView view in units)
                {
                    unit = new Protobuf.Unit
                    {
                        InstanceId = view.Model.Card.InstanceId,
                        AffectObjectType = AffectObjectType.Types.Enum.Character,
                        Parameter = new Parameter() { }
                    };

                    rankBuff.Targets.Add(unit);
                }

                PlayerAction playerAction = new PlayerAction
                {
                    ActionType = PlayerActionType.Types.Enum.RankBuff,
                    PlayerId = playerId,
                    RankBuff = rankBuff
                };

                _queueManager.AddAction(RequestFactory.CreateAction(_pvpManager.MatchMetadata.Id, playerAction));
            }
        }
    }
}
