using System;

namespace ThesisPlayer
{
    class RandomPlayer<RoleT, MoveT> : IPlayer<RoleT, MoveT>
    {
        readonly Random random = new Random();

        public void InitialiseGame(int playerId, IGame<RoleT, MoveT> game, RoleT allocatedRole)
        {
            // Do nothing
        }

        public MoveT PlayMove(MoveT[] legalMoves)
        {
            return random.Select(legalMoves);
        }

        public void ReportPlayedMove(MoveT[] movePlayed, (int sender, Func<IGameState<RoleT, MoveT>, bool> claim)[] claims)
        {
            // Do nothing
        }
    }
}
