using Spacesaver.Helpers;

namespace Spacesaver.Tests;

public class OrganiserPathHelperTests
{
    [Fact]
    public void NormalizePath_RemovesTrailingSeparator()
    {
        Assert.Equal(@"C:\Projects\Alpha", OrganiserPathHelper.NormalizePath(@"C:\Projects\Alpha\"));
    }

    [Fact]
    public void NormalizePath_KeepsDriveRootSeparator()
    {
        Assert.Equal(@"C:\", OrganiserPathHelper.NormalizePath(@"C:\"));
    }

    [Fact]
    public void PathsEqual_IsCaseInsensitive()
    {
        Assert.True(OrganiserPathHelper.PathsEqual(@"C:\Projects\ALPHA", @"c:\projects\alpha\"));
    }

    [Theory]
    [InlineData(@"C:\Projects\Alpha", @"C:\Projects\Alpha\Renders", true)]
    [InlineData(@"C:\Projects\Alpha", @"C:\Projects\Alpha", true)]
    [InlineData(@"C:\Projects\Alpha", @"C:\Projects\AlphaBackup", false)]
    [InlineData(@"C:\Projects\Alpha", @"D:\Projects\Alpha\Renders", false)]
    public void IsSameOrDescendant_DetectsContainment(string parent, string candidate, bool expected)
    {
        Assert.Equal(expected, OrganiserPathHelper.IsSameOrDescendant(parent, candidate));
    }

    [Fact]
    public void Overlaps_IsSymmetric()
    {
        Assert.True(OrganiserPathHelper.Overlaps(@"C:\Projects\Alpha\Renders", @"C:\Projects\Alpha"));
        Assert.True(OrganiserPathHelper.Overlaps(@"C:\Projects\Alpha", @"C:\Projects\Alpha\Renders"));
        Assert.False(OrganiserPathHelper.Overlaps(@"C:\Projects\Alpha", @"C:\Projects\Beta"));
    }

    [Fact]
    public void GetDriveRoot_ReturnsRootForAbsolutePath()
    {
        Assert.Equal(@"D:\", OrganiserPathHelper.GetDriveRoot(@"D:\Assets\Textures"));
    }

    [Fact]
    public void GetDriveLabel_StripsSeparator()
    {
        Assert.Equal("D:", OrganiserPathHelper.GetDriveLabel(@"D:\"));
    }

    [Theory]
    [InlineData(@"C:\Projects\Alpha\Renders", "Renders")]
    [InlineData(@"D:\Assets", "Assets")]
    [InlineData(@"C:\", "C:")]
    public void GetFolderDisplayName_UsesLeafOrDriveLabel(string path, string expected)
    {
        Assert.Equal(expected, OrganiserPathHelper.GetFolderDisplayName(path));
    }
}
