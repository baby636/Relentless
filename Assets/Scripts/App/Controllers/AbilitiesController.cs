﻿using GrandDevs.CZB.Common;
using GrandDevs.CZB.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace GrandDevs.CZB
{
    public class AbilitiesController : IController
    {
        private IGameplayManager _gameplayManager;
        private CardsController _cardsController;
        private PlayerController _playerController;
        private BattlegroundController _battlegroundController;

        private object _lock = new object();

        private ulong _castedAbilitiesIds = 0;
        private List<ActiveAbility> _activeAbilities;

        public AbilitiesController()
        {
            _activeAbilities = new List<ActiveAbility>();


            _gameplayManager = GameClient.Get<IGameplayManager>();
            _cardsController = _gameplayManager.GetController<CardsController>();
            _playerController = _gameplayManager.GetController<PlayerController>();
            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();
        }

        public void Reset()
        {
            lock (_lock)
            {
                foreach (var item in _activeAbilities)
                    item.ability.Dispose();
                _activeAbilities.Clear();
            }

            _castedAbilitiesIds = 0;
        }

        public void Update()
        {
            lock (_lock)
            {
                foreach (var item in _activeAbilities)
                    item.ability.Update();
            }
        }

        public void Dispose()
        {
            Reset();
        }

        public void DeactivateAbility(ulong id)
        {
            lock (_lock)
            {
                var item = _activeAbilities.Find(x => x.id == id);
                if (_activeAbilities.Contains(item))
                    _activeAbilities.Remove(item);

                if (item != null && item.ability != null)
                    item.ability.Dispose();
            }
        }

        public ActiveAbility CreateActiveAbility(AbilityData ability, Enumerators.CardKind kind, object boardObject, Player caller, Data.Card cardOwner)
        {
            lock (_lock)
            {
                ActiveAbility activeAbility = new ActiveAbility()
                {
                    id = _castedAbilitiesIds++,
                    ability = CreateAbilityByType(kind, ability)
                };

                activeAbility.ability.playerCallerOfAbility = caller;
                activeAbility.ability.cardOwnerOfAbility = cardOwner;

                if (kind == Enumerators.CardKind.CREATURE)
                    activeAbility.ability.boardCreature = boardObject as BoardCreature;
                else
                    activeAbility.ability.boardSpell = boardObject as BoardSpell;

                _activeAbilities.Add(activeAbility);

                return activeAbility;
            }
        }

        private AbilityBase CreateAbilityByType(Enumerators.CardKind cardKind, AbilityData abilityData)
        {
            AbilityBase ability = null;
            switch (abilityData.abilityType)
            {
                case Enumerators.AbilityType.HEAL:
                    ability = new HealTargetAbility(cardKind, abilityData);
                    break;
                case Enumerators.AbilityType.DAMAGE_TARGET:
                    ability = new DamageTargetAbility(cardKind, abilityData);
					break;
                case Enumerators.AbilityType.DAMAGE_TARGET_ADJUSTMENTS:
                    ability = new DamageTargetAdjustmentsAbility(cardKind, abilityData);
					break;
                case Enumerators.AbilityType.ADD_GOO_VIAL:
                    ability = new AddGooVialsAbility(cardKind, abilityData);
                    break;
                case Enumerators.AbilityType.MODIFICATOR_STATS:
                    ability = new ModificateStatAbility(cardKind, abilityData);
                    break;
                case Enumerators.AbilityType.MASSIVE_DAMAGE:
                    ability = new MassiveDamageAbility(cardKind, abilityData);
                    break;
                case Enumerators.AbilityType.CHANGE_STAT:
                    ability = new ChangeStatAbility(cardKind, abilityData);
                    break;
                case Enumerators.AbilityType.STUN:
                    ability = new StunAbility(cardKind, abilityData);
					break;
                case Enumerators.AbilityType.STUN_OR_DAMAGE_ADJUSTMENTS:
                    ability = new StunOrDamageAdjustmentsAbility(cardKind, abilityData);
					break;
                case Enumerators.AbilityType.SUMMON:
                    ability = new SummonsAbility(cardKind, abilityData);
                    break;
                case Enumerators.AbilityType.CARD_RETURN:
                    ability = new ReturnToHandAbility(cardKind, abilityData);
                    break;
                case Enumerators.AbilityType.WEAPON:
                    ability = new HeroWeaponAbility(cardKind, abilityData);
                    break;
                default:
                    break;
            }
            return ability;
        }

        public bool HasTargets(AbilityData ability)
        {
            if(ability.abilityTargetTypes.Count > 0)
                return true;
            return false;
        }

        public bool IsAbilityActive(AbilityData ability)
        {
            if (ability.abilityActivityType == Enumerators.AbilityActivityType.ACTIVE)
                return true;
            return false;
        }

        public bool IsAbilityCallsAtStart(AbilityData ability)
        {
            if (ability.abilityCallType == Enumerators.AbilityCallType.AT_START)
                return true;
            return false;
        }

        public bool IsAbilityCanActivateTargetAtStart(AbilityData ability)
        {
            if (HasTargets(ability) && IsAbilityCallsAtStart(ability) && IsAbilityActive(ability))
                return true;
            return false;
        }

        public bool IsAbilityCanActivateWithoutTargetAtStart(AbilityData ability)
        {
            if (HasTargets(ability) && IsAbilityCallsAtStart(ability) && !IsAbilityActive(ability))
                return true;
            return false;
        }

        public bool CheckActivateAvailability(Enumerators.CardKind kind, AbilityData ability, Player localPlayer)
        {
            bool available = false;

            lock (_lock)
            {
               // Debug.Log("ability - " + ability);

                foreach (var item in ability.abilityTargetTypes)
                {
                    //Debug.Log("item - " + item);

                    switch (item)
                    {
                        case Enumerators.AbilityTargetType.OPPONENT_CARD:
                            {
                                if (localPlayer.BoardCards.Count > 0)
                                    available = true;
                            }
                            break;
                        case Enumerators.AbilityTargetType.PLAYER_CARD:
                            {
                                // Debug.Log("localPlayer.boardZone.cards.Count - " + localPlayer.boardZone.cards.Count);
                                // Debug.Log("kind - " + kind);
                                if (localPlayer.BoardCards.Count > 1 || kind == Enumerators.CardKind.SPELL)
                                    available = true;
                            }
                            break;
                        case Enumerators.AbilityTargetType.PLAYER:
                        case Enumerators.AbilityTargetType.OPPONENT:
                        case Enumerators.AbilityTargetType.ALL:
                            available = true;
                            break;
                        default: break;
                    }
                    //Debug.Log("available - " + available);

                }
            }

            return available;
        }

        public int GetStatModificatorByAbility(WorkingCard attacker, WorkingCard attacked)
        {
            int value = 0;

            var attackedCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(attacked.cardId);
            var attackerCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(attacker.cardId);

            var abilities = attackerCard.abilities.FindAll(x =>
            x.abilityType == Enumerators.AbilityType.MODIFICATOR_STATS);

            for (int i = 0; i < abilities.Count; i++)
            {
                if (attackedCard.cardSetType == abilities[i].abilitySetType)
                    value += abilities[i].value;
            }
            return value;
        }

        public static uint[] AbilityTypeToUintArray(List<Enumerators.AbilityType> abilities)
        {
            uint[] abils = new uint[abilities.Count];
            for (int i = 0; i < abilities.Count; i++)
                abils[i] = (uint)abilities[i];

            return abils;
        }

        public static List<Enumerators.AbilityType> AbilityTypeToUintArray(uint[] abilities)
        {
            List<Enumerators.AbilityType> abils = new List<Enumerators.AbilityType>();
            for (int i = 0; i < abilities.Length; i++)
                abils[i] = (Enumerators.AbilityType)abilities[i];

            return abils;
        }

        public void CallAbility(Card libraryCard, CardView card, WorkingCard workingCard, Enumerators.CardKind kind, object boardObject, Action<CardView> action, bool isPlayer, object target = null, HandCard handCard = null)
        {
            Vector3 postionOfCardView = Vector3.zero;

            if (card != null)
                postionOfCardView = card.transform.position;

            bool canUseAbility = false;
            ActiveAbility activeAbility = null;
            foreach (var item in libraryCard.abilities) //todo improve it bcoz can have queue of abilities with targets
            {
                activeAbility = CreateActiveAbility(item, kind, boardObject, workingCard.owner, libraryCard);
                //Debug.Log(_abilitiesController.IsAbilityCanActivateTargetAtStart(item));
                if (IsAbilityCanActivateTargetAtStart(item))
                    canUseAbility = true;
                else //if (_abilitiesController.IsAbilityCanActivateWithoutTargetAtStart(item))
                    activeAbility.ability.Activate();
            }
            // Preemptively move the card so that the effect solver can properly check the availability of targets
            // by also taking into account this card (that is trying to be played).

            if (kind == Enumerators.CardKind.SPELL)
            {
                //if (isPlayer)
                //    currentSpellCard = card;
            }
            else
            {
              //  if (isPlayer)
                {
                    workingCard.owner.CardsInHand.Remove(workingCard);
                    workingCard.owner.CardsOnBoard.Add(workingCard);

                 //   if (currentCreature != null)
                  //      currentCreature._fightTargetingArrowPrefab = fightTargetingArrowPrefab;
                }
               // else
                {
                    //opponentInfo.namedZones[Constants.ZONE_HAND].RemoveCard(runtimeCard);
                    //opponentInfo.namedZones[Constants.ZONE_BOARD].AddCard(runtimeCard);
                }
            }

            if (kind != Enumerators.CardKind.SPELL)
            {
             //   effectSolver.MoveCard(isPlayer ? netId : opponentInfo.netId, workingCard, Constants.ZONE_HAND, Constants.ZONE_BOARD);
            }
            else
            {
                if (handCard != null && isPlayer)
                {
                    handCard.gameObject.SetActive(false);
                }
            }

            if (canUseAbility)
            {
                var ability = libraryCard.abilities.Find(x => IsAbilityCanActivateTargetAtStart(x));

                if (CheckActivateAvailability(kind, ability, workingCard.owner))
                {
                    activeAbility.ability.Activate();

                    if (isPlayer)
                    {
                        activeAbility.ability.ActivateSelectTarget(callback: () =>
                        {
                            if (kind == Enumerators.CardKind.SPELL && isPlayer)
                            {
                                handCard.gameObject.SetActive(true);
                                card.removeCardParticle.Play(); // move it when card should call hide action

                             //   effectSolver.MoveCard(isPlayer ? netId : opponentInfo.netId, workingCard, Constants.ZONE_HAND, Constants.ZONE_BOARD);

                                GameClient.Get<ITimerManager>().AddTimer(_cardsController.RemoveCard, new object[] { card }, 0.5f, false);

                                GameClient.Get<ITimerManager>().AddTimer((creat) =>
                                {
                                    workingCard.owner.GraveyardCardsCount++;
                                }, null, 1.5f);
                            }

                            action?.Invoke(card);
                        },
                        failedCallback: () =>
                        {
                            if (kind == Enumerators.CardKind.SPELL && isPlayer)
                            {
                                handCard.gameObject.SetActive(true);
                                handCard.ResetToHandAnimation();
                                handCard.CheckStatusOfHighlight();

                                _playerController.IsCardSelected = false;
                              //  currentSpellCard = null;

                                GameClient.Get<IUIManager>().GetPage<GameplayPage>().SetEndTurnButtonStatus(true);

                                _battlegroundController.RearrangeHand(true);
                            }
                            else
                            {
                                Debug.Log("RETURN CARD TO HAND MAYBE.. SHOULD BE CASE !!!!!");
                                action?.Invoke(card);
                            }
                        });
                    }
                    else
                    {
                        if (target is BoardCreature)
                            activeAbility.ability.targetCreature = target as BoardCreature;
                        else if (target is PlayerAvatar)
                            activeAbility.ability.targetPlayer = target as PlayerAvatar;

                        activeAbility.ability.SelectedTargetAction(true);

                        _battlegroundController.RearrangeBottomBoard();
                        //  Debug.LogError(activeAbility.ability.abilityType.ToString() + " ABIITY WAS ACTIVATED!!!! on " + (target == null ? target : target.GetType()));
                    }
                }
                else
                {
                    CallPermanentAbilityAction(isPlayer, action, card, target, activeAbility);
                }
            }
            else
            {
                CallPermanentAbilityAction(isPlayer, action, card, target, activeAbility);
            }
        }

        private void CallPermanentAbilityAction(bool isPlayer, Action<CardView> action, CardView card, object target, ActiveAbility activeAbility)
        {
            if (isPlayer)
                action?.Invoke(card);
            else
            {
                if (activeAbility == null)
                    return;
                if (target is BoardCreature)
                    activeAbility.ability.targetCreature = target as BoardCreature;
                else if (target is PlayerAvatar)
                    activeAbility.ability.targetPlayer = target as PlayerAvatar;

                activeAbility.ability.SelectedTargetAction(true);
            }

            _battlegroundController.RearrangeBottomBoard();
            _battlegroundController.RearrangeTopBoard();
        }
    }


    public class ActiveAbility
    {
        public ulong id;
        public AbilityBase ability;
    }
}