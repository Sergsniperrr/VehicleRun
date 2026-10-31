using System;
using System.Collections.Generic;
using _Project.Core.Configs;
using _Project.Core.Economy;
using _Project.Core.Garage;
using _Project.Core.Localization;
using _Project.Core.Track;
using _Project.Core.Vehicles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Tests.EditMode.Configs
{
    public class GameConfigFixture : IDisposable
    {
        private static readonly long[] Incomes = { 1, 2, 5, 11, 23, 52, 113, 249 };
        private static readonly (int Rank, int Capacity)[] GarageSteps = { (3, 9), (5, 12), (7, 15) };
        private static readonly (int Rank, int Capacity)[] TrackSteps = { (4, 4), (8, 6) };

        private const int Version = 1;
        private const long StartCoins = 60;
        private const int StartVehicleRank = 1;
        private const int StartVehicleCount = 2;
        private const int BoxRankLag = 4;
        private const double BoxBasePriceIncomeSeconds = 12;
        private const double BoxPriceGrowth = 1.04;
        private const int BoxPriceGrowthPurchaseLimit = 20;
        private const float FreeBoxIntervalSeconds = 20f;
        private const float OfflineMinAwaySeconds = 60f;
        private const float OfflineMaxAwaySeconds = 14400f;
        private const double OfflineIncomeRate = 0.5;
        private const int GarageColumns = 3;
        private const int GarageRows = 5;
        private const int GarageInitialOpenSlots = 6;
        private const float LapDurationSeconds = 12f;
        private const float StraightHalfLength = 3f;
        private const float TurnRadius = 4.4f;
        private const int TrackInitialCapacity = 2;
        private const float SpritePixelsPerUnit = 100f;

        private readonly List<Object> _createdObjects = new List<Object>();

        public GameConfigFixture()
        {
            Localization = Create<LocalizationConfig>();
            Vehicles = Create<VehicleCatalogConfig>();
            Economy = Create<EconomyConfig>();
            Garage = Create<GarageConfig>();
            Track = Create<TrackConfig>();
            Config = Create<GameConfig>();

            FillLocalization();
            FillVehicles();
            FillEconomy();
            FillGarage();
            FillTrack();
            FillConfig();
        }

        public LocalizationConfig Localization { get; }
        public VehicleCatalogConfig Vehicles { get; }
        public EconomyConfig Economy { get; }
        public GarageConfig Garage { get; }
        public TrackConfig Track { get; }
        public GameConfig Config { get; }

        public void Change(Object target, string propertyPath, Action<SerializedProperty> change)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                throw new ArgumentException(nameof(propertyPath));
            }

            change(property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Dispose()
        {
            foreach (Object createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _createdObjects.Add(instance);

            return instance;
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = Texture2D.whiteTexture;
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, Vector2.zero, SpritePixelsPerUnit);
            _createdObjects.Add(sprite);

            return sprite;
        }

        private string GetNameKey(int index)
        {
            return $"vehicle.test_{index}.name";
        }

        private void FillLocalization()
        {
            SerializedObject localization = new SerializedObject(Localization);
            SerializedProperty entries = localization.FindProperty("_entries");
            entries.arraySize = Incomes.Length;

            for (int i = 0; i < Incomes.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_key").stringValue = GetNameKey(i);
                entry.FindPropertyRelative("_russian").stringValue = $"Машина {i}";
                entry.FindPropertyRelative("_english").stringValue = $"Vehicle {i}";
            }

            localization.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillVehicles()
        {
            SerializedObject catalog = new SerializedObject(Vehicles);
            SerializedProperty vehicles = catalog.FindProperty("_vehicles");
            vehicles.arraySize = Incomes.Length;

            for (int i = 0; i < Incomes.Length; i++)
            {
                SerializedProperty vehicle = vehicles.GetArrayElementAtIndex(i);
                vehicle.FindPropertyRelative("_id").stringValue = $"test_{i}";
                vehicle.FindPropertyRelative("_rank").intValue = i + 1;
                vehicle.FindPropertyRelative("_nameKey").stringValue = GetNameKey(i);
                vehicle.FindPropertyRelative("_incomePerSecond").longValue = Incomes[i];
                vehicle.FindPropertyRelative("_garageSprite").objectReferenceValue = CreateSprite();
                vehicle.FindPropertyRelative("_trackSprite").objectReferenceValue = CreateSprite();
            }

            catalog.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillEconomy()
        {
            SerializedObject economy = new SerializedObject(Economy);
            economy.FindProperty("_startCoins").longValue = StartCoins;
            economy.FindProperty("_startVehicleRank").intValue = StartVehicleRank;
            economy.FindProperty("_startVehicleCount").intValue = StartVehicleCount;
            economy.FindProperty("_boxRankLag").intValue = BoxRankLag;
            economy.FindProperty("_boxBasePriceIncomeSeconds").doubleValue = BoxBasePriceIncomeSeconds;
            economy.FindProperty("_boxPriceGrowth").doubleValue = BoxPriceGrowth;
            economy.FindProperty("_boxPriceGrowthPurchaseLimit").intValue = BoxPriceGrowthPurchaseLimit;
            economy.FindProperty("_freeBoxIntervalSeconds").floatValue = FreeBoxIntervalSeconds;
            economy.FindProperty("_offlineMinAwaySeconds").floatValue = OfflineMinAwaySeconds;
            economy.FindProperty("_offlineMaxAwaySeconds").floatValue = OfflineMaxAwaySeconds;
            economy.FindProperty("_offlineIncomeRate").doubleValue = OfflineIncomeRate;
            economy.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillGarage()
        {
            SerializedObject garage = new SerializedObject(Garage);
            garage.FindProperty("_columns").intValue = GarageColumns;
            garage.FindProperty("_rows").intValue = GarageRows;
            FillSchedule(garage.FindProperty("_openSlots"), GarageInitialOpenSlots, GarageSteps);
            garage.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillTrack()
        {
            SerializedObject track = new SerializedObject(Track);
            track.FindProperty("_lapDurationSeconds").floatValue = LapDurationSeconds;
            track.FindProperty("_straightHalfLength").floatValue = StraightHalfLength;
            track.FindProperty("_turnRadius").floatValue = TurnRadius;
            FillSchedule(track.FindProperty("_capacity"), TrackInitialCapacity, TrackSteps);
            track.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillSchedule(SerializedProperty schedule, int initial, (int Rank, int Capacity)[] steps)
        {
            schedule.FindPropertyRelative("_initial").intValue = initial;

            SerializedProperty stepsProperty = schedule.FindPropertyRelative("_steps");
            stepsProperty.arraySize = steps.Length;

            for (int i = 0; i < steps.Length; i++)
            {
                SerializedProperty step = stepsProperty.GetArrayElementAtIndex(i);
                step.FindPropertyRelative("_rank").intValue = steps[i].Rank;
                step.FindPropertyRelative("_capacity").intValue = steps[i].Capacity;
            }
        }

        private void FillConfig()
        {
            SerializedObject config = new SerializedObject(Config);
            config.FindProperty("_version").intValue = Version;
            config.FindProperty("_vehicles").objectReferenceValue = Vehicles;
            config.FindProperty("_economy").objectReferenceValue = Economy;
            config.FindProperty("_garage").objectReferenceValue = Garage;
            config.FindProperty("_track").objectReferenceValue = Track;
            config.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
