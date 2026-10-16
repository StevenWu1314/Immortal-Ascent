/// <summary>
/// Every status the GDD defines (section 9.1). The enum carries all five now so serialized data never
/// needs renumbering later, but M1 implements only <see cref="Root"/> - see
/// <see cref="StatusSet.IsImplemented"/>, which refuses the rest out loud.
/// </summary>
public enum StatusKind
{
    Root,
    Fire,
    Poison,
    Enfeeblement,
    Explosive
}
