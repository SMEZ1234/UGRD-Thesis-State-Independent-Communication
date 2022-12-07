using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ThesisPlayer
{
    class GameManager<RoleT, MoveT>
    {
        readonly IGame<RoleT, MoveT> game;
        readonly IPlayer<RoleT, MoveT>[] players;

        readonly Random random = new Random();

        public GameManager(IGame<RoleT, MoveT> game, params IPlayer<RoleT, MoveT>[] players)
        {
            this.game = game;
            this.players = players;
        }

        public (RoleT[], int[]) Play()
        {
            var weightedAllocations = new List<RoleT[]>();

            foreach ((var weight, var roles) in game.GetPossibleAllocations())
            {
                for (var i = 0; i < weight; i++) weightedAllocations.Add(roles);
            }

            var allocation = random.Select(weightedAllocations);

            return (allocation, PlayWithAllocation(allocation));
        }

        public int[] PlayWithAllocation(RoleT[] allocation)
        {
            var gameState = game.GetInitialState(allocation);
            int[] utilities;

            for (int i = 0; i < players.Length; i++) players[i].InitialiseGame(i, game, allocation[i]);

            while ((utilities = gameState.GetUtilities()) == null)
            {
                var move = new MoveT[players.Length];
                var legalMovesByPlayer = game.GetPossibleMoves(gameState);

                // Request moves
                for (int i = 0; i < players.Length; i++)
                {
                    move[i] = players[i].PlayMove(legalMovesByPlayer[i]);
                }

                // Report moves by all players
                // TODO Only show visible moves to each player
                var claims = game.GetClaimsFromMoveByReceiver(move);

                for (int i = 0; i < players.Length; i++)
                {
                    players[i].ReportPlayedMove(move, claims.ElementAt(i).ToArray());
                }

                gameState = gameState.AfterMove(move);
            }

            return utilities;
        }
    }
}
