using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThesisPlayer.Games
{
    class ProbableSharedEnvelopeGame : IGame<ProbableSharedEnvelopeGameRole, ProbableSharedEnvelopeGameMove>
    {
        public int GetNumberOfPlayers() => 3;

        public (int, ProbableSharedEnvelopeGameRole[])[] GetPossibleAllocations()
        {
            return new[]
            {
                (9, new[] { ProbableSharedEnvelopeGameRole.ALLIED_FORM_IN_FIRST, ProbableSharedEnvelopeGameRole.ALLIED_FORM, ProbableSharedEnvelopeGameRole.NO_INFO }),
                (9, new[] { ProbableSharedEnvelopeGameRole.ALLIED_FORM_IN_SECOND, ProbableSharedEnvelopeGameRole.ALLIED_FORM, ProbableSharedEnvelopeGameRole.NO_INFO }),
                (1, new[] { ProbableSharedEnvelopeGameRole.OPPOSITIONAL_FORM_IN_FIRST, ProbableSharedEnvelopeGameRole.OPPOSITIONAL_FORM, ProbableSharedEnvelopeGameRole.NO_INFO }),
                (1, new[] { ProbableSharedEnvelopeGameRole.OPPOSITIONAL_FORM_IN_SECOND, ProbableSharedEnvelopeGameRole.OPPOSITIONAL_FORM, ProbableSharedEnvelopeGameRole.NO_INFO }),
            };
        }

        public IGameState<ProbableSharedEnvelopeGameRole, ProbableSharedEnvelopeGameMove> GetInitialState(ProbableSharedEnvelopeGameRole[] allocation)
        {
            return new ProbableSharedEnvelopeGameState(allocation);
        }

        public virtual ProbableSharedEnvelopeGameMove[][] GetPossibleMoves(IGameState<ProbableSharedEnvelopeGameRole, ProbableSharedEnvelopeGameMove> currentState)
        {
            if (currentState.History.Length == 0) return new[]
            {
                new[] { ProbableSharedEnvelopeGameMove.CLAIM_IN_FIRST, ProbableSharedEnvelopeGameMove.CLAIM_IN_SECOND },
                new[] { ProbableSharedEnvelopeGameMove.NO_OP },
                new[] { ProbableSharedEnvelopeGameMove.NO_OP },
            };
            else return new[]
            {
                new[] { ProbableSharedEnvelopeGameMove.NO_OP },
                currentState.Allocation[1] == ProbableSharedEnvelopeGameRole.ALLIED_FORM
                    ? new[] { ProbableSharedEnvelopeGameMove.NO_OP }
                    : new[] { ProbableSharedEnvelopeGameMove.TAKE_FIRST, ProbableSharedEnvelopeGameMove.TAKE_SECOND },
                new[] { ProbableSharedEnvelopeGameMove.TAKE_FIRST, ProbableSharedEnvelopeGameMove.TAKE_SECOND },
            };
        }

        public bool IsClaim(ProbableSharedEnvelopeGameMove move, out int[] receivers, out Func<IGameState<ProbableSharedEnvelopeGameRole, ProbableSharedEnvelopeGameMove>, bool> claim)
        {
            switch (move)
            {
                case ProbableSharedEnvelopeGameMove.CLAIM_IN_FIRST:
                    receivers = new[] { 1, 2 };
                    claim = (gameState) => gameState.Allocation[0] == ProbableSharedEnvelopeGameRole.ALLIED_FORM_IN_FIRST
                                        || gameState.Allocation[0] == ProbableSharedEnvelopeGameRole.OPPOSITIONAL_FORM_IN_FIRST;
                    return true;
                case ProbableSharedEnvelopeGameMove.CLAIM_IN_SECOND:
                    receivers = new[] { 1, 2 };
                    claim = (gameState) => gameState.Allocation[0] == ProbableSharedEnvelopeGameRole.ALLIED_FORM_IN_SECOND
                                        || gameState.Allocation[0] == ProbableSharedEnvelopeGameRole.OPPOSITIONAL_FORM_IN_SECOND;
                    return true;
                default:
                    receivers = null;
                    claim = null;
                    return false;
            }
        }
    }

    class ProbableSharedEnvelopeGameState : IGameState<ProbableSharedEnvelopeGameRole, ProbableSharedEnvelopeGameMove>
    {
        public ProbableSharedEnvelopeGameRole[] Allocation { get; }
        public ProbableSharedEnvelopeGameMove[][] History { get; }

        public ProbableSharedEnvelopeGameState(ProbableSharedEnvelopeGameRole[] allocation) : this(allocation, new ProbableSharedEnvelopeGameMove[0][])
        {
        }

        public ProbableSharedEnvelopeGameState(ProbableSharedEnvelopeGameRole[] allocation, ProbableSharedEnvelopeGameMove[][] history)
        {
            Allocation = allocation;
            History = history;
        }

        public IGameState<ProbableSharedEnvelopeGameRole, ProbableSharedEnvelopeGameMove> AfterMove(IEnumerable<ProbableSharedEnvelopeGameMove> move)
        {
            return new ProbableSharedEnvelopeGameState(Allocation, History.Concat(move.ToArray()));
        }

        public int[] GetUtilities()
        {
            if (History.Length < 2) return null;

            var secondsMove = History[^1][1];
            var thirdsMove = History[^1][2];

            return new[]
            {
                Allocation[1] == ProbableSharedEnvelopeGameRole.ALLIED_FORM ? (IsGuessCorrect(thirdsMove) ? 100 : 0) : (IsGuessCorrect(thirdsMove) ? 0 : 100),
                Allocation[1] == ProbableSharedEnvelopeGameRole.ALLIED_FORM ? (IsGuessCorrect(thirdsMove) ? 100 : 0) : (IsGuessCorrect(secondsMove) ? 100 : 0),
                IsGuessCorrect(thirdsMove) ? 100 : 0,
            };
        }

        private bool IsGuessCorrect(ProbableSharedEnvelopeGameMove guess)
        {
            return (guess == ProbableSharedEnvelopeGameMove.TAKE_FIRST && (Allocation[0] == ProbableSharedEnvelopeGameRole.ALLIED_FORM_IN_FIRST || Allocation[0] == ProbableSharedEnvelopeGameRole.OPPOSITIONAL_FORM_IN_FIRST))
                || (guess == ProbableSharedEnvelopeGameMove.TAKE_SECOND && (Allocation[0] == ProbableSharedEnvelopeGameRole.ALLIED_FORM_IN_SECOND || Allocation[0] == ProbableSharedEnvelopeGameRole.OPPOSITIONAL_FORM_IN_SECOND));
        }
    }

    enum ProbableSharedEnvelopeGameRole
    {
        ALLIED_FORM_IN_FIRST,
        ALLIED_FORM_IN_SECOND,
        OPPOSITIONAL_FORM_IN_FIRST,
        OPPOSITIONAL_FORM_IN_SECOND,
        ALLIED_FORM,
        OPPOSITIONAL_FORM,
        NO_INFO,
    }

    enum ProbableSharedEnvelopeGameMove
    {
        NO_OP,
        CLAIM_IN_FIRST,
        CLAIM_IN_SECOND,
        TAKE_FIRST,
        TAKE_SECOND,
    }
}
