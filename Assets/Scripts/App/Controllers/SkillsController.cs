using System;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;

namespace Loom.ZombieBattleground
{
    public class SkillsController : IController
    {
        private ILoadObjectsManager _loadObjectsManager;

        private IGameplayManager _gameplayManager;

        private ITutorialManager _tutorialManager;

        private IUIManager _uiManager;

        private ITimerManager _timerManager;

        private ISoundManager _soundManager;

        private IMatchManager _matchManager;

        private IPvPManager _pvpManager;

        private VfxController _vfxController;

        private BattleController _battleController;

        private ActionsQueueController _actionsQueueController;

        private ActionsReportController _actionsReportController;

        private CardsController _cardsController;

        private BattlegroundController _battlegroundController;

        private AbilitiesController _abilitiesController;

        private bool _skillsInitialized;

        private bool _isDirection;

        private List<ParametrizedAbilityBoardObject> _targets;

        private GameObject _buildParticlePrefab;

        public BoardSkill OpponentPrimarySkill { get; private set; }

        public BoardSkill OpponentSecondarySkill { get; private set; }

        public BoardSkill PlayerPrimarySkill { get; private set; }

        public BoardSkill PlayerSecondarySkill { get; private set; }
        
        public bool BlockEndTurnButton { get; private set; }

        public void Dispose()
        {
        }

        public void Init()
        {
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _uiManager = GameClient.Get<IUIManager>();
            _timerManager = GameClient.Get<ITimerManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _matchManager = GameClient.Get<IMatchManager>();
            _pvpManager = GameClient.Get<IPvPManager>();

            _vfxController = _gameplayManager.GetController<VfxController>();
            _battleController = _gameplayManager.GetController<BattleController>();
            _actionsQueueController = _gameplayManager.GetController<ActionsQueueController>();
            _actionsReportController = _gameplayManager.GetController<ActionsReportController>();
            _cardsController = _gameplayManager.GetController<CardsController>();
            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();
            _abilitiesController = _gameplayManager.GetController<AbilitiesController>();

            _gameplayManager.GameEnded += GameplayManagerGameEnded;
        }

        public void Update()
        {
            if (_skillsInitialized)
            {
                PlayerPrimarySkill?.Update();
                PlayerSecondarySkill?.Update();
                OpponentPrimarySkill?.Update();
                OpponentSecondarySkill?.Update();
            }
        }

        public void ResetAll()
        {
            PlayerPrimarySkill = null;
            PlayerSecondarySkill = null;
            OpponentPrimarySkill = null;
            OpponentSecondarySkill = null;
            BlockEndTurnButton = false;
        }

        public void InitializeSkills()
        {
            GameplayPage rootPage = _uiManager.GetPage<GameplayPage>();

            rootPage.PlayerPrimarySkillHandler.MouseDownTriggered += PrimarySkillHandlerMouseDownTriggeredHandler;
            rootPage.PlayerPrimarySkillHandler.MouseUpTriggered += PrimarySkillHandlerMouseUpTriggeredHandler;

            rootPage.PlayerSecondarySkillHandler.MouseDownTriggered += SecondarySkillHandlerMouseDownTriggeredHandler;
            rootPage.PlayerSecondarySkillHandler.MouseUpTriggered += SecondarySkillHandlerMouseUpTriggeredHandler;
            rootPage.OpponentPrimarySkillHandler.MouseDownTriggered +=
                OpponentPrimarySkillHandlerMouseDownTriggeredHandler;
            rootPage.OpponentPrimarySkillHandler.MouseUpTriggered += OpponentPrimarySkillHandlerMouseUpTriggeredHandler;

            rootPage.OpponentSecondarySkillHandler.MouseDownTriggered +=
                OpponentSecondarySkillHandlerMouseDownTriggeredHandler;
            rootPage.OpponentSecondarySkillHandler.MouseUpTriggered +=
                OpponentSecondarySkillHandlerMouseUpTriggeredHandler;


            Enumerators.Skill primarySkillType;
            Enumerators.Skill secondarySkillType;
            if (_matchManager.MatchType == Enumerators.MatchType.PVP)
            {
                primarySkillType = (Enumerators.Skill) _gameplayManager.CurrentPlayer.InitialPvPPlayerState.Deck.PrimarySkill;
                secondarySkillType = (Enumerators.Skill) _gameplayManager.CurrentPlayer.InitialPvPPlayerState.Deck.SecondarySkill;
            }
            else
            {
                primarySkillType = _gameplayManager.CurrentPlayerDeck.PrimarySkill;
                secondarySkillType = _gameplayManager.CurrentPlayerDeck.SecondarySkill;
            }

            OverlordSkillPrototype primary = _gameplayManager.CurrentPlayer.SelfOverlord.Prototype.GetSkill(primarySkillType);
            OverlordSkillPrototype secondary = _gameplayManager.CurrentPlayer.SelfOverlord.Prototype.GetSkill(secondarySkillType);

            rootPage.SetupSkills(primary, secondary, false);
            SetPlayerSkills(rootPage, primary, secondary);

            primary = _gameplayManager.OpponentPlayer.SelfOverlord.Prototype.GetSkill(_gameplayManager.OpponentPlayerDeck.PrimarySkill);
            secondary = _gameplayManager.OpponentPlayer.SelfOverlord.Prototype.GetSkill(_gameplayManager.OpponentPlayerDeck.SecondarySkill);

            rootPage.SetupSkills(primary, secondary, true);
            SetOpponentSkills(rootPage, primary, secondary);

            _skillsInitialized = true;
        }

        public void DisableSkillsContent(Player player)
        {
            if (player.IsLocalPlayer)
            {
                PlayerPrimarySkill?.Hide();
                PlayerSecondarySkill?.Hide();
            }
            else
            {
                OpponentPrimarySkill?.Hide();
                OpponentSecondarySkill?.Hide();
            }
        }

        public void BlockSkill(Player player, Enumerators.SkillType type)
        {
            if (player.IsLocalPlayer)
            {
                PlayerPrimarySkill?.BlockSkill();
                PlayerSecondarySkill?.BlockSkill();
            }
            else
            {
                OpponentPrimarySkill?.BlockSkill();
                OpponentSecondarySkill?.BlockSkill();
            }
        }

        public void UnBlockSkill(Player player)
        {
            if (player.IsLocalPlayer)
            {
                PlayerPrimarySkill?.UnBlockSkill();
                PlayerSecondarySkill?.UnBlockSkill();
            }
            else
            {
                OpponentPrimarySkill?.UnBlockSkill();
                OpponentSecondarySkill?.UnBlockSkill();
            }
        }

        public void SetPlayerSkills(GameplayPage rootPage, OverlordSkillPrototype primary, OverlordSkillPrototype secondary)
        {
            if (primary != null)
            {
                PlayerPrimarySkill = new BoardSkill(rootPage.PlayerPrimarySkillHandler.gameObject,
                    _gameplayManager.CurrentPlayer,
                    primary,
                    true);
            }
            if (secondary != null)
            {
                PlayerSecondarySkill = new BoardSkill(rootPage.PlayerSecondarySkillHandler.gameObject,
                    _gameplayManager.CurrentPlayer,
                    secondary,
                    false);
            }
        }

        public void SetOpponentSkills(GameplayPage rootPage, OverlordSkillPrototype primary, OverlordSkillPrototype secondary)
        {
            if (primary != null)
            {
                OpponentPrimarySkill = new BoardSkill(rootPage.OpponentPrimarySkillHandler.gameObject,
                    _gameplayManager.OpponentPlayer,
                    primary,
                    true);
            }
            if (secondary != null)
            {
                OpponentSecondarySkill = new BoardSkill(rootPage.OpponentSecondarySkillHandler.gameObject,
                    _gameplayManager.OpponentPlayer,
                    secondary,
                    false);
            }
        }

        public void DoSkillAction(BoardSkill skill, Action completeCallback, List<ParametrizedAbilityBoardObject> targets)
        {
            if (skill == null || !skill.IsUsing)
            {
                completeCallback?.Invoke();
                return;
            }

            if (skill.FightTargetingArrow != null)
            {
                if (skill.FightTargetingArrow.SelectedPlayer != null)
                {
                    if (CheckSkillByType(skill))
                    {
                        Player targetPlayer = skill.FightTargetingArrow.SelectedPlayer;

                        string soundFile = GetSoundBySkills(skill);
                        if (!string.IsNullOrEmpty(soundFile))
                        {
                            _soundManager.PlaySound(
                                Enumerators.SoundType.OVERLORD_ABILITIES,
                                soundFile,
                                Constants.OverlordAbilitySoundVolume,
                                false);
                        }

                        targets = new List<ParametrizedAbilityBoardObject>()
                        {
                            new ParametrizedAbilityBoardObject(targetPlayer)
                        };

                        skill.UseSkill();
                        skill.SkillUsedAction(targets);
                        GameplayActionQueueAction skillUsageAction = _actionsQueueController.EnqueueAction(null, Enumerators.QueueActionType.OverlordSkillUsageBlocker, blockQueue: true);

                        CreateSkillVfx(
                            GetVfxPrefabBySkill(skill),
                            skill.SelfObject.transform.position,
                            targetPlayer,
                            (x) =>
                            {
                                DoActionByType(skill, targets, completeCallback);
                                skillUsageAction.TriggerActionExternally();
                            }, _isDirection);

                        if (_gameplayManager.CurrentTurnPlayer == _gameplayManager.CurrentPlayer)
                        {
                            PlayOverlordSkill playOverlordSkill = new PlayOverlordSkill(skill, targets);
                            _gameplayManager.PlayerMoves.AddPlayerMove(
                                new PlayerMove(Enumerators.PlayerActionType.PlayOverlordSkill, playOverlordSkill));
                        }
                    }
                    else
                    {
                        completeCallback?.Invoke();
                    }
                }
                else if (skill.FightTargetingArrow.SelectedCard != null)
                {
                    if (CheckSkillByType(skill))
                    {
                        BoardUnitView targetUnitView = skill.FightTargetingArrow.SelectedCard;

                        string soundFile = GetSoundBySkills(skill);
                        if (!string.IsNullOrEmpty(soundFile))
                        {
                            _soundManager.PlaySound(
                                Enumerators.SoundType.OVERLORD_ABILITIES,
                                soundFile,
                                Constants.OverlordAbilitySoundVolume,
                                false);
                        }

                        if (targets == null || targets.Count == 0)
                        {
                            targets = new List<ParametrizedAbilityBoardObject>()
                            {
                                new ParametrizedAbilityBoardObject(targetUnitView.Model)
                            };
                        }
                        skill.UseSkill();
                        _targets = targets;

                        GameplayActionQueueAction skillUsageAction = _actionsQueueController.EnqueueAction(null, Enumerators.QueueActionType.OverlordSkillUsageBlocker, blockQueue: true);
                        CreateSkillVfx(
                            GetVfxPrefabBySkill(skill),
                            skill.SelfObject.transform.position,
                            targetUnitView,
                            (x) =>
                            {
                                DoActionByType(skill, targets, completeCallback);
                                skillUsageAction.TriggerActionExternally();
                                skill.SkillUsedAction(targets);
                            }, _isDirection);

                        if (_gameplayManager.CurrentTurnPlayer == _gameplayManager.CurrentPlayer)
                        {
                            PlayOverlordSkill playOverlordSkill = new PlayOverlordSkill(skill, targets);
                            _gameplayManager.PlayerMoves.AddPlayerMove(
                                new PlayerMove(Enumerators.PlayerActionType.PlayOverlordSkill, playOverlordSkill));
                        }
                    }
                    else
                    {
                        completeCallback?.Invoke();
                    }
                }
                else
                {
                    completeCallback?.Invoke();
                }

                skill.CancelTargetingArrows();
            }
            else if (targets != null && targets.Count > 0)
            {
                if (CheckSkillByType(skill))
                {
                    string soundFile = GetSoundBySkills(skill);
                    if (!string.IsNullOrEmpty(soundFile))
                    {
                        _soundManager.PlaySound(Enumerators.SoundType.OVERLORD_ABILITIES, soundFile, Constants.OverlordAbilitySoundVolume, false);
                    }
                    skill.UseSkill();
                    _targets = targets;
                    GameplayActionQueueAction skillUsageAction = _actionsQueueController.EnqueueAction(null, Enumerators.QueueActionType.OverlordSkillUsageBlocker, blockQueue: true);
                    CreateSkillVfx(
                        GetVfxPrefabBySkill(skill),
                        skill.SelfObject.transform.position,
                        targets[0].BoardObject,
                        (x) =>
                        {
                            DoActionByType(skill, targets, completeCallback);
                            skillUsageAction.TriggerActionExternally();
                            skill.SkillUsedAction(_targets);
                        }, _isDirection);

                    if (_gameplayManager.CurrentTurnPlayer == _gameplayManager.CurrentPlayer)
                    {
                        PlayOverlordSkill playOverlordSkill = new PlayOverlordSkill(skill, targets);
                        _gameplayManager.PlayerMoves.AddPlayerMove(
                            new PlayerMove(Enumerators.PlayerActionType.PlayOverlordSkill, playOverlordSkill));
                    }
                }
                else
                {
                    completeCallback?.Invoke();
                }
            }
            else
            {
                completeCallback?.Invoke();
            }
        }

        private void CreateSkillVfx(GameObject prefab, Vector3 from, object target, Action<object> callbackComplete, bool isDirection = false)
        {
            if (_buildParticlePrefab == null)
            {
                _vfxController.CreateSkillVfx(prefab, from, target, callbackComplete, isDirection);
            }
            else
            {
                _vfxController.CreateSkillBuildVfx(_buildParticlePrefab, prefab, from, target, callbackComplete, isDirection);
            }
        }

        private void GameplayManagerGameEnded(Enumerators.EndGameType obj)
        {
            _skillsInitialized = false;
        }

        private void PrimarySkillHandlerMouseDownTriggeredHandler(GameObject obj)
        {
            PlayerPrimarySkill?.OnMouseDownEventHandler();
        }

        private void PrimarySkillHandlerMouseUpTriggeredHandler(GameObject obj)
        {
            PlayerPrimarySkill?.OnMouseUpEventHandler();
        }

        private void SecondarySkillHandlerMouseDownTriggeredHandler(GameObject obj)
        {
            PlayerSecondarySkill?.OnMouseDownEventHandler();
        }

        private void SecondarySkillHandlerMouseUpTriggeredHandler(GameObject obj)
        {
            PlayerSecondarySkill?.OnMouseUpEventHandler();
        }

        private void OpponentPrimarySkillHandlerMouseDownTriggeredHandler(GameObject obj)
        {
            OpponentPrimarySkill?.OnMouseDownEventHandler();
        }

        private void OpponentPrimarySkillHandlerMouseUpTriggeredHandler(GameObject obj)
        {
            OpponentPrimarySkill?.OnMouseUpEventHandler();
        }

        private void OpponentSecondarySkillHandlerMouseDownTriggeredHandler(GameObject obj)
        {
            OpponentSecondarySkill?.OnMouseDownEventHandler();
        }

        private void OpponentSecondarySkillHandlerMouseUpTriggeredHandler(GameObject obj)
        {
            OpponentSecondarySkill?.OnMouseUpEventHandler();
        }

        private GameObject GetVfxPrefabBySkill(BoardSkill skill)
        {
            _isDirection = false;
            _buildParticlePrefab = null;
            GameObject prefab;
            switch (skill.Skill.Skill)
            {
                case Enumerators.Skill.ICE_BOLT:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/IceBoltVFX");
                    _isDirection = true;
                    break;
                case Enumerators.Skill.FREEZE:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FreezeVFX");
                    break;
                case Enumerators.Skill.SHATTER:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Shatter_Projectile");
                    _buildParticlePrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Shatter_BuildUp");
                    _isDirection = true;
                    break;
                case Enumerators.Skill.POISON_DART:
                case Enumerators.Skill.TOXIC_POWER:
                case Enumerators.Skill.INFECT:
                    _isDirection = true;
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDartVFX");
                    break;
                case Enumerators.Skill.FIREBALL:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBallVFX");
                    break;
                case Enumerators.Skill.FIRE_BOLT:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBoltVFX");
                    break;
                case Enumerators.Skill.HEALING_TOUCH:
                case Enumerators.Skill.MEND:
                case Enumerators.Skill.HARDEN:
                case Enumerators.Skill.STONE_SKIN:
                case Enumerators.Skill.PUSH:
                case Enumerators.Skill.BREAKOUT:
                case Enumerators.Skill.DRAW:
                case Enumerators.Skill.BLIZZARD:
                case Enumerators.Skill.ENHANCE:
                case Enumerators.Skill.EPIDEMIC:
                case Enumerators.Skill.FORTIFY:
                case Enumerators.Skill.FORTRESS:
                case Enumerators.Skill.ICE_WALL:
                case Enumerators.Skill.LEVITATE:
                case Enumerators.Skill.MASS_RABIES:
                case Enumerators.Skill.METEOR_SHOWER:
                case Enumerators.Skill.PHALANX:
                case Enumerators.Skill.REANIMATE:
                case Enumerators.Skill.RESSURECT:
                case Enumerators.Skill.RETREAT:
                case Enumerators.Skill.WIND_SHIELD:
                default:
                    prefab = new GameObject();
                    break;
            }

            return prefab;
        }

        private string GetSoundBySkills(BoardSkill skill)
        {
            string soundFileName = string.Empty;
            switch (skill.Skill.Skill)
            {
                case Enumerators.Skill.ICE_BOLT:
                case Enumerators.Skill.FREEZE:
                case Enumerators.Skill.POISON_DART:
                case Enumerators.Skill.FIRE_BOLT:
                    soundFileName = skill.Skill.Skill.ToString().ToLowerInvariant();
                    break;
                case Enumerators.Skill.HEALING_TOUCH:
                case Enumerators.Skill.FIREBALL:
                case Enumerators.Skill.TOXIC_POWER:
                case Enumerators.Skill.MEND:
                case Enumerators.Skill.HARDEN:
                case Enumerators.Skill.STONE_SKIN:
                case Enumerators.Skill.PUSH:
                case Enumerators.Skill.DRAW:
                default:
                    break;
            }

            return soundFileName;
        }

        private bool CheckSkillByType(BoardSkill skill)
        {
            bool state = true;

            switch (skill.Skill.Skill)
            {
                case Enumerators.Skill.RESSURECT:
                    state = skill.OwnerPlayer.CardsInGraveyard.FindAll(x => x.Prototype.Faction == Enumerators.Faction.LIFE
                               && x.Prototype.Kind == Enumerators.CardKind.CREATURE
                               && x.CurrentCost == skill.Skill.Value
                               && !skill.OwnerPlayer.CardsOnBoard.Any(c => c == x)).Count > 0;
                    break;
                case Enumerators.Skill.PUSH:
                    int ownerGoo = skill.OwnerPlayer.CurrentGoo;
                    int cardCost = skill.FightTargetingArrow.SelectedCard.Model.Prototype.Cost;
                    state = cardCost <= ownerGoo;
                    break;
                default:
                    break;
            }

            return state;
        }

        private void DoActionByType(BoardSkill skill, List<ParametrizedAbilityBoardObject> targets, Action completeCallback)
        {
            switch (skill.Skill.Skill)
            {
                case Enumerators.Skill.FREEZE:
                    FreezeAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.ICE_BOLT:
                    IceBoltAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.POISON_DART:
                    PoisonDartAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.TOXIC_POWER:
                    ToxicPowerAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.HEALING_TOUCH:
                    HealingTouchAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.MEND:
                    MendAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.FIRE_BOLT:
                    FireBoltAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.RABIES:
                    RabiesAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.HARDEN:
                    HardenAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.STONE_SKIN:
                    StoneskinAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.PUSH:
                    PushAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.DRAW:
                    DrawAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.WIND_SHIELD:
                    WindShieldAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.LEVITATE:
                    Levitate(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.RETREAT:
                    RetreatAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.BREAKOUT:
                    BreakoutAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.INFECT:
                    InfectAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.EPIDEMIC:
                    EpidemicAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.RESSURECT:
                    RessurectAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.REANIMATE:
                    ReanimateAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.ENHANCE:
                    EnhanceAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.ICE_WALL:
                    IceWallAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.SHATTER:
                    ShatterAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.BLIZZARD:
                    BlizzardAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.MASS_RABIES:
                    MassRabiesAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.METEOR_SHOWER:
                    MeteorShowerAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.FIREBALL:
                    FireballAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.FORTIFY:
                    FortifyAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.FORTRESS:
                    FortressAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                case Enumerators.Skill.PHALANX:
                    PhalanxAction(skill.OwnerPlayer, skill, skill.Skill, targets);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(skill.Skill.Skill), skill.Skill.Skill, null);
            }

            completeCallback?.Invoke();
        }

        #region actions

        // ADDITINAL

        private void AttackWithModifiers(
            Player owner,
            BoardSkill boardSkill,
            object target)
        {
            if (target is Player player)
            {
                _battleController.AttackPlayerBySkill(owner, boardSkill, player);
            }
            else
            {
                CardModel creature = (CardModel) target;
                int attackModifier = 0;
                _battleController.AttackUnitBySkill(owner, boardSkill, creature, attackModifier);
            }
        }

        private Dictionary<T, int> GetRandomTargetsByAmount<T>(List<T> root, int count, bool useDeterministic = false)
        {
            Dictionary<T, int> targets = InternalTools.GetRandomElementsFromList(root, count, useDeterministic).ToDictionary(x => x, с => 1);

            if (targets.Count < count)
            {
                int delta = count - targets.Count;
                for (int i = 0; i < delta; i++)
                {
                    targets[InternalTools.GetRandomElementsFromList(root, 1, useDeterministic)[0]]++;
                }
            }
            return targets;
        }

        // AIR

        private void PushAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            owner.CurrentGoo = 0;

            CardModel targetUnit = (CardModel) targets[0].BoardObject;

            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PushVFX"),
                targetUnit);

            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.Skill.ToString().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _cardsController.ReturnCardToHand(targetUnit);

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.Push,
                        Target = targets[0].BoardObject
                    }
                }
            });
        }

        private void DrawAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            owner.PlayerCardsController.AddCardFromDeckToHand();
            owner.PlayDrawCardVFX();

            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.Skill.ToString().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPower,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
            });
        }

        private void WindShieldAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();
            List<CardModel> units = new List<CardModel>();
            if (!boardSkill.IsLocal)
            {
                if (targets != null && targets.Count > 0)
                {
                    if (targets.Count > 1 || targets[0].BoardObject is CardModel)
                    {
                        units = targets.Select(target => target.BoardObject as CardModel).ToList();
                    }
                }
            }
            else
            {
                units =
                InternalTools.GetRandomElementsFromList(
                    owner.CardsOnBoard.FindAll(x => x.Card.Prototype.Faction == Enumerators.Faction.AIR && !x.IsDead && x.CurrentDefense > 0 && x.IsUnitActive),
                    skill.Value);

                _targets = units.Select(target => new ParametrizedAbilityBoardObject(target)).ToList();
            }           

            foreach (CardModel unit in units)
            {
                unit.AddBuffShield();

                targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                    Target = unit
                });
            }

            if (targetEffects.Count > 0)
            {
                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = targetEffects
                });
            }
        }

        private void Levitate(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            int value = -skill.Value;
            CardModel cardModel = null;
            if(!boardSkill.IsLocal && targets != null && targets.Count > 0)
            {
                cardModel = owner.CardsInHand.FirstOrDefault(cardInHand => cardInHand.InstanceId.Id.ToString() == targets[0].Parameters.CardName);
            }

            cardModel = _cardsController.LowGooCostOfCardInHand(owner, cardModel, value);

            if(boardSkill.IsLocal)
            {
                _targets = new List<ParametrizedAbilityBoardObject>()
                {
                    new ParametrizedAbilityBoardObject(owner,
                        new ParametrizedAbilityParameters()
                        {
                            CardName = cardModel.InstanceId.Id.ToString()
                        })
                };
            }

            if (owner.IsLocalPlayer)
            {
                BoardCardView boardCardView = _battlegroundController.GetCardViewByModel<BoardCardView>(cardModel);
                GameObject particle = Object.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/LevitateVFX"));
                particle.transform.position = boardCardView.Transform.position;
                particle.transform.SetParent(boardCardView.Transform, true);
                particle.transform.localEulerAngles = Vector3.zero;
                _gameplayManager.GetController<ParticlesController>().RegisterParticleSystem(particle, true, 6f);
            }

            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.Skill.ToString().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.LowGooCost,
                        Target = cardModel,
                        HasValue = true,
                        Value = value
                    }
                }
            });
        }

        private void RetreatAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<CardModel> units = new List<CardModel>();

            units = _battlegroundController.GetAliveUnits(units
               .Concat(_gameplayManager.CurrentPlayer.CardsOnBoard)
               .Concat(_gameplayManager.OpponentPlayer.CardsOnBoard))
               .ToList();
            foreach (CardModel unit in units)
            {
                unit.SetUnitCannotDie(true);
                unit.SetUnitActiveStatus(false);
            }

            Vector3 position = Vector3.left * 2f;

            _vfxController.CreateVfx(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/RetreatVFX"),
                position, delay: 6f);

            InternalTools.DoActionDelayed(() =>
            {
                foreach (CardModel unit in units)
                {
                    _cardsController.ReturnCardToHand(unit);

                    targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ReturnToHand,
                        Target = unit
                    });
                }
            }, 2f);
            InternalTools.DoActionDelayed(() =>
            {
                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                    Caller = boardSkill,
                    TargetEffects = targetEffects
                });
            }, 4f);
        }

        // TOXIC

        private void ToxicPowerAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0 && targets[0].BoardObject is CardModel unit)
            {
                _battleController.AttackUnitBySkill(owner, boardSkill, unit, 0);

                unit.BuffedDamage += skill.Damage;
                unit.AddToCurrentDamageHistory(skill.Damage, Enumerators.ReasonForValueChange.AbilityBuff);

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/ToxicPowerVFX"),
                    unit, isIgnoreCastVfx: true);

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.AttackBuff,
                            Target = unit,
                            HasValue = true,
                            Value = skill.Damage
                        }
                    }
                });
            }
        }

        private void PoisonDartAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            AttackWithModifiers(owner, boardSkill, targets[0].BoardObject);
            _vfxController.CreateVfx(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDart_ImpactVFX"),
                targets[0].BoardObject, isIgnoreCastVfx: true);
            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.Skill.ToString().ToLowerInvariant() + "_Impact",
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            Enumerators.ActionType actionType;

            switch (targets[0].BoardObject)
            {
                case Player _:
                    actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;
                    break;
                case CardModel _:
                    actionType = Enumerators.ActionType.UseOverlordPowerOnCard;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(targets), targets[0].BoardObject, null);
            }

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = actionType,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = targets[0].BoardObject,
                        HasValue = true,
                        Value = -skill.Value
                    }
                }
            });
        }

        private void BreakoutAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();


            Dictionary<IBoardObject, int> sortedTargets = null;

            if (!boardSkill.IsLocal)
            {
                if (targets != null && targets.Count > 0 && (targets[0].BoardObject is CardModel || targets[0].BoardObject is Player))
                {
                    sortedTargets = targets.ToDictionary(target => target.BoardObject, target => target.Parameters.Attack);
                }
            }
            else
            {
                List<IBoardObject> boardObjects = new List<IBoardObject>();
                Player opponent = _gameplayManager.GetOpponentByPlayer(owner);

                boardObjects.Add(opponent);
                boardObjects.AddRange(opponent.CardsOnBoard.Where(x => !x.IsDead && x.CurrentDefense > 0).ToList());

                sortedTargets = GetRandomTargetsByAmount(boardObjects, skill.Count);

                _targets = sortedTargets.Select(target =>
                    new ParametrizedAbilityBoardObject(target.Key,
                        new ParametrizedAbilityParameters()
                        {
                            Attack = target.Value * skill.Value
                        })
                ).ToList();
            }

            GameObject prefabMovedVfx = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDartVFX");
            GameObject prefabImpactVfx = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDart_ImpactVFX");

            int basicValue = skill.Value;
            int value = 0;

            foreach (IBoardObject targetObject in sortedTargets.Keys)
            {
                _vfxController.CreateSkillVfx(
                prefabMovedVfx,
                boardSkill.SelfObject.transform.position,
                targetObject,
                (x) =>
                {
                    value = basicValue * sortedTargets[targetObject];

                    switch (targetObject)
                    {
                        case Player player:
                            _battleController.AttackPlayerBySkill(owner, boardSkill, player, value);
                            break;
                        case CardModel unit:
                            _battleController.AttackUnitBySkill(owner, boardSkill, unit, 0, value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(targetObject), targetObject, null);
                    }

                    _vfxController.CreateVfx(
                        prefabImpactVfx,
                        targetObject, isIgnoreCastVfx: true);
                    _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skill.Title.Trim().ToLowerInvariant() + "_Impact",
                        Constants.OverlordAbilitySoundVolume,
                        Enumerators.CardSoundType.NONE);

                    targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = targetObject,
                        HasValue = true,
                        Value = -value
                    });
                }, true);
            }

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCardsWithOverlord,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        private void InfectAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0 && targets[0].BoardObject is CardModel unit)
            {
                int unitAtk = unit.CurrentDamage;

                _vfxController.CreateVfx(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/InfectVFX"),
                targets[0].BoardObject, delay: 8f, isIgnoreCastVfx:true);

                CardModel targetUnit;
                if (boardSkill.IsLocal)
                {
                    IReadOnlyList<CardModel> opponentUnits = _gameplayManager.GetOpponentByPlayer(owner).CardsOnBoard;

                    if (opponentUnits.Count == 0)
                        return;

                    targetUnit = opponentUnits[UnityEngine.Random.Range(0, opponentUnits.Count)];

                    _targets.Add(new ParametrizedAbilityBoardObject(targetUnit));
                }
                else
                {
                    if (targets.Count == 1)
                        return;

                    targetUnit = targets[1].BoardObject as CardModel;
                }

                InternalTools.DoActionDelayed(() =>
                {
                    _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Infect_ExplosionVFX"),
                    unit, delay: 6f, isIgnoreCastVfx: true);
                    _battlegroundController.DestroyBoardUnit(unit, false, true);

                    _vfxController.CreateSkillVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDartVFX"),
                    _battlegroundController.GetCardViewByModel<BoardUnitView>(unit).Transform.position,
                    targetUnit,
                    (x) =>
                    {
                        _vfxController.CreateVfx(
                        _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDart_ImpactVFX"),
                        targetUnit, isIgnoreCastVfx: true);
                        _battleController.AttackUnitBySkill(owner, boardSkill, targetUnit, 0, unitAtk);

                        _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                        {
                            ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                            Caller = boardSkill,
                            TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                            {
                                new PastActionsPopup.TargetEffectParam()
                                {
                                    ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                                    Target = unit,
                                    HasValue = true,
                                    Value = -unitAtk
                                },
                                new PastActionsPopup.TargetEffectParam()
                                {
                                    ActionEffectType = Enumerators.ActionEffectType.DeathMark,
                                    Target = unit,
                                }
                            }
                        });
                    }, true);                    
                }, 3.5f);
            }
        }

        private void EpidemicAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            IReadOnlyList<CardModel> units = null;
            List<CardModel> opponentUnits = null;
            Dictionary<CardModel, int> unitAttacks = new Dictionary<CardModel, int>();
            List<CardModel> opponentUnitsTakenDamage = new List<CardModel>();
            int unitAtk = 0;
            CardModel opponentUnitModel = null;
            CardModel unitModel = null;
            Action<CardModel> callback = null;
            int count = 0;

            if (!boardSkill.IsLocal)
            {
                if (targets != null && targets.Count > 0)
                {
                    if (targets.Count > 1 || targets[0].BoardObject is CardModel)
                    {
                        count = targets.Count;
                    }
                }
            }
            else
            {
                units = owner.CardsOnBoard.FindAll(x => x.Card.Prototype.Faction == Enumerators.Faction.TOXIC);
                units = _battlegroundController.GetRandomUnits(units.ToList(), skill.Count);
                count = units.Count;
                opponentUnits = _battlegroundController.GetRandomUnits(
                    _gameplayManager.GetOpponentByPlayer(owner).CardsOnBoard.ToList(),
                    skill.Count);

                _targets = new List<ParametrizedAbilityBoardObject>();
            }

            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                callback = null;
                if (boardSkill.IsLocal)
                {
                    unitModel = units[i];
                    unitAtk = unitModel.CurrentDamage;
                    opponentUnitModel = null;

                    if (opponentUnits.Count > 0)
                    {
                        opponentUnitModel = opponentUnits[UnityEngine.Random.Range(0, opponentUnits.Count)];

                        opponentUnits.Remove(opponentUnitModel);
                        opponentUnitsTakenDamage.Add(opponentUnitModel);
                    }
                    else if (opponentUnitsTakenDamage.Count > 0)
                    {
                        opponentUnitModel = opponentUnitsTakenDamage[UnityEngine.Random.Range(0, opponentUnitsTakenDamage.Count)];
                    }
                }
                else
                {
                    unitModel = (CardModel) targets[i].BoardObject;
                    unitAtk = unitModel.CurrentDamage;
                    opponentUnitModel = _gameplayManager.GetOpponentByPlayer(owner).CardsOnBoard.FirstOrDefault(card => card.InstanceId.Id.ToString() == targets[i].Parameters.CardName);
                }

                targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.DeathMark,
                    Target = unitModel
                });

                if (opponentUnitModel != null)
                {
                    if (unitAttacks.ContainsKey(opponentUnitModel))
                    {
                        unitAttacks[opponentUnitModel] += unitAtk;
                        targetEffects.Find(x => x.Target == opponentUnitModel).Value -= unitAtk;
                    }
                    else
                    {
                        unitAttacks.Add(opponentUnitModel, unitAtk);
                        targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                            Target = opponentUnitModel,
                            HasValue = true,
                            Value = -unitAtk
                        });
                    }

                    callback = (unit) =>
                    {
                        if (unitAttacks.ContainsKey(opponentUnitModel))
                        {
                            _battleController.AttackUnitBySkill(owner, boardSkill, unit, 0, unitAttacks[unit]);
                            unitAttacks.Remove(unit);
                        }
                    };

                    if (boardSkill.IsLocal)
                    {
                        _targets.Add(new ParametrizedAbilityBoardObject(unitModel,
                            new ParametrizedAbilityParameters()
                            {
                                CardName = opponentUnitModel.InstanceId.Id.ToString()
                            }));
                    }
                }
                else
                {
                    if (boardSkill.IsLocal)
                    {
                        _targets.Add(new ParametrizedAbilityBoardObject(unitModel,
                            new ParametrizedAbilityParameters()
                            {
                                CardName = unitModel.InstanceId.Id.ToString()
                            }));
                    }
                }

                BlockEndTurnButton = true;
                EpidemicUnit(owner, boardSkill, skill, unitModel, opponentUnitModel, callback);
            }

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }
    
        private void EpidemicUnit(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, CardModel unit, CardModel target, Action<CardModel> callback)
        {
            _vfxController.CreateVfx(
            _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/InfectVFX"),
            unit, delay: 8f, isIgnoreCastVfx: true);

            InternalTools.DoActionDelayed(() =>
            {
                BlockEndTurnButton = false;
                _vfxController.CreateVfx(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Infect_ExplosionVFX"),
                unit, delay: 6f, isIgnoreCastVfx: true);
                _battlegroundController.DestroyBoardUnit(unit, false, true);

                if (target != null)
                {
                    _vfxController.CreateSkillVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDartVFX"),
                    _battlegroundController.GetCardViewByModel<BoardUnitView>(unit).Transform.position,
                    target,
                    (x) =>
                    {
                        callback?.Invoke(target);
                        _vfxController.CreateVfx(
                        _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PoisonDart_ImpactVFX"),
                        target);
                    }, true);
                }
            }, 3.5f);
        }

        // LIFE

        private void HealingTouchAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets.Count > 0)
            {
                if (targets[0].BoardObject is Player player)
                {
                    _battleController.HealPlayerBySkill(owner, boardSkill, player);

                    _vfxController.CreateVfx(
                        _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX"),
                        player);
                    _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skill.Skill.ToString().ToLowerInvariant(),
                        Constants.OverlordAbilitySoundVolume,
                        Enumerators.CardSoundType.NONE);
                }
                else
                {
                    CardModel unit = (CardModel)targets[0].BoardObject;

                    _battleController.HealUnitBySkill(owner, boardSkill, unit);

                    _vfxController.CreateVfx(
                        _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX"),
                        unit);
                    _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skill.Skill.ToString().ToLowerInvariant(),
                        Constants.OverlordAbilitySoundVolume,
                        Enumerators.CardSoundType.NONE);
                }

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                            Target = targets[0].BoardObject,
                            HasValue = true,
                            Value = skill.Value
                        }
                    }
                });
            }
        }

        private void MendAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            owner.Defense = Mathf.Clamp(owner.Defense + skill.Value, 0, owner.MaxCurrentDefense);

            // TODO: remove this empty gameobject logic
            Transform transform = new GameObject().transform;
            transform.position = owner.AvatarObject.transform.position;
            transform.position += Vector3.up * 2;
            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/MendVFX"), transform);
            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.Skill.ToString().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPower,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                   new PastActionsPopup.TargetEffectParam()
                   {
                       ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                       Target = owner,
                       HasValue = true,
                       Value = skill.Value
                   }
                }
            });
        }

        private void RessurectAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            IReadOnlyList<CardModel> cardModels = null;

            if (!boardSkill.IsLocal && targets != null)
            {
                List<CardModel> foundCards = new List<CardModel>();

                foreach (ParametrizedAbilityBoardObject boardObject in targets)
                {
                    foundCards.Add(owner.CardsInGraveyard.FirstOrDefault(card => card.InstanceId.Id.ToString() == boardObject.Parameters.CardName));
                }

                cardModels = foundCards;
            }
            else
            {
                cardModels = owner.CardsInGraveyard.FindAll(x => x.Card.Prototype.Faction == Enumerators.Faction.LIFE
                                                       && x.Card.Prototype.Kind == Enumerators.CardKind.CREATURE
                                                       && x.CurrentCost== skill.Value
                                                       && !owner.CardsOnBoard.Any(c => c.Card == x.Card));

                cardModels = InternalTools.GetRandomElementsFromList(cardModels, skill.Count);

                _targets = cardModels
                    .Select(target => new ParametrizedAbilityBoardObject(owner,
                        new ParametrizedAbilityParameters()
                        {
                            CardName = target.InstanceId.Id.ToString()
                        }))
                    .ToList();
            }

            BoardUnitView unit = null;

            foreach (CardModel cardModel in cardModels)
            {
                cardModel.ResetToInitial();
                unit = owner.PlayerCardsController.SpawnUnitOnBoard(
                    cardModel,
                    ItemPosition.End,
                    onComplete: () =>
                    {
                        _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/ResurrectVFX"),
                            unit,
                            delay: 6,
                            isIgnoreCastVfx: true);
                        InternalTools.DoActionDelayed(() =>
                            {
                                unit.ChangeModelVisibility(true);
                                unit.StopSleepingParticles();

                                if (unit.Model.OwnerPlayer != _gameplayManager.CurrentTurnPlayer)
                                {
                                    unit.Model.IsPlayable = true;
                                }

                                if (unit.Model.OwnerPlayer.IsLocalPlayer)
                                {
                                    _abilitiesController.ActivateAbilitiesOnCard(unit.Model, unit.Model, unit.Model.OwnerPlayer);
                                }
                            },
                            3f);

                        targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.SpawnOnBoard,
                            Target = cardModel,
                        });
                    });
                unit.ChangeModelVisibility(false);
                owner.PlayerCardsController.RemoveCardFromGraveyard(cardModel);
            }

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        private void EnhanceAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<IBoardObject> boardObjects = new List<IBoardObject>();

            if (!boardSkill.IsLocal && targets != null)
            {
                boardObjects = targets.Select(target => target.BoardObject).ToList();
            }
            else
            {
                boardObjects.Add(owner);
                boardObjects.AddRange(owner.CardsOnBoard.Where(x => x.Card.Prototype.Faction == Enumerators.Faction.LIFE));

                _targets = boardObjects.Select(target => new ParametrizedAbilityBoardObject(target)).ToList();
            }

            foreach (IBoardObject targetObject in boardObjects)
            {
                switch (targetObject)
                {
                    case CardModel unit:
                        {
                            _battleController.HealUnitBySkill(owner, boardSkill, unit);
                            _vfxController.CreateVfx(
                            _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX"),
                            _battlegroundController.GetCardViewByModel<BoardUnitView>(unit));
                            _soundManager.PlaySound(
                                Enumerators.SoundType.OVERLORD_ABILITIES,
                                skill.Skill.ToString().ToLowerInvariant(),
                                Constants.OverlordAbilitySoundVolume,
                                Enumerators.CardSoundType.NONE);
                        }
                        break;
                    case Player player:
                        {
                            _battleController.HealPlayerBySkill(owner, boardSkill, player);
                            Transform transform = new GameObject().transform;
                            transform.position = owner.AvatarObject.transform.position;
                            transform.position += Vector3.up * 2.25f;
                            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/MendVFX"), transform);
                            _soundManager.PlaySound(
                                Enumerators.SoundType.OVERLORD_ABILITIES,
                                skill.Skill.ToString().ToLowerInvariant(),
                                Constants.OverlordAbilitySoundVolume,
                                Enumerators.CardSoundType.NONE);
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(targetObject), targetObject, null);
                }

                targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.LifeGain,
                    Target = targetObject,
                    HasValue = true,
                    Value = skill.Value
                });
            }

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnCardsWithOverlord,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        private void ReanimateAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            IReadOnlyList<CardModel> cards = null;

            if (!boardSkill.IsLocal && targets != null)
            {
                List<CardModel> foundCards = new List<CardModel>();

                foreach (ParametrizedAbilityBoardObject boardObject in targets)
                {
                    foundCards.Add(owner.CardsInGraveyard.FirstOrDefault(card => card.InstanceId.Id.ToString() == boardObject.Parameters.CardName));
                }

                cards = foundCards;
            }
            else
            {
                cards = owner.CardsInGraveyard.FindAll(x => x.Prototype.Faction == Enumerators.Faction.LIFE
                                                        && x.Prototype.Kind == Enumerators.CardKind.CREATURE
                                                        && !owner.CardsOnBoard.Any(c => c == x));

                cards = InternalTools.GetRandomElementsFromList(cards, skill.Count);

                _targets = cards
                    .Select(target => new ParametrizedAbilityBoardObject(owner,
                        new ParametrizedAbilityParameters()
                        {
                            CardName = target.InstanceId.Id.ToString()
                        }))
                    .ToList();
            }

            List<BoardUnitView> units = new List<BoardUnitView>();
            BoardUnitView reanimatedUnit = null;

            foreach (CardModel cardModel in cards)
            {
                if (cardModel == null)
                    continue;

                if (owner.CardsOnBoard.Count >= owner.MaxCardsInPlay)
                    break;

                owner.PlayerCardsController.RemoveCardFromGraveyard(cardModel);
                cardModel.ResetToInitial();

                Card prototype = new Card(GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCardByName(cardModel.Card.Prototype.Name));
                InstanceId updatedId = new InstanceId(cardModel.InstanceId.Id, Enumerators.ReasonForInstanceIdChange.Reanimate);
                WorkingCard card = new WorkingCard(prototype, prototype, cardModel.OwnerPlayer, id:updatedId);
                CardModel reanimatedUnitModel = new CardModel(card);

                reanimatedUnit = CreateBoardUnit(reanimatedUnitModel, owner);
                units.Add(reanimatedUnit);
                targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.Reanimate,
                    Target = reanimatedUnitModel
                });
            }

            _gameplayManager.GetController<BoardController>().UpdateCurrentBoardOfPlayer(owner, () =>
            {
                ReanimateUnit(units);
            });

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        private BoardUnitView CreateBoardUnit(CardModel cardModel, Player owner)
        {
            BoardUnitView boardUnitView = _battlegroundController.CreateBoardUnit(owner, cardModel);

            if (owner != _gameplayManager.CurrentTurnPlayer)
            {
                boardUnitView.Model.IsPlayable = true;
            }

            _gameplayManager.CanDoDragActions = true;
            
            boardUnitView.ChangeModelVisibility(false);

            _battlegroundController.RegisterCardView(boardUnitView, owner);

            if (cardModel.Owner.IsLocalPlayer || _gameplayManager.IsLocalPlayerTurn()) 
            {
                _abilitiesController.ResolveAllAbilitiesOnUnit(cardModel, false);
                _abilitiesController.ActivateAbilitiesOnCard(cardModel, cardModel, owner);
            }

            _abilitiesController.ResolveAllAbilitiesOnUnit(cardModel);

            owner.PlayerCardsController.AddCardToBoard(boardUnitView.Model, ItemPosition.End);

            return boardUnitView;
        }

        private void ReanimateUnit(List<BoardUnitView> units)
        {
            foreach (BoardUnitView unit in units)
            {
                unit.StopSleepingParticles();
                _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/ResurrectVFX"), unit, delay: 6, isIgnoreCastVfx: true);
                InternalTools.DoActionDelayed(() =>
                {
                    unit.ChangeModelVisibility(true);
                }, 3f);
            }
        }

        // WATER

        private void FreezeAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets.Count > 0)
            {
                Enumerators.ActionType actionType;

                switch (targets[0].BoardObject)
                {
                    case CardModel unit:
                        unit.Stun(Enumerators.StunType.FREEZE, skill.Value);

                        _vfxController.CreateVfx(
                            _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Freeze_ImpactVFX"),
                            unit, isIgnoreCastVfx: true);

                        _soundManager.PlaySound(
                            Enumerators.SoundType.OVERLORD_ABILITIES,
                            skill.Skill.ToString().ToLowerInvariant() + "_Impact",
                            Constants.OverlordAbilitySoundVolume,
                            Enumerators.CardSoundType.NONE);

                        actionType = Enumerators.ActionType.UseOverlordPowerOnCard;

                        break;
                    case Player player:
                        player.Stun(Enumerators.StunType.FREEZE, skill.Value);

                        _vfxController.CreateVfx(
                            _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Freeze_ImpactVFX"),
                            player, isIgnoreCastVfx: true);
                        _soundManager.PlaySound(
                            Enumerators.SoundType.OVERLORD_ABILITIES,
                            skill.Skill.ToString().ToLowerInvariant() + "_Impact",
                            Constants.OverlordAbilitySoundVolume,
                            Enumerators.CardSoundType.NONE);

                        actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;

                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(targets), targets, null);
                }

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = actionType,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.Freeze,
                        Target = targets[0].BoardObject
                    }
                }
                });
            }
        }

        private void IceBoltAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0 && targets[0].BoardObject is CardModel unit)
            {
                _battleController.AttackUnitBySkill(owner, boardSkill, unit, 0);

                if (unit.CurrentDefense > 0)
                {
                    unit.Stun(Enumerators.StunType.FREEZE, 1);
                }

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/IceBolt_Impact"),
                    unit, isIgnoreCastVfx: true);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Skill.ToString().ToLowerInvariant() + "_Impact",
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                            Target = unit,
                            HasValue = true,
                            Value = -skill.Value
                        },
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.Freeze,
                            Target = unit
                        }
                    }
                });
            }
        }

        private void IceWallAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0)
            {
                IBoardObject target = targets[0].BoardObject;

                Enumerators.ActionType actionType;

                _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/IceWallVFX"), target, delay: 8, isIgnoreCastVfx: true);

                InternalTools.DoActionDelayed(() =>
                {
                    switch (target)
                    {
                        case CardModel unit:
                            unit.BuffedDefense += skill.Value;
                            unit.AddToCurrentDefenseHistory(skill.Value, Enumerators.ReasonForValueChange.AbilityBuff);
                            actionType = Enumerators.ActionType.UseOverlordPowerOnCard;
                            break;
                        case Player player:
                            _battleController.HealPlayerBySkill(owner, boardSkill, player);
                            actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(target), target, null);
                    }

                    _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skill.Title.Trim().ToLowerInvariant(),
                        Constants.OverlordAbilitySoundVolume,
                        Enumerators.CardSoundType.NONE);


                    _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                    {
                        ActionType = actionType,
                        Caller = boardSkill,
                        TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                        {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                            Target = target,
                            HasValue = true,
                            Value = skill.Value
                        }
                        }
                    });
                }, 2f);
            }
        }

        private void ShatterAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0)
            {
                IBoardObject target = targets[0].BoardObject;
                _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Shatter_ImpactVFX"), target, isIgnoreCastVfx: true);

                if (target is CardModel cardModel)
                {
                    cardModel.LastAttackingSetType = owner.SelfOverlord.Prototype.Faction;
                    _battlegroundController.DestroyBoardUnit(cardModel, false, true);

                    _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skill.Skill.ToString().ToLowerInvariant() + "_Impact",
                        Constants.OverlordAbilitySoundVolume,
                        Enumerators.CardSoundType.NONE);
                }

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.DeathMark,
                        Target = target
                    }
                }
                });
            }
        }

        private void BlizzardAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            IReadOnlyList<CardModel> units;

            if (targets != null && !boardSkill.IsLocal)
            {
                units = targets.Select(target => target.BoardObject as CardModel).ToList();
            }
            else
            {
                units = _gameplayManager.GetOpponentByPlayer(owner).CardsOnBoard;
                units = InternalTools.GetRandomElementsFromList(units, skill.Count);

                _targets = units.Select(target => new ParametrizedAbilityBoardObject(target)).ToList();
            }

            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/BlizzardVFX"), Vector3.zero, true, 8);

            GameObject prefabFreeze = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Blizzard_Freeze");

            foreach (CardModel unit in units)
            {
                BoardUnitView unitView = _battlegroundController.GetCardViewByModel<BoardUnitView>(unit);
                Vector3 targetPosition = unitView.Transform.position + Vector3.up * 0.7f;

                _vfxController.CreateVfx(prefabFreeze, targetPosition, true, 6);

                GameplayActionQueueAction SkillProcessingAction = _actionsQueueController.EnqueueAction(null, Enumerators.QueueActionType.OverlordSkillUsageBlocker);

                InternalTools.DoActionDelayed(() =>
                {
                    SkillProcessingAction?.TriggerActionExternally();
                    
                    unit.Stun(Enumerators.StunType.FREEZE, skill.Value);

                    targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.Freeze,
                        Target = unit
                    });
                }, 3.5f);
            }

            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skill.Title.Trim().ToLowerInvariant(),
                Constants.OverlordAbilitySoundVolume,
                Enumerators.CardSoundType.NONE);

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        // FIRE

        private void FireBoltAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0)
            {
                IBoardObject target = targets[0].BoardObject;
                AttackWithModifiers(owner, boardSkill, target);
                _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBolt_ImpactVFX"), target);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Skill.ToString().ToLowerInvariant() + "_Impact",
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                Enumerators.ActionType actionType;

                switch (target)
                {
                    case Player _:
                        actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;
                        break;
                    case CardModel _:
                        actionType = Enumerators.ActionType.UseOverlordPowerOnCard;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(target), target, null);
                }

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = actionType,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = target,
                        HasValue = true,
                        Value = -skill.Value
                    }
                }
                });
            }
        }

        private void RabiesAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0 && targets[0].BoardObject is CardModel unit)
            {
                unit.SetAsFeralUnit();

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/RabiesVFX"),
                    unit, delay: 14f, isIgnoreCastVfx: true);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Skill.ToString().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.Feral,
                            Target = unit
                        }
                    }
                });
            }
        }

        private void FireballAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0)
            {
                IBoardObject target = targets[0].BoardObject;
                AttackWithModifiers(owner, boardSkill, target);

                _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBall_ImpactVFX"), target, isIgnoreCastVfx: true); // vfx Fireball
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Title.Trim().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                Enumerators.ActionType actionType;

                switch (target)
                {
                    case Player _:
                        actionType = Enumerators.ActionType.UseOverlordPowerOnOverlord;
                        break;
                    case CardModel _:
                        actionType = Enumerators.ActionType.UseOverlordPowerOnCard;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(target), target, null);
                }

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = actionType,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                        Target = target,
                        HasValue = true,
                        Value = -skill.Value
                    }
                }
                });
            }
        }

        private void MassRabiesAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            IReadOnlyList<CardModel> units = null;

            if (!boardSkill.IsLocal)
            {
                if (targets != null && targets.Count > 0 && targets[0].BoardObject is CardModel)
                {
                    units = targets.Select(target => target.BoardObject as CardModel).ToList();
                }
            }
            else
            {
                units = owner.CardsOnBoard
                    .FindAll(x => !x.HasFeral && x.Card.Prototype.Faction == owner.SelfOverlord.Prototype.Faction);

                units = InternalTools.GetRandomElementsFromList(units, skill.Count);

                _targets = units.Select(target => new ParametrizedAbilityBoardObject(target)).ToList();
            }

            if (units != null)
            {
                foreach (CardModel unit in units)
                {
                    unit.SetAsFeralUnit();

                    _vfxController.CreateVfx(
                        _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/RabiesVFX"),
                        unit, delay: 14f, isIgnoreCastVfx: true);
                    _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skill.Title.Trim().ToLowerInvariant(),
                        Constants.OverlordAbilitySoundVolume,
                        Enumerators.CardSoundType.NONE);

                    targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.Feral,
                        Target = unit
                    });
                }
            }

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        private void MeteorShowerAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<CardModel> units = new List<CardModel>();

            if (!boardSkill.IsLocal && targets != null)
            {
                units = targets.Select(target => target.BoardObject as CardModel).ToList();
            }
            else
            {
                units =
                    _gameplayManager.CurrentPlayer.CardsOnBoard
                        .Concat(_gameplayManager.OpponentPlayer.CardsOnBoard)
                        .ToList();

                _targets = units.Select(target => new ParametrizedAbilityBoardObject(target)).ToList();
            }

            foreach (CardModel unit in units)
            {
                InternalTools.DoActionDelayed(() =>
                {
                    AttackWithModifiers(owner, boardSkill, unit);
                }, 2.5f);

                GameObject vfxObject = Object.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/MeteorShowerVFX"));
                vfxObject.transform.position =  _battlegroundController.GetCardViewByModel<BoardUnitView>(unit).Transform.position;
                _gameplayManager.GetController<ParticlesController>().RegisterParticleSystem(vfxObject, true, 8);

                string skillTitle = skill.Skill.ToString().ToLowerInvariant();

                ParticleSystem particle = vfxObject.transform.Find("Particle System/MeteorShowerVFX").GetComponent<ParticleSystem>();
                MeteorShowerEmit(particle, 3, skillTitle);

                vfxObject.transform.Find("Particle System/MeteorShowerVFX/Quad").GetComponent<OnBehaviourHandler>().OnParticleCollisionEvent += (obj) =>
                    MeteorShowerImpact(obj, skillTitle);

                targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.ShieldDebuff,
                    Target = unit,
                    HasValue = true,
                    Value = -skill.Value
                });
            }

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        private void MeteorShowerEmit(ParticleSystem particle, int count, string skillTitle)
        {
            if (particle == null)
                return;

            if(count > 0)
            {
                particle.Emit(1);

                _soundManager.PlaySound(
                        Enumerators.SoundType.OVERLORD_ABILITIES,
                        skillTitle + "_Moving_0" + UnityEngine.Random.Range(0, 4).ToString(),
                        Constants.OverlordAbilitySoundVolume,
                        false);

                count--;
                float delay = UnityEngine.Random.Range(0.2f, 0.5f);
                InternalTools.DoActionDelayed(() => MeteorShowerEmit(particle, count, skillTitle), delay);
            }
        }

        private void MeteorShowerImpact(GameObject obj, string skillTitle)
        {
            _soundManager.PlaySound(
                Enumerators.SoundType.OVERLORD_ABILITIES,
                skillTitle + "_Impact_0" + UnityEngine.Random.Range(0, 4).ToString(),
                Constants.OverlordAbilitySoundVolume,
                false);
        }
        // EARTH

        private void StoneskinAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0 && targets[0].BoardObject is CardModel unit)
            {
                unit.BuffedDefense += skill.Value;
                unit.AddToCurrentDefenseHistory(skill.Value, Enumerators.ReasonForValueChange.AbilityBuff);

                Vector3 position = _battlegroundController.GetCardViewByModel<BoardUnitView>(unit).Transform.position;
                position -= Vector3.up * 3.6f;

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HardenStoneSkinVFX"),
                    position, isIgnoreCastVfx:true);
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Skill.ToString().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                            Target = unit,
                            HasValue = true,
                            Value = skill.Value
                        }
                    }
                });
            }
        }

        private void HardenAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            _battleController.HealPlayerBySkill(owner, boardSkill, owner);

            Vector3 position = owner.AvatarObject.transform.position;
            position -= new Vector3(0.07f, 4f, 0f);

            _vfxController.CreateVfx(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HardenStoneSkinVFX"),
                position, isIgnoreCastVfx: true);

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPower,
                Caller = boardSkill,
                TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                {
                    new PastActionsPopup.TargetEffectParam()
                    {
                        ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                        Target = owner,
                        HasValue = true,
                        Value = skill.Value
                    }
                }
            });
        }

        private void FortifyAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            if (targets != null && targets.Count > 0 && targets[0].BoardObject is CardModel unit)
            {
                unit.SetAsHeavyUnit();

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FortressVFX"),
                    unit, isIgnoreCastVfx: true);

                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Skill.ToString().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
                {
                    ActionType = Enumerators.ActionType.UseOverlordPowerOnCard,
                    Caller = boardSkill,
                    TargetEffects = new List<PastActionsPopup.TargetEffectParam>()
                    {
                        new PastActionsPopup.TargetEffectParam()
                        {
                            ActionEffectType = Enumerators.ActionEffectType.Heavy,
                            Target = unit
                        }
                    }
                });
            }
        }

        private void PhalanxAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            IReadOnlyList<CardModel> units = null;

            if (!boardSkill.IsLocal && targets != null)
            {
                units = targets.Select(target => target.BoardObject as CardModel).ToList();
            }
            else
            {
                units = owner.CardsOnBoard.FindAll(x => x.Card.Prototype.Faction == Enumerators.Faction.EARTH);

                _targets = units.Select(target => new ParametrizedAbilityBoardObject(target)).ToList();
            }

            foreach (CardModel unit in units)
            {
                unit.BuffedDefense += skill.Value;
                unit.AddToCurrentDefenseHistory(skill.Value, Enumerators.ReasonForValueChange.AbilityBuff);

                Vector3 position = _battlegroundController.GetCardViewByModel<BoardUnitView>(unit).Transform.position;
                position -= Vector3.up * 3.65f;

                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HardenStoneSkinVFX"),
                    position, delay: 8f, isIgnoreCastVfx: true); // vfx phalanx
                _soundManager.PlaySound(
                    Enumerators.SoundType.OVERLORD_ABILITIES,
                    skill.Title.Trim().ToLowerInvariant(),
                    Constants.OverlordAbilitySoundVolume,
                    Enumerators.CardSoundType.NONE);

                targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.ShieldBuff,
                    Target = unit,
                    HasValue = true,
                    Value = skill.Value
                });
            }

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        private void FortressAction(Player owner, BoardSkill boardSkill, OverlordSkillPrototype skill, List<ParametrizedAbilityBoardObject> targets)
        {
            List<PastActionsPopup.TargetEffectParam> targetEffects = new List<PastActionsPopup.TargetEffectParam>();

            List<CardModel> units;
            if (!boardSkill.IsLocal && targets != null)
            {
                units = targets.Select(target => target.BoardObject as CardModel).ToList();
            }
            else
            {
                units = InternalTools.GetRandomElementsFromList(
                        owner.CardsOnBoard.FindAll(x => x.Card.Prototype.Faction == Enumerators.Faction.EARTH),
                        skill.Count);

                _targets = units.Select(target => new ParametrizedAbilityBoardObject(target)).ToList();
            }

            foreach (CardModel unit in units)
            {
                if (unit == null)
                    continue;

                unit.SetAsHeavyUnit();

                BoardUnitView unitView = _battlegroundController.GetCardViewByModel<BoardUnitView>(unit);
                _vfxController.CreateVfx(
                    _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FortressVFX"), unitView.Transform.position, true, 6f);

                targetEffects.Add(new PastActionsPopup.TargetEffectParam()
                {
                    ActionEffectType = Enumerators.ActionEffectType.Heavy,
                    Target = unit
                });
            }

            _actionsReportController.PostGameActionReport(new PastActionsPopup.PastActionParam()
            {
                ActionType = Enumerators.ActionType.UseOverlordPowerOnMultilpleCards,
                Caller = boardSkill,
                TargetEffects = targetEffects
            });
        }

        #endregion

    }
}
