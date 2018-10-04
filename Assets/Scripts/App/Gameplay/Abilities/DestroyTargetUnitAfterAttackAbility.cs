using System;
using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;

namespace Loom.ZombieBattleground
{
    public class DestroyTargetUnitAfterAttackAbility : AbilityBase
    {
        public DestroyTargetUnitAfterAttackAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            switch (info)
            {
                case BoardUnitModel boardUnitModel:
                    BattlegroundController.DestroyBoardUnit(boardUnitModel);
                    break;
                case Player player:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(info), info, null);
            }
        }

        protected override void UnitAttackedHandler(BoardObject from, int damage, bool isAttacker)
        {
            base.UnitAttackedHandler(from, damage, isAttacker);

            if (AbilityCallType != Enumerators.AbilityCallType.ATTACK || !isAttacker)
                return;

            Action(from);

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>()
            {
                from
            }, AbilityData.AbilityType, Protobuf.AffectObjectType.Character);
        }
    }
}
