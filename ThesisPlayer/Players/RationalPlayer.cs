using System;
using System.Collections.Generic;
using System.Linq;

namespace ThesisPlayer
{
    class RationalPlayer<RoleT, MoveT> : IPlayer<RoleT, MoveT>
    {
        protected readonly Random random = new Random();

        protected int ownId;
        protected IGame<RoleT, MoveT> game;
        protected List<(IGameState<RoleT, MoveT>, double)> possibleGameStates;

        protected readonly static IEqualityComparer<IGameState<RoleT, MoveT>> beliefCacheKeyComparer = new BeliefCacheKeyComparer();
        protected readonly IDictionary<IGameState<RoleT, MoveT>, IEnumerable<IGameState<RoleT, MoveT>>> beliefCache
            = new Dictionary<IGameState<RoleT, MoveT>, IEnumerable<IGameState<RoleT, MoveT>>>(beliefCacheKeyComparer);

        protected readonly static IEqualityComparer<(IGameState<RoleT, MoveT>, int)> utilityCacheKeyComparer = new UtilityCacheKeyComparer();
        protected readonly IDictionary<(IGameState<RoleT, MoveT>, int), MoveUtilityMap> moveUtilityCache = new Dictionary<(IGameState<RoleT, MoveT>, int), MoveUtilityMap>(utilityCacheKeyComparer);

        protected class BeliefCacheKeyComparer : EqualityComparer<IGameState<RoleT, MoveT>>
        {
            public override bool Equals(IGameState<RoleT, MoveT> x, IGameState<RoleT, MoveT> y) => x.IsEquivalentTo(y);

            public override int GetHashCode(IGameState<RoleT, MoveT> obj)
            {
                return 0; // FIXME This is a terrible hash
            }
        }

        protected class UtilityCacheKeyComparer : EqualityComparer<(IGameState<RoleT, MoveT>, int)>
        {
            public override bool Equals((IGameState<RoleT, MoveT>, int) x, (IGameState<RoleT, MoveT>, int) y)
            {
                return x.Item1.IsEquivalentTo(y.Item1) && x.Item2 == y.Item2;
            }

            public override int GetHashCode((IGameState<RoleT, MoveT>, int) obj)
            {
                return obj.Item2; // FIXME This is a terrible hash
            }
        }

        protected class MoveUtilityMap
        {
            readonly Dictionary<MoveT, int> map = new Dictionary<MoveT, int>();

            readonly List<MoveT> bestMoves = new List<MoveT>();
            int bestUtility = 0;

            public void Put(MoveT move, int utility)
            {
                if (utility > bestUtility)
                {
                    bestMoves.Clear();
                    bestUtility = utility;
                }

                if (utility == bestUtility) bestMoves.Add(move);

                map.Add(move, utility);
            }

            public int GetUtility(MoveT move)
            {
                map.TryGetValue(move, out var utility);
                return utility;
            }

            public int GetBestMoves(out IEnumerable<MoveT> moves)
            {
                moves = bestMoves;
                return bestUtility;
            }
        }

        public virtual void InitialiseGame(int playerId, IGame<RoleT, MoveT> game, RoleT allocatedRole)
        {
            ownId = playerId;
            this.game = game;

            possibleGameStates = Enumerable.ToList(
                game.GetPossibleAllocations()
                    .Where((allocation) => allocation.roles[playerId].Equals(allocatedRole))
                    .Select((allocation) => (game.GetInitialState(allocation.roles), (double) allocation.weight))
            );
        }

        public virtual MoveT PlayMove(MoveT[] legalMoves)
        {
            if (legalMoves.Length == 1) return legalMoves[0];

            var weightedUtilities = new double[legalMoves.Length];

            foreach ((var gameState, var weight) in possibleGameStates)
            {
                if (moveUtilityCache.TryGetValue((gameState, ownId), out var cachedUtilities))
                {
                    for (int i = 0; i < legalMoves.Length; i++)
                    {
                        weightedUtilities[i] += cachedUtilities.GetUtility(legalMoves[i]) * weight;
                    }
                }
                else
                {
                    var othersLegalMoves = game.GetPossibleMoves(gameState);
                    var utilities = new MoveUtilityMap();

                    for (int i = 0; i < legalMoves.Length; i++)
                    {
                        var utility = GetExpectedUtility(gameState, ownId, legalMoves[i], othersLegalMoves);
                        utilities.Put(legalMoves[i], utility);

                        weightedUtilities[i] += utility * weight;
                    }

                    moveUtilityCache.Add((gameState, ownId), utilities);
                }
            }

            var bestMoves = new List<MoveT>();
            double bestUtility = 0;

            for (int i = 0; i < legalMoves.Length; i++)
            {
                if (weightedUtilities[i] > bestUtility)
                {
                    bestMoves.Clear();
                    bestUtility = weightedUtilities[i];
                }

                if (weightedUtilities[i] == bestUtility) bestMoves.Add(legalMoves[i]);
            }

            return random.Select(bestMoves);
        }

        protected virtual int GetExpectedUtility(IGameState<RoleT, MoveT> gameState, int player, MoveT move, MoveT[][] movesToConsiderByPlayer)
        {
            var reducedMovesToConsiderByPlayer = (MoveT[][]) movesToConsiderByPlayer.Clone();
            reducedMovesToConsiderByPlayer[player] = new[] { move };

            if (reducedMovesToConsiderByPlayer.All((possibleMovesForPlayer) => possibleMovesForPlayer.Length == 1))
            {
                return GetExpectedUtilityForState(gameState.AfterMove(reducedMovesToConsiderByPlayer.SelectMany((e) => e).ToArray()), player);
            }

            var nPlayers = movesToConsiderByPlayer.Length;
            var bestMovesByPlayer = new List<MoveT>[nPlayers];

            for (int other = 0; other < nPlayers; other++)
            {
                if (reducedMovesToConsiderByPlayer[other].Length == 1)
                {
                    bestMovesByPlayer[other] = (new List<MoveT>() { reducedMovesToConsiderByPlayer[other][0] });
                }
                else
                {
                    var bestMoves = new List<MoveT>();
                    var bestUtility = 0;

                    foreach (var otherMove in reducedMovesToConsiderByPlayer[other])
                    {
                        //var utility = GetExpectedUtility(gameState, other, otherMove, reducedMovesToConsiderByPlayer);
                        var utility = GetStatesToConsiderByOtherPlayer(gameState, other)
                            .Max((gameState) => GetExpectedUtility(gameState, other, otherMove, reducedMovesToConsiderByPlayer));

                        if (utility > bestUtility)
                        {
                            bestMoves.Clear();
                            bestUtility = utility;
                        }

                        if (utility == bestUtility) bestMoves.Add(otherMove);
                    }

                    bestMovesByPlayer[other] = bestMoves;
                }
            }

            var nextGameStatesAssumingRationalPlayers = bestMovesByPlayer
                .CartesianProduct()
                .Select((combinedMove) => gameState.AfterMove(combinedMove));

            var worstNextStateUtility = nextGameStatesAssumingRationalPlayers
                .Min((gameState) => GetExpectedUtilityForState(gameState, player));

            //var nextGameStates = reducedMovesToConsiderByPlayer
            //    .CartesianProduct()
            //    .Select((combinedMove) => gameState.AfterMove(combinedMove));

            //var worstNextStateUtility = nextGameStates
            //    .Min((gameState) => GetExpectedUtilityForState(gameState, player));

            return worstNextStateUtility;
        }

        protected virtual int GetExpectedUtilityForState(IGameState<RoleT, MoveT> gameState, int playerId)
        {
            // Read from cache if it exists
            if (moveUtilityCache.TryGetValue((gameState, playerId), out var cachedUtilities)) return cachedUtilities.GetBestMoves(out _);

            var terminalUtilities = gameState.GetUtilities();
            if (terminalUtilities != null) return terminalUtilities[playerId];

            var utilities = new MoveUtilityMap();
            var legalMovesByPlayer = game.GetPossibleMoves(gameState);

            foreach (var move in legalMovesByPlayer[playerId])
            {
                utilities.Put(move, GetExpectedUtility(gameState, playerId, move, legalMovesByPlayer));
            }

            moveUtilityCache.Add((gameState, playerId), utilities);

            return utilities.GetBestMoves(out _);
        }

        protected virtual IEnumerable<IGameState<RoleT, MoveT>> GetStatesToConsiderByOtherPlayer(IGameState<RoleT, MoveT> actualState, int player)
        {
            // TODO Extract logic from player
            // TODO Choose whether to use real beliefs from self

            if (beliefCache.TryGetValue(actualState, out var cachedStatesToConsider)) return cachedStatesToConsider;

            var pseudoPlayer = CreateBeliefModelCopy();

            pseudoPlayer.InitialiseGame(player, game, actualState.Allocation[player]);
            foreach (var move in actualState.History) pseudoPlayer.ReportPlayedMove(move);

            var statesToConsider = pseudoPlayer.possibleGameStates.Select((e) => e.Item1);
            beliefCache.Add(actualState, statesToConsider);

            return statesToConsider;
        }

        protected virtual RationalPlayer<RoleT, MoveT> CreateBeliefModelCopy()
        {
            return new RationalPlayer<RoleT, MoveT>();
        }

        public virtual void ReportPlayedMove(MoveT[] movePlayed, params (int sender, Func<IGameState<RoleT, MoveT>, bool> claim)[] claims)
        {
            var updatedPossibilities = new List<(IGameState<RoleT, MoveT>, double)>();

            foreach ((var gameState, var weight) in possibleGameStates)
            {
                var legalMoves = game.GetPossibleMoves(gameState);
                var splitStates = new List<(IGameState<RoleT, MoveT>, double)>();

                var isStatePossible = true;

                for (int i = 0; i < movePlayed.Length && isStatePossible; i++)
                {
                    // TODO Handle case where move is unknown

                    //if (!Array.Exists(legalMoves[i], (move) => move.Equals(movePlayed[i]))) isStatePossible = false;
                }

                if (isStatePossible) updatedPossibilities.Add((gameState.AfterMove(movePlayed), weight));
            }

            possibleGameStates = updatedPossibilities;
        }
    }
}
