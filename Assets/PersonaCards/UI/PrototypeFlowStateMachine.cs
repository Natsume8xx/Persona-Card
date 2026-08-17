namespace PersonaCards.UI
{
    public sealed class PrototypeFlowStateMachine
    {
        public PrototypeFlowStage Stage { get; private set; } = PrototypeFlowStage.MainMenu;
        public int BattleNumber { get; private set; }

        public bool StartNewRun() => Move(PrototypeFlowStage.MainMenu, PrototypeFlowStage.PersonaSetup);

        public bool ConfirmPersonaSetup()
        {
            if (!Move(PrototypeFlowStage.PersonaSetup, PrototypeFlowStage.Battle)) return false;
            BattleNumber = 1;
            return true;
        }

        public bool ContinueFromReward()
        {
            if (!Move(PrototypeFlowStage.Reward, PrototypeFlowStage.Battle)) return false;
            BattleNumber = 2;
            return true;
        }

        public bool ContinueFromShop() => Move(PrototypeFlowStage.Shop, PrototypeFlowStage.BossReveal);

        public bool BeginBossBattle()
        {
            if (!Move(PrototypeFlowStage.BossReveal, PrototypeFlowStage.Battle)) return false;
            BattleNumber = 3;
            return true;
        }

        public bool ContinueToForge() => Move(PrototypeFlowStage.RunReport, PrototypeFlowStage.PersonaForge);

        public bool CompleteBattle(bool won)
        {
            if (Stage != PrototypeFlowStage.Battle) return false;
            if (!won)
            {
                Stage = PrototypeFlowStage.FailureResult;
                return true;
            }

            Stage = BattleNumber switch
            {
                1 => PrototypeFlowStage.Reward,
                2 => PrototypeFlowStage.Shop,
                3 => PrototypeFlowStage.RunReport,
                _ => PrototypeFlowStage.FailureResult
            };
            return true;
        }

        public void ReturnToMainMenu()
        {
            Stage = PrototypeFlowStage.MainMenu;
            BattleNumber = 0;
        }

        public void Restore(PrototypeFlowStage stage, int battleNumber)
        {
            if (!System.Enum.IsDefined(typeof(PrototypeFlowStage), stage))
                throw new System.ArgumentOutOfRangeException(nameof(stage));
            if (battleNumber < 0 || battleNumber > 3)
                throw new System.ArgumentOutOfRangeException(nameof(battleNumber));
            Stage = stage;
            BattleNumber = battleNumber;
        }

        private bool Move(PrototypeFlowStage expected, PrototypeFlowStage next)
        {
            if (Stage != expected)
            {
                return false;
            }

            Stage = next;
            return true;
        }
    }
}
