namespace PersonaCards.Core.Random
{
    public interface ISeededRng
    {
        uint NextUInt();

        int NextInt(int exclusiveMax);
    }
}
