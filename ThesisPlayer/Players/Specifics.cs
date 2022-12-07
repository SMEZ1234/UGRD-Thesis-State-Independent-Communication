using System;
using System.Linq;
using ThesisPlayer.Games;

namespace ThesisPlayer
{
    // This file contains specific implementations of our strategies for some games to enable testing
    // despite some difficult to debug recursive errors in CommunicatingPlayer

    class EnvelopeSender : IPlayer<EnvelopeGameRole, EnvelopeGameMove>
    {
        bool isFirstHighest;

        public void InitialiseGame(int playerId, IGame<EnvelopeGameRole, EnvelopeGameMove> game, EnvelopeGameRole allocatedRole)
        {
            isFirstHighest = allocatedRole.inFirst >= allocatedRole.inSecond;
        }

        public EnvelopeGameMove PlayMove(EnvelopeGameMove[] legalMoves)
        {
            if (legalMoves.Length == 1) return legalMoves[0];

            return EnvelopeGameMove.CLAIMS
                .Where((move) => move.amount == 100 && move.isAboutFirst == isFirstHighest)
                .First();
        }

        public void ReportPlayedMove(EnvelopeGameMove[] movePlayed, (int sender, Func<IGameState<EnvelopeGameRole, EnvelopeGameMove>, bool> claim)[] claims)
        {
            // Do nothing as we only act once
        }
    }

    class OneShotSuspect : IPlayer<OneShotInvestigationGameRole, OneShotInvestigationGameMove>
    {
        OneShotInvestigationGameMove bestClaim;

        public void InitialiseGame(int playerId, IGame<OneShotInvestigationGameRole, OneShotInvestigationGameMove> game, OneShotInvestigationGameRole allocatedRole)
        {
            switch (allocatedRole)
            {
                case OneShotInvestigationGameRole.GOOD_OTHER_GOOD:
                case OneShotInvestigationGameRole.EVIL_OTHER_EVIL:
                    bestClaim = OneShotInvestigationGameMove.CLAIM_GOOD_GOOD;
                    break;
                case OneShotInvestigationGameRole.GOOD_OTHER_EVIL:
                case OneShotInvestigationGameRole.EVIL_OTHER_GOOD:
                    bestClaim = playerId == 1 ? OneShotInvestigationGameMove.CLAIM_GOOD_EVIL : OneShotInvestigationGameMove.CLAIM_EVIL_GOOD;
                    break;
            }
        }

        public OneShotInvestigationGameMove PlayMove(OneShotInvestigationGameMove[] legalMoves)
        {
            if (legalMoves.Length == 1) return legalMoves[0];

            return bestClaim;
        }

        public void ReportPlayedMove(OneShotInvestigationGameMove[] movePlayed, (int sender, Func<IGameState<OneShotInvestigationGameRole, OneShotInvestigationGameMove>, bool> claim)[] claims)
        {
            // Do nothing as we only act once
        }
    }

    class OneShotInvestigator : IPlayer<OneShotInvestigationGameRole, OneShotInvestigationGameMove>
    {
        readonly Random random = new Random();
        IGame<OneShotInvestigationGameRole, OneShotInvestigationGameMove> game;
        OneShotInvestigationGameMove decision;

        public void InitialiseGame(int playerId, IGame<OneShotInvestigationGameRole, OneShotInvestigationGameMove> game, OneShotInvestigationGameRole allocatedRole)
        {
            this.game = game;
        }

        public OneShotInvestigationGameMove PlayMove(OneShotInvestigationGameMove[] legalMoves)
        {
            if (legalMoves.Length == 1) return legalMoves[0];

            return decision;
        }

        public void ReportPlayedMove(OneShotInvestigationGameMove[] movePlayed, (int sender, Func<IGameState<OneShotInvestigationGameRole, OneShotInvestigationGameMove>, bool> claim)[] claims)
        {
            if (claims.Length != 2) return;

            var p1Claim = GetAllocationFromClaim(claims[0].claim);
            var p2Claim = GetAllocationFromClaim(claims[1].claim);

            if (p1Claim == OneShotInvestigationGameRole.GOOD_OTHER_GOOD)
            {
                if (p2Claim == OneShotInvestigationGameRole.GOOD_OTHER_GOOD) decision = random.Select(OneShotInvestigationGameMove.GUESS_GOOD_GOOD, OneShotInvestigationGameMove.GUESS_EVIL_EVIL);
                else if (p2Claim == OneShotInvestigationGameRole.EVIL_OTHER_GOOD) decision = OneShotInvestigationGameMove.GUESS_EVIL_GOOD;
                else if (p2Claim == OneShotInvestigationGameRole.GOOD_OTHER_EVIL) decision = random.Select(ALL_GUESSES);
                else decision = OneShotInvestigationGameMove.GUESS_EVIL_EVIL;
            }
            else if (p1Claim == OneShotInvestigationGameRole.EVIL_OTHER_GOOD)
            {
                if (p2Claim == OneShotInvestigationGameRole.GOOD_OTHER_GOOD) decision = random.Select(ALL_GUESSES);
                else if (p2Claim == OneShotInvestigationGameRole.EVIL_OTHER_GOOD) decision = OneShotInvestigationGameMove.GUESS_EVIL_GOOD;
                else if (p2Claim == OneShotInvestigationGameRole.GOOD_OTHER_EVIL) decision = random.Select(OneShotInvestigationGameMove.GUESS_GOOD_EVIL, OneShotInvestigationGameMove.GUESS_EVIL_GOOD);
                else decision = random.Select(OneShotInvestigationGameMove.GUESS_EVIL_GOOD, OneShotInvestigationGameMove.GUESS_EVIL_EVIL);
            }
            else if (p1Claim == OneShotInvestigationGameRole.GOOD_OTHER_EVIL)
            {
                if (p2Claim == OneShotInvestigationGameRole.GOOD_OTHER_GOOD) decision = OneShotInvestigationGameMove.GUESS_GOOD_EVIL;
                else if (p2Claim == OneShotInvestigationGameRole.EVIL_OTHER_GOOD) decision = random.Select(OneShotInvestigationGameMove.GUESS_GOOD_EVIL, OneShotInvestigationGameMove.GUESS_EVIL_GOOD);
                else if (p2Claim == OneShotInvestigationGameRole.GOOD_OTHER_EVIL) decision = OneShotInvestigationGameMove.GUESS_GOOD_EVIL;
                else decision = random.Select(OneShotInvestigationGameMove.GUESS_GOOD_EVIL, OneShotInvestigationGameMove.GUESS_EVIL_GOOD, OneShotInvestigationGameMove.GUESS_EVIL_EVIL);
            }
            else
            {
                if (p2Claim == OneShotInvestigationGameRole.GOOD_OTHER_GOOD) decision = random.Select(OneShotInvestigationGameMove.GUESS_GOOD_EVIL, OneShotInvestigationGameMove.GUESS_EVIL_GOOD, OneShotInvestigationGameMove.GUESS_EVIL_EVIL);
                else if (p2Claim == OneShotInvestigationGameRole.EVIL_OTHER_GOOD) decision = random.Select(OneShotInvestigationGameMove.GUESS_GOOD_EVIL, OneShotInvestigationGameMove.GUESS_EVIL_GOOD);
                else if (p2Claim == OneShotInvestigationGameRole.GOOD_OTHER_EVIL) decision = random.Select(OneShotInvestigationGameMove.GUESS_GOOD_EVIL, OneShotInvestigationGameMove.GUESS_EVIL_EVIL);
                else decision = OneShotInvestigationGameMove.GUESS_EVIL_EVIL;
            }
        }

        OneShotInvestigationGameRole GetAllocationFromClaim(Func<IGameState<OneShotInvestigationGameRole, OneShotInvestigationGameMove>, bool> claim) => game
            .GetPossibleAllocations()
            .Select((alloc) => game.GetInitialState(alloc.roles))
            .Where(claim)
            .First()
            .Allocation[1];

        static OneShotInvestigationGameMove[] ALL_GUESSES = new[] {
            OneShotInvestigationGameMove.GUESS_GOOD_GOOD,
            OneShotInvestigationGameMove.GUESS_GOOD_EVIL,
            OneShotInvestigationGameMove.GUESS_EVIL_GOOD,
            OneShotInvestigationGameMove.GUESS_EVIL_EVIL,
        };
    }
}
