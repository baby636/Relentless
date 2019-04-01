using System;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class ModificateStatAbility : AbilityBase
    {
        private bool _canBeReverted = false;

        public Enumerators.Faction Faction;

        public Enumerators.Faction TargetFaction;

        public Enumerators.Stat StatType;

        public int Value { get; }

        public int Count { get; }

        public ModificateStatAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Faction = ability.Faction;
            TargetFaction = ability.TargetFaction;
            StatType = ability.Stat;
            Value = ability.Value;
            Count = ability.Count;
        }

        public override void Activate()
        {
            base.Activate();

            VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/GreenHealVFX");

            if (AbilityTrigger != Enumerators.AbilityTrigger.ENTRY)
            {
                InvokeUseAbilityEvent();
            }
            else if(AbilityTrigger == Enumerators.AbilityTrigger.ENTRY &&
                    AbilityActivity == Enumerators.AbilityActivity.PASSIVE)
            {
                if (AbilityData.SubTrigger == Enumerators.AbilitySubTrigger.AllAllyUnitsByFactionInPlay)
                {
                    IReadOnlyList<CardModel> units = PlayerCallerOfAbility.CardsOnBoard.FindAll(
                                    x => x.Card.Prototype.Faction == Faction && x != AbilityUnitOwner);
                    units = InternalTools.GetRandomElementsFromList(units, Count);

                    foreach (CardModel unit in units)
                    {
                        ModificateStats(unit);
                    }
                }
                else
                {
                    if (PlayerCallerOfAbility.CardsOnBoard.FindAll(x => x.Card.Prototype.Faction == TargetFaction && x != AbilityUnitOwner).Count > 0)
                    {
                        ModificateStats(AbilityUnitOwner, GameplayManager.CurrentTurnPlayer == PlayerCallerOfAbility);
                    }
                }
            }
        }

        protected override void TurnStartedHandler()
        {
            base.TurnStartedHandler();

            if (AbilityTrigger != Enumerators.AbilityTrigger.TURN)
                return;

            ModificateStats(AbilityUnitOwner, GameplayManager.CurrentTurnPlayer == PlayerCallerOfAbility);
        }

        protected override void UnitDiedHandler()
        {
            base.UnitDiedHandler();

            if (AbilityTrigger != Enumerators.AbilityTrigger.DEATH)
                return;

            if (AbilityData.SubTrigger == Enumerators.AbilitySubTrigger.RandomUnit)
            {
                List<CardModel> targets = new List<CardModel>();

                foreach (Enumerators.Target targetType in AbilityTargets)
                {
                    switch (targetType)
                    {
                        case Enumerators.Target.PLAYER_CARD:
                            targets.AddRange(PlayerCallerOfAbility.CardsOnBoard);
                            break;
                        case Enumerators.Target.OPPONENT_CARD:
                            targets.AddRange(GetOpponentOverlord().CardsOnBoard);
                            break;
                    }
                }

                targets = targets.FindAll(x => x!= AbilityUnitOwner);

                List<CardModel> finalTargets = new List<CardModel>();
                int count = Mathf.Max(1, Count);
                while (count > 0 && targets.Count > 0)
                {   
                    int chosenIndex = MTwister.IRandom(0, targets.Count-1);
                    finalTargets.Add(targets[chosenIndex]);
                    targets.RemoveAt(chosenIndex);
                    count--;
                }

                foreach (CardModel target in finalTargets)
                {
                    ModificateStats(target, false);
                }
            }
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            ModificateStats(TargetUnit);
        }

        protected override void InputEndedHandler()
        {
            base.InputEndedHandler();

            if (IsAbilityResolved)
            {
                Action();
            }
        }

        private void ModificateStats(IBoardObject boardObject, bool revert = false)
        {
            if (revert && !_canBeReverted)
                return;

            switch (boardObject)
            {
                case CardModel boardUnit:
                    {
                        if (Faction == Enumerators.Faction.Undefined || Faction == boardUnit.Card.Prototype.Faction)
                        {
                            switch (StatType)
                            {
                                case Enumerators.Stat.DAMAGE:
                                    boardUnit.BuffedDamage += revert ? -Value : Value;
                                    boardUnit.CurrentDamage += revert ? -Value : Value;
                                    break;
                                case Enumerators.Stat.DEFENSE:
                                    boardUnit.BuffedDefense += revert ? -Value : Value;
                                    boardUnit.CurrentDefense += revert ? -Value : Value;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(StatType), StatType, null);
                            }

                            _canBeReverted = !revert;

                            CreateVfx(BattlegroundController.GetBoardUnitViewByModel<BoardUnitView>(boardUnit).Transform.position);

                            if (AbilityTrigger == Enumerators.AbilityTrigger.ENTRY)
                            {
                                InvokeUseAbilityEvent(
                                    new List<ParametrizedAbilityBoardObject>
                                    {
                                        new ParametrizedAbilityBoardObject(boardUnit)
                                    }
                                );
                            }
                        }
                    }
                    break;
            }
        }
    }
}
