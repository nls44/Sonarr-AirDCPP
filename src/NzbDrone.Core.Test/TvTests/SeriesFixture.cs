using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.TvTests
{
    [TestFixture]
    public class SeriesFixture
    {
        [Test]
        public void should_not_create_a_custom_title_when_one_is_not_provided()
        {
            var series = Builder<Series>.CreateNew()
                                        .With(s => s.Title = "Canonical Title")
                                        .Build();

            series.CustomTitle.Should().BeNull();
            series.Title.Should().Be("Canonical Title");
        }

        [Test]
        public void should_use_custom_title_when_one_is_provided()
        {
            var series = Builder<Series>.CreateNew()
                                        .With(s => s.Title = "Canonical Title")
                                        .With(s => s.CustomTitle = "Custom Title")
                                        .Build();

            series.Title.Should().Be("Custom Title");
        }
    }
}
