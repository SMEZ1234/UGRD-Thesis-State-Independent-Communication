using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThesisPlayer.Games
{
    class GoodOrEvilGame : IGame<GoodOrEvilGameRole, GoodOrEvilGameMove>
    {
        public int GetNumberOfPlayers() => 2;

        public (int, GoodOrEvilGameRole[])[] GetPossibleAllocations()
        {
            return new[]
            {
                (1, new[] { GoodOrEvilGameRole.GUESSER, GoodOrEvilGameRole.GOOD }),
                (3, new[] { GoodOrEvilGameRole.GUESSER, GoodOrEvilGameRole.EVIL }),
            };
        }

        public IGameState<GoodOrEvilGameRole, GoodOrEvilGameMove> GetInitialState(GoodOrEvilGameRole[] allocation)
        {
            return new GoodOrEvilGameState(allocation);
        }

        public virtual GoodOrEvilGameMove[][] GetPossibleMoves(IGameState<GoodOrEvilGameRole, GoodOrEvilGameMove> currentState)
        {
            if (currentState.History.Length == 0) return new[]
            {
                new[] { GoodOrEvilGameMove.NO_OP },
                new[] { GoodOrEvilGameMove.CLAIM_GOOD, GoodOrEvilGameMove.CLAIM_EVIL },
            };
            else return new[]
            {
                new[] { GoodOrEvilGameMove.GUESS_GOOD, GoodOrEvilGameMove.GUESS_EVIL },
                new[] { GoodOrEvilGameMove.NO_OP },
            };
        }

        public bool IsClaim(GoodOrEvilGameMove move, out int[] receivers, out Func<IGameState<GoodOrEvilGameRole, GoodOrEvilGameMove>, bool> claim)
        {
            switch (move)
            {
                case GoodOrEvilGameMove.CLAIM_GOOD:
                    receivers = new[] { 0 };
                    claim = (gameState) => gameState.Allocation[1] == GoodOrEvilGameRole.GOOD;
                    return true;
                case GoodOrEvilGameMove.CLAIM_EVIL:
                    receivers = new[] { 0 };
                    claim = (gameState) => gameState.Allocation[1] == GoodOrEvilGameRole.EVIL;
                    return true;
                default:
                    receivers = null;
                    claim = null;
                    return false;
            }
        }
    }

    class TruthfulGoodOrEvilGame : GoodOrEvilGame
    {
        public override GoodOrEvilGameMove[][] GetPossibleMoves(IGameState<GoodOrEvilGameRole, GoodOrEvilGameMove> currentState)
        {
            if (currentState.History.Length == 0) return new[]
            {
                new[] { GoodOrEvilGameMove.NO_OP },
                new[] { currentState.Allocation[1] == GoodOrEvilGameRole.GOOD ? GoodOrEvilGameMove.CLAIM_GOOD : GoodOrEvilGameMove.CLAIM_EVIL },
            };
            else return new[]
            {
                new[] { GoodOrEvilGameMove.GUESS_GOOD, GoodOrEvilGameMove.GUESS_EVIL },
                new[] { GoodOrEvilGameMove.NO_OP },
            };
        }
    }

    class GoodOrEvilGameState : IGameState<GoodOrEvilGameRole, GoodOrEvilGameMove>
    {
        public GoodOrEvilGameRole[] Allocation { get; }
        public GoodOrEvilGameMove[][] History { get; }

        public GoodOrEvilGameState(GoodOrEvilGameRole[] allocation) : this(allocation, new GoodOrEvilGameMove[0][])
        {
        }

        public GoodOrEvilGameState(GoodOrEvilGameRole[] allocation, GoodOrEvilGameMove[][] history)
        {
            Allocation = allocation;
            History = history;
        }

        public IGameState<GoodOrEvilGameRole, GoodOrEvilGameMove> AfterMove(IEnumerable<GoodOrEvilGameMove> move)
        {
            return new GoodOrEvilGameState(Allocation, History.Concat(move.ToArray()));
        }

        public int[] GetUtilities()
        {
            if (History.Length < 2) return null;

            var guessersMove = History[^1][0];

            return new[]
            {
                IsGuessCorrect(guessersMove) ? 100 : 0,
                guessersMove == GoodOrEvilGameMove.GUESS_GOOD ? 100 : 0
            };
        }

        private bool IsGuessCorrect(GoodOrEvilGameMove guess)
        {
            return (guess == GoodOrEvilGameMove.GUESS_GOOD && Allocation[1] == GoodOrEvilGameRole.GOOD) || (guess == GoodOrEvilGameMove.GUESS_EVIL && Allocation[1] == GoodOrEvilGameRole.EVIL);
        }
    }

    enum GoodOrEvilGameRole
    {
        GUESSER,
        GOOD,
        EVIL,
    }

    enum GoodOrEvilGameMove
    {
        GUESS_GOOD,
        GUESS_EVIL,
        NO_OP,
        CLAIM_GOOD,
        CLAIM_EVIL,
    }
}
