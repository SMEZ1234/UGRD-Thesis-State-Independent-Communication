using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThesisPlayer.Games
{
    class MiniTacToe : IGame<MiniTacToeRole, MiniTacToeMove>
    {
        public int GetNumberOfPlayers() => 2;

        readonly List<MiniTacToeMove> SQUARES = Enum.GetValues(typeof(MiniTacToeMove))
            .Cast<MiniTacToeMove>()
            .Where((move) => !move.Equals(MiniTacToeMove.NO_OP))
            .ToList();

        public (int, MiniTacToeRole[])[] GetPossibleAllocations()
        {
            return new[]
            {
                (1, new[] { MiniTacToeRole.CROSSES, MiniTacToeRole.NOUGHTS }),
            };
        }

        public IGameState<MiniTacToeRole, MiniTacToeMove> GetInitialState(MiniTacToeRole[] allocation)
        {
            return new MiniTacToeState(allocation);
        }

        public virtual MiniTacToeMove[][] GetPossibleMoves(IGameState<MiniTacToeRole, MiniTacToeMove> currentState)
        {
            var emptySquares = SQUARES
                .Except(currentState.History.SelectMany((e) => e))
                .ToArray();

            if (currentState.History.Length % 2 == 0) return new[]
            {
                emptySquares,
                new[] { MiniTacToeMove.NO_OP },
            };
            else return new[]
            {
                new[] { MiniTacToeMove.NO_OP },
                emptySquares,
            };
        }

        public bool IsClaim(MiniTacToeMove move, out int[] receivers, out Func<IGameState<MiniTacToeRole, MiniTacToeMove>, bool> claim)
        {
            receivers = null;
            claim = null;
            return false;
        }
    }

    class MiniTacToeState : IGameState<MiniTacToeRole, MiniTacToeMove>
    {
        public MiniTacToeRole[] Allocation { get; }
        public MiniTacToeMove[][] History { get; }

        public MiniTacToeState(MiniTacToeRole[] allocation) : this(allocation, new MiniTacToeMove[0][])
        {
        }

        public MiniTacToeState(MiniTacToeRole[] allocation, MiniTacToeMove[][] history)
        {
            Allocation = allocation;
            History = history;
        }

        public IGameState<MiniTacToeRole, MiniTacToeMove> AfterMove(IEnumerable<MiniTacToeMove> move)
        {
            return new MiniTacToeState(Allocation, History.Concat(move.ToArray()));
        }

        public int[] GetUtilities()
        {
            var firstPlayerSquares = History.Select((move) => move[0]).ToHashSet();
            if (FormsLine(firstPlayerSquares)) return new[] { 100, 0 };

            var secondPlayerSquares = History.Select((move) => move[1]).ToHashSet();
            if (FormsLine(secondPlayerSquares)) return new[] { 0, 100 };

            if (History.Length == 4) return new[] { 50, 50 };

            return null;
        }

        private bool FormsLine(ISet<MiniTacToeMove> squares)
        {
            return squares.ContainsAll(new[] { MiniTacToeMove.TOP_LEFT, MiniTacToeMove.BOTTOM_RIGHT })
                || squares.ContainsAll(new[] { MiniTacToeMove.TOP_RIGHT, MiniTacToeMove.BOTTOM_LEFT });
        }

        public bool IsEquivalentTo(IGameState<MiniTacToeRole, MiniTacToeMove> other)
        {
            if (History.Length != other.History.Length) return false;

            var firstPlayerSquares = History.Select((move) => move[0]).Except(new[] { MiniTacToeMove.NO_OP });
            var secondPlayerSquares = History.Select((move) => move[1]).Except(new[] { MiniTacToeMove.NO_OP });

            var otherFirstPlayerSquares = other.History.Select((move) => move[0]).Except(new[] { MiniTacToeMove.NO_OP });
            var otherSecondPlayerSquares = other.History.Select((move) => move[1]).Except(new[] { MiniTacToeMove.NO_OP });

            return firstPlayerSquares.ContainsAll(otherFirstPlayerSquares) && secondPlayerSquares.ContainsAll(otherSecondPlayerSquares);
        }
    }

    enum MiniTacToeRole
    {
        CROSSES,
        NOUGHTS,
    }

    enum MiniTacToeMove
    {
        NO_OP,
        TOP_LEFT, TOP_RIGHT,
        BOTTOM_LEFT, BOTTOM_RIGHT,
    }
}
