using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Project.UI.Tutorial
{
    public class TutorialWorldFocus
    {
        private readonly List<SortingGroup> _groups = new List<SortingGroup>();
        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private readonly List<Component> _sorted = new List<Component>();
        private readonly List<int> _layers = new List<int>();
        private readonly List<int> _orders = new List<int>();

        private Transform _target;

        public Transform Target => _target;

        public void Focus(Transform target, int sortingLayerId, int sortingOrder)
        {
            if (_target == target)
            {
                return;
            }

            Release();
            _target = target;
            Collect(target);

            for (int i = 0; i < _sorted.Count; i++)
            {
                _layers.Add(GetLayerId(_sorted[i]));
                _orders.Add(GetOrder(_sorted[i]));
                Apply(_sorted[i], sortingLayerId, sortingOrder + i);
            }
        }

        public void Release()
        {
            for (int i = 0; i < _sorted.Count; i++)
            {
                if (_sorted[i] == null)
                {
                    continue;
                }

                Apply(_sorted[i], _layers[i], _orders[i]);
            }

            _sorted.Clear();
            _layers.Clear();
            _orders.Clear();
            _target = null;
        }

        private void Collect(Transform target)
        {
            target.GetComponentsInChildren(true, _groups);
            target.GetComponentsInChildren(true, _renderers);
            _sorted.AddRange(_groups);

            foreach (SpriteRenderer renderer in _renderers)
            {
                if (HasGroupAbove(renderer.transform, target) == false)
                {
                    _sorted.Add(renderer);
                }
            }

            _sorted.Sort(CompareDrawOrder);
        }

        private bool HasGroupAbove(Transform child, Transform target)
        {
            Transform current = child;

            while (current != null)
            {
                if (current.GetComponent<SortingGroup>() != null)
                {
                    return true;
                }

                if (current == target)
                {
                    return false;
                }

                current = current.parent;
            }

            return false;
        }

        private int CompareDrawOrder(Component left, Component right)
        {
            int leftLayer = SortingLayer.GetLayerValueFromID(GetLayerId(left));
            int rightLayer = SortingLayer.GetLayerValueFromID(GetLayerId(right));

            if (leftLayer != rightLayer)
            {
                return leftLayer.CompareTo(rightLayer);
            }

            return GetOrder(left).CompareTo(GetOrder(right));
        }

        private int GetLayerId(Component component)
        {
            if (component is SortingGroup group)
            {
                return group.sortingLayerID;
            }

            return ((SpriteRenderer)component).sortingLayerID;
        }

        private int GetOrder(Component component)
        {
            if (component is SortingGroup group)
            {
                return group.sortingOrder;
            }

            return ((SpriteRenderer)component).sortingOrder;
        }

        private void Apply(Component component, int sortingLayerId, int sortingOrder)
        {
            if (component is SortingGroup group)
            {
                group.sortingLayerID = sortingLayerId;
                group.sortingOrder = sortingOrder;

                return;
            }

            SpriteRenderer renderer = (SpriteRenderer)component;
            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
