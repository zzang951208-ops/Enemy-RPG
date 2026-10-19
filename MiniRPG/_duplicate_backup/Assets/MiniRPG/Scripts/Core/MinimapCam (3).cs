using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>미니맵 카메라: 플레이어 위에서 내려다봄</summary>
    public class MinimapCam : MonoBehaviour
    {
        public Transform target;
        void LateUpdate()
        {
            if (target == null) return;
            transform.position = new Vector3(target.position.x, 60f, target.position.z);
            transform.rotation = Quaternion.Euler(90f, 0, 0);
        }
    }
}
