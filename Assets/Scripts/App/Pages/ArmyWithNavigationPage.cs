using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Loom.ZombieBattleground.BackendCommunication;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class ArmyWithNavigationPage : IUIElement
    {
        private IUIManager _uiManager;

        private IDataManager _dataManager;

        private ILoadObjectsManager _loadObjectsManager;

        private GameObject _selfPage;

        private Button _buttonBuyPacks,
                       _buttonMarketplace;

        private RectTransform _allCardsContent;

        private TMP_InputField _inputFieldSearchName;

        #region Cache Data

        private const float BoardCardScale = 0.33f;

        private CardFilter _cardFilter;

        private List<UnitCardUI> _cardUIList = new List<UnitCardUI>();
        private Camera _mainCamera;

        private int _allCardsCount;

        private TextMeshProUGUI _cardCounter;

        private GameObject CardCreaturePrefab;

        private CardHighlightingVFXItem _highlightingVFXItem;

        #endregion

        #region IUIElement

        public void Init()
        {
            _uiManager = GameClient.Get<IUIManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _dataManager = GameClient.Get<IDataManager>();

            CardCreaturePrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Cards/CreatureCard_UI");

            _cardFilter = new CardFilter();
            _cardFilter.Init();
            _cardFilter.UpdateElementFilterEvent += UpdateElementFilterHandler;
            _cardFilter.UpdateRankFilterEvent += UpdateRankFilterHandler;
            _cardFilter.UpdateGooCostFilterEvent += UpdateGooCostFilterHandler;

            _mainCamera = Camera.main;
        }

        public void Show()
        {
            _selfPage = Object.Instantiate(
                _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/UI/Pages/MyCardsPage"));
            _selfPage.transform.SetParent(_uiManager.Canvas.transform, false);

            _uiManager.DrawPopup<SideMenuPopup>(SideMenuPopup.MENU.MY_CARDS);
            _uiManager.DrawPopup<AreaBarPopup>();

            _highlightingVFXItem = new CardHighlightingVFXItem(Object.Instantiate(
            _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/UI/ArmyCardSelection"), _selfPage.transform, true));

            _cardCounter = _selfPage.transform.Find("Panel_Frame/Upper_Items/Image_CardCounter/Text_CardCounter").GetComponent<TextMeshProUGUI>();

            _buttonMarketplace = _selfPage.transform.Find("Panel_Frame/Upper_Items/Button_MarketPlace").GetComponent<Button>();
            _buttonMarketplace.onClick.AddListener(ButtonMarketplace);

            _buttonBuyPacks = _selfPage.transform.Find("Panel_Frame/Lower_Items/Button_BuyMorePacks").GetComponent<Button>();
            _buttonBuyPacks.onClick.AddListener(ButtonBuyPacksHandler);

            _inputFieldSearchName = _selfPage.transform.Find("Panel_Frame/Upper_Items/Image_SearchBar/InputText_Search").GetComponent<TMP_InputField>();
            _inputFieldSearchName.onEndEdit.AddListener(OnInputFieldSearchEndedEdit);
            _inputFieldSearchName.text = "";

            _allCardsContent = _selfPage.transform.Find("Panel_Content/Army/Element/Scroll View")
                .GetComponent<ScrollRect>().content;

            UpdatePageScaleToMatchResolution();

            LoadAllCards();
            _allCardsCount = _cardUIList.Count;

            UpdateCardsUiList();
            _cardFilter.Show(_selfPage.transform.Find("Panel_Frame/Lower_Items/Filters").gameObject);
        }

        private void UpdateCardsUiList()
        {
            List<Enumerators.Faction> availableSetType = _cardFilter.FilterData.GetFilteredFactionList();
            List<Card> resultList = new List<Card>();
            foreach (Enumerators.Faction item in availableSetType)
            {
                Faction  set = SetTypeUtility.GetCardFaction(_dataManager, item);
                List<Card> cards = set.Cards.ToList();
                foreach(Card card in cards)
                {
                    if
                    (
                        CheckIfSatisfyFactionFilter(card) &&
                        CheckIfSatisfyGooCostFilter(card) &&
                        CheckIfSatisfyRankFilter(card)
                    )
                    {
                        resultList.Add(card);
                    }
                }
            }

            DisplayCards(resultList);
            UpdateCardCounterText(resultList.Count);
        }

        private void UpdateElementFilterHandler(Enumerators.Faction faction)
        {
            _inputFieldSearchName.text = string.Empty;
            OnInputFieldSearchEndedEdit(string.Empty);

            List<Enumerators.Faction> factionList = _cardFilter.FilterData.GetFilteredFactionList();
            List<UnitCardUI> cardUiList = _cardUIList.FindAll(card => card.GetCard().Faction == faction);
            bool isFilter = factionList.Contains(faction);
            for (int i = 0; i < cardUiList.Count; i++)
            {
                Card card = cardUiList[i].GetCard();
                if(CheckIfSatisfyRankFilter(card) && CheckIfSatisfyGooCostFilter(card))
                    cardUiList[i].EnableObject(isFilter);
            }

            UpdateCardCounterText();
        }

        private void UpdateRankFilterHandler(Enumerators.CardRank rank)
        {
            _inputFieldSearchName.text = string.Empty;
            OnInputFieldSearchEndedEdit(string.Empty);

            List<Enumerators.CardRank> rankList = _cardFilter.FilterData.GetFilteredRankList();
            List<UnitCardUI> cardUiList = _cardUIList.FindAll(card => card.GetCard().Rank == rank);
            bool isFilter = rankList.Contains(rank);
            for (int i = 0; i < cardUiList.Count; i++)
            {
                Card card = cardUiList[i].GetCard();
                if(CheckIfSatisfyFactionFilter(card) && CheckIfSatisfyGooCostFilter(card))
                    cardUiList[i].EnableObject(isFilter);
            }

            UpdateCardCounterText();
        }

        private void UpdateGooCostFilterHandler(int gooCost)
        {
            _inputFieldSearchName.text = string.Empty;
            OnInputFieldSearchEndedEdit(string.Empty);

            List<int> gooCostList = _cardFilter.FilterData.GetGooCostList();
            List<UnitCardUI> cardUiList = _cardUIList.FindAll(card => card.GetCard().Cost == gooCost);
            bool isFilter = gooCostList.Contains(gooCost);
            for (int i = 0; i < cardUiList.Count; i++)
            {
                Card card = cardUiList[i].GetCard();
                if(CheckIfSatisfyFactionFilter(card) && CheckIfSatisfyRankFilter(card))
                    cardUiList[i].EnableObject(isFilter);
            }

            UpdateCardCounterText();
        }

        private void LoadAllCards()
        {
            _cardUIList = new List<UnitCardUI>();

            for (int i = 0; i < _dataManager.CachedCardsLibraryData.Factions.Count; i++)
            {
                for (int j = 0; j < _dataManager.CachedCardsLibraryData.Factions[i].Cards.Count; j++)
                {
                    Card card = _dataManager.CachedCardsLibraryData.Factions[i].Cards[j];
                    GameObject go = Object.Instantiate(CardCreaturePrefab);
                    go.transform.SetParent(_allCardsContent);
                    go.transform.localScale = Vector3.one * BoardCardScale;

                    UnitCardUI unitCard = new UnitCardUI();
                    unitCard.Init(go);
                    int index = _dataManager.CachedCollectionData.Cards.FindIndex(cardData => cardData.MouldId == card.MouldId);
                    int cardCount = index != -1 ?_dataManager.CachedCollectionData.Cards[index].Amount : 0;
                    unitCard.FillCardData(card, cardCount);
                    _cardUIList.Add(unitCard);

                    MultiPointerClickHandler multiPointerClickHandler = go.AddComponent<MultiPointerClickHandler>();
                    multiPointerClickHandler.SingleClickReceived += () =>
                    {
                        BoardCardSingleClickHandler(unitCard.GetCard());
                    };
                    multiPointerClickHandler.DoubleClickReceived += () =>
                    {
                        BoardCardSingleClickHandler(unitCard.GetCard());
                    };
                }
            }
        }

        public void Hide()
        {
            Dispose();

            if (_selfPage == null)
                return;

            _cardFilter.Hide();

            _selfPage.SetActive(false);
            Object.Destroy(_selfPage);
            _selfPage = null;
            _uiManager.HidePopup<SideMenuPopup>();
            _uiManager.HidePopup<AreaBarPopup>();
        }

        public void Update()
        {
            UpdateRenderForCards();
        }

        private void UpdateRenderForCards()
        {
            if (_selfPage == null)
                return;

            for (int i = 0; i < _cardUIList.Count; i++)
            {
                _cardUIList[i]
                    .EnableRenderer(RendererExtensions.IsVisibleFrom(_cardUIList[i].GetFrameRectTransform(),
                        _mainCamera));
            }
        }

        public void Dispose()
        {

        }

        #endregion

        private void UpdatePageScaleToMatchResolution()
        {
            float screenRatio = (float)Screen.width/Screen.height;
            if(screenRatio < 1.76f)
            {
                _selfPage.transform.localScale = Vector3.one * 0.93f;
            }
        }

        #region UI Handlers

        private void ButtonBuyPacksHandler()
        {
            PlayClickSound();
            GameClient.Get<IAppStateManager>().ChangeAppState(Enumerators.AppState.SHOP);
        }

        private void ButtonMarketplace()
        {
            PlayClickSound();
            _uiManager.GetPopup<QuestionPopup>().ConfirmationReceived += ConfirmRedirectMarketplaceLink;
            _uiManager.DrawPopup<QuestionPopup>("Would you like to visit the Marketplace website?");
        }

        private void ConfirmRedirectMarketplaceLink(bool status)
        {
            _uiManager.GetPopup<QuestionPopup>().ConfirmationReceived -= ConfirmRedirectMarketplaceLink;
            if(status)
            {
                Application.OpenURL(Constants.MarketPlaceLink);
            }
        }

        public void OnInputFieldSearchEndedEdit(string value)
        {
            string keyword = _inputFieldSearchName.text.Trim().ToLower();
            Faction set;
            List<Card> cards;
            List<Card> resultList = new List<Card>();
            List<Enumerators.Faction> allAvailableSetTypeList = _cardFilter.AllAvailableFactionList;

            foreach (Enumerators.Faction item in allAvailableSetTypeList)
            {
                set = SetTypeUtility.GetCardFaction(_dataManager, item);
                cards = set.Cards.ToList();

                foreach (Card card in cards)
                {
                    if
                    (
                        card.Name.ToLower().Contains(keyword) &&
                        CheckIfSatisfyFactionFilter(card) &&
                        CheckIfSatisfyGooCostFilter(card) &&
                        CheckIfSatisfyRankFilter(card)
                    )
                    {
                        resultList.Add(card);
                    }
                }
            }

            DisplayCards(resultList);
            UpdateCardCounterText(resultList.Count);
        }

        private void DisplayCards(List<Card> cards)
        {
            List<UnitCardUI> cardUiList = _cardUIList.Where(uiCard =>
                cards.Any(resultCard => uiCard.GetCard().MouldId == resultCard.MouldId)).ToList();
            for (int i = 0; i < cardUiList.Count; i++)
            {
                cardUiList[i].EnableObject(true);
            }

            cardUiList.Clear();
            cardUiList = _cardUIList.Where(uiCard =>
                cards.All(resultCard => uiCard.GetCard().MouldId != resultCard.MouldId)).ToList();
            for (int i = 0; i < cardUiList.Count; i++)
            {
                cardUiList[i].EnableObject(false);
            }
        }

        #endregion

        #region Board Cards
        private void BoardCardSingleClickHandler(Card selectedCard)
        {
            if (_uiManager.GetPopup<CardInfoWithSearchPopup>().Self != null)
                return;

            List<IReadOnlyCard> cardList = _cardUIList.Select(card => card.GetCardInteface()).ToList();
            _uiManager.DrawPopup<CardInfoWithSearchPopup>(new object[]
            {
                cardList,
                selectedCard,
                CardInfoWithSearchPopup.PopupType.NONE
            });
        }

        #endregion

        private void UpdateCardCounterText(int cardCount)
        {
            _cardCounter.text = cardCount + "/" + _allCardsCount;
        }

        private void UpdateCardCounterText()
        {
            var activeCards = _cardUIList.FindAll(card => card.IsActive());
           _cardCounter.text = activeCards.Count + "/" + _allCardsCount;
        }

        private bool CheckIfSatisfyGooCostFilter(Card card)
        {
            if (card.Cost < 0)
                return false;

            if(card.Cost >= 10)
            {
                if (_cardFilter.FilterData.GooCostList[10])
                    return true;
            }
            else
            {
                if(_cardFilter.FilterData.GooCostList[card.Cost])
                    return true;
            }

            return false;
        }

        private bool CheckIfSatisfyRankFilter(Card card)
        {
            return _cardFilter.FilterData.RankDictionary[card.Rank];
        }

        private bool CheckIfSatisfyFactionFilter(Card card)
        {
            return _cardFilter.FilterData.FactionDictionary[card.Faction];
        }

        #region Util

        public void PlayClickSound()
        {
            GameClient.Get<ISoundManager>().PlaySound(Enumerators.SoundType.CLICK, Constants.SfxSoundVolume, false, false, true);
        }
        #endregion
    }
}
