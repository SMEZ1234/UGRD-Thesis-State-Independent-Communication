using System;

namespace ThesisPlayer
{
    interface IPlayer<RoleT, MoveT>
    {
        void InitialiseGame(int playerId, IGame<RoleT, MoveT> game, RoleT allocatedRole);
        MoveT PlayMove(MoveT[] legalMoves);
        void ReportPlayedMove(MoveT[] movePlayed, (int sender, Func<IGameState<RoleT, MoveT>, bool> claim)[] claims);
    }
}
