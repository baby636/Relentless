using System;
using System.Collections.Generic;
using DG.Tweening;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Gameplay;
using Loom.ZombieBattleground.Helpers;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class BoardCard
    {
        public int CardsAmountDeckEditing;

        public bool CardShouldBeChanged;

        public bool IsNewCard;

        public bool IsPreview;

        public int InitialCost;

        public Card LibraryCard;

        protected ILoadObjectsManager LoadObjectsManager;

        protected ISoundManager SoundManager;

        protected IDataManager DataManager;

        protected IGameplayManager GameplayManager;

        protected ITimerManager TimerManager;

        protected CardsController CardsController;

        protected AbilitiesController AbilitiesController;

        protected BattlegroundController BattlegroundController;

        protected SpriteRenderer GlowSprite;

        protected SpriteRenderer PictureSprite;

        protected SpriteRenderer BackgroundSprite;

        protected GameObject DistibuteCardObject;

        protected TextMeshPro CostText;

        protected TextMeshPro NameText;

        protected TextMeshPro BodyText;

        protected TextMeshPro AmountText;

        protected TextMeshPro AmountTextForArmy;

        protected Animator CardAnimator;

        protected Vector3 PositionOnHand;

        protected Vector3 RotationOnHand;

        protected Vector3 ScaleOnHand;

        protected AnimationEventTriggering AnimationEventTriggering;

        protected OnBehaviourHandler BehaviourHandler;

        protected List<ElementSlotOfCards> ElementSlotsOfCards;

        protected Transform ParentOfEditingGroupUI;

        protected List<BuffOnCardInfoObject> BuffOnCardInfoObjects;

        protected Transform ParentOfLeftBlockOfCardInfo, ParentOfRightBlockOfCardInfo;

        public BoardCard(GameObject selfObject)
        {
            LoadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            SoundManager = GameClient.Get<ISoundManager>();
            DataManager = GameClient.Get<IDataManager>();
            GameplayManager = GameClient.Get<IGameplayManager>();
            TimerManager = GameClient.Get<ITimerManager>();

            CardsController = GameplayManager.GetController<CardsController>();
            AbilitiesController = GameplayManager.GetController<AbilitiesController>();
            BattlegroundController = GameplayManager.GetController<BattlegroundController>();

            GameObject = selfObject;

            ElementSlotsOfCards = new List<ElementSlotOfCards>();

            CardAnimator = GameObject.GetComponent<Animator>();
            CardAnimator.enabled = false;

            GlowSprite = Transform.Find("Glow").GetComponent<SpriteRenderer>();
            PictureSprite = Transform.Find("Picture").GetComponent<SpriteRenderer>();
            BackgroundSprite = Transform.Find("Frame").GetComponent<SpriteRenderer>();

            CostText = Transform.Find("GooText").GetComponent<TextMeshPro>();
            NameText = Transform.Find("TitleText").GetComponent<TextMeshPro>();
            BodyText = Transform.Find("BodyText").GetComponent<TextMeshPro>();
            AmountText = Transform.Find("Amount/Text").GetComponent<TextMeshPro>();
            AmountTextForArmy = Transform.Find("AmountForArmy/Text").GetComponent<TextMeshPro>();

            RemoveCardParticle = Transform.Find("RemoveCardParticle").GetComponent<ParticleSystem>();

            DistibuteCardObject = Transform.Find("DistributeCardObject").gameObject;

            ParentOfEditingGroupUI = Transform.Find("DeckEditingGroupUI");

            AnimationEventTriggering = GameObject.GetComponent<AnimationEventTriggering>();
            BehaviourHandler = GameObject.GetComponent<OnBehaviourHandler>();

            AnimationEventTriggering.AnimationEventTriggered += OnAnimationEvent;

            CardsController.UpdateCardsStatusEvent += UpdateCardsStatusEventHandler;

            BehaviourHandler.MouseDownTriggered += MouseDownTriggeredHandler;
            BehaviourHandler.MouseUpTriggered += MouseUpTriggeredHandler;

            BehaviourHandler.Destroying += DestroyingHandler;
        }

        public int ManaCost { get; protected set; }

        public ParticleSystem RemoveCardParticle { get; protected set; }

        public Transform Transform => GameObject.transform;

        public GameObject GameObject { get; }

        public int CurrentTurn { get; set; }

        public WorkingCard WorkingCard { get; private set; }

        public HandBoardCard HandBoardCard { get; set; }

        public virtual void Init(WorkingCard card)
        {
            WorkingCard = card;

            LibraryCard = WorkingCard.LibraryCard;

            NameText.text = LibraryCard.Name;
            BodyText.text = LibraryCard.Description;
            CostText.text = LibraryCard.Cost.ToString();

            IsNewCard = true;

            InitialCost = WorkingCard.InitialCost;
            ManaCost = InitialCost;

            WorkingCard.Owner.PlayerGooChanged += PlayerGooChangedHandler;

            string rarity = Enum.GetName(typeof(Enumerators.CardRank), WorkingCard.LibraryCard.CardRank);

            string setName = LibraryCard.CardSetType.ToString();

            string frameName = string.Format("Images/Cards/Frames/frame_{0}_{1}", setName, rarity);

            if (!string.IsNullOrEmpty(LibraryCard.Frame))
            {
                frameName = "Images/Cards/Frames/" + LibraryCard.Frame;
            }

            BackgroundSprite.sprite = LoadObjectsManager.GetObjectByPath<Sprite>(frameName);
            PictureSprite.sprite = LoadObjectsManager.GetObjectByPath<Sprite>(string.Format(
                "Images/Cards/Illustrations/{0}_{1}_{2}", setName.ToLower(), rarity.ToLower(),
                WorkingCard.LibraryCard.Picture.ToLower()));

            AmountText.transform.parent.gameObject.SetActive(false);
            AmountTextForArmy.transform.parent.gameObject.SetActive(false);
            DistibuteCardObject.SetActive(false);

            if (LibraryCard.CardKind == Enumerators.CardKind.CREATURE)
            {
                ParentOfLeftBlockOfCardInfo = Transform.Find("Group_LeftBlockInfo");
                ParentOfRightBlockOfCardInfo = Transform.Find("Group_RightBlockInfo");

                if (!InternalTools.IsTabletScreen())
                {
                    ParentOfLeftBlockOfCardInfo.transform.localScale = new Vector3(.7f, .7f, .7f);
                    ParentOfLeftBlockOfCardInfo.transform.localPosition = new Vector3(10f, 6.8f, 0f);

                    ParentOfRightBlockOfCardInfo.transform.localScale = new Vector3(.7f, .7f, .7f);
                    ParentOfRightBlockOfCardInfo.transform.localPosition = new Vector3(17f, 6.8f, 0f);
                }
            }
        }

        public virtual void Init(Card card, int amount = 0)
        {
            LibraryCard = card;

            NameText.text = LibraryCard.Name;
            BodyText.text = LibraryCard.Description;
            AmountText.text = amount.ToString();
            CostText.text = LibraryCard.Cost.ToString();

            InitialCost = LibraryCard.Cost;
            ManaCost = InitialCost;

            string rarity = Enum.GetName(typeof(Enumerators.CardRank), card.CardRank);

            string setName = LibraryCard.CardSetType.ToString();

            string frameName = string.Format("Images/Cards/Frames/frame_{0}_{1}", setName, rarity);

            if (!string.IsNullOrEmpty(LibraryCard.Frame))
            {
                frameName = "Images/Cards/Frames/" + LibraryCard.Frame;
            }

            BackgroundSprite.sprite = LoadObjectsManager.GetObjectByPath<Sprite>(frameName);

            PictureSprite.sprite = LoadObjectsManager.GetObjectByPath<Sprite>(string.Format(
                "Images/Cards/Illustrations/{0}_{1}_{2}", setName.ToLower(), rarity.ToLower(), card.Picture.ToLower()));

            DistibuteCardObject.SetActive(false);
        }

        public void SetCardCost(int value, bool changeRealCost = false)
        {
            if (changeRealCost)
            {
                WorkingCard.LibraryCard.Cost = value;
                WorkingCard.RealCost = value;
                ManaCost = WorkingCard.RealCost;
                CostText.text = ManaCost.ToString();
            }
            else
            {
                ManaCost = value;
                CostText.text = ManaCost.ToString();
            }

            UpdateColorOfCost();
        }

        public void ChangeCardCostOn(int value, bool changeRealCost = false)
        {
            if (changeRealCost)
            {
                WorkingCard.RealCost += value;
                ManaCost = WorkingCard.RealCost;
                CostText.text = ManaCost.ToString();
            }
            else
            {
                ManaCost = WorkingCard.RealCost + value;
                CostText.text = ManaCost.ToString();
            }

            UpdateColorOfCost();
        }

        public virtual void UpdateAmount(int amount)
        {
            AmountText.text = amount.ToString();
        }

        public virtual void UpdateCardPositionInHand(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            if (IsPreview)
                return;

            PositionOnHand = position;
            RotationOnHand = rotation;
            ScaleOnHand = scale;

            if (!IsNewCard)
            {
                UpdatePositionOnHand();
            }
            else if (CurrentTurn != 0)
            {
                CardAnimator.enabled = true;
                CardAnimator.SetTrigger("DeckToHand");

                SoundManager.PlaySound(Enumerators.SoundType.CARD_DECK_TO_HAND_SINGLE, Constants.CardsMoveSoundVolume);
            }

            IsNewCard = false;
        }

        public virtual void MoveCardFromDeckToCenter()
        {
            CardAnimator.enabled = true;
            CardAnimator.SetTrigger("DeckToCenterDistribute");

            SoundManager.PlaySound(Enumerators.SoundType.CARD_DECK_TO_HAND_MULTIPLE, Constants.CardsMoveSoundVolume);
        }

        public virtual void SetDefaultAnimation(int id)
        {
            if (IsPreview)
                return;

            CardAnimator.enabled = true;
            CardAnimator.SetTrigger("DeckToHandDefault");

            if (DataManager.CachedUserLocalData.Tutorial)
            {
                CardAnimator.SetFloat("Id", 2);
            }
            else
            {
                CardAnimator.SetFloat("Id", id);
            }

            SoundManager.PlaySound(Enumerators.SoundType.CARD_DECK_TO_HAND_MULTIPLE, Constants.CardsMoveSoundVolume);
        }

        public virtual void OnAnimationEvent(string name)
        {
            switch (name)
            {
                case "DeckToHandEnd":
                    CardAnimator.enabled = false;

                    if (!CardsController.CardDistribution)
                    {
                        UpdatePositionOnHand();
                    }

                    break;
            }
        }

        public virtual bool CanBePlayed(Player owner)
        {
#if !DEV_MODE
            return GameplayManager.GetController<PlayerController>()
                .IsActive; // && owner.manaStat.effectiveValue >= manaCost;
#else
            return true;
#endif
        }

        public virtual bool CanBeBuyed(Player owner)
        {
#if !DEV_MODE
            return owner.Goo >= ManaCost;
#else
            return true;
#endif
        }

        public void SetHighlightingEnabled(bool enabled)
        {
            if (GlowSprite != null && GlowSprite)
            {
                GlowSprite.enabled = enabled;
            }
        }

        public void Dispose()
        {
            Object.Destroy(GameObject);
        }

        public void ReturnCardToDeck()
        {
            if (!CardsController.CardDistribution)
                return;

            CardsController.ReturnCardToDeck(
                this,
                () =>
                {
                    WorkingCard.Owner.DistributeCard();
                });
        }

        public void DrawCardFromOpponentDeckToPlayer()
        {
            GameObject.transform.localScale = Vector3.zero;

            GameObject.transform.DOScale(new Vector3(0.2f, 0.2f, 0.2f), 0.15f);

            CardAnimator.enabled = true;
            CardAnimator.StopPlayback();
            CardAnimator.Play("MoveCardFromOpponentDeckToPlayerHand");

            TimerManager.AddTimer(
                x =>
                {
                    CardAnimator.enabled = false;

                    BattlegroundController.PlayerHandCards.Add(this);

                    BattlegroundController.UpdatePositionOfCardsInPlayerHand(true);
                },
                null,
                2f);
        }

        // editing deck page
        public void SetAmountOfCardsInEditingPage(bool init, uint maxCopies, int amount, bool isArmy = false)
        {
            CardsAmountDeckEditing = amount;
            if (init)
            {
                AmountTextForArmy.transform.parent.gameObject.SetActive(isArmy);

                foreach (Transform child in ParentOfEditingGroupUI)
                {
                    Object.Destroy(child.gameObject);
                }

                foreach (ElementSlotOfCards item in ElementSlotsOfCards)
                {
                    Object.Destroy(item.SelfObject);
                }

                ElementSlotsOfCards.Clear();

                for (int i = 0; i < maxCopies; i++)
                {
                    ElementSlotsOfCards.Add(new ElementSlotOfCards(ParentOfEditingGroupUI, false));
                }
            }

            for (int i = 0; i < maxCopies; i++)
            {
                if (i >= ElementSlotsOfCards.Count)
                {
                    ElementSlotsOfCards.Add(new ElementSlotOfCards(ParentOfEditingGroupUI, false));
                }

                ElementSlotsOfCards[i].SetStatus(i < amount);
            }

            float offset = 0.5f;
            float spacing = 2f;
            float offsetY = 0f;

            if (maxCopies > 3)
            {
                offset = 0.8f;
            }
            else if (maxCopies > 2)
            {
                offset = 0.5f;
            }
            else if (maxCopies > 1)
            {
                offset = 0.7f;
            }


            if (isArmy)
            {
                spacing = 1.4f;
                offset = -0.55f;
                offsetY = -0.5f;
                AmountTextForArmy.text = amount.ToString();
            }
            InternalTools.GroupHorizontalObjects(ParentOfEditingGroupUI, offset, spacing, offsetY, isArmy);
        }

        public void DrawTooltipInfoOfUnit(BoardUnit unit)
        {
            GameClient.Get<ICameraManager>().FadeIn(0.8f, 1);

            BuffOnCardInfoObjects = new List<BuffOnCardInfoObject>();

            const float offset = 0f;
            const float spacing = -6.75f;

            BuffOnCardInfoObject buff;

            List<BuffTooltipInfo> buffs = new List<BuffTooltipInfo>();

            // left block info ------------------------------------
            if (unit.Card.LibraryCard.CardRank != Enumerators.CardRank.MINION)
            {
                TooltipContentData.RankInfo rankInfo =
                    DataManager.GetRankInfoByType(unit.Card.LibraryCard.CardRank.ToString());
                if (rankInfo != null)
                {
                    TooltipContentData.RankInfo.RankDescription rankDescription = rankInfo.Info.Find(y =>
                        y.Element.ToLower().Equals(CardsController.GetSetOfCard(unit.Card.LibraryCard).ToLower()));

                    buffs.Add(
                        new BuffTooltipInfo
                        {
                            Title = rankInfo.Name,
                            Description = rankDescription.Tooltip,
                            TooltipObjectType = Enumerators.TooltipObjectType.RANK,
                            Value = -1
                        });
                }
            }

            if (unit.InitialUnitType != Enumerators.CardType.WALKER &&
                unit.InitialUnitType != Enumerators.CardType.NONE)
            {
                TooltipContentData.BuffInfo buffInfo = DataManager.GetBuffInfoByType(unit.InitialUnitType.ToString());
                if (buffInfo != null)
                {
                    buffs.Add(
                        new BuffTooltipInfo
                        {
                            Title = buffInfo.Name,
                            Description = buffInfo.Tooltip,
                            TooltipObjectType = Enumerators.TooltipObjectType.UNIT_TYPE,
                            Value = -1
                        });
                }
            }

            if (unit.Card.LibraryCard.Abilities != null)
            {
                foreach (AbilityData abil in unit.Card.LibraryCard.Abilities)
                {
                    TooltipContentData.BuffInfo buffInfo = DataManager.GetBuffInfoByType(abil.BuffType);
                    if (buffInfo != null)
                    {
                        buffs.Add(
                            new BuffTooltipInfo
                            {
                                Title = buffInfo.Name,
                                Description = buffInfo.Tooltip,
                                TooltipObjectType = Enumerators.TooltipObjectType.ABILITY,
                                Value = GetValueOfAbilityByType(abil)
                            });
                    }
                }
            }

            for (int i = 0; i < buffs.Count; i++)
            {
                if (i >= 3)
                {
                    break;
                }

                if (BuffOnCardInfoObjects.Find(x => x.BuffTooltipInfo.Title.Equals(buffs[i].Title)) != null)
                {
                    continue;
                }

                buff = new BuffOnCardInfoObject(buffs[i], ParentOfLeftBlockOfCardInfo, offset + spacing * i);

                BuffOnCardInfoObjects.Add(buff);
            }

            float cardSize = 7.2f;
            float centerOffset = -7f;

            if (!InternalTools.IsTabletScreen())
            {
                cardSize = 6.6f;
                centerOffset = -10f;
            }

            InternalTools.GroupVerticalObjects(ParentOfLeftBlockOfCardInfo, 0f, centerOffset, cardSize);

            Transform parent = buffs.Count > 0 ? ParentOfRightBlockOfCardInfo : ParentOfLeftBlockOfCardInfo;

            buffs.Clear();

            // right block info ------------------------------------

            // IMPROVE!!!
            foreach (AbilityBase abil in AbilitiesController.GetAbilitiesConnectedToUnit(unit))
            {
                TooltipContentData.BuffInfo buffInfo = DataManager.GetBuffInfoByType(abil.AbilityData.BuffType);
                if (buffInfo != null)
                {
                    buffs.Add(
                        new BuffTooltipInfo
                        {
                            Title = buffInfo.Name,
                            Description = buffInfo.Tooltip,
                            TooltipObjectType = Enumerators.TooltipObjectType.BUFF,
                            Value = -1
                        });
                }
            }

            // IMPROVE!!!
            foreach (Enumerators.BuffType buffOnUnit in unit.BuffsOnUnit)
            {
                TooltipContentData.BuffInfo buffInfo = DataManager.GetBuffInfoByType(buffOnUnit.ToString());
                if (buffInfo != null)
                {
                    buffs.Add(
                        new BuffTooltipInfo
                        {
                            Title = buffInfo.Name,
                            Description = buffInfo.Tooltip,
                            TooltipObjectType = Enumerators.TooltipObjectType.BUFF,
                            Value = -1
                        });
                }
            }

            for (int i = 0; i < buffs.Count; i++)
            {
                if (i >= 3)
                {
                    break;
                }

                if (BuffOnCardInfoObjects.Find(x => x.BuffTooltipInfo.Title.Equals(buffs[i].Title)) != null)
                {
                    continue;
                }

                buff = new BuffOnCardInfoObject(buffs[i], parent, offset + spacing * i);

                BuffOnCardInfoObjects.Add(buff);
            }

            buffs.Clear();

            InternalTools.GroupVerticalObjects(parent, 0f, centerOffset, cardSize);
        }

        public void DrawTooltipInfoOfCard(BoardCard boardCard)
        {
            GameClient.Get<ICameraManager>().FadeIn(0.8f, 1);

            if (boardCard.WorkingCard.LibraryCard.CardKind == Enumerators.CardKind.SPELL)
                return;

            BuffOnCardInfoObjects = new List<BuffOnCardInfoObject>();

            const float offset = 0f;
            const float spacing = -6.75f;

            BuffOnCardInfoObject buff;

            List<BuffTooltipInfo> buffs = new List<BuffTooltipInfo>();

            // left block info ------------------------------------
            if (boardCard.WorkingCard.LibraryCard.CardRank != Enumerators.CardRank.MINION)
            {
                TooltipContentData.RankInfo rankInfo =
                    DataManager.GetRankInfoByType(boardCard.WorkingCard.LibraryCard.CardRank.ToString());
                if (rankInfo != null)
                {
                    TooltipContentData.RankInfo.RankDescription rankDescription = rankInfo.Info.Find(y =>
                        y.Element.ToLower()
                            .Equals(CardsController.GetSetOfCard(boardCard.WorkingCard.LibraryCard).ToLower()));

                    buffs.Add(
                        new BuffTooltipInfo
                        {
                            Title = rankInfo.Name,
                            Description = rankDescription.Tooltip,
                            TooltipObjectType = Enumerators.TooltipObjectType.RANK,
                            Value = -1
                        });
                }
            }

            if (boardCard.WorkingCard.Type != Enumerators.CardType.WALKER &&
                boardCard.WorkingCard.Type != Enumerators.CardType.NONE)
            {
                TooltipContentData.BuffInfo buffInfo =
                    DataManager.GetBuffInfoByType(boardCard.WorkingCard.Type.ToString());
                if (buffInfo != null)
                {
                    buffs.Add(
                        new BuffTooltipInfo
                        {
                            Title = buffInfo.Name,
                            Description = buffInfo.Tooltip,
                            TooltipObjectType = Enumerators.TooltipObjectType.UNIT_TYPE,
                            Value = -1
                        });
                }
            }

            if (boardCard.WorkingCard.LibraryCard.Abilities != null)
            {
                foreach (AbilityData abil in boardCard.WorkingCard.LibraryCard.Abilities)
                {
                    TooltipContentData.BuffInfo buffInfo = DataManager.GetBuffInfoByType(abil.BuffType);
                    if (buffInfo != null)
                    {
                        buffs.Add(
                            new BuffTooltipInfo
                            {
                                Title = buffInfo.Name,
                                Description = buffInfo.Tooltip,
                                TooltipObjectType = Enumerators.TooltipObjectType.ABILITY,
                                Value = GetValueOfAbilityByType(abil)
                            });
                    }
                }
            }

            for (int i = 0; i < buffs.Count; i++)
            {
                if (i >= 3)
                {
                    break;
                }

                if (BuffOnCardInfoObjects.Find(x => x.BuffTooltipInfo.Title.Equals(buffs[i].Title)) != null)
                {
                    continue;
                }

                buff = new BuffOnCardInfoObject(buffs[i], ParentOfLeftBlockOfCardInfo, offset + spacing * i);

                BuffOnCardInfoObjects.Add(buff);
            }

            buffs.Clear();

            float cardSize = 7.2f;
            float centerOffset = -7f;

            if (!InternalTools.IsTabletScreen())
            {
                cardSize = 6.6f;
                centerOffset = -10f;
            }

            InternalTools.GroupVerticalObjects(ParentOfLeftBlockOfCardInfo, 0f, centerOffset, cardSize);
        }

        protected void UpdatePositionOnHand()
        {
            if (IsPreview)
                return;

            Transform.DOScale(ScaleOnHand, 0.5f);
            Transform.DOMove(PositionOnHand, 0.5f);
            Transform.DORotate(RotationOnHand, 0.5f);
        }

        private void DestroyingHandler(GameObject obj)
        {
        }

        private void PlayerGooChangedHandler(int obj)
        {
            UpdateCardsStatusEventHandler(WorkingCard.Owner);
        }

        private void UpdateColorOfCost()
        {
            if (ManaCost > InitialCost)
            {
                CostText.color = Color.red;
            }
            else if (ManaCost < InitialCost)
            {
                CostText.color = Color.green;
            }
            else
            {
                CostText.color = Color.white;
            }
        }

        private void UpdateCardsStatusEventHandler(Player player)
        {
            if (IsPreview)
                return;

            if (CanBePlayed(player) && CanBeBuyed(player))
            {
                SetHighlightingEnabled(true);
            }
            else
            {
                SetHighlightingEnabled(false);
            }
        }

        private void MouseUpTriggeredHandler(GameObject obj)
        {
            if (!CardsController.CardDistribution)
            {
            }
        }

        private void MouseDownTriggeredHandler(GameObject obj)
        {
            if (!CardsController.CardDistribution)
                return;

            CardShouldBeChanged = !CardShouldBeChanged;

            DistibuteCardObject.SetActive(CardShouldBeChanged);
        }

        private int GetValueOfAbilityByType(AbilityData ability)
        {
            switch (ability.BuffType)
            {
                case "DELAYED":
                    return ability.Delay;
                default: return ability.Value;
            }
        }

        public class BuffTooltipInfo
        {
            public string Title, Description;

            public Enumerators.TooltipObjectType TooltipObjectType;

            public int Value;
        }

        public class BuffOnCardInfoObject
        {
            public BuffTooltipInfo BuffTooltipInfo;

            private readonly ILoadObjectsManager _loadObjectsManager;

            private readonly GameObject _selfObject;

            private readonly SpriteRenderer _buffIconPicture;

            private readonly TextMeshPro _callTypeText;

            private readonly TextMeshPro _descriptionText;

            public BuffOnCardInfoObject(BuffTooltipInfo buffTooltipInfo, Transform parent, float offsetY)
            {
                _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();

                BuffTooltipInfo = buffTooltipInfo;

                _selfObject =
                    Object.Instantiate(
                        _loadObjectsManager.GetObjectByPath<GameObject>(
                            "Prefabs/Gameplay/Tooltips/Tooltip_BuffOnCardInfo"), parent, false);

                Transform.localPosition = new Vector3(0, offsetY, 0f);

                _callTypeText = _selfObject.transform.Find("Text_CallType").GetComponent<TextMeshPro>();
                _descriptionText = _selfObject.transform.Find("Text_Description").GetComponent<TextMeshPro>();

                _buffIconPicture = _selfObject.transform.Find("Image_IconBackground/Image_BuffIcon")
                    .GetComponent<SpriteRenderer>();

                _callTypeText.text = "    " + ReplaceXByValue(buffTooltipInfo.Title, buffTooltipInfo.Value).ToUpper();
                _descriptionText.text = ReplaceXByValue(buffTooltipInfo.Description, buffTooltipInfo.Value);

                switch (buffTooltipInfo.TooltipObjectType)
                {
                    case Enumerators.TooltipObjectType.RANK:
                        _buffIconPicture.sprite = _loadObjectsManager.GetObjectByPath<Sprite>(
                            "Images/IconsRanks/battleground_rank_icon_" +
                            buffTooltipInfo.Title.Replace(" ", string.Empty).ToLower() + "_large");
                        break;
                    case Enumerators.TooltipObjectType.ABILITY:
                    case Enumerators.TooltipObjectType.BUFF:
                        _buffIconPicture.sprite = _loadObjectsManager.GetObjectByPath<Sprite>(
                            "Images/IconsBuffTypes/battleground_mechanic_icon_" +
                            buffTooltipInfo.Title.Replace(" ", string.Empty).ToLower() + "_large");
                        break;
                    case Enumerators.TooltipObjectType.UNIT_TYPE:
                        _buffIconPicture.sprite = _loadObjectsManager.GetObjectByPath<Sprite>(
                            "Images/IconsUnitTypes/battleground_mechanic_icon_" +
                            buffTooltipInfo.Title.Replace(" ", string.Empty).ToLower() + "_large");
                        break;
                    default:
                        _buffIconPicture.sprite = null;
                        break;
                }
            }

            public Transform Transform => _selfObject.transform;

            public void Dispose()
            {
                Object.Destroy(_selfObject);
            }

            private string ReplaceXByValue(string val, int intVal)
            {
                return val.Replace("X", intVal.ToString());
            }
        }

        public class ElementSlotOfCards
        {
            public GameObject SelfObject;

            public GameObject UsedObject, FreeObject;

            public ElementSlotOfCards(Transform parent, bool used)
            {
                SelfObject =
                    Object.Instantiate(
                        GameClient.Get<ILoadObjectsManager>()
                            .GetObjectByPath<GameObject>("Prefabs/Gameplay/Element_SlotOfCards"), parent, false);

                FreeObject = SelfObject.transform.Find("Object_Free").gameObject;
                UsedObject = SelfObject.transform.Find("Object_Used").gameObject;

                SetStatus(used);
            }

            public void SetStatus(bool used)
            {
                if (used)
                {
                    FreeObject.SetActive(false);
                    UsedObject.SetActive(true);
                }
                else
                {
                    FreeObject.SetActive(true);
                    UsedObject.SetActive(false);
                }
            }
        }
    }
}
