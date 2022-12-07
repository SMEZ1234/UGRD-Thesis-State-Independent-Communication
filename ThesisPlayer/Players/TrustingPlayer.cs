using System;
using System.Linq;

namespace ThesisPlayer
{
    class TrustingPlayer<RoleT, MoveT> : RationalPlayer<RoleT, MoveT>
    {
        public override MoveT PlayMove(MoveT[] legalMoves)
        {
            if (possibleGameStates.Count > 0) return base.PlayMove(legalMoves);

            return random.Select(legalMoves);
        }

        public override void ReportPlayedMove(MoveT[] movePlayed, (int sender, Func<IGameState<RoleT, MoveT>, bool> claim)[] claims)
        {
            foreach ((_, var claim) in claims)
            {
                possibleGameStates = possibleGameStates
                    .Where((pair) => claim(pair.Item1))
                    .ToList();
            }

            base.ReportPlayedMove(movePlayed, claims);
        }
    }
}
