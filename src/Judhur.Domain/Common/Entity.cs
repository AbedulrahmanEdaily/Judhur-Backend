namespace Judhur.Domain.Common;

public abstract class Entity
{
    protected Entity()
    { }

    protected Entity(Guid id)
    {
        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
    }

    public Guid Id { get; }
}
