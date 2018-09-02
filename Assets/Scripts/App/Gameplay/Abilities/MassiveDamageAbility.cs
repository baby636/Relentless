using LoomNetwork.CZB.Common;
using LoomNetwork.CZB.Data;
using LoomNetwork.Internal;
using UnityEngine;

namespace LoomNetwork.CZB
{
    public class MassiveDamageAbility : AbilityBase
    {
        public int Value = 1;

        public MassiveDamageAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Value = ability.Value;
        }

        public override void Activate()
        {
            base.Activate();
            if (AbilityCallType != Enumerators.AbilityCallType.Entry)
                return;

            Action();

            // _vfxObject = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/healVFX");
        }

        public override void Update()
        {
        }

        public override void Dispose()
        {
        }

        protected override void OnInputEndEventHandler()
        {
            base.OnInputEndEventHandler();
        }

        protected override void UnitOnDieEventHandler()
        {
            base.UnitOnDieEventHandler();
            if (AbilityCallType != Enumerators.AbilityCallType.Death)
                return;

            Debug.Log("CreatureOnDieEventHandler");
            Action();
        }

        protected override void CreateVfx(Vector3 pos, bool autoDestroy = false, float duration = 3f, bool justPosition = false)
        {
            int playerPos = PlayerCallerOfAbility.IsLocalPlayer?1:-1;

            switch (AbilityEffectType)
            {
                case Enumerators.AbilityEffectType.MassiveWaterWave:
                    VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Spells/ToxicMassiveAllVFX");
                    break;
                case Enumerators.AbilityEffectType.MassiveFire:
                    VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Spells/SpellMassiveFireVFX");
                    break;
                case Enumerators.AbilityEffectType.MassiveLightning:
                    VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Spells/LightningVFX");
                    pos = Vector3.up * 0.5f;
                    break;
                case Enumerators.AbilityEffectType.MassiveToxicAll:
                    VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Spells/ToxicMassiveAllVFX");
                    pos = Vector3.zero;
                    break;
            }

            pos = Utilites.CastVfxPosition(pos * playerPos);

            ClearParticles();

            base.CreateVfx(pos, true, 5f);
        }

        private void Action()
        {
            object caller = AbilityUnitOwner != null?AbilityUnitOwner:(object)BoardSpell;

            Player opponent = PlayerCallerOfAbility == GameplayManager.CurrentPlayer?GameplayManager.OpponentPlayer:GameplayManager.CurrentPlayer;
            foreach (Enumerators.AbilityTargetType target in AbilityTargetTypes)
            {
                switch (target)
                {
                    case Enumerators.AbilityTargetType.OpponentAllCards:

                        // BoardCreature[] creatures = new BoardCreature[playerCallerOfAbility.opponentBoardCardsList.Count];
                        // player.BoardCards.CopyTo(creatures);
                        foreach (BoardUnit cardOpponent in opponent.BoardCards)
                        {
                            BattleController.AttackUnitByAbility(caller, AbilityData, cardOpponent);
                        }

                        CreateVfx(Vector3.up * 1.5f);

                        // Array.Clear(creatures, 0, creatures.Length);
                        // creatures = null;
                        break;
                    case Enumerators.AbilityTargetType.PlayerAllCards:

                        // RuntimeCard[] cards = new RuntimeCard[playerCallerOfAbility.boardZone.cards.Count];
                        // playerCallerOfAbility.boardZone.cards.CopyTo(cards);
                        // foreach (var cardPlayer in cards)
                        // {
                        // playerCallerOfAbility.FightCreatureBySkill(value, cardPlayer);
                        // CreateVFX(cardPlayer.transform.position);
                        // }
                        // Array.Clear(cards, 0, cards.Length);
                        // cards = null;
                        foreach (BoardUnit cardPlayer in PlayerCallerOfAbility.BoardCards)
                        {
                            BattleController.AttackUnitByAbility(caller, AbilityData, cardPlayer);
                            CreateVfx(cardPlayer.Transform.position);
                        }

                        break;
                    case Enumerators.AbilityTargetType.Opponent:
                        BattleController.AttackPlayerByAbility(caller, AbilityData, opponent);

                        // CreateVFX(targetCreature.transform.position);
                        break;
                    case Enumerators.AbilityTargetType.Player:
                        BattleController.AttackPlayerByAbility(caller, AbilityData, PlayerCallerOfAbility);

                        // CreateVFX(targetCreature.transform.position);
                        break;
                }
            }
        }
    }
}
