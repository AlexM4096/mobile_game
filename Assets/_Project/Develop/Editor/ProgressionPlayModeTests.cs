using System.Collections;
using Arch.Core;
using Arch.Unity.Conversion;
using NUnit.Framework;
using _Project.Gameplay.Features.Player;
using _Project.Gameplay.Features.Progression;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace _Project.Editor.Tests
{
    public sealed class ProgressionPlayModeTests
    {
        private static readonly QueryDescription PlayerDescription =
            new QueryDescription().WithAll<PlayerTag, CharacterProgress, UpgradeRanks>();

        [UnityTest]
        public IEnumerator LevelUpPausesShowsSelectionAppliesUpgradeAndResumes()
        {
            EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/MovementSwarmTestScene.unity",
                OpenSceneMode.Single);

            yield return new EnterPlayMode();
            yield return null;

            var world = EntityConversion.DefaultWorld;
            var player = default(Entity);
            world.Query(in PlayerDescription, (
                Entity entity,
                ref CharacterProgress _,
                ref UpgradeRanks __) => player = entity);
            Assert.That(world.IsAlive(player), Is.True);

            world.Create(new GrantExperienceRequest { Target = player, Amount = 2 });
            yield return null;

            var documentObject = GameObject.Find("Progression UI Document");
            Assert.That(documentObject, Is.Not.Null);
            var document = documentObject.GetComponent<UIDocument>();
            var root = document.rootVisualElement;
            var screenRoot = root.Q<VisualElement>("progressionRoot");
            var experienceBar = root.Q<ProgressBar>("experienceBar");
            var experienceTrack = experienceBar.Q<VisualElement>(
                className: "unity-progress-bar__background");
            var experienceFill = experienceBar.Q<VisualElement>(
                className: "unity-progress-bar__progress");
            Assert.That(experienceBar.title, Is.EqualTo("2 / 5 XP"));
            Assert.That(experienceTrack.layout.width, Is.GreaterThan(0f));
            Assert.That(experienceTrack.layout.height, Is.GreaterThan(0f));
            Assert.That(experienceFill.layout.width, Is.GreaterThan(0f));
            Assert.That(experienceFill.layout.width, Is.LessThan(experienceTrack.layout.width));
            Assert.That(
                experienceFill.layout.width / experienceTrack.layout.width,
                Is.EqualTo(0.4f).Within(0.08f));

            world.Create(new GrantExperienceRequest { Target = player, Amount = 3 });
            yield return null;

            var levelLabel = root.Q<Label>("levelLabel");
            Assert.That(levelLabel.text, Is.EqualTo("LEVEL 2"));
            Assert.That(
                levelLabel.resolvedStyle.unityFontDefinition.font,
                Is.Not.Null,
                "The progression UI must resolve a runtime font.");

            var overlay = root.Q<VisualElement>("selectionOverlay");
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(overlay.ClassListContains("is-hidden"), Is.False);
            Assert.That(root.Q<Label>("card1Title").text, Is.Not.Empty);
            Assert.That(root.Q<Label>("card1Description").text, Is.Not.Empty);
            Assert.That(root.Q<Label>("card1Rank").text, Is.Not.Empty);
            var cardsBounds = root.Q<Button>("card1Button").worldBound;
            cardsBounds.xMin = Mathf.Min(
                cardsBounds.xMin,
                root.Q<Button>("card2Button").worldBound.xMin);
            cardsBounds.xMax = Mathf.Max(
                cardsBounds.xMax,
                root.Q<Button>("card3Button").worldBound.xMax);
            Assert.That(
                cardsBounds.center.x,
                Is.EqualTo(screenRoot.worldBound.center.x).Within(2f));
            Assert.That(overlay.resolvedStyle.alignItems, Is.EqualTo(Align.Center));
            Assert.That(overlay.resolvedStyle.justifyContent, Is.EqualTo(Justify.Center));
            Assert.That(
                root.Q<VisualElement>("cardsContainer").resolvedStyle.justifyContent,
                Is.EqualTo(Justify.Center));
            Assert.That(
                world.Get<CharacterProgress>(player).PendingSelections,
                Is.EqualTo(1));

            world.Create(new ApplyUpgradeRequest
            {
                Target = player,
                Upgrade = UpgradeId.AdditionalBlade
            });
            yield return null;

            Assert.That(world.Get<UpgradeRanks>(player).AdditionalBlade, Is.EqualTo(1));
            Assert.That(world.Get<CharacterProgress>(player).PendingSelections, Is.Zero);
            Assert.That(overlay.ClassListContains("is-hidden"), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            yield return new ExitPlayMode();
        }
    }
}
