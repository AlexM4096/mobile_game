using NUnit.Framework;
using _Project.Utilities;
using UnityEngine;

namespace _Project.Editor.Tests
{
    public sealed class GameplayPauseServiceTests
    {
        private float _originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 0.75f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;
        }

        [Test]
        public void NestedLeasesRestorePreviousScaleAfterLastRelease()
        {
            var service = new GameplayPauseService();
            var first = service.Acquire("First");
            var second = service.Acquire("Second");

            Assert.That(service.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            first.Dispose();
            first.Dispose();
            Assert.That(service.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);

            second.Dispose();
            Assert.That(service.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0.75f));
        }

        [Test]
        public void DisposingServiceRestoresScaleAndDetachesLeases()
        {
            var service = new GameplayPauseService();
            var lease = service.Acquire("Selection");

            service.Dispose();
            lease.Dispose();

            Assert.That(service.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0.75f));
            Assert.Throws<System.ObjectDisposedException>(() => service.Acquire("Later"));
        }
    }
}
