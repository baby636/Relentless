using System;
using System.Collections.Generic;
using DG.Tweening;
using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;
using LoomNetwork.CZB.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace LoomNetwork.CZB
{
    public class CardsController : IController
    {
        public GameObject CreatureCardViewPrefab, OpponentCardPrefab, SpellCardViewPrefab;

        private IGameplayManager _gameplayManager;

        private ITimerManager _timerManager;

        private ILoadObjectsManager _loadObjectsManager;

        private IDataManager _dataManager;

        private ISoundManager _soundManager;

        private ITutorialManager _tutorialManager;

        private IUIManager _uiManager;

        private BattlegroundController _battlegroundController;

        private VfxController _vfxController;

        private AbilitiesController _abilitiesController;

        private ActionsQueueController _actionsQueueController;

        private AnimationsController _animationsController;

        private RanksController _ranksController;

        private GameObject _playerBoard;

        private GameObject _opponentBoard;

        private BoardUnit _fakeBoardCard;

        private int _cardInstanceId;

        private int _indexOfCard;

        public event Action<Player> UpdateCardsStatusEvent;

        public bool CardDistribution { get; set; }

        public void Init()
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _timerManager = GameClient.Get<ITimerManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _dataManager = GameClient.Get<IDataManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _uiManager = GameClient.Get<IUIManager>();

            _battlegroundController = _gameplayManager.GetController<BattlegroundController>();
            _vfxController = _gameplayManager.GetController<VfxController>();
            _abilitiesController = _gameplayManager.GetController<AbilitiesController>();
            _actionsQueueController = _gameplayManager.GetController<ActionsQueueController>();
            _animationsController = _gameplayManager.GetController<AnimationsController>();
            _ranksController = _gameplayManager.GetController<RanksController>();

            CreatureCardViewPrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Cards/CreatureCard");
            SpellCardViewPrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Cards/SpellCard");
            OpponentCardPrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Cards/OpponentCard");

            _gameplayManager.OnGameStartedEvent += OnGameStartedEventHandler;
            _gameplayManager.OnGameEndedEvent += OnGameEndedEventHandler;

            _indexOfCard = -1;
        }

        public void Dispose()
        {
        }

        public void ResetAll()
        {
        }

        public void Update()
        {
        }

        public int GetNewCardInstanceId()
        {
            return _cardInstanceId++;
        }

        public void StartCardDistribution()
        {
            CardDistribution = true;

            GameClient.Get<ICameraManager>().FadeIn(0.8f, 0, false);

            if (_gameplayManager.IsTutorial)
            {
                EndCardDistribution();
            }
            else
            {
                _uiManager.GetPage<GameplayPage>().KeepButtonVisibility(true);
            }

            // _timerManager.AddTimer(DirectlyEndCardDistribution, null, Constants.CARD_DISTRIBUTION_TIME);
        }

        public void EndCardDistribution()
        {
            if (!CardDistribution)
                return;

            _gameplayManager.IsPrepairingEnded = true;

            GameClient.Get<ICameraManager>().FadeOut(immediately: true);

            _timerManager.StopTimer(DirectlyEndCardDistribution);

            // for local player
            List<BoardCard> cards = new List<BoardCard>();
            cards.AddRange(_gameplayManager.CurrentPlayer.CardsPreparingToHand.FindAll(x => x.CardShouldBeChanged));
            foreach (BoardCard card in cards)
            {
                // _gameplayManager.CurrentPlayer.CardsInDeck.IndexOf(card.WorkingCard);
                _gameplayManager.CurrentPlayer.CardsInDeck.Remove(card.WorkingCard);
                _gameplayManager.CurrentPlayer.CardsInDeck.Add(card.WorkingCard);
                card.ReturnCardToDeck();
            }

            foreach (BoardCard card in _gameplayManager.CurrentPlayer.CardsPreparingToHand)
            {
                SortingGroup sortingGroup = card.GameObject.GetComponent<SortingGroup>();
                sortingGroup.sortingLayerName = "Foreground";
                sortingGroup.sortingOrder = 1;
                _gameplayManager.CurrentPlayer.RemoveCardFromDeck(card.WorkingCard);
                _gameplayManager.CurrentPlayer.CardsInHand.Add(card.WorkingCard);
                _battlegroundController.PlayerHandCards.Add(card);

                _timerManager.AddTimer(
                    x =>
                    {
                        card.HandBoardCard.Enabled = true;
                    },
                    null,
                    2f);
            }

            if (_gameplayManager.CurrentPlayer.CardsPreparingToHand.Count > 0)
            {
                _battlegroundController.UpdatePositionOfCardsInPlayerHand(true);
                _gameplayManager.CurrentPlayer.CardsPreparingToHand.Clear();
            }

            CardDistribution = false;

            _gameplayManager.CurrentPlayer.ThrowOnHandChanged();

            if (!_gameplayManager.IsTutorial)
            {
                _gameplayManager.CurrentPlayer.CardsInDeck = _gameplayManager.CurrentPlayer.ShuffleCardsList(_gameplayManager.CurrentPlayer.CardsInDeck);

                /*
                               if (_gameplayManager.CurrentTurnPlayer.Equals(_gameplayManager.CurrentPlayer))
                               {
                                   AddCardToHand(_gameplayManager.CurrentPlayer, _gameplayManager.CurrentPlayer.CardsInDeck[0]);
                                   AddCardToHand(_gameplayManager.OpponentPlayer, _gameplayManager.OpponentPlayer.CardsInDeck[0]);
                                   AddCardToHand(_gameplayManager.OpponentPlayer, _gameplayManager.OpponentPlayer.CardsInDeck[0]);
                               }
                               else
                               {
                                   AddCardToHand(_gameplayManager.OpponentPlayer, _gameplayManager.OpponentPlayer.CardsInDeck[0]);
                                   AddCardToHand(_gameplayManager.CurrentPlayer, _gameplayManager.CurrentPlayer.CardsInDeck[0]);
                                   AddCardToHand(_gameplayManager.CurrentPlayer, _gameplayManager.CurrentPlayer.CardsInDeck[0]);
                               }    
                               _timerManager.AddTimer((x) =>
                               {
                                   _battlegroundController.StartGameplayTurns();
                               }, null, 2f); */
                _battlegroundController.StartGameplayTurns();
            }
            else
            {
                _battlegroundController.StartGameplayTurns();
            }
        }

        public void AddCardToDistributionState(Player player, WorkingCard card)
        {
            BoardCard boardCard = CreateBoardCard(card);
            SortingGroup sortingGroup = boardCard.GameObject.GetComponent<SortingGroup>();
            sortingGroup.sortingLayerName = Constants.KLayerGameUI1;
            player.CardsPreparingToHand.Add(boardCard);
            boardCard.HandBoardCard.Enabled = false;
            boardCard.MoveCardFromDeckToCenter();
        }

        public void UpdatePositionOfCardsForDistribution(Player player)
        {
            int count = player.CardsPreparingToHand.Count;

            float handWidth = 0.0f;
            float spacing = -5f;

            handWidth += (spacing * count) - 1;

            Vector3 pivot = new Vector3(-3f, 0, 0);

            Vector3 moveToPosition = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                moveToPosition = new Vector3(pivot.x - (handWidth / 2f), 0, 0);
                player.CardsPreparingToHand[i].Transform.DOMove(moveToPosition, 1f, false);
                player.CardsPreparingToHand[i].Transform.DOScale(Vector3.one * 0.4f, 1);

                pivot.x += handWidth / count;
            }
        }

        public void ReturnCardToDeck(BoardCard card, Action callback)
        {
            card.WorkingCard.Owner.CardsPreparingToHand.Remove(card);
            Object.Destroy(card.GameObject);

            callback?.Invoke();

            // UpdatePositionOfCardsForDistribution(card.WorkingCard.owner);
        }

        public void AddCardToHand(Player player, WorkingCard card = null)
        {
            if (card == null)
            {
                if (player.CardsInDeck.Count == 0)
                {
                    player.DamageByNoMoreCardsInDeck++;
                    player.Hp -= player.DamageByNoMoreCardsInDeck;
                    _vfxController.SpawnGotDamageEffect(player, -player.DamageByNoMoreCardsInDeck);
                    return;
                }

                card = player.CardsInDeck[0];
            }

            if (CheckIsMoreThanMaxCards(card, player))
                return;

            player.RemoveCardFromDeck(card);
            player.AddCardToHand(card);
        }

        public void AddCardToHandFromOtherPlayerDeck(Player player, Player otherPlayer, WorkingCard card = null)
        {
            if (card == null)
            {
                if (otherPlayer.CardsInDeck.Count == 0)
                {
                    otherPlayer.DamageByNoMoreCardsInDeck++;
                    otherPlayer.Hp -= otherPlayer.DamageByNoMoreCardsInDeck;
                    _vfxController.SpawnGotDamageEffect(otherPlayer, -otherPlayer.DamageByNoMoreCardsInDeck);
                    return;
                }

                card = otherPlayer.CardsInDeck[0];
            }

            otherPlayer.RemoveCardFromDeck(card);

            if (CheckIsMoreThanMaxCards(card, player))
                return;

            if (player.Equals(otherPlayer))
            {
                player.AddCardToHand(card);
            }
            else
            {
                player.AddCardToHandFromOpponentDeck(otherPlayer, card);
            }
        }

        public GameObject AddCardToHand(WorkingCard card, bool silent = false)
        {
            BoardCard boardCard = CreateBoardCard(card);

            if (_battlegroundController.CurrentTurn == 0)
            {
                boardCard.SetDefaultAnimation(boardCard.WorkingCard.Owner.CardsInHand.Count);

                // if (playerHandCards.Count == 4)
                // RearrangeHand();
            }

            _battlegroundController.PlayerHandCards.Add(boardCard);

            if (silent)
            {
                boardCard.HandBoardCard.Enabled = false;

                _timerManager.AddTimer(
                    x =>
                    {
                        boardCard.HandBoardCard.Enabled = true;
                        boardCard.HandBoardCard.CheckStatusOfHighlight();
                    },
                    null,
                    2f);
            }
            else
            {
                boardCard.HandBoardCard.CheckStatusOfHighlight();
            }

            // UpdateCardsStatusEvent?.Invoke(card.owner);
            return boardCard.GameObject;
        }

        public GameObject AddCardToOpponentHand(WorkingCard card, bool silent = false)
        {
            GameObject go = CreateOpponentBoardCard();

            _battlegroundController.OpponentHandCards.Add(go);

            _abilitiesController.CallAbilitiesInHand(null, card);

            return go;
        }

        public GameObject CreateOpponentBoardCard()
        {
            Player opponent = _gameplayManager.OpponentPlayer;
            GameObject go = Object.Instantiate(OpponentCardPrefab);
            go.GetComponent<SortingGroup>().sortingOrder = opponent.CardsInHand.Count;

            return go;
        }

        public void RemoveCard(object[] param)
        {
            _soundManager.PlaySound(Enumerators.SoundType.CardBattlegroundToTrash, Constants.CardsMoveSoundVolume, false, false);

            BoardCard card = param[0] as BoardCard;

            // BoardCreature currentCreature = null;
            // if (param.Length > 1)
            // currentCreature = param[1] as BoardCreature;
            GameObject go = card.GameObject;

            // if (!go.transform.Find("BackgroundBack").gameObject.activeSelf)
            // return;
            SortingGroup sortingGroup = card.GameObject.GetComponent<SortingGroup>();

            Sequence animationSequence3 = DOTween.Sequence();

            // animationSequence3.Append(go.transform.DORotate(new Vector3(go.transform.eulerAngles.x, 90, 90), .2f));
            animationSequence3.Append(go.transform.DORotate(new Vector3(0, 90, 90), .3f));

            // go.transform.DOScale(new Vector3(.19f, .19f, .19f), .2f);
            go.transform.DOScale(new Vector3(.195f, .195f, .195f), .2f);
            animationSequence3.OnComplete(
                () =>
                {
                    go.transform.Find("Back").gameObject.SetActive(true);
                    Sequence animationSequence4 = DOTween.Sequence();

                    // animationSequence4.Append(go.transform.DORotate(new Vector3(40f, 180, 90f), .3f));
                    animationSequence4.Append(go.transform.DORotate(new Vector3(0, 180, 0f), .45f));

                    // animationSequence4.AppendInterval(2f);

                    // Changing layers to all child objects to set them Behind the Graveyard Card
                    sortingGroup.sortingLayerName = Constants.KLayerForeground;
                    sortingGroup.sortingOrder = 7;

                    sortingGroup.gameObject.layer = 0;

                    for (int i = 0; i < sortingGroup.transform.childCount; i++)
                    {
                        Transform child = sortingGroup.transform.GetChild(i);

                        if (child.name != "Back")
                        {
                            child.gameObject.SetActive(false);
                        }
                        else
                        {
                            child.gameObject.layer = 0;
                        }
                    }
                });

            Sequence animationSequence2 = DOTween.Sequence();

            // animationSequence2.Append(go.transform.DOMove(new Vector3(-4.1f, -1, 0), .3f));
            animationSequence2.Append(go.transform.DOMove(new Vector3(-7.74f, -1, 0), 0.7f));

            animationSequence2.OnComplete(
                () =>
                {
                    for (int i = 0; i < sortingGroup.transform.childCount; i++)
                    {
                        Transform child = sortingGroup.transform.GetChild(i);

                        if (child.name == "Back")
                        {
                            child.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
                        }
                    }

                    Sequence animationSequence5 = DOTween.Sequence();
                    animationSequence5.Append(go.transform.DOMove(new Vector3(-7.74f, -4.352f, 0), .5f));
                    animationSequence5.OnComplete(
                        () =>
                        {
                            Object.Destroy(go);
                        });
                });
        }

        public void RemoveOpponentCard(object[] param)
        {
            _soundManager.PlaySound(Enumerators.SoundType.CardBattlegroundToTrash, Constants.CardsMoveSoundVolume, false, false);

            GameObject go = param[0] as GameObject;
            BoardUnit currentCreature = null;
            if (param.Length > 1)
            {
                currentCreature = param[1] as BoardUnit;
            }

            // if (!go.transform.Find("BackgroundBack").gameObject.activeSelf)
            // return;
            SortingGroup sortingGroup = go.GetComponent<SortingGroup>();

            Sequence animationSequence3 = DOTween.Sequence();

            // animationSequence3.Append(go.transform.DORotate(new Vector3(go.transform.eulerAngles.x, 0, 90), .2f));
            animationSequence3.Append(go.transform.DORotate(new Vector3(go.transform.eulerAngles.x, 0, -30f), .4f));
            go.transform.DOScale(new Vector3(1, 1, 1), .2f);
            animationSequence3.OnComplete(
                () =>
                {
                    // if (go.transform.Find("BackgroundBack") != null)
                    // go.transform.Find("BackgroundBack").gameObject.SetActive(true);
                    // //Sequence animationSequence4 = DOTween.Sequence();
                    // //animationSequence4.Append(go.transform.DORotate(new Vector3(40f, 180, 90f), .3f));
                    // //animationSequence4.AppendInterval(2f);
                });

            Sequence animationSequence2 = DOTween.Sequence();

            // animationSequence2.Append(go.transform.DOMove(new Vector3(-4.85f, 6.3f, 0), .3f));
            animationSequence2.Append(go.transform.DOMove(new Vector3(7.7f, 14f, 0), .6f));

            animationSequence2.OnComplete(
                () =>
                {
                    go.layer = 0;
                    for (int i = 0; i < go.transform.childCount; i++)
                    {
                        go.transform.GetChild(i).gameObject.layer = 0;
                    }

                    // sortingGroup.sortingLayerName = "Default";
                    sortingGroup.sortingOrder = 7; // Foreground layer

                    Sequence animationSequence4 = DOTween.Sequence();
                    animationSequence4.Append(go.transform.DORotate(new Vector3(go.transform.eulerAngles.x, 0f, 0f), .2f));

                    Sequence animationSequence5 = DOTween.Sequence();
                    animationSequence5.Append(go.transform.DOMove(new Vector3(7.7f, 6.306f, 0), .5f));
                    animationSequence5.OnComplete(
                        () =>
                        {
                            Object.Destroy(go);
                        });
                });
        }

        public void HoverPlayerCardOnBattleground(Player player, BoardCard card, HandBoardCard handCard)
        {
            Card libraryCard = card.WorkingCard.LibraryCard;
            if (libraryCard.CardKind == Enumerators.CardKind.Creature)
            {
                int newIndexOfCard = 0;
                float newCreatureCardPosition = card.Transform.position.x;

                // set correct position on board depends from card view position
                for (int i = 0; i < player.BoardCards.Count; i++)
                {
                    if (newCreatureCardPosition > player.BoardCards[i].Transform.position.x)
                    {
                        newIndexOfCard = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if ((player.BoardCards.Count > 0) && (_indexOfCard != newIndexOfCard))
                {
                    _indexOfCard = newIndexOfCard;

                    List<BoardUnit> playerCards = _gameplayManager.CurrentPlayer.BoardCards;
                    List<BoardUnit> toArrangeList = new List<BoardUnit>();

                    for (int i = 0; i < playerCards.Count; i++)
                    {
                        toArrangeList.Add(playerCards[i]);
                    }

                    if (_fakeBoardCard != null)
                    {
                        Object.Destroy(_fakeBoardCard.GameObject);
                        _fakeBoardCard = null;
                    }

                    _fakeBoardCard = new BoardUnit(_playerBoard.transform);
                    toArrangeList.Insert(_indexOfCard, _fakeBoardCard);

                    _battlegroundController.UpdatePositionOfBoardUnitsOfPlayer(toArrangeList);
                }
            }
        }

        public void ResetPlayerCardsOnBattlegroundPosition()
        {
            if (_indexOfCard != -1)
            {
                _battlegroundController.UpdatePositionOfBoardUnitsOfPlayer(_gameplayManager.CurrentPlayer.BoardCards);
                _indexOfCard = -1;
                if (_fakeBoardCard != null)
                {
                    Object.Destroy(_fakeBoardCard.GameObject);
                    _fakeBoardCard = null;
                }
            }
        }

        public void PlayPlayerCard(Player player, BoardCard card, HandBoardCard handCard)
        {
            if (card.CanBePlayed(card.WorkingCard.Owner))
            {
                Card libraryCard = card.WorkingCard.LibraryCard;

                card.Transform.DORotate(Vector3.zero, .1f);
                card.HandBoardCard.Enabled = false;

                _soundManager.PlaySound(Enumerators.SoundType.CardFlyHandToBattleground, Constants.CardsMoveSoundVolume, false, false);

                // GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CARDS, libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_PLAY, Constants.ZOMBIES_SOUND_VOLUME, false, true);
                player.ThrowPlayCardEvent(card.WorkingCard);

                if (libraryCard.CardKind == Enumerators.CardKind.Creature)
                {
                    int indexOfCard = 0;
                    float newCreatureCardPosition = card.Transform.position.x;

                    // set correct position on board depends from card view position
                    for (int i = 0; i < player.BoardCards.Count; i++)
                    {
                        if (newCreatureCardPosition > player.BoardCards[i].Transform.position.x)
                        {
                            indexOfCard = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    BoardUnit boardUnit = new BoardUnit(_playerBoard.transform);
                    boardUnit.Transform.tag = Constants.KTagPlayerOwned;
                    boardUnit.Transform.parent = _playerBoard.transform;
                    boardUnit.Transform.position = new Vector2(1.9f * player.BoardCards.Count, 0);
                    boardUnit.OwnerPlayer = card.WorkingCard.Owner;
                    boardUnit.SetObjectInfo(card.WorkingCard);

                    _battlegroundController.PlayerHandCards.Remove(card);
                    _battlegroundController.PlayerBoardCards.Add(boardUnit);
                    player.AddCardToBoard(card.WorkingCard);
                    player.RemoveCardFromHand(card.WorkingCard);

                    player.BoardCards.Insert(indexOfCard, boardUnit);

                    // _ranksController.UpdateRanksBuffs(player, boardUnit.Card.libraryCard.cardRank);
                    _timerManager.AddTimer(
                        creat =>
                        {
                            card.WorkingCard.Owner.GraveyardCardsCount++;
                        },
                        null,
                        1f);

                    // Destroy(card.gameObject);
                    card.RemoveCardParticle.Play();

                    _actionsQueueController.PostGameActionReport(_actionsQueueController.FormatGameActionReport(Enumerators.ActionType.PlayUnitCard, new object[] { boardUnit.OwnerPlayer, boardUnit }));

                    UpdateCardsStatusEvent?.Invoke(player);

                    Sequence animationSequence = DOTween.Sequence();
                    animationSequence.Append(card.Transform.DOScale(new Vector3(.27f, .27f, .27f), 1f));
                    animationSequence.OnComplete(
                        () =>
                        {
                            RemoveCard(new object[] { card });

                            _timerManager.AddTimer(
                                param =>
                                {
                                    boardUnit.PlayArrivalAnimation();
                                    _battlegroundController.UpdatePositionOfBoardUnitsOfPlayer(
                                        _gameplayManager.CurrentPlayer.BoardCards,
                                        () =>
                                        {
                                            _abilitiesController.CallAbility(libraryCard, card, card.WorkingCard, Enumerators.CardKind.Creature, boardUnit, CallCardPlay, true, null);
                                        });
                                },
                                null,
                                0.1f,
                                false);
                        });

                    player.Goo -= card.ManaCost;
                    _tutorialManager.ReportAction(Enumerators.TutorialReportAction.MoveCard);
                }
                else if (libraryCard.CardKind == Enumerators.CardKind.Spell)
                {
                    player.CardsInHand.Remove(card.WorkingCard);
                    _battlegroundController.PlayerHandCards.Remove(card);
                    _battlegroundController.UpdatePositionOfCardsInPlayerHand();

                    card.GameObject.GetComponent<SortingGroup>().sortingLayerName = Constants.KLayerBoardCards;
                    card.GameObject.GetComponent<SortingGroup>().sortingOrder = 1000;

                    BoardSpell boardSpell = new BoardSpell(card.GameObject, card.WorkingCard);
                    boardSpell.Transform.position = Vector3.zero;

                    _abilitiesController.CallAbility(libraryCard, card, card.WorkingCard, Enumerators.CardKind.Spell, boardSpell, CallSpellCardPlay, true, null, handCard: handCard);
                }
            }
            else
            {
                card.HandBoardCard.ResetToInitialPosition();
            }
        }

        public void PlayOpponentCard(Player player, WorkingCard card, object target, Action<WorkingCard, object> completePlayCardCallback)
        {
            GameObject randomCard = _battlegroundController.OpponentHandCards[Random.Range(0, _battlegroundController.OpponentHandCards.Count)];

            _battlegroundController.OpponentHandCards.Remove(randomCard);

            _tutorialManager.ReportAction(Enumerators.TutorialReportAction.MoveCard);

            _soundManager.PlaySound(Enumerators.SoundType.CardFlyHandToBattleground, Constants.CardsMoveSoundVolume, false, false);

            player.ThrowPlayCardEvent(card);

            randomCard.transform.DOMove(Vector3.up * 2.5f, 0.6f).OnComplete(
                () =>
                {
                    // GameClient.Get<ITimerManager>().AddTimer(DestroyRandomCard, new object[] { randomCard }, 1f, false);
                    // randomCard.GetComponent<Animator>().SetTrigger("RemoveCard");
                    randomCard.transform.Find("RemoveCardParticle").GetComponent<ParticleSystem>().Play();

                    randomCard.transform.DOScale(Vector3.one * 1.2f, 0.6f).OnComplete(
                        () =>
                        {
                            RemoveOpponentCard(new object[] { randomCard });

                            _timerManager.AddTimer(
                                x =>
                                {
                                    completePlayCardCallback?.Invoke(card, target);
                                },
                                null,
                                0.1f);
                            _ranksController.UpdateRanksByElements(player.BoardCards, card.LibraryCard);
                            _timerManager.AddTimer(
                                x =>
                                {
                                    player.GraveyardCardsCount++;
                                },
                                null,
                                1f);
                        });
                });

            randomCard.transform.DORotate(Vector3.zero, 0.5f);

            _battlegroundController.UpdatePositionOfCardsInOpponentHand(true);
        }

        public void DrawCardInfo(WorkingCard card)
        {
            GameObject go = null;
            BoardCard boardCard = null;
            if (card.LibraryCard.CardKind == Enumerators.CardKind.Creature)
            {
                go = Object.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Cards/CreatureCard"));
                boardCard = new UnitBoardCard(go);
            }
            else if (card.LibraryCard.CardKind == Enumerators.CardKind.Spell)
            {
                go = Object.Instantiate(_loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/Gameplay/Cards/SpellCard"));
                boardCard = new SpellBoardCard(go);
            }

            boardCard.Init(card);
            go.transform.position = new Vector3(-6, 0, 0);
            go.transform.localScale = Vector3.one * .3f;
            boardCard.SetHighlightingEnabled(false);

            Object.Destroy(go, 2f);
        }

        public void ReturnToHandBoardUnit(WorkingCard workingCard, Player player, Vector3 cardPosition)
        {
            if (CheckIsMoreThanMaxCards(workingCard, player))
                return;

            GameObject cardObject = player.AddCardToHand(workingCard, true);
            cardObject.transform.position = cardPosition;

            if (player.IsLocalPlayer)
            {
                cardObject.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f); // size of the cards in hand         
            }
        }

        public void LowGooCostOfCardInHand(Player player, WorkingCard card = null, int value = 1)
        {
            if ((card == null) && (player.CardsInHand.Count > 0))
            {
                card = player.CardsInHand[Random.Range(0, player.CardsInHand.Count)];
            }

            if (card == null)
                return;

            if (player.IsLocalPlayer)
            {
                BoardCard boardCard = _battlegroundController.PlayerHandCards.Find(x => x.WorkingCard.Equals(card));

                boardCard.ChangeCardCostOn(value, true);
            }
            else
            {
                card.RealCost = Mathf.Clamp(card.LibraryCard.Cost - value, 0, card.LibraryCard.Cost);
            }
        }

        public void SetGooCostOfCardInHand(Player player, WorkingCard card, int value, BoardCard boardCard = null)
        {
            if (player.IsLocalPlayer)
            {
                if (boardCard == null)
                {
                    boardCard = _battlegroundController.PlayerHandCards.Find(x => x.WorkingCard.Equals(card));
                }

                boardCard.SetCardCost(value);
            }
            else
            {
                card.RealCost = Mathf.Clamp(value, 0, 99);
            }
        }

        public string GetSetOfCard(Card card)
        {
            CardSet set = _dataManager.CachedCardsLibraryData.Sets.Find(x => x.Cards.Find(y => y.Name.Equals(card.Name)) != null);

            if (set != null)
            {
                return set.Name;
            }

            return string.Empty;
        }

        public void CreateNewCardByNameAndAddToHand(Player player, string name)
        {
            float animationDuration = 1.5f;

            Card card = _dataManager.CachedCardsLibraryData.GetCardFromName(name).Clone();
            WorkingCard workingCard = new WorkingCard(card, player);

            if (CheckIsMoreThanMaxCards(workingCard, player))
                return;

            if (player.IsLocalPlayer)
            {
                BoardCard boardCard = CreateBoardCard(workingCard);

                boardCard.Transform.position = Vector3.zero;
                boardCard.Transform.localScale = Vector3.zero;

                boardCard.Transform.DOScale(Vector3.one * .3f, animationDuration);

                _timerManager.AddTimer(
                    x =>
                    {
                        _battlegroundController.PlayerHandCards.Add(boardCard);

                        player.CardsInHand.Add(workingCard);

                        _battlegroundController.UpdatePositionOfCardsInPlayerHand(true);
                    },
                    null,
                    animationDuration);
            }
            else
            {
                GameObject boardCard = AddCardToOpponentHand(workingCard);
                boardCard.transform.position = Vector3.zero;
                boardCard.transform.localScale = Vector3.zero;

                boardCard.transform.DOScale(Vector3.one, animationDuration);

                _timerManager.AddTimer(
                    x =>
                    {
                        player.CardsInHand.Add(workingCard);
                        _battlegroundController.UpdatePositionOfCardsInOpponentHand(true, false);
                    },
                    null,
                    animationDuration);
            }
        }

        public BoardCard GetBoardCard(WorkingCard card)
        {
            return CreateBoardCard(card);
        }

        public GameObject GetOpponentBoardCard(WorkingCard card)
        {
            return CreateOpponentBoardCard();
        }

        public void ReturnCardToHand(Player player, BoardUnit unit)
        {
            Player unitOwner = unit.OwnerPlayer;
            WorkingCard returningCard = unit.Card;

            returningCard.InitialCost = returningCard.LibraryCard.Cost;
            returningCard.RealCost = returningCard.InitialCost;

            Vector3 unitPosition = unit.Transform.position;

            _timerManager.AddTimer(
                x =>
                {
                    // STEP 1 - REMOVE UNIT FROM BOARD
                    unitOwner.BoardCards.Remove(unit);

                    // STEP 2 - DESTROY UNIT ON THE BOARD OR ANIMATE
                    unit.Die(true);
                    Object.Destroy(unit.GameObject);

                    // STEP 3 - REMOVE WORKING CARD FROM BOARD
                    unitOwner.RemoveCardFromBoard(returningCard);

                    // STEP 4 - RETURN CARD TO HAND
                    ReturnToHandBoardUnit(returningCard, unitOwner, unitPosition);

                    // STEP 4 - REARRANGE HANDS
                    _gameplayManager.RearrangeHands();
                },
                null,
                2f);
        }

        private void OnGameEndedEventHandler(Enumerators.EndGameType obj)
        {
            CardDistribution = false;
        }

        private void OnGameStartedEventHandler()
        {
            _cardInstanceId = 0;

            _playerBoard = GameObject.Find("PlayerBoard");
            _opponentBoard = GameObject.Find("OpponentBoard");
        }

        private void DirectlyEndCardDistribution(object[] param)
        {
            EndCardDistribution();
        }

        private BoardCard CreateBoardCard(WorkingCard card)
        {
            string cardSetName = GetSetOfCard(card.LibraryCard);
            GameObject go = null;
            BoardCard boardCard = null;
            if (card.LibraryCard.CardKind == Enumerators.CardKind.Creature)
            {
                go = Object.Instantiate(CreatureCardViewPrefab);
                boardCard = new UnitBoardCard(go);
            }
            else if (card.LibraryCard.CardKind == Enumerators.CardKind.Spell)
            {
                go = Object.Instantiate(SpellCardViewPrefab);
                boardCard = new SpellBoardCard(go);
            }

            boardCard.Init(card);
            boardCard.CurrentTurn = _battlegroundController.CurrentTurn;

            HandBoardCard handCard = new HandBoardCard(go, boardCard);
            handCard.OwnerPlayer = card.Owner;
            handCard.BoardZone = _playerBoard;
            boardCard.HandBoardCard = handCard;
            handCard.CheckStatusOfHighlight();
            boardCard.Transform.localScale = Vector3.one * .3f;

            _abilitiesController.CallAbilitiesInHand(boardCard, card);

            return boardCard;
        }

        private void CallCardPlay(BoardCard card)
        {
        }

        private void CallSpellCardPlay(BoardCard card)
        {
        }

        private bool CheckIsMoreThanMaxCards(WorkingCard workingCard, Player player)
        {
            if (player.CardsInHand.Count >= Constants.MaxCardsInHand)
            {
                // IMPROVE ANIMATION
                return true;
            }

            return false;
        }
    }
}
