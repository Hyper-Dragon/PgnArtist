namespace PgnArtist.Generic;

[AttributeUsage(AttributeTargets.Class)]
internal class AutoRegisterAttribute : Attribute
{
    private readonly RegistrationType _regType;

    public enum RegistrationType { TRANSIENT, SCOPED, SINGLETON };

    public AutoRegisterAttribute(RegistrationType regType)
    {
        _regType = regType;
    }

    public RegistrationType RegType => _regType;
}
