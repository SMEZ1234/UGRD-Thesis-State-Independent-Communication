using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThesisPlayer.Games
{
    class TicTacToe : IGame<TicTacToeRole, TicTacToeMove>
    {
        public int GetNumberOfPlayers() => 2;

        readonly List<TicTacToeMove> SQUARES = Enum.GetValues(typeof(TicTacToeMove))
            .Cast<TicTacToeMove>()
            .Where((move) => !move.Equals(TicTacToeMove.NO_OP))
            .ToList();

        public (int, TicTacToeRole[])[] GetPossibleAllocations()
        {
            return new[]
            {
                (1, new[] { TicTacToeRole.CROSSES, TicTacToeRole.NOUGHTS }),
            };
        }

        public IGameState<TicTacToeRole, TicTacToeMove> GetInitialState(TicTacToeRole[] allocation)
        {
            return new TicTacToeState(allocation);
        }

        public virtual TicTacToeMove[][] GetPossibleMoves(IGameState<TicTacToeRole, TicTacToeMove> currentState)
        {
            var emptySquares = SQUARES
                .Except(currentState.History.SelectMany((e) => e))
                .ToArray();

            if (currentState.History.Length % 2 == 0) return new[]
            {
                emptySquares,
                new[] { TicTacToeMove.NO_OP },
            };
            else return new[]
            {
                new[] { TicTacToeMove.NO_OP },
                emptySquares,
            };
        }

        public bool IsClaim(TicTacToeMove move, out int[] receivers, out Func<IGameState<TicTacToeRole, TicTacToeMove>, bool> claim)
        {
            receivers = null;
            claim = null;
            return false;
        }
    }

    class TicTacToeState : IGameState<TicTacToeRole, TicTacToeMove>
    {
        public TicTacToeRole[] Allocation { get; }
        public TicTacToeMove[][] History { get; }

        public TicTacToeState(TicTacToeRole[] allocation) : this(allocation, new TicTacToeMove[0][])
        {
        }

        public TicTacToeState(TicTacToeRole[] allocation, TicTacToeMove[][] history)
        {
            Allocation = allocation;
            History = history;
        }

        public IGameState<TicTacToeRole, TicTacToeMove> AfterMove(IEnumerable<TicTacToeMove> move)
        {
            return new TicTacToeState(Allocation, History.Concat(move.ToArray()));
        }

        public int[] GetUtilities()
        {
            var firstPlayerSquares = History.Select((move) => move[0]).ToHashSet();
            if (FormsLine(firstPlayerSquares)) return new[] { 100, 0 };

            var secondPlayerSquares = History.Select((move) => move[1]).ToHashSet();
            if (FormsLine(secondPlayerSquares)) return new[] { 0, 100 };

            if (History.Length == 9) return new[] { 50, 50 };

            return null;
        }

        private bool FormsLine(ISet<TicTacToeMove> squares)
        {
            return squares.ContainsAll(new[] { TicTacToeMove.TOP_LEFT, TicTacToeMove.TOP_MIDDLE, TicTacToeMove.TOP_RIGHT })
                || squares.ContainsAll(new[] { TicTacToeMove.MIDDLE_LEFT, TicTacToeMove.CENTER, TicTacToeMove.MIDDLE_RIGHT })
                || squares.ContainsAll(new[] { TicTacToeMove.BOTTOM_LEFT, TicTacToeMove.BOTTOM_MIDDLE, TicTacToeMove.BOTTOM_RIGHT })

                || squares.ContainsAll(new[] { TicTacToeMove.TOP_LEFT, TicTacToeMove.MIDDLE_LEFT, TicTacToeMove.BOTTOM_LEFT })
                || squares.ContainsAll(new[] { TicTacToeMove.TOP_MIDDLE, TicTacToeMove.CENTER, TicTacToeMove.BOTTOM_MIDDLE })
                || squares.ContainsAll(new[] { TicTacToeMove.TOP_RIGHT, TicTacToeMove.MIDDLE_RIGHT, TicTacToeMove.BOTTOM_RIGHT })

                || squares.ContainsAll(new[] { TicTacToeMove.TOP_LEFT, TicTacToeMove.CENTER, TicTacToeMove.BOTTOM_RIGHT })
                || squares.ContainsAll(new[] { TicTacToeMove.TOP_RIGHT, TicTacToeMove.CENTER, TicTacToeMove.BOTTOM_LEFT });
        }

        public bool IsEquivalentTo(IGameState<TicTacToeRole, TicTacToeMove> other)
        {
            if (History.Length != other.History.Length) return false;

            var firstPlayerSquares = History.Select((move) => move[0]).Except(new[] { TicTacToeMove.NO_OP });
            var secondPlayerSquares = History.Select((move) => move[1]).Except(new[] { TicTacToeMove.NO_OP });

            var otherFirstPlayerSquares = other.History.Select((move) => move[0]).Except(new[] { TicTacToeMove.NO_OP });
            var otherSecondPlayerSquares = other.History.Select((move) => move[1]).Except(new[] { TicTacToeMove.NO_OP });

            return firstPlayerSquares.ContainsAll(otherFirstPlayerSquares) && secondPlayerSquares.ContainsAll(otherSecondPlayerSquares);
        }
    }

    enum TicTacToeRole
    {
        CROSSES,
        NOUGHTS,
    }

    enum TicTacToeMove
    {
        NO_OP,
        TOP_LEFT, TOP_MIDDLE, TOP_RIGHT,
        MIDDLE_LEFT, CENTER, MIDDLE_RIGHT,
        BOTTOM_LEFT, BOTTOM_MIDDLE, BOTTOM_RIGHT,
    }
}
