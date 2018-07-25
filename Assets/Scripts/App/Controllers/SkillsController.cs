﻿// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;
using UnityEngine;

namespace LoomNetwork.CZB
{
    public class SkillsController : IController
    {
        private ILoadObjectsManager _loadObjectsManager;
        private IGameplayManager _gameplayManager;
        private ITutorialManager _tutorialManager;
        private IUIManager _uiManager;
        private ITimerManager _timerManager;

        private VFXController _vfxController;
        private BattleController _battleController;
        private ActionsQueueController _actionsQueueController;
        private CardsController _cardsController;

        private BoardSkill _playerPrimarySkill,
                           _playerSecondarySkill;

        public BoardSkill opponentPrimarySkill,
                          opponentSecondarySkill;

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

            _vfxController = _gameplayManager.GetController<VFXController>();
            _battleController = _gameplayManager.GetController<BattleController>();
            _actionsQueueController = _gameplayManager.GetController<ActionsQueueController>();
            _cardsController = _gameplayManager.GetController<CardsController>();
        }

        public void Update()
        {
        }

        public void InitializeSkills()
        {
            var rootPage = _uiManager.GetPage<GameplayPage>();


            rootPage.playerPrimarySkillHandler.OnMouseDownEvent += PrimarySkillHandlerOnMouseDownEventHandler;
            rootPage.playerPrimarySkillHandler.OnMouseUpEvent += PrimarySkillHandlerOnMouseUpEventHandler;

            rootPage.playerSecondarySkillHandler.OnMouseDownEvent += SecondarySkillHandlerOnMouseDownEventHandler;
            rootPage.playerSecondarySkillHandler.OnMouseUpEvent += SecondarySkillHandlerOnMouseUpEventHandler;


            int primary = _gameplayManager.CurrentPlayer.SelfHero.primarySkill;
            int secondary = _gameplayManager.CurrentPlayer.SelfHero.secondarySkill;

            if (primary < _gameplayManager.CurrentPlayer.SelfHero.skills.Count && secondary < _gameplayManager.CurrentPlayer.SelfHero.skills.Count)
                SetPlayerSkills(rootPage, _gameplayManager.CurrentPlayer.SelfHero.skills[primary], _gameplayManager.CurrentPlayer.SelfHero.skills[secondary]);

            primary = _gameplayManager.OpponentPlayer.SelfHero.primarySkill;
            secondary = _gameplayManager.OpponentPlayer.SelfHero.secondarySkill;

            if (primary < _gameplayManager.OpponentPlayer.SelfHero.skills.Count && secondary < _gameplayManager.OpponentPlayer.SelfHero.skills.Count)
                SetOpponentSkills(rootPage, _gameplayManager.OpponentPlayer.SelfHero.skills[primary], _gameplayManager.OpponentPlayer.SelfHero.skills[secondary]);
        }

        public void DisableSkillsContent(Player player)
        {
            if(player.IsLocalPlayer)
            {
                _playerPrimarySkill.Hide();
                _playerSecondarySkill.Hide();
            }
            else
            {
                opponentPrimarySkill.Hide();
                opponentSecondarySkill.Hide();
            }
        }


        private void PrimarySkillHandlerOnMouseDownEventHandler(GameObject obj)
        {
            if(_playerPrimarySkill != null)
            _playerPrimarySkill.StartDoSkill();
        }

        private void PrimarySkillHandlerOnMouseUpEventHandler(GameObject obj)
        {
            if (_playerPrimarySkill != null)
                _playerPrimarySkill.EndDoSkill();
        }

        private void SecondarySkillHandlerOnMouseDownEventHandler(GameObject obj)
        {
            if (_playerSecondarySkill != null)
                _playerSecondarySkill.StartDoSkill();
        }

        private void SecondarySkillHandlerOnMouseUpEventHandler(GameObject obj)
        {
            if (_playerSecondarySkill != null)
                _playerSecondarySkill.EndDoSkill();
        }

        public void SetPlayerSkills(GameplayPage rootPage, HeroSkill primary, HeroSkill secondary)
        {
            _playerPrimarySkill = new BoardSkill(rootPage.playerPrimarySkillHandler.gameObject, _gameplayManager.CurrentPlayer, primary, 2, true);
            _playerSecondarySkill = new BoardSkill(rootPage.playerSecondarySkillHandler.gameObject, _gameplayManager.CurrentPlayer, secondary, 1, false);
        }

        public void SetOpponentSkills(GameplayPage rootPage, HeroSkill primary, HeroSkill secondary)
        {
            opponentPrimarySkill = new BoardSkill(rootPage.opponentPrimarySkillHandler, _gameplayManager.OpponentPlayer, primary, 2, true);
            opponentSecondarySkill = new BoardSkill(rootPage.opponentSecondarySkillHandler, _gameplayManager.OpponentPlayer, secondary, 1, false);
        }

        private void SkillParticleActionCompleted(object target)
        {
            //switch (skillType)
            //{
            //    case Enumerators.SetType.WATER:
            //        FreezeAction(target);
            //        break;
            //    case Enumerators.SetType.TOXIC:
            //        ToxicDamageAction(target);
            //        break;
            //    case Enumerators.SetType.FIRE:
            //        FireDamageAction(target);
            //        break;
            //    case Enumerators.SetType.LIFE:
            //        HealAnyAction(target);
            //        break;
            //    case Enumerators.SetType.AIR:
            //        //   CardReturnAction(target);
            //        break;
            //    default:
            //        break;
            //}
        }

        public void DoSkillAction(BoardSkill skill, object target = null)
        {
            if (skill == null)
                return;

            if (skill.IsUsing)
            {
                if (skill.fightTargetingArrow != null)
                {
                    if (skill.fightTargetingArrow.selectedPlayer != null)
                    {
                        var targetPlayer = skill.fightTargetingArrow.selectedPlayer;

                        _vfxController.CreateSkillVFX(skill.owner.SelfHero.heroElement, skill.selfObject.transform.position, targetPlayer, (x) =>
                        {
                            skill.UseSkill(targetPlayer);
                            DoActionByType(skill, targetPlayer);
                            _tutorialManager.ReportAction(Enumerators.TutorialReportAction.USE_ABILITY);
                        });
                    }
                    else if (skill.fightTargetingArrow.selectedCard != null)
                    {
                        var targetUnit = skill.fightTargetingArrow.selectedCard;

                        _vfxController.CreateSkillVFX(skill.owner.SelfHero.heroElement, skill.selfObject.transform.position, targetUnit, (x) =>
                        {
                            DoActionByType(skill, targetUnit);
                            skill.UseSkill(targetUnit);
                            _tutorialManager.ReportAction(Enumerators.TutorialReportAction.USE_ABILITY);
                        });  
                    }

                    skill.CancelTargetingArrows();
                    skill.fightTargetingArrow = null;
                }
                else if(target != null)
                {
                    _vfxController.CreateSkillVFX(skill.owner.SelfHero.heroElement, skill.selfObject.transform.position, target, (x) =>
                    {
                        DoActionByType(skill, target);
                        skill.UseSkill(target);
                        _tutorialManager.ReportAction(Enumerators.TutorialReportAction.USE_ABILITY);
                    });   
                }
            }
        }


        private void DoActionByType(BoardSkill skill, object target)
        {
            switch(skill.owner.SelfHero.heroElement)
            {
                case Enumerators.SetType.WATER:
                    FreezeAction(skill.owner, skill.skill, target);
                    break;
                case Enumerators.SetType.TOXIC:
                    ToxicDamageAction(skill.owner, skill.skill, target);
                    break;
                case Enumerators.SetType.FIRE:
                    FireDamageAction(skill.owner, skill.skill, target);
                    break;
                case Enumerators.SetType.LIFE:
                    HealAnyAction(skill.owner, skill.skill, target);
                    break;
                case Enumerators.SetType.EARTH:
                    HealAction(skill.owner, skill.skill);
                    break;
                case Enumerators.SetType.AIR:
                    CardReturnAction(skill.owner, skill.skill, target);
                    break;
                default:
                    break;
            }
        }

        #region actions

        private void FreezeAction(Player owner, HeroSkill skill, object target)
        {
            if (target is BoardUnit)
            {
                var unit = target as BoardUnit;
                unit.Stun(Enumerators.StunType.FREEZE, skill.value);

                _vfxController.CreateVFX(Enumerators.SetType.WATER, unit.transform.position);

                _actionsQueueController.PostGameActionReport(_actionsQueueController.FormatGameActionReport(Enumerators.ActionType.STUN_CREATURE_BY_SKILL, new object[]
                {
                    owner,
                    unit
                }));
            }
            else if (target is Player)
            {

            }
        }

        private void ToxicDamageAction(Player owner, HeroSkill skill, object target)
        {
            AttackWithModifiers(owner, skill, target, Enumerators.SetType.TOXIC, Enumerators.SetType.LIFE);
        }

        private void FireDamageAction(Player owner, HeroSkill skill, object target)
        {
            AttackWithModifiers(owner, skill, target, Enumerators.SetType.FIRE, Enumerators.SetType.TOXIC);
        }

        private void HealAnyAction(Player owner, HeroSkill skill, object target)
        {
            if (target is Player)
            {
                var player = target as Player;

                _battleController.HealPlayerBySkill(owner, skill, player);

                _vfxController.CreateVFX(Enumerators.SetType.LIFE, player.AvatarObject.transform.position);
            }
            else
            {
                var unit = target as BoardUnit;

                _battleController.HealCreatureBySkill(owner, skill, unit);

                _vfxController.CreateVFX(Enumerators.SetType.LIFE, unit.transform.position);
            }
        }

        private void HealAction(Player owner, HeroSkill skill)
        {
            _battleController.HealPlayerBySkill(owner, skill, owner);

            _vfxController.CreateVFX(Enumerators.SetType.EARTH, owner.AvatarObject.transform.position - Vector3.right * 2.3f);
        }

        private void AttackWithModifiers(Player owner, HeroSkill skill, object target, Enumerators.SetType attackType, Enumerators.SetType setType)
        {
            if (target is Player)
            {
                var player = target as Player;
                //TODO additional damage to heros

                _battleController.AttackPlayerBySkill(owner, skill, player);

                _vfxController.CreateVFX(attackType, player.AvatarObject.transform.position);
            }
            else
            {
                var creature = target as BoardUnit;
                var attackModifier = 0;

                if (creature.Card.libraryCard.cardSetType == setType)
                    attackModifier = 1;

                _battleController.AttackCreatureBySkill(owner, skill, creature, attackModifier);

                _vfxController.CreateVFX(attackType, creature.transform.position);
            }
        }
        
        private void CardReturnAction(Player owner, HeroSkill skill, object target)
        {
            BoardUnit targetUnit = (target as BoardUnit);
            Player unitOwner = targetUnit.ownerPlayer;
            WorkingCard returningCard = targetUnit.Card;
            Vector3 unitPosition = targetUnit.transform.position;

            _vfxController.CreateVFX(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PushVFX"), unitPosition);

            _timerManager.AddTimer((x) =>
            {

                // STEP 1 - REMOVE UNIT FROM BOARD
                unitOwner.BoardCards.Remove(targetUnit);

                // STEP 2 - DESTROY UNIT ON THE BOARD OR ANIMATE;
                targetUnit.Die(true);
                MonoBehaviour.Destroy(targetUnit.gameObject);

                // STEP 3 - REMOVE WORKING CARD FROM BOARD
                unitOwner.RemoveCardFromBoard(returningCard);

                // STEP 4 - RETURN CARD TO HAND
                _cardsController.ReturnToHandBoardUnit(returningCard, unitOwner, unitPosition);

                // STEP 4 - REARRANGE HANDS
                _gameplayManager.RearrangeHands();

                _actionsQueueController.PostGameActionReport(_actionsQueueController.FormatGameActionReport(Enumerators.ActionType.RETURN_TO_HAND_CARD_SKILL, new object[]
                {
                owner,
                skill,
                targetUnit
                }));

                _gameplayManager.GetController<RanksController>().UpdateRanksBuffs(unitOwner);
            }, null, 2f);
        }

        #endregion
    }
}