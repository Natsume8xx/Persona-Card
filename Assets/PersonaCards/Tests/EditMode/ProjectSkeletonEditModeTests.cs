using NUnit.Framework;

namespace PersonaCards.Tests.EditMode
{
    public sealed class ProjectSkeletonEditModeTests
    {
        [Test]
        public void RuntimeAssembliesAreAvailable()
        {
            Assert.That(typeof(PersonaCards.Core.AssemblyMarker), Is.Not.Null);
            Assert.That(typeof(PersonaCards.Data.AssemblyMarker), Is.Not.Null);
            Assert.That(typeof(PersonaCards.Cards.AssemblyMarker), Is.Not.Null);
            Assert.That(typeof(PersonaCards.Battle.AssemblyMarker), Is.Not.Null);
        }
    }
}
