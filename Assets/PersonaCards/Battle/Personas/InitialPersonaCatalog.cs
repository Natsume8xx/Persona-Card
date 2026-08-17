using PersonaCards.Cards.Hands;

namespace PersonaCards.Battle.Personas
{
    public static class InitialPersonaCatalog
    {
        public static PersonaCardDefinition Accumulator { get; } = new PersonaCardDefinition(
            "persona.initial.accumulator",
            "积累者",
            PersonaConditionKind.Always,
            HandType.HighCard,
            PersonaEffectKind.AddChips,
            15m);

        public static PersonaCardDefinition Executor { get; } = new PersonaCardDefinition(
            "persona.initial.executor",
            "执行者",
            PersonaConditionKind.Always,
            HandType.HighCard,
            PersonaEffectKind.AddMultiplier,
            2m);

        public static PersonaCardDefinition Ambitious { get; } = new PersonaCardDefinition(
            "persona.initial.ambitious",
            "野心者",
            PersonaConditionKind.MinimumHandPriority,
            HandType.Pair,
            PersonaEffectKind.MultiplyFinal,
            1.10m);

        public static PersonaLoadout CreateDefaultLoadout()
        {
            return new PersonaLoadout(new[]
            {
                new PersonaSlot(0, Accumulator),
                new PersonaSlot(1, Executor),
                new PersonaSlot(2, Ambitious),
                new PersonaSlot(3, null)
            });
        }
    }
}
