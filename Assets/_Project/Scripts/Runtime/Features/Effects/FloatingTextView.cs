using System;
using TMPro;
using UnityEngine;

namespace _Project.Features.Effects
{
    [RequireComponent(typeof(TMP_Text))]
    public class FloatingTextView : MonoBehaviour
    {
        private const float OpaqueAlpha = 1f;

        [SerializeField] private float _riseHeight = 1f;
        [SerializeField] private float _lifetime = 0.9f;
        [SerializeField] private float _fadeStartTime = 0.5f;

        private TMP_Text _label;
        private Vector3 _startPosition;
        private float _age;

        public event Action<FloatingTextView> Finished;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float progress = Mathf.Clamp01(_age / _lifetime);
            transform.localPosition = _startPosition + Vector3.up * (_riseHeight * progress);
            _label.alpha = OpaqueAlpha - Mathf.InverseLerp(_fadeStartTime, _lifetime, _age);

            if (_age >= _lifetime)
            {
                gameObject.SetActive(false);
                Finished?.Invoke(this);
            }
        }

        public void Show(string text, Vector3 localPosition)
        {
            gameObject.SetActive(true);
            _label.text = text;
            _label.alpha = OpaqueAlpha;
            _startPosition = localPosition;
            _age = 0f;
            transform.localPosition = localPosition;
        }
    }
}
