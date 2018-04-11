// Copyright (C) 2016-2017 David Pol. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.Rendering;

using DG.Tweening;
using TMPro;

using CCGKit;
using GrandDevs.CZB;
using GrandDevs.CZB.Common;

/// <summary>
/// The demo player is a subclass of the core HumanPlayer type which extends it with demo-specific
/// functionality. Most of which is straightforward updating of the user interface when receiving
/// new state from the server.
/// </summary>
public class DemoHumanPlayer : DemoPlayer
{
    public event Action OnEndTurnEvent;
    public event Action OnStartTurnEvent;

    [SerializeField]
    private GameObject creatureCardViewPrefab;

    [SerializeField]
    private GameObject spellCardViewPrefab;

    [SerializeField]
    private GameObject opponentCardPrefab;

    [SerializeField]
    private GameObject boardCreaturePrefab;

    [SerializeField]
    private GameObject spellTargetingArrowPrefab;

    [SerializeField]
    private GameObject fightTargetingArrowPrefab;

    [SerializeField]
    private GameObject opponentTargetingArrowPrefab;

    protected List<CardView> playerHandCards = new List<CardView>();
    protected List<GameObject> opponentHandCards = new List<GameObject>();
    protected List<BoardCreature> playerBoardCards = new List<BoardCreature>();
    protected List<BoardCreature> opponentBoardCards = new List<BoardCreature>();
    protected List<BoardCreature> playerGraveyardCards = new List<BoardCreature>();
    protected List<BoardCreature> opponentGraveyardCards = new List<BoardCreature>();

    protected BoardCreature currentCreature;
    protected CardView currentSpellCard;

    protected GameUI gameUI;
    //protected PopupChat chatPopup;

    protected float accTime;
    protected float secsAccTime;

    public List<BoardCreature> opponentBoardCardsList
    {
        get { return opponentBoardCards; }
    }

    public List<BoardCreature> playerBoardCardsList
    {
        get { return playerBoardCards; }
    }

    public Stat lifeStat { get; protected set; }
    public Stat manaStat { get; protected set; }

    protected Stat opponentLifeStat { get; set; }
    protected Stat opponentManaStat { get; set; }

    public RuntimeZone deckZone;
    public RuntimeZone handZone;
    public RuntimeZone boardZone;
    public RuntimeZone graveyardZone;
    public RuntimeZone opponentDeckZone;
    public RuntimeZone opponentHandZone;
    public RuntimeZone opponentBoardZone;
    public RuntimeZone opponentGraveyardZone;

    public bool isCardSelected;
    protected GameObject currentCardPreview;
    protected bool isPreviewActive;
    protected int currentPreviewedCardId;
    protected Coroutine createPreviewCoroutine;

    protected AbilitiesController _abilitiesController;

    protected override void Awake()
    {
        base.Awake();

        Assert.IsNotNull(creatureCardViewPrefab);
        Assert.IsNotNull(spellCardViewPrefab);
        Assert.IsNotNull(opponentCardPrefab);
        Assert.IsNotNull(boardCreaturePrefab);
        Assert.IsNotNull(spellTargetingArrowPrefab);
        Assert.IsNotNull(fightTargetingArrowPrefab);
        Assert.IsNotNull(opponentTargetingArrowPrefab);

        isHuman = true;
    }

    protected override void Start()
    {
        base.Start();

        //chatPopup = GameObject.Find("PopupChat").GetComponent<PopupChat>();

        //changed by Basil
        GameClient.Get<IPlayerManager>().PlayerGraveyardCards = playerGraveyardCards;
        GameClient.Get<IPlayerManager>().OpponentGraveyardCards = opponentGraveyardCards;

        _abilitiesController = GameClient.Get<IGameplayManager>().GetController<AbilitiesController>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        gameUI = GameObject.Find("GameUI").GetComponent<GameUI>();
        Assert.IsNotNull(gameUI);

        foreach (var entry in playerInfo.stats)
        {
            if (entry.Value.name == "Life")
            {
                lifeStat = entry.Value;
            }
            else if (entry.Value.name == "Mana")
            {
                manaStat = entry.Value;
            }
        }
        foreach (var entry in opponentInfo.stats)
        {
            if (entry.Value.name == "Life")
            {
                opponentLifeStat = entry.Value;
            }
            else if (entry.Value.name == "Mana")
            {
                opponentManaStat = entry.Value;
            }
        }

        lifeStat.onValueChanged += (oldValue, newValue) =>
        {
            gameUI.SetPlayerHealth(lifeStat.effectiveValue);
        };
        manaStat.onValueChanged += (oldValue, newValue) =>
        {
            gameUI.SetPlayerMana(manaStat.effectiveValue);
            UpdateHandCardsHighlight();
        };

        opponentLifeStat.onValueChanged += (oldValue, newValue) =>
        {
            gameUI.SetOpponentHealth(opponentLifeStat.effectiveValue);
        };
        opponentManaStat.onValueChanged += (oldValue, newValue) =>
        {
            gameUI.SetOpponentMana(opponentManaStat.effectiveValue);
        };

        deckZone = playerInfo.namedZones["Deck"];
        deckZone.onZoneChanged += numCards =>
        {
            gameUI.SetPlayerDeckCards(numCards);
        };

        handZone = playerInfo.namedZones["Hand"];
        handZone.onZoneChanged += numCards =>
        {
            gameUI.SetPlayerHandCards(numCards);
        };
        handZone.onCardAdded += card =>
        {
            //Debug.Log("%%%%%");
            AddCardToHand(card);
            RearrangeHand();
        };
        handZone.onCardRemoved += card =>
        {
            var handCard = playerHandCards.Find(x => x.card == card);
            if (handCard != null)
            {
                playerHandCards.Remove(handCard);
                RearrangeHand();
            }
        };

        boardZone = playerInfo.namedZones["Board"];
        boardZone.onCardRemoved += card =>
        {
            //Changed by Basil to destroy card
            var graveyardPos = GameObject.Find("GraveyardPlayer").transform.position + new Vector3(0.0f, -0.2f, 0.0f);
            var boardCard = playerBoardCards.Find(x => x.card == card);
            if (boardCard != null)
            {
                playerGraveyardCards.Add(boardCard);
                playerBoardCards.Remove(boardCard);
                boardCard.transform.DOKill();
                //boardCard.transform.DOMove(graveyardPos, 0.7f);
                boardCard.SetHighlightingEnabled(false);
                boardCard.StopSleepingParticles();
                RearrangeBottomBoard();
                boardCard.GetComponent<SortingGroup>().sortingLayerName = "BoardCards";
                boardCard.GetComponent<SortingGroup>().sortingOrder = playerGraveyardCards.Count;
                Destroy(boardCard.GetComponent<BoxCollider2D>());
                GameClient.Get<IPlayerManager>().OnBoardCardKilled(card);
            }
            else if (currentSpellCard != null && card == currentSpellCard.card)
            {
                currentSpellCard.SetHighlightingEnabled(false);
                currentSpellCard.GetComponent<SortingGroup>().sortingLayerName = "BoardCards";
                currentSpellCard.GetComponent<SortingGroup>().sortingOrder = playerGraveyardCards.Count;
                Destroy(currentSpellCard.GetComponent<BoxCollider2D>());
                currentSpellCard.transform.DOMove(graveyardPos - Vector3.right * 5, 0.5f);
                currentSpellCard.transform.DOScale(new Vector2(0.6f, 0.6f), 0.5f);
                currentSpellCard.GetComponent<HandCard>().enabled = false;
                currentSpellCard = null;
            }
        };

        graveyardZone = playerInfo.namedZones["Graveyard"];
        graveyardZone.onZoneChanged += numCards =>
        {
            gameUI.SetPlayerGraveyardCards(numCards);
        };

        opponentDeckZone = opponentInfo.namedZones["Deck"];
        opponentDeckZone.onZoneChanged += numCards =>
        {
            gameUI.SetOpponentDeckCards(numCards);
        };

        opponentHandZone = opponentInfo.namedZones["Hand"];
        opponentHandZone.onZoneChanged += numCards =>
        {
            gameUI.SetOpponentHandCards(numCards);
        };
        opponentHandZone.onCardRemoved += card =>
        {
            var randomIndex = UnityEngine.Random.Range(0, opponentHandCards.Count);
            var randomCard = opponentHandCards[randomIndex];
            opponentHandCards.Remove(randomCard);
            Destroy(randomCard);
            RearrangeOpponentHand();
        };

        opponentBoardZone = opponentInfo.namedZones["Board"];
        opponentBoardZone.onCardRemoved += card =>
        {
            //Changed by Basil to destroy card
            var graveyardPos = GameObject.Find("GraveyardOpponent").transform.position + new Vector3(0.0f, -0.2f, 0.0f);
            var boardCard = opponentBoardCards.Find(x => x.card == card);
            if (boardCard != null)
            {
                opponentGraveyardCards.Add(boardCard);
                opponentBoardCards.Remove(boardCard);
                boardCard.transform.DOKill();
                //boardCard.transform.DOMove(graveyardPos, 0.7f);
                boardCard.SetHighlightingEnabled(false);
                boardCard.StopSleepingParticles();
                RearrangeTopBoard();
                boardCard.GetComponent<SortingGroup>().sortingLayerName = "BoardCards";
                boardCard.GetComponent<SortingGroup>().sortingOrder = opponentGraveyardCards.Count;
                Destroy(boardCard.GetComponent<BoxCollider2D>());
                GameClient.Get<IPlayerManager>().OnBoardCardKilled(card);
            }
            else if (currentSpellCard != null && card == currentSpellCard.card)
            {
                currentSpellCard.SetHighlightingEnabled(false);
                currentSpellCard.GetComponent<SortingGroup>().sortingLayerName = "BoardCards";
                currentSpellCard.GetComponent<SortingGroup>().sortingOrder = opponentGraveyardCards.Count;
                Destroy(currentSpellCard.GetComponent<BoxCollider2D>());
                var sequence = DOTween.Sequence();
                sequence.PrependInterval(2.0f);
                sequence.Append(currentSpellCard.transform.DOMove(graveyardPos, 0.5f));
                sequence.Append(currentSpellCard.transform.DOScale(new Vector2(0.6f, 0.6f), 0.5f));
                sequence.OnComplete(() =>
                {
                    currentSpellCard = null;
                });
            }
        };

        opponentGraveyardZone = opponentInfo.namedZones["Graveyard"];
        opponentGraveyardZone.onZoneChanged += numCards =>
        {
            gameUI.SetOpponentGraveyardCards(numCards);
        };
    }

    public override void OnStartGame(StartGameMessage msg)
    {
        base.OnStartGame(msg);

        GameObject.Find("Player/Avatar").GetComponent<PlayerAvatar>().playerInfo = playerInfo;
        GameObject.Find("Opponent/Avatar").GetComponent<PlayerAvatar>().playerInfo = opponentInfo;

        for (var i = 0; i < opponentHandZone.numCards; i++)
        {
            AddCardToOpponentHand();
        }

        RearrangeOpponentHand();

        // Update the UI as appropriate.
        gameUI.SetPlayerHealth(lifeStat.effectiveValue);
        gameUI.SetOpponentHealth(opponentLifeStat.effectiveValue);
        gameUI.SetPlayerMana(manaStat.effectiveValue);
        gameUI.SetOpponentMana(opponentManaStat.effectiveValue);

        gameUI.SetPlayerHandCards(handZone.cards.Count);
        gameUI.SetPlayerGraveyardCards(graveyardZone.numCards);
        gameUI.SetPlayerDeckCards(deckZone.numCards);
        gameUI.SetOpponentHandCards(opponentHandZone.numCards);
        gameUI.SetOpponentGraveyardCards(opponentGraveyardZone.numCards);
        gameUI.SetOpponentDeckCards(opponentDeckZone.numCards);

        // Set the player nicknames in the UI.
        for (var i = 0; i < msg.nicknames.Length; i++)
        {
            var nickname = msg.nicknames[i];
            if (i == msg.playerIndex)
            {
                gameUI.SetPlayerName(nickname);
            }
            else
            {
                gameUI.SetOpponentName(nickname);
            }
        }

        var gameScene = GameObject.Find("GameScene");
        if (gameScene != null)
        {
#if USING_MASTER_SERVER_KIT
            if (gameScene.GetComponent<MSK_GameScene>() != null)
            {
                gameScene.GetComponent<MSK_GameScene>().CloseWaitingWindow();
            }
#else
            if (gameScene.GetComponent<GameScene>() != null)
            {
                gameScene.GetComponent<GameScene>().CloseWaitingWindow();
            }
#endif
        }

        var endTurnButton = GameObject.Find("EndTurnButton");
        if (endTurnButton != null)
        {
            endTurnButton.GetComponent<EndTurnButton>().player = this;
        }

        GameClient.Get<IPlayerManager>().OnLocalPlayerSetUp();

        GameObject.Find("Player/Spell").GetComponent<BoardSkill>().ownerPlayer = this;
    }

    public override void OnStartTurn(StartTurnMessage msg)
    {
        base.OnStartTurn(msg);

        if (GameClient.Get<IDataManager>().CachedUserLocalData.tutorial && !GameClient.Get<ITutorialManager>().IsTutorial)
			GameClient.Get<ITutorialManager>().StartTutorial();

        gameUI.SetPlayerActive(msg.isRecipientTheActivePlayer);
        gameUI.SetOpponentActive(!msg.isRecipientTheActivePlayer);
        gameUI.SetEndTurnButtonEnabled(msg.isRecipientTheActivePlayer);

        foreach (var card in opponentHandCards)
        {
            Destroy(card);
        }
        opponentHandCards.Clear();
        for (var i = 0; i < opponentHandZone.numCards; i++)
        {
            AddCardToOpponentHand();
        }
        RearrangeOpponentHand();

        if (msg.isRecipientTheActivePlayer)
        {
            UpdateHandCardsHighlight();

            foreach (var card in playerBoardCards)
            {
                card.OnStartTurn();
            }

            var scene = GameObject.Find("GameScene").GetComponent<GameScene>();
            scene.OpenPopup<PopupTurnStart>("PopupTurnStart", null, false);

            gameUI.StartTurnCountdown(turnDuration);
        }
        else
        {
            foreach (var card in opponentBoardCards)
            {
                card.OnStartTurn();
            }

            foreach (var card in playerHandCards)
            {
                card.SetHighlightingEnabled(false);
            }
            foreach (var card in playerBoardCards)
            {
                card.SetHighlightingEnabled(false);
            }

            gameUI.HideTurnCountdown();
        }

        OnStartTurnEvent?.Invoke();
    }

    protected virtual void RearrangeHand()
    {
        var handWidth = 0.0f;
        var spacing = -1.0f;
        foreach (var card in playerHandCards)
        {
            handWidth += card.GetComponent<SpriteRenderer>().bounds.size.x;
            handWidth += spacing;
        }
        handWidth -= spacing;

        var pivot = Camera.main.ViewportToWorldPoint(new Vector3(0.54f, 0.05f, 0.0f)); // changed by Basil
        var totalTwist = -20f;
        if (playerHandCards.Count == 1)
        {
            totalTwist = 0;
        }
        var twistPerCard = totalTwist / playerHandCards.Count;
        float startTwist = -1f * (totalTwist / 2);
        var scalingFactor = 0.06f;
        for (var i = 0; i < playerHandCards.Count; i++)
        {
            var card = playerHandCards[i];
            var twist = startTwist + (i * twistPerCard);
            var nudge = Mathf.Abs(twist);
            nudge *= scalingFactor;
            card.transform.position = new Vector2(pivot.x - handWidth / 2, pivot.y - nudge);
            card.transform.rotation = Quaternion.Euler(0, 0, twist);
            pivot.x += handWidth / playerHandCards.Count;
            card.GetComponent<SortingGroup>().sortingLayerName = "HandCards";
            card.GetComponent<SortingGroup>().sortingOrder = i;
        }
    }

    protected virtual void RearrangeOpponentHand()
    {
        var handWidth = 0.0f;
        var spacing = -1.0f;
        foreach (var card in opponentHandCards)
        {
            handWidth += card.GetComponent<SpriteRenderer>().bounds.size.x;
            handWidth += spacing;
        }
        handWidth -= spacing;

        var pivot = Camera.main.ViewportToWorldPoint(new Vector3(0.53f, 0.99f, 0.0f)); // changed by Basil
        var totalTwist = -20f;
        if (opponentHandCards.Count == 1)
        {
            totalTwist = 0;
        }
        var twistPerCard = totalTwist / opponentHandCards.Count;
        float startTwist = -1f * (totalTwist / 2);
        var scalingFactor = 0.06f;
        for (var i = 0; i < opponentHandCards.Count; i++)
        {
            var card = opponentHandCards[i];
            var twist = startTwist + (i * twistPerCard);
            var nudge = Mathf.Abs(twist);
            nudge *= scalingFactor;
            card.transform.position = new Vector2(pivot.x - handWidth / 2, pivot.y + nudge);
            card.transform.rotation = Quaternion.Euler(0, 0, -twist);
            pivot.x += handWidth / opponentHandCards.Count;
            card.GetComponent<SortingGroup>().sortingOrder = i;
        }
    }

    protected virtual void RearrangeTopBoard(Action onComplete = null)
    {
        var boardWidth = 0.0f;
        var spacing = -0.2f;
        var cardWidth = 0.0f;
        foreach (var card in opponentBoardCards)
        {
            cardWidth = card.GetComponent<SpriteRenderer>().bounds.size.x;
            boardWidth += cardWidth;
            boardWidth += spacing;
        }
        boardWidth -= spacing;

        var newPositions = new List<Vector2>(opponentBoardCards.Count);
        var pivot = GameObject.Find("OpponentBoard").transform.position;
        for (var i = 0; i < opponentBoardCards.Count; i++)
        {
            var card = opponentBoardCards[i];
            newPositions.Add(new Vector2(pivot.x - boardWidth / 2 + cardWidth / 2, pivot.y - 0.2f));
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
    }

    public virtual void RearrangeBottomBoard(Action onComplete = null)
    {
        var boardWidth = 0.0f;
        var spacing = -0.2f;
        var cardWidth = 0.0f;
        foreach (var card in playerBoardCards)
        {
            cardWidth = card.GetComponent<SpriteRenderer>().bounds.size.x;
            boardWidth += cardWidth;
            boardWidth += spacing;
        }
        boardWidth -= spacing;

        var newPositions = new List<Vector2>(playerBoardCards.Count);
        var pivot = GameObject.Find("PlayerBoard").transform.position;
        for (var i = 0; i < playerBoardCards.Count; i++)
        {
            var card = playerBoardCards[i];
            newPositions.Add(new Vector2(pivot.x - boardWidth / 2 + cardWidth / 2, pivot.y - 1.4f));
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
    }

    public override void OnEndTurn(EndTurnMessage msg)
    {
        base.OnEndTurn(msg);

        if (msg.isRecipientTheActivePlayer)
        {
            gameUI.SetEndTurnButtonEnabled(false);

            foreach (var card in playerBoardCards)
            {
                card.OnEndTurn();
            }

            GameObject.Find("Player/Spell").GetComponent<BoardSkill>().OnEndTurn();

            if (currentCreature != null)
            {
                playerBoardCards.Remove(currentCreature);
                RearrangeBottomBoard();

                playerInfo.namedZones["Hand"].AddCard(currentCreature.card);
                playerInfo.namedZones["Board"].RemoveCard(currentCreature.card);

                Destroy(currentCreature.gameObject);
                currentCreature = null;
            }

            if (currentSpellCard != null)
            {
                Destroy(currentSpellCard.GetComponent<BoardSpell>());
                currentSpellCard = null;
                RearrangeHand();
            }
        }
        else
        {
            foreach (var card in opponentBoardCards)
            {
                card.OnEndTurn();
            }
        }

        if(isHuman)
            OnEndTurnEvent?.Invoke();
    }

    public override void StopTurn()
    {
        GameClient.Get<ITutorialManager>().ReportAction(Enumerators.TutorialReportAction.END_TURN);
		var msg = new StopTurnMessage();
        client.Send(NetworkProtocol.StopTurn, msg);
    }

    protected virtual void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (!gameStarted)
        {
            return;
        }

        //if (chatPopup.isVisible)
        //{
        //    return;
        //}

        if (GameClient.Get<ITutorialManager>().IsTutorial && (GameClient.Get<ITutorialManager>().CurrentStep != 1 && 
                                                              GameClient.Get<ITutorialManager>().CurrentStep != 13))
            return;


        var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            if (isActivePlayer && currentSpellCard == null)
            {
                var hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
                var hitCards = new List<GameObject>();
                foreach (var hit in hits)
                {
                    if (hit.collider != null &&
                        hit.collider.gameObject != null &&
                        hit.collider.gameObject.GetComponent<CardView>() != null &&
                        !hit.collider.gameObject.GetComponent<CardView>().isPreview &&
                        hit.collider.gameObject.GetComponent<CardView>().CanBePlayed(this))
                    {
                        hitCards.Add(hit.collider.gameObject);
                    }
                }
                if (hitCards.Count > 0)
                {
                    DestroyCardPreview();
                    hitCards = hitCards.OrderBy(x => x.GetComponent<SortingGroup>().sortingOrder).ToList();
                    var topmostCardView = hitCards[hitCards.Count - 1].GetComponent<CardView>();
                    var topmostHandCard = topmostCardView.GetComponent<HandCard>();
                    if (topmostHandCard != null)
                    {
                        topmostCardView.GetComponent<HandCard>().OnSelected();
                    }
                }
            }
        }
        else if (!isCardSelected)
        {
            var hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
            var hitCards = new List<GameObject>();
            var hitHandCard = false;
            var hitBoardCard = false;
            foreach (var hit in hits)
            {
                if (hit.collider != null &&
                    hit.collider.gameObject != null &&
                    hit.collider.gameObject.GetComponent<CardView>() != null)
                {
                    hitCards.Add(hit.collider.gameObject);
                    hitHandCard = true;
                }
            }
            if (!hitHandCard)
            {
                foreach (var hit in hits)
                {
                    if (hit.collider != null &&
                        hit.collider.gameObject != null &&
                        hit.collider.gameObject.GetComponent<BoardCreature>() != null)
                    {
                        hitCards.Add(hit.collider.gameObject);
                        hitBoardCard = true;
                    }
                }
            }

            if (hitHandCard)
            {
                if (hitCards.Count > 0)
                {
                    hitCards = hitCards.OrderBy(x => x.GetComponent<SortingGroup>().sortingOrder).ToList();
                    var topmostCardView = hitCards[hitCards.Count - 1].GetComponent<CardView>();
                    if (!topmostCardView.isPreview)
                    {
                        if (!isPreviewActive || topmostCardView.card.instanceId != currentPreviewedCardId)
                        {
                            DestroyCardPreview();
                            CreateCardPreview(topmostCardView.card, topmostCardView.transform.position, topmostCardView.IsHighlighted());
                        }
                    }
                }
            }
            else if (hitBoardCard)
            {
                if (hitCards.Count > 0)
                {
                    hitCards = hitCards.OrderBy(x => x.GetComponent<SortingGroup>().sortingOrder).ToList();
                    var selectedBoardCreature = hitCards[hitCards.Count - 1].GetComponent<BoardCreature>();
                    if (!isPreviewActive || selectedBoardCreature.card.instanceId != currentPreviewedCardId)
                    {
                        DestroyCardPreview();
                        CreateCardPreview(selectedBoardCreature.card, selectedBoardCreature.transform.position);
                    }
                }
            }
            else
            {
                DestroyCardPreview();
            }
        }
    }

    public virtual void CreateCardPreview(RuntimeCard card, Vector3 pos, bool highlight = false)
    {
        isPreviewActive = true;
        currentPreviewedCardId = card.instanceId;
        createPreviewCoroutine = StartCoroutine(CreateCardPreviewAsync(card, pos, highlight));
    }

    protected virtual IEnumerator CreateCardPreviewAsync(RuntimeCard card, Vector3 pos, bool highlight)
    {
        yield return new WaitForSeconds(0.3f);

        var libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(card.cardId);

        string cardSetName = string.Empty;
        foreach (var cardSet in GameClient.Get<IDataManager>().CachedCardsLibraryData.sets)
        {
            if (cardSet.cards.IndexOf(libraryCard) > -1)
                cardSetName = cardSet.name;
        }

        if ((Enumerators.CardKind)libraryCard.cardTypeId == Enumerators.CardKind.CREATURE)
        {
            currentCardPreview = MonoBehaviour.Instantiate(creatureCardViewPrefab as GameObject);
        }
        else if ((Enumerators.CardKind)libraryCard.cardTypeId == Enumerators.CardKind.SPELL)
        {
            currentCardPreview = MonoBehaviour.Instantiate(spellCardViewPrefab as GameObject);
        }

        var cardView = currentCardPreview.GetComponent<CardView>();
        cardView.PopulateWithInfo(card, cardSetName);
        cardView.SetHighlightingEnabled(highlight);
        cardView.isPreview = true;

        var newPos = pos;
        newPos.y += 2.0f;
        currentCardPreview.transform.position = newPos;
        currentCardPreview.transform.localRotation = Quaternion.Euler(Vector3.zero);
        currentCardPreview.transform.localScale = new Vector2(1.5f, 1.5f);
        currentCardPreview.GetComponent<SortingGroup>().sortingOrder = 1000;
        currentCardPreview.layer = LayerMask.NameToLayer("Ignore Raycast");
        currentCardPreview.transform.DOMoveY(newPos.y + 1.0f, 0.1f);
    }

    public virtual void DestroyCardPreview()
    {
        StartCoroutine(DestroyCardPreviewAsync());
        if (createPreviewCoroutine != null)
        {
            StopCoroutine(createPreviewCoroutine);
        }
        isPreviewActive = false;
    }

    protected virtual IEnumerator DestroyCardPreviewAsync()
    {
        if (currentCardPreview != null)
        {
            var oldCardPreview = currentCardPreview;
            foreach (var renderer in oldCardPreview.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.DOFade(0.0f, 0.2f);
            }
            foreach (var text in oldCardPreview.GetComponentsInChildren<TextMeshPro>())
            {
                text.DOFade(0.0f, 0.2f);
            }
            yield return new WaitForSeconds(0.5f);
            Destroy(oldCardPreview.gameObject);
        }
    }

    protected virtual void AddCardToHand(RuntimeCard card)
    {
        var libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(card.cardId);

        string cardSetName = string.Empty;
		foreach (var cardSet in GameClient.Get<IDataManager>().CachedCardsLibraryData.sets)
		{
			if (cardSet.cards.IndexOf(libraryCard) > -1)
				cardSetName = cardSet.name;
		}

        GameObject go = null;
        if ((Enumerators.CardKind)libraryCard.cardTypeId == Enumerators.CardKind.CREATURE)
        {
            go = MonoBehaviour.Instantiate(creatureCardViewPrefab as GameObject);
        }
        else if ((Enumerators.CardKind)libraryCard.cardTypeId == Enumerators.CardKind.SPELL)
        {
            go = MonoBehaviour.Instantiate(spellCardViewPrefab as GameObject);
        }

    var cardView = go.GetComponent<CardView>();
        cardView.PopulateWithInfo(card, cardSetName);

        var handCard = go.AddComponent<HandCard>();
        handCard.ownerPlayer = this;
        handCard.boardZone = GameObject.Find("PlayerBoard");

        playerHandCards.Add(cardView);

        go.GetComponent<SortingGroup>().sortingOrder = playerHandCards.Count;
    }

    protected virtual void AddCardToOpponentHand()
    {
        var go = Instantiate(opponentCardPrefab as GameObject);
        opponentHandCards.Add(go);
        go.GetComponent<SortingGroup>().sortingOrder = opponentHandCards.Count;
    }

    public void PlayCard(CardView card)
    {
        if (card.CanBePlayed(this))
        {
            gameUI.endTurnButton.SetEnabled(false);

            GameClient.Get<ITutorialManager>().ReportAction(Enumerators.TutorialReportAction.MOVE_CARD);

            var libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(card.card.cardId);

            string cardSetName = string.Empty;
            foreach (var cardSet in GameClient.Get<IDataManager>().CachedCardsLibraryData.sets)
            {
                if (cardSet.cards.IndexOf(libraryCard) > -1)
                    cardSetName = cardSet.name;
            }

            if ((Enumerators.CardKind)libraryCard.cardTypeId == Enumerators.CardKind.CREATURE)
            {
                var boardCreature = Instantiate(boardCreaturePrefab);

                SetCardType(libraryCard, boardCreature);

                var board = GameObject.Find("PlayerBoard");
                boardCreature.tag = "PlayerOwned";
                boardCreature.transform.parent = board.transform;
                boardCreature.transform.position = new Vector2(1.9f * playerBoardCards.Count, 0);
                boardCreature.GetComponent<BoardCreature>().ownerPlayer = this;
                boardCreature.GetComponent<BoardCreature>().PopulateWithInfo(card.card, cardSetName);

                playerHandCards.Remove(card);
                RearrangeHand();
                playerBoardCards.Add(boardCreature.GetComponent<BoardCreature>());

                Destroy(card.gameObject);

                currentCreature = boardCreature.GetComponent<BoardCreature>();

                RearrangeBottomBoard(() =>
                {
                    CallAbility(libraryCard, card, Enumerators.CardKind.CREATURE, currentCreature, CallCardPlay);
                });

            }
            else if ((Enumerators.CardKind)libraryCard.cardTypeId == Enumerators.CardKind.SPELL)
            {
                
                var spellsPivot = GameObject.Find("PlayerSpellsPivot");
                var sequence = DOTween.Sequence();
                sequence.Append(card.transform.DOMove(spellsPivot.transform.position, 0.5f));
                sequence.Insert(0, card.transform.DORotate(Vector3.zero, 0.2f));
                sequence.Play().OnComplete(() =>
                {
                    card.GetComponent<SortingGroup>().sortingLayerName = "BoardCards";
                    card.GetComponent<SortingGroup>().sortingOrder = 1000;

                    var boardSpell = card.gameObject.AddComponent<BoardSpell>();
                    Debug.Log(card.name);
                    CallAbility(libraryCard, card, Enumerators.CardKind.SPELL, boardSpell, CallSpellCardPlay);
                });
            }              
        }
        else
        {
            card.GetComponent<HandCard>().ResetToInitialPosition();
        }
    }

    private void CallAbility(GrandDevs.CZB.Data.Card libraryCard, CardView card, Enumerators.CardKind kind, object boardObject, Action<CardView> action)
    {
        bool canUseAbility = false;
        ActiveAbility activeAbility = null;
        foreach (var item in libraryCard.abilities) //todo improve it bcoz can have queue of abilities with targets
        {
            activeAbility = _abilitiesController.CreateActiveAbility(item, kind, boardObject, this);
            //Debug.Log(_abilitiesController.IsAbilityCanActivateTargetAtStart(item));
            if (_abilitiesController.IsAbilityCanActivateTargetAtStart(item))
                canUseAbility = true;
            else //if (_abilitiesController.IsAbilityCanActivateWithoutTargetAtStart(item))
                activeAbility.ability.Activate();
        }
        // Preemptively move the card so that the effect solver can properly check the availability of targets
        // by also taking into account this card (that is trying to be played).

        if (kind == Enumerators.CardKind.SPELL)
            currentSpellCard = card;
        else
        {
            playerInfo.namedZones[Constants.ZONE_HAND].RemoveCard(card.card);
            playerInfo.namedZones[Constants.ZONE_BOARD].AddCard(card.card);
            currentCreature.fightTargetingArrowPrefab = fightTargetingArrowPrefab;
        }
        effectSolver.MoveCard(netId, card.card, Constants.ZONE_HAND, Constants.ZONE_BOARD);
        if (canUseAbility)
        {
            var ability = libraryCard.abilities.Find(x => _abilitiesController.IsAbilityCanActivateTargetAtStart(x));

            if (_abilitiesController.CheckActivateAvailability(kind, ability, this))
            {
                activeAbility.ability.Activate();

                activeAbility.ability.ActivateSelectTarget(callback: () =>
                {
                    action(card);

                },
                failedCallback: () =>
                {
                    Debug.Log("RETURN CARD TO HAND MAYBE.. SHOULD BE CASE !!!!!");
                    action(card);
                });
            }
            else
                action(card);
        }
        else
            action(card);
    }

    private void CallCardPlay(CardView card)
    {
        PlayCreatureCard(card.card);
        currentCreature = null;
        gameUI.endTurnButton.SetEnabled(true);
    }

    private void CallSpellCardPlay(CardView card)
    {
        PlaySpellCard(card.card);
        currentSpellCard = null;
        gameUI.endTurnButton.SetEnabled(true);
    }

    private void SetCardType(GrandDevs.CZB.Data.Card card, GameObject cardobject)
    {
        if (card.type != Enumerators.CardType.FERAL)
        {
            cardobject.transform.Find("TypeIcon").gameObject.SetActive(false);
        }
        if (card.type == Enumerators.CardType.HEAVY)
        {
            cardobject.transform.Find("Armor").gameObject.SetActive(true);
        }
    }

    protected void UpdateHandCardsHighlight()
    {
        foreach (var card in playerHandCards)
        {
            if (card.CanBePlayed(this))
            {
                card.SetHighlightingEnabled(true);
            }
            else
            {
                card.SetHighlightingEnabled(false);
            }
        }
    }

    public override void OnEndGame(EndGameMessage msg)
    {
        base.OnEndGame(msg);

        var scene = GameObject.Find("GameScene").GetComponent<GameScene>();
        scene.OpenPopup<PopupOneButton>("PopupOneButton", popup =>
        {
            if (msg.winnerPlayerIndex == playerInfo.netId)
            {
                popup.text.text = "You win!";
            }
            else
            {
                popup.text.text = "You lose!";
            }
            popup.buttonText.text = "Exit";
            popup.button.onClickEvent.AddListener(() =>
            {
                if (NetworkingUtils.GetLocalPlayer().isServer)
                {
                    NetworkManager.singleton.StopHost();
                }
                else
                {
                    NetworkManager.singleton.StopClient();
                }
                GameClient.Get<ITutorialManager>().StopTutorial();
                GameClient.Get<IAppStateManager>().ChangeAppState(GrandDevs.CZB.Common.Enumerators.AppState.DECK_SELECTION);
            });
        });
    }

    public override void OnCardMoved(CardMovedMessage msg)
    {
        base.OnCardMoved(msg);

        var libraryCard = GameClient.Get<IDataManager>().CachedCardsLibraryData.GetCard(msg.card.cardId);

        string cardSetName = string.Empty;
        foreach (var cardSet in GameClient.Get<IDataManager>().CachedCardsLibraryData.sets)
        {
            if (cardSet.cards.IndexOf(libraryCard) > -1)
                cardSetName = cardSet.name;
        }

        var randomIndex = UnityEngine.Random.Range(0, opponentHandCards.Count);
        var randomCard = opponentHandCards[randomIndex];
        opponentHandCards.Remove(randomCard);
        Destroy(randomCard);
        RearrangeOpponentHand();
        gameUI.SetOpponentHandCards(opponentHandCards.Count);

        if ((Enumerators.CardKind)libraryCard.cardTypeId == Enumerators.CardKind.CREATURE)
        {
            var opponentBoard = opponentInfo.namedZones["Board"];
            effectSolver.SetDestroyConditions(opponentBoard.cards[opponentBoard.cards.Count - 1]);
            effectSolver.SetTriggers(opponentBoard.cards[opponentBoard.cards.Count - 1]);
            var boardCreature = Instantiate(boardCreaturePrefab);
            SetCardType(libraryCard, boardCreature);
            boardCreature.tag = "OpponentOwned";
            boardCreature.GetComponent<BoardCreature>().PopulateWithInfo(opponentBoard.cards[opponentBoard.cards.Count - 1], cardSetName);
            boardCreature.transform.parent = GameObject.Find("OpponentBoard").transform;
            opponentBoardCards.Add(boardCreature.GetComponent<BoardCreature>());
            RearrangeTopBoard(() =>
            {
                opponentHandZone.numCards -= 1;
                opponentManaStat.baseValue -= libraryCard.cost;

                if (msg.targetInfo != null && msg.targetInfo.Length >= 2)
                {
                    var targetingArrow = Instantiate(opponentTargetingArrowPrefab).GetComponent<OpponentTargetingArrow>();
                    targetingArrow.Begin(boardCreature.transform.position);
                    var playerCard = playerInfo.zones[msg.targetInfo[0]].cards.Find(x => x.instanceId == msg.targetInfo[1]);
                    var opponentCard = opponentInfo.zones[msg.targetInfo[0]].cards.Find(x => x.instanceId == msg.targetInfo[1]);
                    if (playerCard != null)
                    {
                        var playerBoardCard = playerBoardCards.Find(x => x.card.instanceId == playerCard.instanceId);
                        targetingArrow.SetTarget(playerBoardCard.gameObject);
                    }
                    else if (opponentCard != null)
                    {
                        var opponentBoardCard = opponentBoardCards.Find(x => x.card.instanceId == opponentCard.instanceId);
                        targetingArrow.SetTarget(opponentBoardCard.gameObject);
                    }

                    effectSolver.MoveCard(opponentInfo.netId, opponentBoard.cards[opponentBoard.cards.Count - 1], "Hand", "Board", new List<int>(msg.targetInfo));

                    StartCoroutine(RemoveOpponentTargetingArrow(targetingArrow));
                }
                else
                {
                    effectSolver.MoveCard(opponentInfo.netId, opponentBoard.cards[opponentBoard.cards.Count - 1], "Hand", "Board", new List<int>(msg.targetInfo));
                }
            });
        }
        else if ((Enumerators.CardKind)libraryCard.cardTypeId == Enumerators.CardKind.SPELL)
        {
            var opponentBoard = opponentInfo.namedZones["Board"];
            effectSolver.SetDestroyConditions(opponentBoard.cards[opponentBoard.cards.Count - 1]);
            effectSolver.SetTriggers(opponentBoard.cards[opponentBoard.cards.Count - 1]);
            var spellCard = Instantiate(spellCardViewPrefab);
            spellCard.transform.position = GameObject.Find("OpponentSpellsPivot").transform.position;
            spellCard.GetComponent<SpellCardView>().PopulateWithInfo(opponentBoard.cards[opponentBoard.cards.Count - 1], cardSetName);
            spellCard.GetComponent<SpellCardView>().SetHighlightingEnabled(false);

            currentSpellCard = spellCard.GetComponent<SpellCardView>();

            opponentManaStat.baseValue -= libraryCard.cost;

            if (msg.targetInfo != null && msg.targetInfo.Length >= 2)
            {
                var targetingArrow = Instantiate(opponentTargetingArrowPrefab).GetComponent<OpponentTargetingArrow>();
                targetingArrow.Begin(spellCard.transform.position);
                var playerCard = playerInfo.zones[msg.targetInfo[0]].cards.Find(x => x.instanceId == msg.targetInfo[1]);
                var opponentCard = opponentInfo.zones[msg.targetInfo[0]].cards.Find(x => x.instanceId == msg.targetInfo[1]);
                if (playerCard != null)
                {
                    var playerBoardCard = playerBoardCards.Find(x => x.card.instanceId == playerCard.instanceId);
                    targetingArrow.SetTarget(playerBoardCard.gameObject);
                }
                else if (opponentCard != null)
                {
                    var opponentBoardCard = opponentBoardCards.Find(x => x.card.instanceId == opponentCard.instanceId);
                    targetingArrow.SetTarget(opponentBoardCard.gameObject);
                }

                effectSolver.MoveCard(opponentInfo.netId, opponentBoard.cards[opponentBoard.cards.Count - 1], "Hand", "Board", new List<int>(msg.targetInfo));

                StartCoroutine(RemoveOpponentTargetingArrow(targetingArrow));
            }
            else
            {
                effectSolver.MoveCard(opponentInfo.netId, opponentBoard.cards[opponentBoard.cards.Count - 1], "Hand", "Board", new List<int>(msg.targetInfo));
            }
        }
    }

    private IEnumerator RemoveOpponentSpellCard(SpellCardView spellCard)
    {
        yield return new WaitForSeconds(2.0f);
    }

    private IEnumerator RemoveOpponentTargetingArrow(TargetingArrow arrow)
    {
        yield return new WaitForSeconds(2.0f);
        Destroy(arrow.gameObject);
    }

    public override void OnPlayerAttacked(PlayerAttackedMessage msg)
    {
        base.OnPlayerAttacked(msg);

        var attackingCard = opponentBoardCards.Find(x => x.card.instanceId == msg.attackingCardInstanceId);
        if (attackingCard != null)
        {
            CombatAnimation.PlayFightAnimation(attackingCard.gameObject, GameObject.Find("Player/Avatar"), 0.1f, () =>
            {
                effectSolver.FightPlayer(msg.attackingPlayerNetId, msg.attackingCardInstanceId);
            });
        }
    }

    public override void OnCreatureAttacked(CreatureAttackedMessage msg)
    {
        base.OnCreatureAttacked(msg);
        var attackingCard = opponentBoardCards.Find(x => x.card.instanceId == msg.attackingCardInstanceId);
        var attackedCard = playerBoardCards.Find(x => x.card.instanceId == msg.attackedCardInstanceId);
        if (attackingCard != null && attackedCard != null)
        {
            CombatAnimation.PlayFightAnimation(attackingCard.gameObject, attackedCard.gameObject, 0.5f, () =>
            {
                effectSolver.FightCreature(msg.attackingPlayerNetId, attackingCard.card, attackedCard.card);
            });
        }
    }

    public override void OnReceiveChatText(NetworkInstanceId senderNetId, string text)
    {
        //chatPopup.SendText(senderNetId, text);
    }
}
