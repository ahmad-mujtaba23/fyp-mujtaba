using DG.Tweening;
using UnityEngine;

namespace Osmosis
{
    /// The lab camera (task 4.9): a fixed bench view that tweens to a close-up when a step needs one
    /// (measuring on the ruler, watching a strip soak) and eases back. Lives on the Main Camera.
    // ponytail: no zoom buttons / joysticks (reference had them); auto-focus covers every close-up the
    // practical needs. Add manual controls only if device testing shows the bench view is too tight.
    public class CameraRig : MonoBehaviour
    {
        Vector3 homePos;
        Quaternion homeRot;

        public bool AtHome => (transform.position - homePos).sqrMagnitude < 1e-6f;

        void Awake()
        {
            homePos = transform.position;
            homeRot = transform.rotation;
        }

        /// Move to `focus + offset`, looking at `focus`.
        public void Focus(Vector3 focus, Vector3 offset, float seconds = 0.6f)
        {
            Vector3 pos = focus + offset;
            transform.DOKill();
            transform.DOMove(pos, seconds).SetEase(Ease.InOutSine);
            transform.DORotateQuaternion(Quaternion.LookRotation(focus - pos), seconds).SetEase(Ease.InOutSine);
        }

        public void Home(float seconds = 0.5f)
        {
            transform.DOKill();
            transform.DOMove(homePos, seconds).SetEase(Ease.InOutSine);
            transform.DORotateQuaternion(homeRot, seconds).SetEase(Ease.InOutSine);
        }
    }
}
