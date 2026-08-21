using FizzWare.NBuilder;
using NUnit.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test
{
    [SetUpFixture]
    public class NBuilderSetup
    {
        [OneTimeSetUp]
        public void Configure()
        {
            BuilderSetup.DisablePropertyNamingFor<Series, string>(series => series.CustomTitle);
        }
    }
}
