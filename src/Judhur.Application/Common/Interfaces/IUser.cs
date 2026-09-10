namespace Judhur.Application.Common.Interfaces;

/// <summary>
/// The user behind the current request, as far as the Application layer needs
/// to know: an id, or nothing at all for an anonymous caller.
/// </summary>
public interface IUser
{
    Guid? Id { get; }
}
