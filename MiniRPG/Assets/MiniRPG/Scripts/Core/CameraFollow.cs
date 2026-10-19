using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 13f, -9.5f);
        Vector3 vel;
        float shakeAmt, shakeTime;

        public void Snap()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(-offset.normalized + Vector3.up * 0.0f);
        }

        public void Shake(float amount, float time)
        {
            shakeAmt = Mathf.Max(shakeAmt, amount);
            shakeTime = Mathf.Max(shakeTime, time);
        }

        void LateUpdate()
        {
            if (target == null) return;
            var desired = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, 0.12f, Mathf.Infinity, Time.unscaledDeltaTime);
            transform.rotation = Quaternion.LookRotation((target.position + Vector3.up * 1f) - (target.position + offset));
            if (shakeTime > 0f)
            {
                shakeTime -= Time.unscaledDeltaTime;
                transform.position += Random.insideUnitSphere * shakeAmt;
                if (shakeTime <= 0f) shakeAmt = 0f;
            }
        }
    }
}
