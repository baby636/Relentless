﻿// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using DG.Tweening;
using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using LoomNetwork.CZB.Gameplay;

namespace LoomNetwork.CZB
{
    public class BattlegroundController : IController
    {
        public event Action<int> OnPlayerGraveyardUpdatedEvent;
        public event Action<int> OnOpponentGraveyardUpdatedEvent;
        public event Action OnTurnStartedEvent;
        public event Action OnTurnEndeddEvent;

        private IGameplayManager _gameplayManager;
        private ITimerManager _timerManager;
        private ISoundManager _soundManager;
        private IDataManager _dataManager;
        private ITutorialManager _tutorialManager;
        private IUIManager _uiManager;
        private ILoadObjectsManager _loadObjectsManager;
        private IPlayerManager _playerManager;


        private CardsController _cardsController;
        private PlayerController _playerController;
        private AIController _aiController;
        private RanksController _ranksController;

        private bool _battleDynamic = false;

        private List<BoardUnit> _cardsInDestroy;

        //  public int TurnDuration { get; private set; }
        public int currentTurn;
        public bool gameFinished;
        public bool cardsZoomed = false;


        public GameObject currentBoardCard;
        public int currentPreviewedCardId;


        public bool _rearrangingBottomBoard,
                    _rearrangingTopBoard,
                    isPreviewActive;

        public GameObject playerBoardObject,
                           opponentBoardObject,
                           playerGraveyardObject,
                           opponentGraveyardObject;

        public Coroutine createPreviewCoroutine;

        public List<BoardCard> playerHandCards = new List<BoardCard>();
        public List<GameObject> opponentHandCards = new List<GameObject>();

        public List<BoardUnit> playerBoardCards = new List<BoardUnit>();
        public List<BoardUnit> opponentBoardCards = new List<BoardUnit>();

        public List<BoardUnit> playerGraveyardCards = new List<BoardUnit>();
        public List<BoardUnit> opponentGraveyardCards = new List<BoardUnit>();


        public void Init()
        {
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _timerManager = GameClient.Get<ITimerManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _dataManager = GameClient.Get<IDataManager>();
            _tutorialManager = GameClient.Get<ITutorialManager>();
            _uiManager = GameClient.Get<IUIManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _playerManager = GameClient.Get<IPlayerManager>();

            _playerController = _gameplayManager.GetController<PlayerController>();
            _cardsController = _gameplayManager.GetController<CardsController>();
            _aiController = _gameplayManager.GetController<AIController>();

            _ranksController = _gameplayManager.GetController<RanksController>();

            _cardsInDestroy = new List<BoardUnit>();

            LoadGameConfiguration();

            _gameplayManager.OnGameEndedEvent += OnGameEndedEventHandler;

        }

        public void Dispose()
        {
        }

        public void Update()
        {
            if (_gameplayManager.GameStarted && !_gameplayManager.GameEnded)
            {
                CheckGameDynamic();

                foreach (var item in playerBoardCards)
                    item.Update();

                foreach (var item in opponentBoardCards)
                    item.Update();
            }
        }

        public void ResetAll()
        {
            if (createPreviewCoroutine != null)
                MainApp.Instance.StopCoroutine(createPreviewCoroutine);

            createPreviewCoroutine = null;

            if (currentBoardCard != null && currentBoardCard)
                MonoBehaviour.Destroy(currentBoardCard);
            currentBoardCard = null;

            ClearBattleground();
        }

        private void LoadGameConfiguration()
        {
            // TurnDuration = Constants.DEFAULT_TURN_DURATION;

            //  if (_gameplayManager.IsTutorial)
            //     TurnDuration = 10000000;
        }

        public void KillBoardCard(BoardUnit cardToDestroy)
        {
            if (cardToDestroy == null)
                return;

            bool isOpponentCard = cardToDestroy.ownerPlayer == _gameplayManager.CurrentPlayer ? false : true;

            cardToDestroy.transform.position = new Vector3(cardToDestroy.transform.position.x, cardToDestroy.transform.position.y, cardToDestroy.transform.position.z + 0.2f);

            _timerManager.AddTimer((x) =>
            {
                cardToDestroy.transform.DOShakePosition(.7f, 0.25f, 10, 90, false, false);

                string cardDeathSoundName = cardToDestroy.Card.libraryCard.name.ToLower() + "_" + Constants.CARD_SOUND_DEATH;
                float soundLength = 0f;

                if (!cardToDestroy.ownerPlayer.Equals(_gameplayManager.CurrentTurnPlayer))
                {
                    _soundManager.PlaySound(Enumerators.SoundType.CARDS, cardDeathSoundName, Constants.ZOMBIES_SOUND_VOLUME, Enumerators.CardSoundType.DEATH);
                    soundLength = _soundManager.GetSoundLength(Enumerators.SoundType.CARDS, cardDeathSoundName);
                }

                _timerManager.AddTimer((t) =>
                {
                    cardToDestroy.ownerPlayer.BoardCards.Remove(cardToDestroy);
                    cardToDestroy.ownerPlayer.RemoveCardFromBoard(cardToDestroy.Card);
                    cardToDestroy.ownerPlayer.AddCardToGraveyard(cardToDestroy.Card);

                    //_ranksController.UpdateRanksBuffs(cardToDestroy.ownerPlayer);

                    cardToDestroy.ThrowOnDieEvent();
                    cardToDestroy.transform.DOKill();
                    MonoBehaviour.Destroy(cardToDestroy.gameObject);

                    _timerManager.AddTimer((f) =>
                    {
                        UpdatePositionOfBoardUnitsOfOpponent();
                        UpdatePositionOfBoardUnitsOfPlayer();
                    }, null, Time.deltaTime, false);

                }, null, soundLength);

            }, null, 1f);
        }

        public void CheckGameDynamic()
        {
            //if (_gameplayManager.OpponentPlayer.HP > 9 && _gameplayManager.CurrentPlayer.HP > 9)
            //{
            //    if (_battleDynamic)
            //        _soundManager.CrossfaidSound(Enumerators.SoundType.BACKGROUND, null, true);
            //    _battleDynamic = false;
            //}
            //else
            //{
            if (!_battleDynamic)
                _soundManager.CrossfaidSound(Enumerators.SoundType.BATTLEGROUND, null, true);
            _battleDynamic = true;
            //  }
        }


        public void UpdateGraveyard(int index, Player player)
        {
            if (player.IsLocalPlayer)
                OnPlayerGraveyardUpdatedEvent?.Invoke(index);
            else
                OnOpponentGraveyardUpdatedEvent?.Invoke(index);
        }

        public void ClearBattleground()
        {
            playerHandCards.Clear();
            opponentHandCards.Clear();

            playerBoardCards.Clear();
            opponentBoardCards.Clear();

            playerGraveyardCards.Clear();
            opponentGraveyardCards.Clear();

            _cardsInDestroy.Clear();
        }

        public void InitializeBattleground()
        {
            currentTurn = Constants.FIRST_GAME_TURN_INDEX;

            gameFinished = false;

            //_timerManager.StopTimer(RunTurnAsync);


            if (_gameplayManager.IsTutorial)
            {
                _gameplayManager.OpponentPlayer.HP = 12;
                _gameplayManager.OpponentPlayer.GooOnCurrentTurn = 10;
                _gameplayManager.OpponentPlayer.Goo = 10;
                _gameplayManager.CurrentPlayer.GooOnCurrentTurn = 7;
                _gameplayManager.CurrentPlayer.Goo = 7;
            }



            if (Constants.DEV_MODE)
                _gameplayManager.OpponentPlayer.HP = 99;

            if (Constants.DEV_MODE)
            {
                _gameplayManager.CurrentPlayer.HP = 99;
            }

            _playerManager.OpponentGraveyardCards = opponentGraveyardCards;

            playerBoardObject = GameObject.Find("PlayerBoard");
            opponentBoardObject = GameObject.Find("OpponentBoard");
            playerGraveyardObject = GameObject.Find("GraveyardPlayer");
            opponentGraveyardObject = GameObject.Find("GraveyardOpponent");
        }

        public void StartGameplayTurns()
        {
            StartTurn();

            if (!_gameplayManager.IsTutorial)
            {
                var player = _gameplayManager.CurrentTurnPlayer.IsLocalPlayer ? _gameplayManager.OpponentPlayer : _gameplayManager.CurrentPlayer;
                _cardsController.AddCardToHand(player);
            }

            //    if (!_gameplayManager.IsTutorial)
            //     _timerManager.AddTimer(RunTurnAsync, null, TurnDuration, true, false);
        }

        public void OnGameEndedEventHandler(Enumerators.EndGameType endGameType)
        {
            // _timerManager.StopTimer(RunTurnAsync);

            gameFinished = true;
            currentTurn = 0;

            ClearBattleground();
        }

        /* private void RunTurnAsync(object[] param)
         {
             EndTurn();

             if (!gameFinished)
                 StartTurn();
             else
                 _timerManager.StopTimer(RunTurnAsync);
         } */

        public void StartTurn()
        {
            if (_gameplayManager.GameEnded)
                return;

            currentTurn++;

            _gameplayManager.CurrentTurnPlayer.turn++;

            if (_dataManager.CachedUserLocalData.tutorial && !_tutorialManager.IsTutorial)
                _tutorialManager.StartTutorial();

            _uiManager.GetPage<GameplayPage>().SetEndTurnButtonStatus(_gameplayManager.IsLocalPlayerTurn());

            UpdatePositionOfCardsInOpponentHand();
            //RearrangeOpponentHand(!_gameplayManager.IsLocalPlayerTurn(), true);

            _playerController.IsActive = _gameplayManager.IsLocalPlayerTurn();

            if (_gameplayManager.IsLocalPlayerTurn())
            {

                List<BoardUnit> creatures = new List<BoardUnit>();

                foreach (var card in playerBoardCards)
                {
                    if (_playerController == null || !card.gameObject)
                    {
                        creatures.Add(card);
                        continue;
                    }

                    card.OnStartTurn();
                }

                foreach (var item in creatures)
                    playerBoardCards.Remove(item);
                creatures.Clear();
                creatures = null;

                foreach (var card in playerBoardCards)
                {
                    card.SetHighlightingEnabled(true);
                }

                _uiManager.DrawPopup<YourTurnPopup>();
            }
            else
            {
                foreach (var card in opponentBoardCards)
                    card.OnStartTurn();

                foreach (var card in playerHandCards)
                    card.SetHighlightingEnabled(false);

                foreach (var card in playerBoardCards)
                    card.SetHighlightingEnabled(false);
            }

            _gameplayManager.CurrentPlayer.CallOnStartTurnEvent();
            _gameplayManager.OpponentPlayer.CallOnStartTurnEvent();

            _playerController.UpdateHandCardsHighlight();

            OnTurnStartedEvent?.Invoke();
        }

        public void EndTurn()
        {
            if (_gameplayManager.GameEnded)
                return;

            if (_gameplayManager.IsLocalPlayerTurn())
            {
                _uiManager.GetPage<GameplayPage>().SetEndTurnButtonStatus(false);

                foreach (var card in playerBoardCards)
                    card.OnEndTurn();

            }
            else
            {
                foreach (var card in opponentBoardCards)
                    card.OnEndTurn();
            }

            _gameplayManager.CurrentPlayer.CallOnEndTurnEvent();
            _gameplayManager.OpponentPlayer.CallOnEndTurnEvent();

            _gameplayManager.CurrentTurnPlayer = _gameplayManager.IsLocalPlayerTurn() ? _gameplayManager.OpponentPlayer : _gameplayManager.CurrentPlayer;

            _tutorialManager.ReportAction(Enumerators.TutorialReportAction.END_TURN);

            OnTurnEndeddEvent?.Invoke();
        }

        public void StopTurn()
        {
            //   _timerManager.StopTimer(RunTurnAsync);

            EndTurn();
            StartTurn();

            //  if (!_gameplayManager.IsTutorial)
            //     _timerManager.AddTimer(RunTurnAsync, null, TurnDuration, true, false);
        }

        public void RemovePlayerCardFromBoardToGraveyard(WorkingCard card)
        {
            var graveyardPos = playerGraveyardObject.transform.position + new Vector3(0.0f, -0.2f, 0.0f);
            var boardCard = playerBoardCards.Find(x => x.Card == card);
            if (boardCard != null)
            {

                boardCard.transform.localPosition = new Vector3(boardCard.transform.localPosition.x, boardCard.transform.localPosition.y, -0.2f);

                playerBoardCards.Remove(boardCard);
                playerGraveyardCards.Add(boardCard);

                boardCard.SetHighlightingEnabled(false);
                boardCard.StopSleepingParticles();
                boardCard.gameObject.GetComponent<SortingGroup>().sortingLayerName = Constants.LAYER_BOARD_CARDS;

                MonoBehaviour.Destroy(boardCard.gameObject.GetComponent<BoxCollider2D>());
            }
        }

        public void RemoveOpponentCardFromBoardToGraveyard(WorkingCard card)
        {
            var graveyardPos = opponentGraveyardObject.transform.position + new Vector3(0.0f, -0.2f, 0.0f);
            var boardCard = opponentBoardCards.Find(x => x.Card == card);
            if (boardCard != null)
            {

                boardCard.transform.localPosition = new Vector3(boardCard.transform.localPosition.x, boardCard.transform.localPosition.y, -0.2f);

                opponentBoardCards.Remove(boardCard);

                boardCard.SetHighlightingEnabled(false);
                boardCard.StopSleepingParticles();
                boardCard.gameObject.GetComponent<SortingGroup>().sortingLayerName = Constants.LAYER_BOARD_CARDS;
                MonoBehaviour.Destroy(boardCard.gameObject.GetComponent<BoxCollider2D>());
            }
            else if (_aiController.currentSpellCard != null && card == _aiController.currentSpellCard.WorkingCard)
            {
                _aiController.currentSpellCard.SetHighlightingEnabled(false);
                _aiController.currentSpellCard.gameObject.GetComponent<SortingGroup>().sortingLayerName = Constants.LAYER_BOARD_CARDS;
                MonoBehaviour.Destroy(_aiController.currentSpellCard.gameObject.GetComponent<BoxCollider2D>());
                var sequence = DOTween.Sequence();
                sequence.PrependInterval(2.0f);
                sequence.Append(_aiController.currentSpellCard.transform.DOMove(graveyardPos, 0.5f));
                sequence.Append(_aiController.currentSpellCard.transform.DOScale(new Vector2(0.6f, 0.6f), 0.5f));
                sequence.OnComplete(() =>
                {
                    _aiController.currentSpellCard = null;
                });
            }
        }

        public void UpdatePositionOfBoardUnitsOfPlayer(Action onComplete = null)
        {
            if (_rearrangingBottomBoard)
            {
                _timerManager.AddTimer((x) =>
                {
                    UpdatePositionOfBoardUnitsOfPlayer(onComplete);
                }, null, .3f, false);
                return;
            }


            if (_gameplayManager.GameEnded)
                return;

            var playerBoardCards = _gameplayManager.CurrentPlayer.BoardCards;

            _rearrangingBottomBoard = true;

            var boardWidth = 0.0f;
            var spacing = 0.2f; // -0.2
            var cardWidth = 0.0f;
            foreach (var card in playerBoardCards)
            {
                cardWidth = 2.5f;
                boardWidth += cardWidth;
                boardWidth += spacing;
            }
            boardWidth -= spacing;

            var newPositions = new List<Vector2>(playerBoardCards.Count);
            var pivot = playerBoardObject.transform.position;

            for (var i = 0; i < playerBoardCards.Count; i++)
            {
                var card = playerBoardCards[i];
                newPositions.Add(new Vector2(pivot.x - boardWidth / 2 + cardWidth / 2, pivot.y - 1.7f));
                pivot.x += boardWidth / playerBoardCards.Count;
            }

            var sequence = DOTween.Sequence();
            for (var i = 0; i < playerBoardCards.Count; i++)
            {
                var card = playerBoardCards[i];
                sequence.Insert(0, card.transform.DOMove(newPositions[i], 0.4f).SetEase(Ease.OutSine));
            }
            sequence.OnComplete(() =>
            {
                if (onComplete != null)
                {
                    onComplete();
                }
            });

            _timerManager.AddTimer((x) =>
            {
                _rearrangingBottomBoard = false;
            }, null, 1.5f, false);
        }


        public void UpdatePositionOfBoardUnitsOfOpponent(Action onComplete = null)
        {
            if (_rearrangingTopBoard)
            {
                _timerManager.AddTimer((x) =>
                {
                    UpdatePositionOfBoardUnitsOfOpponent(onComplete);
                }, null, .1f, false);

                return;
            }

            if (_gameplayManager.GameEnded)
                return;

            var opponentBoardCards = _gameplayManager.OpponentPlayer.BoardCards;

            _rearrangingTopBoard = true;

            var boardWidth = 0.0f;
            var spacing = 0.2f;
            var cardWidth = 0.0f;

            foreach (var card in opponentBoardCards)
            {
                cardWidth = 2.5f;
                boardWidth += cardWidth;
                boardWidth += spacing;
            }
            boardWidth -= spacing;

            var newPositions = new List<Vector2>(opponentBoardCards.Count);
            var pivot = opponentBoardObject.transform.position;

            for (var i = 0; i < opponentBoardCards.Count; i++)
            {
                var card = opponentBoardCards[i];
                newPositions.Add(new Vector2(pivot.x - boardWidth / 2 + cardWidth / 2, pivot.y + 0.0f));
                pivot.x += boardWidth / opponentBoardCards.Count;
            }

            var sequence = DOTween.Sequence();
            for (var i = 0; i < opponentBoardCards.Count; i++)
            {
                var card = opponentBoardCards[i];
                sequence.Insert(0, card.transform.DOMove(newPositions[i], 0.4f).SetEase(Ease.OutSine));
            }
            sequence.OnComplete(() =>
            {
                if (onComplete != null)
                {
                    onComplete();
                }
            });

            _timerManager.AddTimer((x) =>
            {
                _rearrangingTopBoard = false;
            }, null, 1.5f, false);
        }

        // rewrite
        public void CreateCardPreview(object target, Vector3 pos, bool highlight = true)
        {
            isPreviewActive = true;

            if (target is BoardCard)
            {
                currentPreviewedCardId = (target as BoardCard).WorkingCard.instanceId;

            }
            else if (target is BoardUnit)
            {
                currentPreviewedCardId = (target as BoardUnit).Card.instanceId;
            }


            createPreviewCoroutine = MainApp.Instance.StartCoroutine(CreateCardPreviewAsync(target, pos, highlight));
        }

        // rewrite
        public IEnumerator CreateCardPreviewAsync(object target, Vector3 pos, bool highlight)
        {
            yield return new WaitForSeconds(0.3f);

            WorkingCard card = null;

            if (target is BoardCard)
            {
                card = (target as BoardCard).WorkingCard;
            }
            else if (target is BoardUnit)
            {
                card = (target as BoardUnit).Card;
            }

            string cardSetName = _cardsController.GetSetOfCard(card.libraryCard);

            BoardCard boardCard = null;
            if (card.libraryCard.cardKind == Enumerators.CardKind.CREATURE)
            {
                currentBoardCard = MonoBehaviour.Instantiate(_cardsController.creatureCardViewPrefab);
                boardCard = new UnitBoardCard(currentBoardCard);
            }
            else if (card.libraryCard.cardKind == Enumerators.CardKind.SPELL)
            {
                currentBoardCard = MonoBehaviour.Instantiate(_cardsController.spellCardViewPrefab);
                boardCard = new SpellBoardCard(currentBoardCard);

            }
            boardCard.Init(card);
            if (highlight)
                highlight = boardCard.CanBePlayed(card.owner) && boardCard.CanBeBuyed(card.owner);
            boardCard.SetHighlightingEnabled(highlight);
            boardCard.isPreview = true;

            InternalTools.SetLayerRecursively(boardCard.gameObject, 0);

            if (target is BoardUnit)
                boardCard.DrawTooltipInfoOfUnit(target as BoardUnit);
            else if (target is BoardCard)
                boardCard.DrawTooltipInfoOfCard(target as BoardCard);

            var newPos = pos;
            newPos.y += 2.0f;
            currentBoardCard.transform.position = newPos;
            currentBoardCard.transform.localRotation = Quaternion.Euler(Vector3.zero);


            Vector3 sizeOfCard = Vector3.one;

            if (!InternalTools.IsTabletScreen())
                sizeOfCard = new Vector3(.8f, .8f, .8f);
            else
                sizeOfCard = new Vector3(.4f, .4f, .4f);


            currentBoardCard.transform.localScale = sizeOfCard;

            currentBoardCard.GetComponent<SortingGroup>().sortingOrder = 1000;
            currentBoardCard.layer = LayerMask.NameToLayer("Default");
            currentBoardCard.transform.DOMoveY(newPos.y + 1.0f, 0.1f);
        }

        // rewrite
        public void DestroyCardPreview()
        {
            if (!isPreviewActive)
                return;

            GameClient.Get<ICameraManager>().FadeOut(null, 1, true);

            MainApp.Instance.StartCoroutine(DestroyCardPreviewAsync());
            if (createPreviewCoroutine != null)
            {
                MainApp.Instance.StopCoroutine(createPreviewCoroutine);
            }
            isPreviewActive = false;
        }
        // rewrite
        public IEnumerator DestroyCardPreviewAsync()
        {
            if (currentBoardCard != null)
            {
                var oldCardPreview = currentBoardCard;
                foreach (var renderer in oldCardPreview.GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.DOFade(0.0f, 0.2f);
                }
                foreach (var text in oldCardPreview.GetComponentsInChildren<TextMeshPro>())
                {
                    text.DOFade(0.0f, 0.2f);
                }
                yield return new WaitForSeconds(0.5f);
                MonoBehaviour.Destroy(oldCardPreview.gameObject);
            }
        }

        public void UpdatePositionOfCardsInPlayerHand(bool isMove = false)
        {
            var handWidth = 0.0f;
            var spacing = -1.5f;
            var scaling = 0.25f;
            var pivot = new Vector3(6f, -7.5f, 0f);
            var twistPerCard = -5;


            if (cardsZoomed)
            {
                spacing = -2.6f;
                scaling = 0.31f;
                pivot = new Vector3(-1.3f, -6.5f, 0f);
                twistPerCard = -3;
            }

            foreach (var card in playerHandCards)
                handWidth += spacing;
            handWidth -= spacing;


            if (playerHandCards.Count == 1)
                twistPerCard = 0;

            var totalTwist = twistPerCard * playerHandCards.Count;
            float startTwist = ((totalTwist - twistPerCard) / 2f);
            var scalingFactor = 0.04f;
            Vector3 moveToPosition = Vector3.zero;

            for (var i = 0; i < playerHandCards.Count; i++)
            {
                var card = playerHandCards[i];
                var twist = startTwist - (i * twistPerCard);
                var nudge = Mathf.Abs(twist);

                nudge *= scalingFactor;
                moveToPosition = new Vector3(pivot.x - handWidth / 2, pivot.y - nudge, (playerHandCards.Count - i) * 0.1f);

                if (isMove)
                    card.isNewCard = false;

                card.UpdateCardPositionInHand(moveToPosition, Vector3.forward * twist, Vector3.one * scaling);

                pivot.x += handWidth / playerHandCards.Count;

                card.gameObject.GetComponent<SortingGroup>().sortingLayerName = Constants.LAYER_HAND_CARDS;
                card.gameObject.GetComponent<SortingGroup>().sortingOrder = i;
            }
        }

        public void UpdatePositionOfCardsInOpponentHand(bool isMove = false, bool isNewCard = false)
        {
            var handWidth = 0.0f;
            var spacing = -1.0f;

            foreach (var card in opponentHandCards)
                handWidth += spacing;
            handWidth -= spacing;

            var pivot = new Vector3(-3.2f, 8.5f, 0f);
            var twistPerCard = 5;

            if (opponentHandCards.Count == 1)
                twistPerCard = 0;

            var totalTwist = twistPerCard * opponentHandCards.Count;
            float startTwist = ((totalTwist - twistPerCard) / 2f);
            var scalingFactor = 0.04f;
            Vector3 movePosition = Vector3.zero;
            Vector3 rotatePosition = Vector3.zero;

            for (var i = 0; i < opponentHandCards.Count; i++)
            {
                var card = opponentHandCards[i];
                var twist = startTwist - (i * twistPerCard);
                var nudge = Mathf.Abs(twist);

                nudge *= scalingFactor;
                movePosition = new Vector2(pivot.x - handWidth / 2, pivot.y);
                rotatePosition = new Vector3(0, 0, twist);

                if (isMove)
                {
                    if (i == opponentHandCards.Count - 1 && isNewCard)
                    {
                        card.transform.position = new Vector3(-8.2f, 5.7f, 0);
                        card.transform.eulerAngles = Vector3.forward * 90f;
                    }

                    card.transform.DOMove(movePosition, 0.5f);
                    card.transform.DORotate(rotatePosition, 0.5f);
                }
                else
                {
                    card.transform.position = movePosition;
                    card.transform.rotation = Quaternion.Euler(rotatePosition);
                }

                pivot.x += handWidth / opponentHandCards.Count;

                card.GetComponent<SortingGroup>().sortingOrder = i;
            }
        }

        public BoardUnit GetBoardUnitFromHisObject(GameObject unitObject)
        {
            var unit = playerBoardCards.Find(x => x.gameObject.Equals(unitObject));

            if (unit == null)
                unit = opponentBoardCards.Find(x => x.gameObject.Equals(unitObject));

            return unit;
        }

        public BoardCard GetBoardCardFromHisObject(GameObject cardObject)
        {
            var card = playerHandCards.Find(x => x.gameObject.Equals(cardObject));

            return card;
        }

        public void DestroyBoardUnit(BoardUnit unit)
        {
            if (unit != null)
                unit.Die();
        }

        public void TakeControlUnit(Player to, BoardUnit unit)
        {
            // implement functionality of the take control
        }

        public BoardUnit CreateBoardUnit(Player owner, WorkingCard card)
        {
            GameObject _playerBoard = owner.IsLocalPlayer ? playerBoardObject : opponentBoardObject;

            var boardUnit = new BoardUnit(_playerBoard.transform);
            boardUnit.transform.tag = owner.IsLocalPlayer ? Constants.TAG_PLAYER_OWNED : Constants.TAG_OPPONENT_OWNED;
            boardUnit.transform.SetParent(_playerBoard.transform);
            boardUnit.transform.position = new Vector2(1.9f * owner.BoardCards.Count, 0);
            boardUnit.ownerPlayer = owner;
            boardUnit.SetObjectInfo(card);

            boardUnit.PlayArrivalAnimation();

            return boardUnit;
        }
    }
}