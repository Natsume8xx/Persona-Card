using NUnit.Framework;

namespace PersonaCards.Tests.PlayMode
{
    public sealed class ProjectSkeletonPlayModeTests
    {
        [Test]
        public void PresentationAssemblyIsAvailable()
        {
            Assert.That(typeof(PersonaCards.UI.AssemblyMarker), Is.Not.Null);
        }
    }
}
