using DG.Tweening;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class DamageTargetAbilityView : AbilityViewBase<DamageTargetAbility>
    {

        private const float DELAY_BEFORE_MOVE_SHROOM = 0.5f;

        private const float DELAY_BEFORE_DESTROY_MOVED_SHROOM = 2f;

        private const float DELAY_AFTER_IMPACT_SHROOM = 4.5f;

        private const float DELAY_BEFORE_DESTROY_IMPACT_SHROOM = 10f;

        private float _delayBeforeMove;

        private float _delayBeforeDestroyMoved;

        private float _delayAfterImpact;

        private float _delayBeforeDestroyImpact;

        private BattlegroundController _battlegroundController;

        public DamageTargetAbilityView(DamageTargetAbility ability) : base(ability)
        {
            _battlegroundController = GameClient.Get<IGameplayManager>().GetController<BattlegroundController>();
        }

        protected override void OnAbilityAction(object info = null)
        {
            SetDelays();

            if (Ability.AbilityData.HasVisualEffectType(Enumerators.VisualEffectType.Moving))
            {
                Vector3 targetPosition = Ability.AffectObjectType == Enumerators.AffectObjectType.Character ?
                _battlegroundController.GetBoardUnitViewByModel(Ability.TargetUnit).Transform.position :
                Ability.TargetPlayer.AvatarObject.transform.position;

                VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>(Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Moving).Path);

                VfxObject = Object.Instantiate(VfxObject);
                VfxObject.transform.position = _battlegroundController.GetBoardUnitViewByModel(Ability.AbilityUnitOwner).Transform.position;
                InternalTools.DoActionDelayed(() =>
                {
                    VfxObject.transform.DOMove(targetPosition, 0.5f).OnComplete(ActionCompleted);
                    ParticleIds.Add(ParticlesController.RegisterParticleSystem(VfxObject));
                }, _delayBeforeMove);
            }
            else
            {
                ActionCompleted();
            }
        }

        private void ActionCompleted()
        {
            InternalTools.DoActionDelayed(ClearParticles, _delayBeforeDestroyMoved);

            _delayBeforeMove = 0f;

            if (Ability.AbilityData.HasVisualEffectType(Enumerators.VisualEffectType.Impact))
            {
                Vector3 targetPosition = VfxObject.transform.position;

                VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>(Ability.AbilityData.GetVisualEffectByType(Enumerators.VisualEffectType.Impact).Path);
                CreateVfx(targetPosition, true, _delayBeforeDestroyImpact, true);
            }

            InternalTools.DoActionDelayed(Ability.InvokeVFXAnimationEnded, _delayAfterImpact);
        }


        protected override void CreateVfx(Vector3 pos, bool autoDestroy = false, float duration = 3, bool justPosition = false)
        {
            base.CreateVfx(pos, autoDestroy, duration, justPosition);
        }

        private void SetDelays()
        {
            _delayBeforeMove = 0;
            _delayAfterImpact = 0;
            _delayBeforeDestroyImpact = 0;
            _delayBeforeDestroyMoved = 0;

            switch (Ability.AbilityEffectType)
            {
                case Enumerators.AbilityEffectType.TARGET_LIFE:
                    _delayBeforeMove = DELAY_BEFORE_MOVE_SHROOM;
                    _delayAfterImpact = DELAY_AFTER_IMPACT_SHROOM;
                    _delayBeforeDestroyImpact = DELAY_BEFORE_DESTROY_IMPACT_SHROOM;
                    _delayBeforeDestroyMoved = DELAY_BEFORE_DESTROY_MOVED_SHROOM;
                    break;
                default:
                    break;
            }
        }
    }
}
