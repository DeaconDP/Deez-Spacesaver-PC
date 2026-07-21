namespace Spacesaver.Models;

public sealed class OrganiserProject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<OrganiserFolder> Folders { get; set; } = [];
}
