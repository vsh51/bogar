using Bogar.BLL.Core;
using Xunit;

namespace Bogar.Tests.BLL.Core;

public class PositionTests
{

    [Fact]
    public void CanInstantiatePosition()
    {
        var pos = new Position();
        Assert.NotNull(pos);
    }
}
