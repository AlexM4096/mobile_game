using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace _Project.Editor.Tests
{
    public sealed class MovementSwarmPlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator MovementSwarmSceneEntersAndExitsPlayMode()
        {
            EditorSceneManager.OpenScene(
                "Assets/_Project/Scenes/MovementSwarmTestScene.unity",
                OpenSceneMode.Single);

            yield return new EnterPlayMode();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MovementSwarmTestScene"));

            yield return null;
            yield return null;
            yield return new ExitPlayMode();
        }
    }
}
