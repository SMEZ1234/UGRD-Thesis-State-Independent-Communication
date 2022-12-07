using System;
using System.Collections.Generic;
using System.Linq;
using ThesisPlayer.Games;

namespace ThesisPlayer
{
    class Program
    {
        static void Main()
        {
            //Test2PlayerGame(new GoodOrEvilGame(), 10000);
            //Test2PlayerGame(new CooperationGame(), 10000);
            //Test2PlayerGame(new EnvelopeGame(), 10000);
            //TestProbableSharedEnvelopeGame(10000);
            TestOneShotInvestigation(1000000);

            //var game = new GoodOrEvilGame();
            //var guesser = new CommunicatingPlayer<GoodOrEvilGameRole, GoodOrEvilGameMove>();
            //var subject = new TruthfulPlayer<GoodOrEvilGameRole, GoodOrEvilGameMove>();

            //var manager = new GameManager<GoodOrEvilGameRole, GoodOrEvilGameMove>(game, guesser, subject);

            //var totalUtilities = new double[] { 0, 0 };
            //int nPlays = 100;

            //for (int i = 0; i < nPlays; i++)
            //{
            //    (var allocation, var utilities) = manager.Play();

            //    for (int j = 0; j < utilities.Length; j++) totalUtilities[j] += utilities[j];
            //}

            //Console.WriteLine($"Utilities: {totalUtilities[0] / nPlays}, {totalUtilities[1] / nPlays}");


            //var game = new TicTacToe();
            //var player = new RationalPlayer<TicTacToeRole, TicTacToeMove>();

            //var state = new TicTacToeState(new[] { TicTacToeRole.CROSSES, TicTacToeRole.NOUGHTS }, new[] {
            //    new[] { TicTacToeMove.TOP_LEFT, TicTacToeMove.NO_OP },
            //    new[] { TicTacToeMove.NO_OP, TicTacToeMove.CENTER },
            //    new[] { TicTacToeMove.TOP_RIGHT, TicTacToeMove.NO_OP },
            //    new[] { TicTacToeMove.NO_OP, TicTacToeMove.TOP_MIDDLE },
            //});

            //// Progress player to current position
            //player.InitialiseGame(1, game, TicTacToeRole.NOUGHTS);
            //foreach (var move in state.History) player.ReportPlayedMove(move);

            //player.PlayMove(game.GetPossibleMoves(state)[1]);


            //var game = new CooperationGame();
            //var player = new CommunicatingPlayer<CooperationGameRole, CooperationGameMove>();
            //var id = 0;

            //var state = new CooperationGameState(new[] { CooperationGameRole.GUESSER, CooperationGameRole.HAS_ONE }, new[] {
            //    new[] { CooperationGameMove.NO_OP, CooperationGameMove.CLAIM_ONE },
            //});

            //// Progress player to current position
            //player.InitialiseGame(id, game, state.Allocation[id]);
            //foreach (var move in state.History) player.ReportPlayedMove(move, ((IGame<CooperationGameRole, CooperationGameMove>)game).GetClaimsFromMoveByReceiver(move).ElementAt(id).ToArray());

            //player.PlayMove(game.GetPossibleMoves(state)[id]);
        }

        static void Test2PlayerGame<RoleT, MoveT>(IGame<RoleT, MoveT> game, int nPlays)
        {
            var receivers = PossibleReceivers(game);
            var senders = PossibleSenders(game);

            var results = new List<string>();

            foreach (var receiver in receivers)
            {
                foreach (var sender in senders)
                {
                    var utilities = TestPlayers(game, nPlays, receiver.player, sender.player);
                    results.Add(string.Join(", ", utilities));

                    Console.WriteLine($"{receiver.label} receiver: {utilities[0]}, {sender.label} sender: {utilities[1]}");
                }
            }

            for (var i = 0; i < receivers.Length; i++)
            {
                Console.WriteLine(string.Join("  |  ", results.GetRange(i * senders.Length, senders.Length)));
            }
        }

        static void TestProbableSharedEnvelopeGame(int nPlays)
        {
            var game = new ProbableSharedEnvelopeGame();

            var senders = PossibleSenders(game);
            var receivers2nd = PossibleReceivers(game);
            var receivers3rd = PossibleReceivers(game);

            var results = new List<string>();

            for (var i = 0; i < receivers2nd.Length; i++)
            {
                foreach (var sender in senders)
                {
                    var utilities = TestPlayers(game, nPlays, sender.player, receivers2nd[i].player, receivers3rd[i].player);
                    results.Add(string.Join(", ", utilities[1], utilities[2], utilities[0]));

                    Console.WriteLine($"{receivers2nd[i].label} receivers: {utilities[1]}, {utilities[2]}, {sender.label} sender: {utilities[0]}");
                }
            }

            for (var i = 0; i < receivers2nd.Length; i++)
            {
                Console.WriteLine(string.Join("  |  ", results.GetRange(i * senders.Length, senders.Length)));
            }
        }

        static void TestOneShotInvestigation(int nPlays)
        {
            var game = new OneShotInvestigationGame();

            var suspects1 = PossibleSenders(game);
            var suspects2 = PossibleSenders(game);
            var investigators = PossibleReceivers(game);

            var results = new List<string>();

            //var suspectPairs = new List<(IPlayer<OneShotInvestigationGameRole, OneShotInvestigationGameMove>>
            var suspectPairs = suspects1
                .SelectMany((suspect1, i) => suspects2
                    .Skip(i)
                    .Select((suspect2) => (suspect1, suspect2))
                );

            foreach (var investigator in investigators)
            {
                foreach (var suspectPair in suspectPairs)
                {
                    var utilities = TestPlayers(game, nPlays, investigator.player, suspectPair.suspect1.player, suspectPair.suspect2.player);
                    results.Add(string.Join(", ", utilities));

                    Console.WriteLine($"{investigator.label} investigator: {utilities[0]}, {suspectPair.suspect1.label} suspect: {utilities[1]}, {suspectPair.suspect2.label} suspect: {utilities[2]}");
                }
            }            

            for (var i = 0; i < investigators.Length; i++)
            {
                Console.WriteLine(string.Join("  |  ", results.GetRange(i * suspectPairs.Count(), suspectPairs.Count())));
            }
        }

        static (string label, IPlayer<RoleT, MoveT> player)[] PossibleReceivers<RoleT, MoveT>(IGame<RoleT, MoveT> game)
        {
            return new (string label, IPlayer<RoleT, MoveT> player)[]
            {
                ("Random", new RandomPlayer<RoleT, MoveT>()),
                //("Rational", new RationalPlayer<RoleT, MoveT>()),
                ("Trusting", new TrustingPlayer<RoleT, MoveT>()),
                ("Proposed", GetCommunicatingReceiverForGame(game)),
            };
        }

        static IPlayer<RoleT, MoveT> GetCommunicatingReceiverForGame<RoleT, MoveT>(IGame<RoleT, MoveT> game)
        {
            if (game is OneShotInvestigationGame) return (IPlayer<RoleT, MoveT>)new OneShotInvestigator();

            return new CommunicatingPlayer<RoleT, MoveT>();
        }

        static (string label, IPlayer<RoleT, MoveT> player)[] PossibleSenders<RoleT, MoveT>(IGame<RoleT, MoveT> game)
        {
            return new (string label, IPlayer<RoleT, MoveT> player)[]
            {
                ("Random", new RandomPlayer<RoleT, MoveT>()),
                ("Truthful", new TruthfulPlayer<RoleT, MoveT>()),
                ("Proposed", GetCommunicatingSenderForGame(game)),
            };
        }

        static IPlayer<RoleT, MoveT> GetCommunicatingSenderForGame<RoleT, MoveT>(IGame<RoleT, MoveT> game)
        {
            if (game is EnvelopeGame) return (IPlayer<RoleT, MoveT>)new EnvelopeSender();
            if (game is OneShotInvestigationGame) return (IPlayer<RoleT, MoveT>)new OneShotSuspect();

            return new CommunicatingPlayer<RoleT, MoveT>();
        }

        static double[] TestPlayers<RoleT, MoveT>(IGame<RoleT, MoveT> game, int nPlays, params IPlayer<RoleT, MoveT>[] players)
        {
            var manager = new GameManager<RoleT, MoveT>(game, players);

            var totalUtilities = players.Select((p) => 0d).ToArray();

            for (int i = 0; i < nPlays; i++)
            {
                (var allocation, var utilities) = manager.Play();

                for (int j = 0; j < utilities.Length; j++) totalUtilities[j] += utilities[j];
            }

            return totalUtilities.Select((total) => total / nPlays).ToArray();
        }
    }

    public static class Extensions
    {
        internal static T Select<T>(this Random random, IEnumerable<T> list)
        {
            return list.ElementAt(random.Next(list.Count()));
        }
        internal static OneShotInvestigationGameMove Select(this Random random, params OneShotInvestigationGameMove[] list)
        {
            return list[random.Next(list.Length)];
        }

        internal static T[] Concat<T>(this T[] array, T element)
        {
            var result = new T[array.Length + 1];
            array.CopyTo(result, 0);
            result[array.Length] = element;

            return result;
        }

        internal static bool ContainsAll<T>(this IEnumerable<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items) if (!collection.Contains(item)) return false;
            return true;
        }

        /**
         * Taken from https://ericlippert.com/2010/06/28/computing-a-cartesian-product-with-linq/
         */
        internal static IEnumerable<IEnumerable<T>> CartesianProduct<T>
            (this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct =
              new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
              emptyProduct,
              (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item }));
        }
    }
}
