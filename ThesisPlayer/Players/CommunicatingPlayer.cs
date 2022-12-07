using System;
using System.Collections.Generic;
using System.Linq;

namespace ThesisPlayer
{
    class CommunicatingPlayer<RoleT, MoveT> : IPlayer<RoleT, MoveT>
    {
        readonly Random random = new Random();

        IGame<RoleT, MoveT> game;
        Beliefs<RoleT, MoveT> beliefs;

        Dictionary<IGameState<RoleT, MoveT>, Beliefs<RoleT, MoveT>>[] beliefOverrides;

        public void InitialiseGame(int playerId, IGame<RoleT, MoveT> game, RoleT allocatedRole)
        {
            this.game = game;
            beliefs = new Beliefs<RoleT, MoveT>(playerId, game, allocatedRole);

            beliefOverrides = new Dictionary<IGameState<RoleT, MoveT>, Beliefs<RoleT, MoveT>>[game.GetNumberOfPlayers()];
            for (int i = 0; i < beliefOverrides.Length; i++) beliefOverrides[i] = new Dictionary<IGameState<RoleT, MoveT>, Beliefs<RoleT, MoveT>>(new GameStateEqualityComparer<RoleT, MoveT>());
        }

        public MoveT PlayMove(MoveT[] legalMoves) => random.Select(beliefs.GetBestMoves(legalMoves, GetUtilityForMove));

        private double GetUtilityForMove(IGameState<RoleT, MoveT> state, int player, MoveT individualMove)
        {
            // assume that the other players play a Pareto-optimal move that is the worst for this player
            var paretoOptimalMovesByPlayer = GetParetoOptimalMoves(state).ToArray();
            paretoOptimalMovesByPlayer[player] = new MoveT[] { individualMove };

            return paretoOptimalMovesByPlayer
                .CartesianProduct()
                .Min((move) => GetUtilitiesForState(state.AfterMove(move)).ElementAt(player));
        }

        private IEnumerable<IEnumerable<MoveT>> GetParetoOptimalMoves(IGameState<RoleT, MoveT> state)
        {
            var candidateMovesByPlayer = game.GetPossibleMoves(state);

            for (bool updated = true; updated;)
            {
                updated = false;

                for (int player = 0; player < game.GetNumberOfPlayers() && !updated; player++)
                {
                    var candidateMovesByPlayerWithoutSelf = (MoveT[][])candidateMovesByPlayer.Clone();
                    candidateMovesByPlayerWithoutSelf[player] = new MoveT[] { default };

                    var possibleCombinedMovesByOthers = candidateMovesByPlayerWithoutSelf.CartesianProduct();

                    var ownMoveUtilities = new Dictionary<MoveT, IEnumerable<double>>();

                    foreach (var individualMove in candidateMovesByPlayer[player])
                    {
                        ownMoveUtilities[individualMove] = possibleCombinedMovesByOthers
                            .Select((move) =>
                            {
                                var moveWithSelf = move.ToArray();
                                moveWithSelf[player] = individualMove;

                                return GetUtilitiesForState(state.AfterMove(moveWithSelf)).ElementAt(player);
                            });
                    }

                    foreach (var move1 in ownMoveUtilities.Keys)
                    {
                        var move1Utilities = ownMoveUtilities[move1];
                        var dominated = false;

                        foreach (var move2 in ownMoveUtilities.Keys)
                        {
                            if (move1.Equals(move2)) continue;

                            var move2Utilities = ownMoveUtilities[move2];

                            var zipped = move1Utilities.Zip(move2Utilities);

                            if (zipped.All((e) => e.First <= e.Second) && zipped.Any((e) => e.First < e.Second))
                            {
                                dominated = true;
                                updated = true;
                                break;
                            }
                        }

                        if (dominated)
                        {
                            var newCandidateMoves = candidateMovesByPlayer[player].ToList();
                            newCandidateMoves.Remove(move1);

                            candidateMovesByPlayer[player] = newCandidateMoves.ToArray();
                        }

                    }
                }
            }

            return candidateMovesByPlayer;
        }

        private IEnumerable<double> GetUtilitiesForState(IGameState<RoleT, MoveT> state)
        {
            var terminalUtilities = state.GetUtilities();
            if (terminalUtilities != null) return terminalUtilities.Select((e) => (double) e);

            var beliefsByPlayer = GetBeliefsByPlayerFromState(state);

            var legalMovesByPlayer = game.GetPossibleMoves(state);
            var expectedMovesByPlayer = beliefsByPlayer.Select((beliefs, player) => beliefs.GetBestMoves(legalMovesByPlayer[player], GetUtilityForMove));

            var expectedCombinedMoves = expectedMovesByPlayer.CartesianProduct();

            return from player
                   in Enumerable.Range(0, game.GetNumberOfPlayers())
                   select expectedCombinedMoves.Average((move) => GetUtilitiesForState(state.AfterMove(move)).ElementAt(player));
        }

        private Beliefs<RoleT, MoveT> GetBeliefsAfterMove(int player, Beliefs<RoleT, MoveT> beliefs, IEnumerable<MoveT> move)
        {
            for (int sender = 0; sender < game.GetNumberOfPlayers(); sender++)
            {
                var individualMove = move.ElementAt(sender);

                if (game.IsClaim(individualMove, out var receivers, out var claim))
                {
                    foreach (var receiver in receivers)
                    {
                        if (receiver == player) beliefs = UpdateBeliefsWithClaim(beliefs, sender, claim, move);
                    }
                }
            }

            return UpdateBeliefsWithPlayedMove(beliefs, move);
        }

        public void ReportPlayedMove(MoveT[] movePlayed, (int sender, Func<IGameState<RoleT, MoveT>, bool> claim)[] claims)
        {
            beliefs = claims.Aggregate(beliefs, (beliefs, e) => UpdateBeliefsWithClaim(beliefs, e.sender, e.claim, movePlayed));
            beliefs = UpdateBeliefsWithPlayedMove(beliefs, movePlayed);
        }

        private Beliefs<RoleT, MoveT> UpdateBeliefsWithClaim(Beliefs<RoleT, MoveT> currentBeliefs, int senderId, Func<IGameState<RoleT, MoveT>, bool> claim, IEnumerable<MoveT> movePlayed)
        {
            var newBeliefsIfTrusted = currentBeliefs.Filter(claim);
            if (newBeliefsIfTrusted.IsEmpty()) return currentBeliefs.Copy();

            // Note: Due to difficult to debug recursion errors, for some cases we defer to Players/Specifics.cs

            // If it would detriment the claimer to believe it if it were false, believe it
            var gameStatesWhereFalse = currentBeliefs.possibleGameStates
                .Where((e) => !claim(e.state))
                .Select((e) => e.state);

            var utilityPairsForGameStatesWhereFalse = gameStatesWhereFalse
                .Select((state) => {
                    var player = currentBeliefs.player;
                    var nextState = state.AfterMove(movePlayed);
                    //var beliefsByPlayer = GetBeliefsByPlayerFromState(state);

                    //var beliefsByPlayerIfTrusted = (Beliefs<RoleT, MoveT>[]) beliefsByPlayer.Clone();
                    //beliefsByPlayerIfTrusted[player] = newBeliefsIfTrusted;

                    beliefOverrides[player][state] = currentBeliefs;
                    beliefOverrides[player][nextState] = UpdateBeliefsWithPlayedMove(currentBeliefs, movePlayed);
                    var defaultUtility = GetUtilitiesForState(nextState).ElementAt(senderId);

                    beliefOverrides[player][state] = newBeliefsIfTrusted;
                    beliefOverrides[player][nextState] = UpdateBeliefsWithPlayedMove(newBeliefsIfTrusted, movePlayed);
                    var utilityWithBelief = GetUtilitiesForState(nextState).ElementAt(senderId);

                    beliefOverrides[player].Remove(state);
                    beliefOverrides[player].Remove(nextState);

                    return (defaultUtility, utilityWithBelief);
                });

            if (utilityPairsForGameStatesWhereFalse.All((e) => e.defaultUtility >= e.utilityWithBelief)
                && utilityPairsForGameStatesWhereFalse.Any((e) => e.defaultUtility > e.utilityWithBelief)) return newBeliefsIfTrusted;

            return currentBeliefs.Copy();
        }

        private Beliefs<RoleT, MoveT> UpdateBeliefsWithPlayedMove(Beliefs<RoleT, MoveT> currentBeliefs, IEnumerable<MoveT> movePlayed)
        {
            return currentBeliefs
                .Filter((state) =>
                {
                    var legalMoves = game.GetPossibleMoves(state);

                    for (int i = 0; i < movePlayed.Count(); i++)
                    {
                        // TODO Handle case where move is unknown
                        //if (!Array.Exists(legalMoves[i], (move) => move.Equals(movePlayed.ElementAt(i)))) return false;
                    }

                    return true;
                })
                .Update(movePlayed);
        }

        private Beliefs<RoleT, MoveT>[] GetBeliefsByPlayerFromState(IGameState<RoleT, MoveT> state)
        {
            var beliefsByPlayer = new Beliefs<RoleT, MoveT>[game.GetNumberOfPlayers()];

            for (int player = 0; player < game.GetNumberOfPlayers(); player++) beliefsByPlayer[player] = GetBeliefsFromState(player, state);

            return beliefsByPlayer;
        }

        private Beliefs<RoleT, MoveT> GetBeliefsFromState(int player, IGameState<RoleT, MoveT> state)
        {
            if (state.History.Length == 0) return new Beliefs<RoleT, MoveT>(player, game, state.Allocation[player]);
            if (beliefOverrides[player].TryGetValue(state, out var immediateBeliefs)) return immediateBeliefs;

            if (state.History.Length > 1)
            {
                var stateHistory = new IGameState<RoleT, MoveT>[state.History.Length - 1];
                stateHistory[0] = game.GetInitialState(state.Allocation).AfterMove(state.History[0]);

                for (int i = 1; i < stateHistory.Length; i++) stateHistory[i] = stateHistory[i - 1].AfterMove(state.History[i]);

                for (int i = stateHistory.Length - 1; i >= 0; i--)
                {
                    if (beliefOverrides[player].TryGetValue(stateHistory[i], out var beliefs))
                    {
                        for (int j = i + 1; j < state.History.Length; j++)
                        {
                            beliefs = GetBeliefsAfterMove(player, beliefs, state.History[j]);
                        }

                        return beliefs;
                    }
                }
            }

            return state.History.Aggregate(
                new Beliefs<RoleT, MoveT>(player, game, state.Allocation[player]),
                (beliefs, move) => GetBeliefsAfterMove(player, beliefs, move)
            );
        }
    }

    internal class Beliefs<RoleT, MoveT>
    {
        internal readonly int player;
        internal readonly IEnumerable<(IGameState<RoleT, MoveT> state, double weight)> possibleGameStates; // TODO Refactor visibility

        public Beliefs(int player, IGame<RoleT, MoveT> game, RoleT allocatedRole)
        {
            this.player = player;

            possibleGameStates = game
                .GetPossibleAllocations()
                .Where((allocation) => allocation.roles[player].Equals(allocatedRole))
                .Select((allocation) => (game.GetInitialState(allocation.roles), (double) allocation.weight));
        }

        private Beliefs(int player, IEnumerable<(IGameState<RoleT, MoveT>, double)> possibleGameStates)
        {
            this.player = player;
            this.possibleGameStates = possibleGameStates;
        }

        internal IEnumerable<MoveT> GetBestMoves(IEnumerable<MoveT> legalMoves, Func<IGameState<RoleT, MoveT>, int, MoveT, double> getUtilityForMove)
        {
            if (legalMoves.Count() == 1) return new[] { legalMoves.ElementAt(0) };

            var bestMoves = new List<MoveT>();
            double bestUtility = 0;

            foreach (var individualMove in legalMoves)
            {
                double weightedUtility = 0;

                foreach ((var state, var weight) in possibleGameStates) weightedUtility += getUtilityForMove(state, player, individualMove) * weight;

                if (bestUtility < weightedUtility)
                {
                    bestMoves.Clear();
                    bestUtility = weightedUtility;
                }

                if (bestUtility == weightedUtility) bestMoves.Add(individualMove);
            }

            //utility = bestUtility / possibleGameStates.Sum((e) => e.Item2);

            return bestMoves;
        }

        internal bool IsEmpty()
        {
            return possibleGameStates.Count() == 0;
        }

        internal Beliefs<RoleT, MoveT> Copy()
        {
            return new Beliefs<RoleT, MoveT>(player, possibleGameStates.Select((e) => e));
        }

        internal Beliefs<RoleT, MoveT> Filter(Func<IGameState<RoleT, MoveT>, bool> knowledge)
        {
            return new Beliefs<RoleT, MoveT>(player, possibleGameStates.Where((e) => knowledge(e.Item1)));
        }

        internal Beliefs<RoleT, MoveT> Update(IEnumerable<MoveT> move)
        {
            return new Beliefs<RoleT, MoveT>(player, possibleGameStates.Select((e) => (e.Item1.AfterMove(move), e.Item2)));
        }
    }
}
