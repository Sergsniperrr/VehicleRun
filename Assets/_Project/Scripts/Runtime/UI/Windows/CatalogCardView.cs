using System;
using _Project.Core.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Windows
{
    public class CatalogCardView : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _rankLabel;
        [SerializeField] private TMP_Text _caption;
        [SerializeField] private TMP_Text _income;

        public int Rank { get; private set; }

        public void ShowVehicle(VehicleDefinition definition, string vehicleName, string income)
        {
            ApplyRank(definition);
            _icon.color = Color.white;
            _caption.text = vehicleName;
            _income.gameObject.SetActive(true);
            _income.text = income;
        }

        public void ShowSilhouette(VehicleDefinition definition, string lockedText)
        {
            ApplyRank(definition);
            _icon.color = Color.black;
            _caption.text = lockedText;
            _income.gameObject.SetActive(false);
        }

        private void ApplyRank(VehicleDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            Rank = definition.Rank;
            _rankLabel.text = Rank.ToString();
            _icon.sprite = definition.GarageSprite;
        }
    }
}
