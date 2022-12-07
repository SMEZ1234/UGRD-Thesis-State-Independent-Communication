using System;
using System.Collections.Generic;
using System.Linq;

namespace ThesisPlayer
{
    class TruthfulPlayer<RoleT, MoveT> : IPlayer<RoleT, MoveT>
    {
        readonly Random random = new Random();
        protected IGame<RoleT, MoveT> game;
        protected List<IGameState<RoleT, MoveT>> possibleGameStates;


        public void InitialiseGame(int playerId, IGame<RoleT, MoveT> game, RoleT allocatedRole)
        {
            this.game = game;

            possibleGameStates = Enumerable.ToList(
                game.GetPossibleAllocations()
                    .Where((allocation) => allocation.roles[playerId].Equals(allocatedRole))
                    .Select((allocation) => game.GetInitialState(allocation.roles))
            );
        }

        public MoveT PlayMove(MoveT[] legalMoves)
        {
            var trueClaims = new List<MoveT>();

            foreach (var move in legalMoves)
            {
                if (game.IsClaim(move, out _, out var claim) && possibleGameStates.All(claim))
                {
                    trueClaims.Add(move);
                }
            }

            return random.Select(
                trueClaims.Count > 0 ? trueClaims
              : legalMoves.Where((move) => !game.IsClaim(move, out _, out _))
            );
        }

        public void ReportPlayedMove(MoveT[] movePlayed, (int sender, Func<IGameState<RoleT, MoveT>, bool> claim)[] claims)
        {
            possibleGameStates = possibleGameStates
                .Select((state) => state.AfterMove(movePlayed))
                .ToList();
        }
    }
}
