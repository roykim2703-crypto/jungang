/// <summary>Abilities whose activation must not reveal a delayed trap or hidden stone.</summary>
public static class AugmentVisibility
{
    public static bool IsSecret(int id)=>id==8||id==12||id==16;
}
