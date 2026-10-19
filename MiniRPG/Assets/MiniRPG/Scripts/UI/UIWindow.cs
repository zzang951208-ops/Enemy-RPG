using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniRPG
{
    /// <summary>모든 창의 공통 부모</summary>
    public abstract class UIWindow : MonoBehaviour
    {
        public RectTransform root;
        public bool IsOpen { get { return root != null && root.gameObject.activeSelf; } }
        public virtual bool CanClose { get { return true; } }

        protected void ShowRoot()
        {
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
        }

        public virtual void Close()
        {
            if (root != null && root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(false);
                GameManager.I.UI.MarkClosed();
            }
        }
    }
}
