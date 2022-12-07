using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThesisPlayer.Games
{
    class OneShotInvestigationGame : IGame<OneShotInvestigationGameRole, OneShotInvestigationGameMove>
    {
        public int GetNumberOfPlayers() => 3;

        public (int, OneShotInvestigationGameRole[])[] GetPossibleAllocations()
        {
            return new[]
            {
                (1, new[] { OneShotInvestigationGameRole.GUESSER, OneShotInvestigationGameRole.GOOD_OTHER_GOOD, OneShotInvestigationGameRole.GOOD_OTHER_GOOD }),
                (1, new[] { OneShotInvestigationGameRole.GUESSER, OneShotInvestigationGameRole.GOOD_OTHER_EVIL, OneShotInvestigationGameRole.EVIL_OTHER_GOOD }),
                (1, new[] { OneShotInvestigationGameRole.GUESSER, OneShotInvestigationGameRole.EVIL_OTHER_GOOD, OneShotInvestigationGameRole.GOOD_OTHER_EVIL }),
                (1, new[] { OneShotInvestigationGameRole.GUESSER, OneShotInvestigationGameRole.EVIL_OTHER_EVIL, OneShotInvestigationGameRole.EVIL_OTHER_EVIL }),
            };
        }

        public IGameState<OneShotInvestigationGameRole, OneShotInvestigationGameMove> GetInitialState(OneShotInvestigationGameRole[] allocation)
        {
            return new OneShotInvestigationGameState(allocation);
        }

        public virtual OneShotInvestigationGameMove[][] GetPossibleMoves(IGameState<OneShotInvestigationGameRole, OneShotInvestigationGameMove> currentState)
        {
            if (currentState.History.Length == 0) return new[]
            {
                new[] { OneShotInvestigationGameMove.NO_OP },
                new[] { OneShotInvestigationGameMove.CLAIM_GOOD_GOOD, OneShotInvestigationGameMove.CLAIM_GOOD_EVIL, OneShotInvestigationGameMove.CLAIM_EVIL_GOOD, OneShotInvestigationGameMove.CLAIM_EVIL_EVIL },
                new[] { OneShotInvestigationGameMove.CLAIM_GOOD_GOOD, OneShotInvestigationGameMove.CLAIM_GOOD_EVIL, OneShotInvestigationGameMove.CLAIM_EVIL_GOOD, OneShotInvestigationGameMove.CLAIM_EVIL_EVIL },
            };
            else return new[]
            {
                new[] { OneShotInvestigationGameMove.GUESS_GOOD_GOOD, OneShotInvestigationGameMove.GUESS_GOOD_EVIL, OneShotInvestigationGameMove.GUESS_EVIL_GOOD, OneShotInvestigationGameMove.GUESS_EVIL_EVIL },
                new[] { OneShotInvestigationGameMove.NO_OP },
                new[] { OneShotInvestigationGameMove.NO_OP },
            };
        }

        public bool IsClaim(OneShotInvestigationGameMove move, out int[] receivers, out Func<IGameState<OneShotInvestigationGameRole, OneShotInvestigationGameMove>, bool> claim)
        {
            switch (move)
            {
                case OneShotInvestigationGameMove.CLAIM_GOOD_GOOD:
                    receivers = new[] { 0 };
                    claim = (gameState) => gameState.Allocation[1] == OneShotInvestigationGameRole.GOOD_OTHER_GOOD;
                    return true;
                case OneShotInvestigationGameMove.CLAIM_GOOD_EVIL:
                    receivers = new[] { 0 };
                    claim = (gameState) => gameState.Allocation[1] == OneShotInvestigationGameRole.GOOD_OTHER_EVIL;
                    return true;
                case OneShotInvestigationGameMove.CLAIM_EVIL_GOOD:
                    receivers = new[] { 0 };
                    claim = (gameState) => gameState.Allocation[1] == OneShotInvestigationGameRole.EVIL_OTHER_GOOD;
                    return true;
                case OneShotInvestigationGameMove.CLAIM_EVIL_EVIL:
                    receivers = new[] { 0 };
                    claim = (gameState) => gameState.Allocation[1] == OneShotInvestigationGameRole.EVIL_OTHER_EVIL;
                    return true;
                default:
                    receivers = null;
                    claim = null;
                    return false;
            }
        }
    }

    class OneShotInvestigationGameState : IGameState<OneShotInvestigationGameRole, OneShotInvestigationGameMove>
    {
        public OneShotInvestigationGameRole[] Allocation { get; }
        public OneShotInvestigationGameMove[][] History { get; }

        public OneShotInvestigationGameState(OneShotInvestigationGameRole[] allocation) : this(allocation, new OneShotInvestigationGameMove[0][])
        {
        }

        public OneShotInvestigationGameState(OneShotInvestigationGameRole[] allocation, OneShotInvestigationGameMove[][] history)
        {
            Allocation = allocation;
            History = history;
        }

        public IGameState<OneShotInvestigationGameRole, OneShotInvestigationGameMove> AfterMove(IEnumerable<OneShotInvestigationGameMove> move)
        {
            return new OneShotInvestigationGameState(Allocation, History.Concat(move.ToArray()));
        }

        public int[] GetUtilities()
        {
            if (History.Length < 2) return null;

            var guessersMove = History[^1][0];

            return new[]
            {
                IsGuessCorrect(guessersMove) ? 100 : 0,
                IsPlayerGood(1) ? (IsGuessCorrect(guessersMove) ? 100 : 0) : (WasAnEvilAccused(guessersMove) ? 0 : 100),
                IsPlayerGood(2) ? (IsGuessCorrect(guessersMove) ? 100 : 0) : (WasAnEvilAccused(guessersMove) ? 0 : 100),
            };
        }

        private bool IsGuessCorrect(OneShotInvestigationGameMove guess)
        {
            switch (guess)
            {
                case OneShotInvestigationGameMove.GUESS_GOOD_GOOD:
                    return Allocation[1] == OneShotInvestigationGameRole.GOOD_OTHER_GOOD;
                case OneShotInvestigationGameMove.GUESS_GOOD_EVIL:
                    return Allocation[1] == OneShotInvestigationGameRole.GOOD_OTHER_EVIL;
                case OneShotInvestigationGameMove.GUESS_EVIL_GOOD:
                    return Allocation[1] == OneShotInvestigationGameRole.EVIL_OTHER_GOOD;
                case OneShotInvestigationGameMove.GUESS_EVIL_EVIL:
                    return Allocation[1] == OneShotInvestigationGameRole.EVIL_OTHER_EVIL;
            }

            return false;
        }

        private bool IsPlayerGood(int player) => Allocation[player] == OneShotInvestigationGameRole.GOOD_OTHER_GOOD
                                              || Allocation[player] == OneShotInvestigationGameRole.GOOD_OTHER_EVIL;

        private bool WasAnEvilAccused(OneShotInvestigationGameMove guess)
        {
            switch (guess)
            {
                case OneShotInvestigationGameMove.GUESS_GOOD_GOOD:
                    return false;
                case OneShotInvestigationGameMove.GUESS_GOOD_EVIL:
                    return !IsPlayerGood(2);
                case OneShotInvestigationGameMove.GUESS_EVIL_GOOD:
                    return !IsPlayerGood(1);
                case OneShotInvestigationGameMove.GUESS_EVIL_EVIL:
                    return !IsPlayerGood(1) || !IsPlayerGood(2);
            }

            return false;
        }
    }

    enum OneShotInvestigationGameRole
    {
        GUESSER,
        GOOD_OTHER_GOOD,
        GOOD_OTHER_EVIL,
        EVIL_OTHER_GOOD,
        EVIL_OTHER_EVIL,
    }

    enum OneShotInvestigationGameMove
    {
        NO_OP,
        CLAIM_GOOD_GOOD,
        CLAIM_GOOD_EVIL,
        CLAIM_EVIL_GOOD,
        CLAIM_EVIL_EVIL,
        GUESS_GOOD_GOOD,
        GUESS_GOOD_EVIL,
        GUESS_EVIL_GOOD,
        GUESS_EVIL_EVIL,
    }
}
