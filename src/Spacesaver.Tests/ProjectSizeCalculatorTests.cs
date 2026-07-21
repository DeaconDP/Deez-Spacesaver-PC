using Spacesaver.Models;
using Spacesaver.Services;

namespace Spacesaver.Tests;

public class ProjectSizeCalculatorTests
{
    private static OrganiserProject BuildProject() => new()
    {
        Name = "Alpha",
        Folders =
        [
            new OrganiserFolder { Path = @"C:\Projects\Alpha", LastMeasuredBytes = 100 },
            new OrganiserFolder { Path = @"C:\Assets\Alpha", LastMeasuredBytes = 50 },
            new OrganiserFolder { Path = @"E:\Backups\Alpha", LastMeasuredBytes = 200 },
            new OrganiserFolder { Path = @"D:\Other\Alpha", LastMeasuredBytes = null }
        ]
    };

    [Fact]
    public void TotalBytes_SumsCachedSizes_IgnoringUnmeasured()
    {
        Assert.Equal(350, ProjectSizeCalculator.TotalBytes(BuildProject()));
    }

    [Fact]
    public void ByDrive_GroupsByDriveRoot_LargestFirst()
    {
        var usage = ProjectSizeCalculator.ByDrive(BuildProject());

        Assert.Equal(2, usage.Count);
        Assert.Equal(@"E:\", usage[0].Root);
        Assert.Equal(200, usage[0].Bytes);
        Assert.Equal(@"C:\", usage[1].Root);
        Assert.Equal(150, usage[1].Bytes);
    }

    [Fact]
    public void ByDrive_UnmeasuredFoldersProduceNoDriveEntry()
    {
        var usage = ProjectSizeCalculator.ByDrive(BuildProject());
        Assert.DoesNotContain(usage, u => u.Root.StartsWith(@"D:", StringComparison.OrdinalIgnoreCase));
    }
}
