using BoozeBlocks.Prototype;
using NUnit.Framework;
using UnityEngine;

namespace BoozeBlocks.Tests
{
    public sealed class StylizedGeometryTests
    {
        [Test]
        public void ChamferedCube_IsSharedLightweightAndKeepsUnitBounds()
        {
            Mesh first = StylizedGeometry.ChamferedCube;
            Mesh second = StylizedGeometry.ChamferedCube;

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.vertexCount, Is.EqualTo(96));
            Assert.That(first.GetIndexCount(0), Is.EqualTo(132));
            Assert.That(first.bounds.size.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(first.bounds.size.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(first.bounds.size.z, Is.EqualTo(1f).Within(0.001f));
        }
    }
}
