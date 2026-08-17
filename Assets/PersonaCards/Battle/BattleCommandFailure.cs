namespace PersonaCards.Battle
{
    public enum BattleCommandFailure
    {
        None,
        PresentationInProgress,
        BattleFinished,
        CardNotInHand,
        SelectionLimitReached,
        NoCardsSelected,
        NoPlaysRemaining,
        NoDiscardsRemaining
    }
}
