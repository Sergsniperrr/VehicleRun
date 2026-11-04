using System;
using System.Threading;
using _Project.Core.Localization;
using _Project.Features.Boxes;
using _Project.Features.Garage;
using _Project.Features.Vehicles;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.UI.Windows
{
    public class RecycleConfirmWindow : MonoBehaviour, IRecycleConfirmation
    {
        private const string VehicleQuestionKey = "popup.recycle.vehicle_question";
        private const string BoxQuestionKey = "popup.recycle.box_question";

        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _question;
        [SerializeField] private Button _yesButton;
        [SerializeField] private Button _noButton;
        [SerializeField] private Button _closeButton;

        private ILocalizationService _localization;
        private UniTaskCompletionSource<bool> _answer;

        private bool IsOpen => _answer != null;

        private void Awake()
        {
            _panel.SetActive(false);
            _yesButton.onClick.AddListener(OnYesClicked);
            _noButton.onClick.AddListener(OnNoClicked);
            _closeButton.onClick.AddListener(OnNoClicked);
        }

        private void Update()
        {
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Answer(false);
            }
        }

        private void OnDestroy()
        {
            _yesButton.onClick.RemoveListener(OnYesClicked);
            _noButton.onClick.RemoveListener(OnNoClicked);
            _closeButton.onClick.RemoveListener(OnNoClicked);
        }

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        public UniTask<bool> ConfirmVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            if (vehicle == null)
            {
                throw new ArgumentNullException(nameof(vehicle));
            }

            string vehicleName = _localization.Get(vehicle.Definition.NameKey);
            string question = string.Format(_localization.Get(VehicleQuestionKey), vehicleName);

            return AskAsync(question, cancellationToken);
        }

        public UniTask<bool> ConfirmBoxAsync(Box box, CancellationToken cancellationToken)
        {
            if (box == null)
            {
                throw new ArgumentNullException(nameof(box));
            }

            string question = string.Format(_localization.Get(BoxQuestionKey), box.Rank);

            return AskAsync(question, cancellationToken);
        }

        private async UniTask<bool> AskAsync(string question, CancellationToken cancellationToken)
        {
            if (IsOpen)
            {
                throw new InvalidOperationException(nameof(_answer));
            }

            _answer = new UniTaskCompletionSource<bool>();
            _question.text = question;
            _panel.SetActive(true);

            try
            {
                return await _answer.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                _answer = null;

                if (_panel != null)
                {
                    _panel.SetActive(false);
                }
            }
        }

        private void Answer(bool isConfirmed)
        {
            _answer?.TrySetResult(isConfirmed);
        }

        private void OnYesClicked()
        {
            Answer(true);
        }

        private void OnNoClicked()
        {
            Answer(false);
        }
    }
}
