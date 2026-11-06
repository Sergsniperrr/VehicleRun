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
    public class RecycleConfirmWindow : Window, IRecycleConfirmation
    {
        private const string VehicleQuestionKey = "popup.recycle.vehicle_question";
        private const string BoxQuestionKey = "popup.recycle.box_question";

        [SerializeField] private TMP_Text _question;
        [SerializeField] private Button _yesButton;
        [SerializeField] private Button _noButton;

        private ILocalizationService _localization;
        private UniTaskCompletionSource<bool> _answer;

        private bool IsAsking => _answer != null;

        protected override void Awake()
        {
            base.Awake();
            _yesButton.onClick.AddListener(OnYesClicked);
            _noButton.onClick.AddListener(OnNoClicked);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _yesButton.onClick.RemoveListener(OnYesClicked);
            _noButton.onClick.RemoveListener(OnNoClicked);
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

        public override void Dismiss()
        {
            Answer(false);
        }

        private async UniTask<bool> AskAsync(string question, CancellationToken cancellationToken)
        {
            if (IsAsking)
            {
                throw new InvalidOperationException(nameof(_answer));
            }

            _answer = new UniTaskCompletionSource<bool>();
            _question.text = question;
            Open();

            try
            {
                return await _answer.Task.AttachExternalCancellation(cancellationToken);
            }
            finally
            {
                _answer = null;

                if (this != null)
                {
                    Close();
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
