using System;
using System.Collections.Generic;
using System.Linq;

namespace ThesisPlayer.Games
{
    class EnvelopeGame : IGame<EnvelopeGameRole, EnvelopeGameMove>
    {
        public int GetNumberOfPlayers() => 2;

        public (int, EnvelopeGameRole[])[] GetPossibleAllocations()
        {
            return new[] { new[] { EnvelopeGameRole.GUESSER }, EnvelopeGameRole.DISTRIBUTIONS }
                .CartesianProduct()
                .Select((e) => (1, e.ToArray()))
                .ToArray();
        }

        public IGameState<EnvelopeGameRole, EnvelopeGameMove> GetInitialState(EnvelopeGameRole[] allocation)
        {
            return new EnvelopeGameState(allocation);
        }

        public virtual EnvelopeGameMove[][] GetPossibleMoves(IGameState<EnvelopeGameRole, EnvelopeGameMove> currentState)
        {
            if (currentState.History.Length == 0) return new[]
            {
                new[] { EnvelopeGameMove.NO_OP },
                EnvelopeGameMove.CLAIMS.ToArray(),
            };
            else return new[]
            {
                new[] { EnvelopeGameMove.SELECT_FIRST, EnvelopeGameMove.SELECT_SECOND },
                new[] { EnvelopeGameMove.NO_OP },
            };
        }

        public bool IsClaim(EnvelopeGameMove move, out int[] receivers, out Func<IGameState<EnvelopeGameRole, EnvelopeGameMove>, bool> claim)
        {
            if (move.type == EnvelopeGameMoveType.CLAIM)
            {
                receivers = new[] { 0 };
                claim = (gameState) => (move.isAboutFirst ? gameState.Allocation[1].inFirst : gameState.Allocation[1].inSecond) == move.amount;
                return true;
            }

            receivers = null;
            claim = null;
            return false;
        }
    }

    //class TruthfulEnvelopeGame : EnvelopeGame
    //{
    //    public override EnvelopeGameMove[][] GetPossibleMoves(IGameState<EnvelopeGameRole, EnvelopeGameMove> currentState)
    //    {
    //        if (currentState.History.Length == 0) return new[]
    //        {
    //            new[] { EnvelopeGameMove.NO_OP },
    //            new[] { currentState.Allocation[1] == EnvelopeGameRole.GOOD ? EnvelopeGameMove.CLAIM_GOOD : EnvelopeGameMove.CLAIM_EVIL },
    //        };
    //        else return new[]
    //        {
    //            new[] { EnvelopeGameMove.GUESS_GOOD, EnvelopeGameMove.GUESS_EVIL },
    //            new[] { EnvelopeGameMove.NO_OP },
    //        };
    //    }
    //}

    class EnvelopeGameState : IGameState<EnvelopeGameRole, EnvelopeGameMove>
    {
        public EnvelopeGameRole[] Allocation { get; }
        public EnvelopeGameMove[][] History { get; }

        public EnvelopeGameState(EnvelopeGameRole[] allocation) : this(allocation, new EnvelopeGameMove[0][])
        {
        }

        public EnvelopeGameState(EnvelopeGameRole[] allocation, EnvelopeGameMove[][] history)
        {
            Allocation = allocation;
            History = history;
        }

        public IGameState<EnvelopeGameRole, EnvelopeGameMove> AfterMove(IEnumerable<EnvelopeGameMove> move)
        {
            return new EnvelopeGameState(Allocation, History.Concat(move.ToArray()));
        }

        public int[] GetUtilities()
        {
            if (History.Length < 2) return null;

            var guessersMove = History[^1][0];
            var amount = guessersMove == EnvelopeGameMove.SELECT_FIRST ? Allocation[1].inFirst : Allocation[1].inSecond;

            return new[] { amount, amount };
        }
    }

    class EnvelopeGameRole
    {
        internal int inFirst, inSecond;

        private const int MAX_AMOUNT = 4;

        public static readonly IEnumerable<int> POSSIBLE_AMOUNTS = Enumerable.Range(0, MAX_AMOUNT + 1)
            .Select((x) => x * 100 / MAX_AMOUNT);

        public static readonly EnvelopeGameRole GUESSER = new EnvelopeGameRole(-1, -1);
        public static readonly IEnumerable<EnvelopeGameRole> DISTRIBUTIONS = new[] { POSSIBLE_AMOUNTS, POSSIBLE_AMOUNTS }
            .CartesianProduct()
            .Select((e) => new EnvelopeGameRole(e.ElementAt(0), e.ElementAt(1)));

        private EnvelopeGameRole(int inFirst, int inSecond)
        {
            this.inFirst = inFirst;
            this.inSecond = inSecond;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            var other = (EnvelopeGameRole)obj;

            return other.inFirst == inFirst && other.inSecond == inSecond;
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    class EnvelopeGameMove
    {
        internal EnvelopeGameMoveType type;
        internal bool isAboutFirst;
        internal int amount;

        public static readonly EnvelopeGameMove
            NO_OP = new EnvelopeGameMove(EnvelopeGameMoveType.NO_OP),
            SELECT_FIRST = new EnvelopeGameMove(EnvelopeGameMoveType.SELECT_FIRST),
            SELECT_SECOND = new EnvelopeGameMove(EnvelopeGameMoveType.SELECT_SECOND);

        public static readonly IEnumerable<EnvelopeGameMove> CLAIMS = EnvelopeGameRole.POSSIBLE_AMOUNTS
            .Select((amount) => new[] {
                new EnvelopeGameMove(EnvelopeGameMoveType.CLAIM, true, amount),
                new EnvelopeGameMove(EnvelopeGameMoveType.CLAIM, false, amount),
            }).SelectMany((e) => e);

        private EnvelopeGameMove(EnvelopeGameMoveType type, bool isAboutFirst = false, int amount = 0)
        {
            this.type = type;
            this.isAboutFirst = isAboutFirst;
            this.amount = amount;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            var other = (EnvelopeGameMove) obj;

            return other.type == type && other.isAboutFirst == isAboutFirst && other.amount == amount;
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    enum EnvelopeGameMoveType
    {
        NO_OP,
        CLAIM,
        SELECT_FIRST,
        SELECT_SECOND,
    }
}
