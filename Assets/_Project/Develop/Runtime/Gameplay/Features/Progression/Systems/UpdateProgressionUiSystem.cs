using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Unity.Toolkit;
using Cysharp.Text;
using _Project.Gameplay.Features.Death;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Spear;
using _Project.Utilities;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace _Project.Gameplay.Features.Progression.Systems
{
    public sealed class UpdateProgressionUiSystem : UnitySystemBase
    {
        private const string HiddenClass = "is-hidden";
        private const string PortraitClass = "portrait";
        private const string CompactLandscapeClass = "compact-landscape";
        private const float CompactLandscapeHeight = 600f;

        private static readonly QueryDescription _description =
            new QueryDescription()
                .WithAll<PlayerTag, CharacterProgress, UpgradeRanks>()
                .WithNone<DeadTag>();

        private readonly ProgressionConfig _config;
        private readonly UpgradeOfferGenerator _offerGenerator;
        private readonly IUpgradeAvailabilityService _availability;
        private readonly IGameplayPauseService _pauseService;
        private readonly List<UpgradeCardDefinition> _offers = new();
        private readonly Button[] _buttons = new Button[3];
        private readonly Image[] _icons = new Image[3];
        private readonly Label[] _titles = new Label[3];
        private readonly Label[] _descriptions = new Label[3];
        private readonly Label[] _ranks = new Label[3];

        private GameObject _documentObject;
        private VisualElement _root;
        private VisualElement _selectionOverlay;
        private Label _levelLabel;
        private ProgressBar _experienceBar;
        private IDisposable _pauseLease;
        private Entity _activePlayer;
        private bool _isEnabled;
        private bool _isSelectionVisible;
        private bool _isAwaitingApplication;
        private int _pendingBeforeSubmission;
        private int _lastLevel = -1;
        private int _lastExperience = -1;
        private int _lastRequired = -1;

        public UpdateProgressionUiSystem(
            World world,
            ProgressionConfig config,
            UpgradeOfferGenerator offerGenerator,
            IUpgradeAvailabilityService availability,
            IGameplayPauseService pauseService) : base(world)
        {
            _config = config;
            _offerGenerator = offerGenerator;
            _availability = availability;
            _pauseService = pauseService;
        }

        public override void Initialize()
        {
            if (!_config.TryValidate(out var error))
            {
                Debug.LogError($"Progression feature is disabled: {error}");
                return;
            }

            _documentObject = new GameObject("Progression UI Document");
            _documentObject.SetActive(false);
            var document = _documentObject.AddComponent<UIDocument>();
            document.panelSettings = _config.PanelSettings;
            document.visualTreeAsset = _config.VisualTree;
            document.sortingOrder = 100;
            _documentObject.SetActive(true);

            _root = document.rootVisualElement;
            _selectionOverlay = Require<VisualElement>("selectionOverlay");
            _levelLabel = Require<Label>("levelLabel");
            _experienceBar = Require<ProgressBar>("experienceBar");

            for (var index = 0; index < _buttons.Length; index++)
            {
                var cardNumber = index + 1;
                _buttons[index] = Require<Button>($"card{cardNumber}Button");
                _icons[index] = Require<Image>($"card{cardNumber}Icon");
                _icons[index].scaleMode = ScaleMode.ScaleToFit;
                _titles[index] = Require<Label>($"card{cardNumber}Title");
                _descriptions[index] = Require<Label>($"card{cardNumber}Description");
                _ranks[index] = Require<Label>($"card{cardNumber}Rank");
            }

            _buttons[0].clicked += SelectFirst;
            _buttons[1].clicked += SelectSecond;
            _buttons[2].clicked += SelectThird;
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _selectionOverlay.AddToClassList(HiddenClass);
            _isEnabled = true;
        }

        public override void Update(in SystemState state)
        {
            if (!_isEnabled)
            {
                return;
            }

            var foundPlayer = false;
            World.Query(in _description, (
                Entity entity,
                ref CharacterProgress progress,
                ref UpgradeRanks ranks) =>
            {
                if (foundPlayer)
                {
                    return;
                }

                foundPlayer = true;
                _activePlayer = entity;
                UpdateHud(in progress);
                UpdateSelection(entity, ref progress, in ranks);
            });

            if (!foundPlayer)
            {
                CloseSelection();
            }
        }

        public override void Dispose()
        {
            CloseSelection();
            if (_root != null)
            {
                _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            }

            if (_buttons[0] != null) _buttons[0].clicked -= SelectFirst;
            if (_buttons[1] != null) _buttons[1].clicked -= SelectSecond;
            if (_buttons[2] != null) _buttons[2].clicked -= SelectThird;

            if (_documentObject != null)
            {
                Object.Destroy(_documentObject);
            }

            base.Dispose();
        }

        private void UpdateHud(in CharacterProgress progress)
        {
            if (_lastLevel == progress.Level &&
                _lastExperience == progress.Experience &&
                _lastRequired == progress.RequiredExperience)
            {
                return;
            }

            _lastLevel = progress.Level;
            _lastExperience = progress.Experience;
            _lastRequired = progress.RequiredExperience;
            _levelLabel.text = ZString.Format("LEVEL {0}", progress.Level);

            if (progress.Level >= _availability.MaximumRunLevel)
            {
                _experienceBar.lowValue = 0f;
                _experienceBar.highValue = 1f;
                _experienceBar.value = 1f;
                _experienceBar.title = "MAX";
                return;
            }

            _experienceBar.lowValue = 0f;
            _experienceBar.highValue = progress.RequiredExperience;
            _experienceBar.value = progress.Experience;
            _experienceBar.title = ZString.Format(
                "{0} / {1} XP",
                progress.Experience,
                progress.RequiredExperience);
        }

        private void UpdateSelection(
            Entity player,
            ref CharacterProgress progress,
            in UpgradeRanks ranks)
        {
            if (progress.PendingSelections <= 0)
            {
                CloseSelection();
                return;
            }

            if (_isAwaitingApplication)
            {
                if (progress.PendingSelections >= _pendingBeforeSubmission)
                {
                    return;
                }

                _isAwaitingApplication = false;
                BindOffers(player, ref progress, in ranks);
                return;
            }

            if (!_isSelectionVisible)
            {
                _pauseLease ??= _pauseService.Acquire("Level-up selection");
                _selectionOverlay.RemoveFromClassList(HiddenClass);
                _isSelectionVisible = true;
                BindOffers(player, ref progress, in ranks);
            }
        }

        private void BindOffers(
            Entity player,
            ref CharacterProgress progress,
            in UpgradeRanks ranks)
        {
            if (progress.PendingSelections <= 0)
            {
                CloseSelection();
                return;
            }

            _offerGenerator.Generate(
                in ranks,
                World.Has<SpearAbility>(player),
                _offers);
            if (_offers.Count == 0)
            {
                Debug.LogError("No eligible upgrades remain for a pending level-up selection.");
                progress.PendingSelections = 0;
                CloseSelection();
                return;
            }

            for (var index = 0; index < _buttons.Length; index++)
            {
                var isVisible = index < _offers.Count;
                _buttons[index].EnableInClassList(HiddenClass, !isVisible);
                _buttons[index].SetEnabled(isVisible);
                if (!isVisible)
                {
                    continue;
                }

                var card = _offers[index];
                var currentRank = UpgradeRules.GetRank(in ranks, card.Id);
                _icons[index].sprite = card.Icon;
                _titles[index].text = card.Title;
                _descriptions[index].text = card.Description;
                _ranks[index].text = card.Id == UpgradeId.Spear
                    ? "NEW"
                    : ZString.Format(
                        "RANK {0}  →  {1}",
                        currentRank,
                        currentRank + 1);
            }
        }

        private void Select(int index)
        {
            if (!_isSelectionVisible ||
                _isAwaitingApplication ||
                index < 0 ||
                index >= _offers.Count ||
                !World.IsAlive(_activePlayer) ||
                !World.Has<CharacterProgress>(_activePlayer))
            {
                return;
            }

            _isAwaitingApplication = true;
            _pendingBeforeSubmission =
                World.Get<CharacterProgress>(_activePlayer).PendingSelections;
            for (var buttonIndex = 0; buttonIndex < _buttons.Length; buttonIndex++)
            {
                _buttons[buttonIndex].SetEnabled(false);
            }

            World.Create(new ApplyUpgradeRequest
            {
                Target = _activePlayer,
                Upgrade = _offers[index].Id
            });
        }

        private void CloseSelection()
        {
            if (_selectionOverlay != null)
            {
                _selectionOverlay.AddToClassList(HiddenClass);
            }

            _offers.Clear();
            _isSelectionVisible = false;
            _isAwaitingApplication = false;
            _pauseLease?.Dispose();
            _pauseLease = null;
        }

        private T Require<T>(string name) where T : VisualElement
        {
            var element = _root.Q<T>(name);
            if (element == null)
            {
                throw new InvalidOperationException(
                    $"Progression UI is missing required {typeof(T).Name} '{name}'.");
            }

            return element;
        }

        private void OnGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            var isPortrait = geometryEvent.newRect.width < geometryEvent.newRect.height;
            _root.EnableInClassList(
                PortraitClass,
                isPortrait);
            _root.EnableInClassList(
                CompactLandscapeClass,
                !isPortrait && geometryEvent.newRect.height < CompactLandscapeHeight);
        }

        private void SelectFirst() => Select(0);
        private void SelectSecond() => Select(1);
        private void SelectThird() => Select(2);
    }
}
