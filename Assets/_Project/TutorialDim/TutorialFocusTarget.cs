using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Runtime.UI.Tutorial
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class TutorialFocusTarget : MonoBehaviour
    {
        public void Focus(int sortingOrder)
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        public void Release()
        {
            GetComponent<Canvas>().overrideSorting = false;
        }
    }
}
