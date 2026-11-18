using UnityEngine;

namespace _Project.Features.Garage
{
    public class RecycleGearsView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _gear1;
        [SerializeField] private SpriteRenderer _gear2;
        [SerializeField] private float _degreesPerSecond = 160f;
        [SerializeField] private float _snapAngle = 60f;
        [SerializeField] private Color _activeColor = new Color(1f, 0.251f, 0.271f, 1f);

        private float _initialAngle1;
        private float _initialAngle2;
        private float _progress;
        private bool _isSpinning;

        private void Awake()
        {
            _initialAngle1 = _gear1.transform.localEulerAngles.z;
            _initialAngle2 = _gear2.transform.localEulerAngles.z;
        }

        private void Update()
        {
            if (_isSpinning == false)
            {
                return;
            }

            _progress += _degreesPerSecond * Time.deltaTime;

            if (_progress >= _snapAngle)
            {
                _progress = 0f;
            }

            ApplyRotation();
        }

        public void SetSpinning(bool isSpinning)
        {
            if (_isSpinning == isSpinning)
            {
                return;
            }

            _isSpinning = isSpinning;

            if (isSpinning)
            {
                _gear1.color = _activeColor;
                _gear2.color = _activeColor;

                return;
            }

            _progress = 0f;
            _gear1.color = Color.white;
            _gear2.color = Color.white;
            ApplyRotation();
        }

        private void ApplyRotation()
        {
            SetZAngle(_gear1.transform, _initialAngle1 + _progress);
            SetZAngle(_gear2.transform, _initialAngle2 - _progress);
        }

        private void SetZAngle(Transform gear, float angle)
        {
            Vector3 euler = gear.localEulerAngles;
            gear.localEulerAngles = new Vector3(euler.x, euler.y, angle);
        }
    }
}
