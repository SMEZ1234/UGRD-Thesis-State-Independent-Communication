using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThesisPlayer.Games
{
    class CooperationGame : IGame<CooperationGameRole, CooperationGameMove>
    {
        public int GetNumberOfPlayers() => 2;

        public (int, CooperationGameRole[])[] GetPossibleAllocations()
        {
            return new[]
            {
                (1, new[] { CooperationGameRole.GUESSER, CooperationGameRole.HAS_ONE }),
                (1, new[] { CooperationGameRole.GUESSER, CooperationGameRole.HAS_TWO }),
            };
        }

        public IGameState<CooperationGameRole, CooperationGameMove> GetInitialState(CooperationGameRole[] allocation)
        {
            return new CooperationGameState(allocation);
        }

        public virtual CooperationGameMove[][] GetPossibleMoves(IGameState<CooperationGameRole, CooperationGameMove> currentState)
        {
            if (currentState.History.Length == 0) return new[]
            {
                new[] { CooperationGameMove.NO_OP },
                new[] { CooperationGameMove.CLAIM_ONE, CooperationGameMove.CLAIM_TWO },
            };
            else return new[]
            {
                new[] { CooperationGameMove.GUESS_ONE, CooperationGameMove.GUESS_TWO },
                new[] { CooperationGameMove.NO_OP },
            };
        }

        public bool IsClaim(CooperationGameMove move, out int[] receivers, out Func<IGameState<CooperationGameRole, CooperationGameMove>, bool> claim)
        {
            switch (move)
            {
                case CooperationGameMove.CLAIM_ONE:
                    receivers = new[] { 0 };
                    claim = (gameState) => gameState.Allocation[1] == CooperationGameRole.HAS_ONE;
                    return true;
                case CooperationGameMove.CLAIM_TWO:
                    receivers = new[] { 0 };
                    claim = (gameState) => gameState.Allocation[1] == CooperationGameRole.HAS_TWO;
                    return true;
                default:
                    receivers = null;
                    claim = null;
                    return false;
            }
        }
    }

    class CooperationGameState : IGameState<CooperationGameRole, CooperationGameMove>
    {
        public CooperationGameRole[] Allocation { get; }
        public CooperationGameMove[][] History { get; }

        public CooperationGameState(CooperationGameRole[] allocation) : this(allocation, new CooperationGameMove[0][])
        {
        }

        public CooperationGameState(CooperationGameRole[] allocation, CooperationGameMove[][] history)
        {
            Allocation = allocation;
            History = history;
        }

        public IGameState<CooperationGameRole, CooperationGameMove> AfterMove(IEnumerable<CooperationGameMove> move)
        {
            return new CooperationGameState(Allocation, History.Concat(move.ToArray()));
        }

        public int[] GetUtilities()
        {
            if (History.Length < 2) return null;

            var guessersMove = History[^1][0];

            return new[]
            {
                IsGuessCorrect(guessersMove) ? 100 : 0,
                IsGuessCorrect(guessersMove) ? 100 : 0,
            };
        }

        private bool IsGuessCorrect(CooperationGameMove guess)
        {
            return (guess == CooperationGameMove.GUESS_ONE && Allocation[1] == CooperationGameRole.HAS_ONE) || (guess == CooperationGameMove.GUESS_TWO && Allocation[1] == CooperationGameRole.HAS_TWO);
        }
    }

    enum CooperationGameRole
    {
        GUESSER,
        HAS_ONE,
        HAS_TWO,
    }

    enum CooperationGameMove
    {
        GUESS_ONE,
        GUESS_TWO,
        NO_OP,
        CLAIM_ONE,
        CLAIM_TWO,
    }
}
