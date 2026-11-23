using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Tutorial
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class TutorialFocusTarget : MonoBehaviour
    {
        private Canvas _canvas;
        private int _sortingLayerId;
        private int _sortingOrder;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _sortingLayerId = _canvas.sortingLayerID;
            _sortingOrder = _canvas.sortingOrder;
        }

        public void Focus(int sortingLayerId, int sortingOrder)
        {
            _canvas.overrideSorting = true;
            _canvas.sortingLayerID = sortingLayerId;
            _canvas.sortingOrder = sortingOrder;
        }

        public void Release()
        {
            _canvas.overrideSorting = false;
            _canvas.sortingLayerID = _sortingLayerId;
            _canvas.sortingOrder = _sortingOrder;
        }
    }
}
