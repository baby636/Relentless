// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using System;

using UnityEngine;
using UnityEngine.Rendering;

using DG.Tweening;
using TMPro;
using LoomNetwork.CZB.Common;
using System.Collections.Generic;
using LoomNetwork.CZB.Helpers;
using LoomNetwork.Internal;
using LoomNetwork.CZB.Data;

namespace LoomNetwork.CZB
{
    public class BoardUnit
    {
        public event Action CreatureOnDieEvent;
        public event Action<object> CreatureOnAttackEvent;

        public event Action<int, int> CreatureHPChangedEvent;
        public event Action<int, int> CreatureDamageChangedEvent;

        private Action<int, int> damageChangedDelegate;
        private Action<int, int> healthChangedDelegate;

        private ILoadObjectsManager _loadObjectsManager;
        private IGameplayManager _gameplayManager;
        private ISoundManager _soundManager;
        private ITimerManager _timerManager;
        private ITutorialManager _tutorialManager;
        private PlayerController _playerController;
        private BattlegroundController _battlegroundController;
        private AnimationsController _animationsController;
        private BattleController _battleController;
        private ActionsQueueController _actionsQueueController;

        private GameObject _fightTargetingArrowPrefab;

        private GameObject _selfObject;

        private SpriteRenderer pictureSprite;
        private SpriteRenderer frozenSprite;
        private SpriteRenderer glowSprite;

        private TextMeshPro attackText;
        private TextMeshPro healthText;

        private ParticleSystem sleepingParticles;

        private int _damage;
        private int _health;

        private int _stunTurns = 0;

        private BoardArrow abilitiesTargetingArrow;
        private BattleBoardArrow fightTargetingArrow;

        private AnimationEventTriggering arrivalAnimationEventHandler;

        private OnBehaviourHandler _onBehaviourHandler;

        private GameObject creatureContentObject;

        private Animator creatureAnimator;

        private bool _dead = false;

        public bool hasImpetus;
        public bool hasProvoke;
        public int numTurnsOnBoard;

        public int initialDamage;
        public int initialHP;

        public Player ownerPlayer;

        public List<UnitAnimatorInfo> animatorControllers;

        public int Damage
        {
            get
            {
                return _damage;
            }
            set
            {
                var oldDamage = _damage;
                _damage = value;

                CreatureDamageChangedEvent?.Invoke(oldDamage, _damage);
            }
        }

        public int HP
        {
            get
            {
                return _health;
            }
            set
            {
                var oldHealth = _health;
                _health = value;

                _health = Mathf.Clamp(_health, 0, 99);

                CreatureHPChangedEvent?.Invoke(oldHealth, _health);
            }
        }

        public Transform transform { get { return _selfObject.transform; } }
        public GameObject gameObject { get { return _selfObject; } }
        public Sprite sprite { get { return pictureSprite.sprite; } }

        public bool IsPlayable { get; set; }

        public WorkingCard Card { get; private set; }

        public int InstanceId { get; private set; }

        public bool IsStun
        {
            get { return (_stunTurns > 0 ? true : false); }
        }

        public BoardUnit(Transform parent)
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _timerManager = GameClient.Get<ITimerManager>();

            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();
            _playerController = _gameplayManager.GetController<PlayerController>();
            _animationsController = _gameplayManager.GetController<AnimationsController>();
            _battleController = _gameplayManager.GetController<BattleController>();
            _actionsQueueController = _gameplayManager.GetController<ActionsQueueController>();

            _selfObject = MonoBehaviour.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/BoardCreature"));
            _selfObject.transform.SetParent(parent, false);

            _fightTargetingArrowPrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/FightTargetingArrow");

            pictureSprite = _selfObject.transform.Find("GraphicsAnimation/PictureRoot/CreaturePicture").GetComponent<SpriteRenderer>();
            frozenSprite = _selfObject.transform.Find("Other/Frozen").GetComponent<SpriteRenderer>();
            glowSprite = _selfObject.transform.Find("Other/Glow").GetComponent<SpriteRenderer>();

            attackText = _selfObject.transform.Find("Other/AttackAndDefence/AttackText").GetComponent<TextMeshPro>();
            healthText = _selfObject.transform.Find("Other/AttackAndDefence/DefenceText").GetComponent<TextMeshPro>();

            sleepingParticles = _selfObject.transform.Find("Other/SleepingParticles").GetComponent<ParticleSystem>();

            creatureAnimator = _selfObject.transform.Find("GraphicsAnimation").GetComponent<Animator>();

            creatureContentObject = _selfObject.transform.Find("Other").gameObject;

            arrivalAnimationEventHandler = _selfObject.transform.Find("GraphicsAnimation").GetComponent<AnimationEventTriggering>();

            _onBehaviourHandler = _selfObject.GetComponent<OnBehaviourHandler>();

            arrivalAnimationEventHandler.OnAnimationEvent += ArrivalAnimationEventHandler;

            _onBehaviourHandler.OnMouseUpEvent += OnMouseUp;
            _onBehaviourHandler.OnMouseDownEvent += OnMouseDown;
            _onBehaviourHandler.OnTriggerEnter2DEvent += OnTriggerEnter2D;
            _onBehaviourHandler.OnTriggerExit2DEvent += OnTriggerExit2D;

            animatorControllers = new List<UnitAnimatorInfo>();
            for (int i = 0; i < Enum.GetNames(typeof(Enumerators.CardType)).Length; i++)
            {
                animatorControllers.Add(new UnitAnimatorInfo()
                {
                    animator = _loadObjectsManager.GetObjectByPath<RuntimeAnimatorController>("Animators/" + ((Enumerators.CardType)i).ToString() + "ArrivalController"),
                    cardType = (Enumerators.CardType)i
                });
            }
        }

        public void Die()
        {
            CreatureHPChangedEvent -= healthChangedDelegate;
            CreatureDamageChangedEvent -= damageChangedDelegate;

            _dead = true;

            _battlegroundController.KillBoardCard(this);

            CreatureOnDieEvent?.Invoke();
        }

        public void ArrivalAnimationEventHandler(string param)
        {
            if (param.Equals("ArrivalAnimationDone"))
            {
                creatureContentObject.SetActive(true);
                if (hasImpetus)
                {
                    //  frameSprite.sprite = frameSprites[1];
                    StopSleepingParticles();
                    if (ownerPlayer != null)
                        SetHighlightingEnabled(true);
                }


                InternalTools.SetLayerRecursively(_selfObject, 0);

                if (Card.libraryCard.cardRarity == Enumerators.CardRarity.EPIC)
                {
                    _soundManager.PlaySound(Enumerators.SoundType.CARDS, Card.libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_PLAY + "1", Constants.ZOMBIES_SOUND_VOLUME, false, true);
                    _soundManager.PlaySound(Enumerators.SoundType.CARDS, Card.libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_PLAY + "2", Constants.ZOMBIES_SOUND_VOLUME / 2f, false, true);
                }
                else
                {
                    _soundManager.PlaySound(Enumerators.SoundType.CARDS, Card.libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_PLAY, Constants.ZOMBIES_SOUND_VOLUME, false, true);
                }


                if (Card.libraryCard.name.Equals("Freezzee"))
                {
                    var freezzees = GetEnemyCreaturesList(this).FindAll(x => x.Card.libraryCard.id == Card.libraryCard.id);

                    if (freezzees.Count > 0)
                    {
                        foreach (var creature in freezzees)
                        {
                            creature.Stun(1);
                            CreateFrozenVFX(creature.transform.position);
                        }
                    }
                }
            }
        }

        private void CreateFrozenVFX(Vector3 pos)
        {
            var _frozenVFX = MonoBehaviour.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/FrozenVFX"));
            _frozenVFX.transform.position = Utilites.CastVFXPosition(pos + Vector3.forward);
            DestroyCurrentParticle(_frozenVFX);
        }

        private void DestroyCurrentParticle(GameObject currentParticle, bool isDirectly = false, float time = 5f)
        {
            if (isDirectly)
                DestroyParticle(new object[] { currentParticle });
            else
                _timerManager.AddTimer(DestroyParticle, new object[] { currentParticle }, time, false);
        }

        private void DestroyParticle(object[] param)
        {
            GameObject particleObj = param[0] as GameObject;
            MonoBehaviour.Destroy(particleObj);
        }

        private List<BoardUnit> GetEnemyCreaturesList(BoardUnit creature)
        {
            if (_gameplayManager.GetLocalPlayer().BoardCards.Contains(creature))
                return _gameplayManager.GetOpponentPlayer().BoardCards;
            return _gameplayManager.GetLocalPlayer().BoardCards;
        }

        public void SetObjectInfo(WorkingCard card, string setName = "")
        {
            Card = card;

            pictureSprite.sprite = _loadObjectsManager.GetObjectByPath<Sprite>(string.Format("Images/Cards/Illustrations/{0}_{1}_{2}", setName.ToLower(), Card.libraryCard.cardRarity.ToString().ToLower(), Card.libraryCard.picture.ToLower()));

            pictureSprite.transform.localPosition = MathLib.FloatVector3ToVector3(Card.libraryCard.cardViewInfo.position);
            pictureSprite.transform.localScale = MathLib.FloatVector3ToVector3(Card.libraryCard.cardViewInfo.scale);

            creatureAnimator.runtimeAnimatorController = animatorControllers.Find(x => x.cardType == Card.libraryCard.cardType).animator;
            if (Card.libraryCard.cardType == Enumerators.CardType.WALKER)
            {
                sleepingParticles.transform.position += Vector3.up * 0.7f;
            }

            Damage = card.damage;
            HP = card.health;

            initialDamage = Damage;
            initialHP = HP;

            attackText.text = Damage.ToString();
            healthText.text = HP.ToString();

            damageChangedDelegate = (oldValue, newValue) =>
            {
                UpdateUnitInfoText(attackText, Damage, initialDamage, true);
            };

            CreatureDamageChangedEvent += damageChangedDelegate;

            healthChangedDelegate = (oldValue, newValue) =>
            {
                UpdateUnitInfoText(healthText, HP, initialHP, false);
                CheckOnDie();
            };

            CreatureHPChangedEvent += healthChangedDelegate;

            switch (Card.libraryCard.cardType)
            {
                case Enumerators.CardType.FERAL:
                    hasImpetus = true;
                    IsPlayable = true;
                    _soundManager.PlaySound(Enumerators.SoundType.FERAL_ARRIVAL, Constants.ARRIVAL_SOUND_VOLUME, false, false, true);
                    break;
                case Enumerators.CardType.HEAVY:
                    _soundManager.PlaySound(Enumerators.SoundType.HEAVY_ARRIVAL, Constants.ARRIVAL_SOUND_VOLUME, false, false, true);
                    hasProvoke = true;
                    break;
                case Enumerators.CardType.WALKER:
                default:
                    _soundManager.PlaySound(Enumerators.SoundType.WALKER_ARRIVAL, Constants.ARRIVAL_SOUND_VOLUME, false, false, true);
                    break;
            }

            if (hasProvoke)
            {
                //   glowSprite.gameObject.SetActive(false);
                //  pictureMaskTransform.localScale = new Vector3(50, 55, 1);
                // frameSprite.sprite = frameSprites[2];
            }
            SetHighlightingEnabled(false);

            creatureAnimator.StopPlayback();
            creatureAnimator.Play(0);
        }

        private void CheckOnDie()
        {
            if (HP <= 0 && !_dead)
                Die();
        }

        public void PlayArrivalAnimation()
        {
            creatureAnimator.SetTrigger("Active");
        }

        public void OnStartTurn()
        {
            numTurnsOnBoard += 1;
            StopSleepingParticles();

            if (ownerPlayer != null && IsPlayable && _gameplayManager.WhoseTurn.Equals(ownerPlayer))
                SetHighlightingEnabled(true);
        }

        public void OnEndTurn()
        {
            if (_stunTurns > 0)
                _stunTurns--;
            if (_stunTurns == 0)
            {
                IsPlayable = true;
                frozenSprite.DOFade(0, 1);
            }

            CancelTargetingArrows();
        }

        public void Stun(int turns)
        {
            Debug.Log("WAS STUNED");
            if (turns > _stunTurns)
                _stunTurns = turns;
            IsPlayable = false;

            frozenSprite.DOFade(1, 1);
            //sleepingParticles.Play();
        }

        public void CancelTargetingArrows()
        {
            if (abilitiesTargetingArrow != null)
            {
                MonoBehaviour.Destroy(abilitiesTargetingArrow.gameObject);
            }
            if (fightTargetingArrow != null)
            {
                MonoBehaviour.Destroy(fightTargetingArrow.gameObject);
            }
        }

        private void UpdateUnitInfoText(TextMeshPro text, int stat, int initialStat, bool isDamage)
        {
            if (text == null || !text)
                return;

            text.text = stat.ToString();

            if (stat > initialStat)
                text.color = Color.green;
            else if (stat < initialStat)
                text.color = Color.red;
            else
            {
                if (isDamage)
                    text.color = Color.white;
                else
                    text.color = Color.black;
            }
            var sequence = DOTween.Sequence();
            sequence.Append(text.transform.DOScale(new Vector3(1.4f, 1.4f, 1.0f), 0.4f));
            sequence.Append(text.transform.DOScale(new Vector3(1.0f, 1.0f, 1.0f), 0.2f));
            sequence.Play();
        }

        public void SetHighlightingEnabled(bool enabled)
        {
            glowSprite.enabled = enabled;
        }

        public void StopSleepingParticles()
        {
            if (sleepingParticles != null)
                sleepingParticles.Stop();
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.transform.parent != null)
            {
                var targetingArrow = collider.transform.parent.parent.GetComponent<BoardArrow>();
                if (targetingArrow != null)
                {
                    targetingArrow.OnCardSelected(this);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collider)
        {
            if (collider.transform.parent != null)
            {
                var targetingArrow = collider.transform.parent.parent.GetComponent<BoardArrow>();
                if (targetingArrow != null)
                {
                    targetingArrow.OnCardUnselected(this);
                }
            }
        }

        private void OnMouseDown(GameObject obj)
        {
            //if (fightTargetingArrowPrefab == null)
            //    return;

            //Debug.LogError(IsPlayable + " | " + ownerPlayer.isActivePlayer + " | " + ownerPlayer);

            if (ownerPlayer != null && ownerPlayer.IsLocalPlayer && _playerController.IsActive && IsPlayable)
            {
                fightTargetingArrow = MonoBehaviour.Instantiate(_fightTargetingArrowPrefab).GetComponent<BattleBoardArrow>();
                fightTargetingArrow.targetsType = new List<Enumerators.SkillTargetType>() { Enumerators.SkillTargetType.OPPONENT, Enumerators.SkillTargetType.OPPONENT_CARD };
                fightTargetingArrow.BoardCards = _gameplayManager.PlayersInGame.Find(x => x != ownerPlayer).BoardCards;
                fightTargetingArrow.Begin(transform.position);

                if (ownerPlayer.Equals(_gameplayManager.GetLocalPlayer()))
                {
                    _battlegroundController.DestroyCardPreview();
                    _playerController.IsCardSelected = true;

                    if (_tutorialManager.IsTutorial)
                        _tutorialManager.DeactivateSelectTarget();
                }
            }
        }

        private void OnMouseUp(GameObject obj)
        {
            if (ownerPlayer != null && ownerPlayer.IsLocalPlayer && _playerController.IsActive && IsPlayable)
            {
                if (fightTargetingArrow != null)
                {
                    fightTargetingArrow.End(this);

                    if (ownerPlayer.Equals(_gameplayManager.GetLocalPlayer()))
                    {
                        _playerController.IsCardSelected = false;
                    }
                }
            }
        }

        public void DoCombat()
        {
            var sortingGroup = _selfObject.GetComponent<SortingGroup>();
            if (fightTargetingArrow != null)
            {
                if (fightTargetingArrow.selectedPlayer != null)
                {
                    var targetPlayer = fightTargetingArrow.selectedPlayer;
                    SetHighlightingEnabled(false);
                    IsPlayable = false;


                    //         GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARDS, libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_ATTACK, Constants.ZOMBIES_SOUND_VOLUME, false, true);

                    //sortingGroup.sortingOrder = 100;

                    _actionsQueueController.AddNewActionInToQueue((parameter, completeCallback) =>
                    {
                        _animationsController.PlayFightAnimation(_selfObject, targetPlayer.AvatarObject, 0.1f, () =>
                        {
                            _soundManager.PlaySound(Enumerators.SoundType.CARDS, Card.libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_ATTACK, Constants.ZOMBIES_SOUND_VOLUME, false, true);

                            Vector3 positionOfVFX = targetPlayer.Transform.position;
                            positionOfVFX.y = 4.45f;

                            _gameplayManager.GetController<VFXController>().PlayAttackVFX(Card.libraryCard.cardType, positionOfVFX, Damage);


                            _battleController.AttackPlayerByCreature(this, targetPlayer);
                            CreatureOnAttackEvent?.Invoke(targetPlayer);
                        },
                        () =>
                        {
                            //sortingGroup.sortingOrder = 0;
                            fightTargetingArrow = null;

                            completeCallback?.Invoke();
                        });
                    });
                }
                if (fightTargetingArrow.selectedCard != null)
                {
                    var targetCard = fightTargetingArrow.selectedCard;
                    SetHighlightingEnabled(false);
                    IsPlayable = false;

                    _actionsQueueController.AddNewActionInToQueue((parameter, completeCallback) =>
                    {
                        _soundManager.PlaySound(Enumerators.SoundType.CARDS, Card.libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_ATTACK, Constants.ZOMBIES_SOUND_VOLUME, false, true);

                        //sortingGroup.sortingOrder = 100;
                       // if (targetCard.transform== null)
                        {

                            // play sound when target creature attack more than our
                            if (targetCard.Damage > Damage)
                            {
                                _soundManager.PlaySound(Enumerators.SoundType.CARDS, targetCard.Card.libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_ATTACK, Constants.ZOMBIES_SOUND_VOLUME, false, true);
                            }

                            _animationsController.PlayFightAnimation(_selfObject, targetCard.transform.gameObject, 0.5f, () =>
                            {
                                _gameplayManager.GetController<VFXController>().PlayAttackVFX(Card.libraryCard.cardType, targetCard.transform.position, Damage);

                                _battleController.AttackCreatureByCreature(this, targetCard);
                                CreatureOnAttackEvent?.Invoke(targetCard);
                            },
                            () =>
                            {
                                //sortingGroup.sortingOrder = 0;
                                fightTargetingArrow = null;

                                completeCallback?.Invoke();
                            });
                        }
                    });
                }
                if (fightTargetingArrow.selectedCard == null && fightTargetingArrow.selectedPlayer == null)
                {
                    if (_tutorialManager.IsTutorial)
                        _tutorialManager.ActivateSelectTarget();
                }
            }
        }

        public void CreatureOnAttack(object target)
        {
            CreatureOnAttackEvent?.Invoke(target);
        }
    }

    [Serializable]
    public class UnitAnimatorInfo
    {
        public Enumerators.CardType cardType;
        public RuntimeAnimatorController animator;
    }
}