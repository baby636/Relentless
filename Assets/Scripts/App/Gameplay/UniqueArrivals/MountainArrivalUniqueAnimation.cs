using UnityEngine;
using System;
using Loom.ZombieBattleground.Helpers;
using Object = UnityEngine.Object;

namespace Loom.ZombieBattleground
{
    public class MountainArrivalUniqueAnimation : UniqueAnimation
    {
        public override void Play(IBoardObject boardObject, Action startGeneralArrivalCallback, Action endArrivalCallback)
        {
            startGeneralArrivalCallback?.Invoke();

            IsPlaying = true;

            Vector3 offset = new Vector3(-0.78f, 2.0f, 0f);

            const float delayBeforeSpawn = 4f;

            BoardUnitView unitView = BattlegroundController.GetCardViewByModel<BoardUnitView>(boardObject as CardModel);

            unitView.GameObject.SetActive(false);

            GameObject animationVFX = Object.Instantiate(LoadObjectsManager.GetObjectByPath<GameObject>(
                                                        "Prefabs/VFX/UniqueArrivalAnimations/Mountain_Arrival"));

            Transform cameraVFXObj = animationVFX.transform.Find("!! Camera shake");

            Transform cameraGroupTransform = CameraManager.GetGameplayCameras();
            cameraGroupTransform.SetParent(cameraVFXObj);

            //PlaySound("CZB_AUD_Cherno_Bill_Arrival_F1_EXP");

            animationVFX.transform.position = unitView.PositionOfBoard + offset;

            Vector3 cameraLocalPosition = animationVFX.transform.position * -1;
            cameraGroupTransform.localPosition = cameraLocalPosition;

            InternalTools.DoActionDelayed(() =>
            {
                unitView.GameObject.SetActive(true);
                unitView.battleframeAnimator.Play(0, -1, 1);

                cameraGroupTransform.SetParent(null);
                cameraGroupTransform.position = Vector3.zero;
                Object.Destroy(animationVFX);

                endArrivalCallback?.Invoke();

                BoardController.UpdateCurrentBoardOfPlayer(unitView.Model.OwnerPlayer, null);

                IsPlaying = false;

            }, delayBeforeSpawn);
        }
    }
}
