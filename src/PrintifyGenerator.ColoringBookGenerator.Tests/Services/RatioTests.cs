using PrintifyGenerator.ColoringBookGenerator.Services;

namespace PrintifyGenerator.ColoringBookGenerator.Tests.Services;

public class RatioTests
{
    [Fact] public void Ratio_4_3_Exists() =>
        Assert.Equal(0, (int)Ratio.ratio_4_3);

    [Fact] public void Ratio_3_4_Exists() =>
        Assert.Equal(1, (int)Ratio.ratio_3_4);

    [Fact] public void Ratio_1_1_Exists() =>
        Assert.Equal(2, (int)Ratio.ratio_1_1);

    [Fact] public void Ratio_16_9_Exists() =>
        Assert.Equal(3, (int)Ratio.ratio_16_9);

    [Fact] public void Ratio_9_16_Exists() =>
        Assert.Equal(4, (int)Ratio.ratio_9_16);
}
