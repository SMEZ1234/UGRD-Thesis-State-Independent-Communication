using System;
using System.Collections.Generic;
using System.Linq;

namespace ThesisPlayer
{
    interface IGame<RoleT, MoveT>
    {
        int GetNumberOfPlayers();
        (int weight, RoleT[] roles)[] GetPossibleAllocations();
        IGameState<RoleT, MoveT> GetInitialState(RoleT[] allocation);
        MoveT[][] GetPossibleMoves(IGameState<RoleT, MoveT> currentState);
        bool IsClaim(MoveT move, out int[] receivers, out Func<IGameState<RoleT, MoveT>, bool> claim);

        IEnumerable<(int sender, Func<IGameState<RoleT, MoveT>, bool> claim)>[] GetClaimsFromMoveByReceiver(IEnumerable<MoveT> move)
        {
            var nPlayers = GetNumberOfPlayers();
            var claims = new List<(int sender, Func<IGameState<RoleT, MoveT>, bool> claim)>[nPlayers];

            for (int receiver = 0; receiver < nPlayers; receiver++) claims[receiver] = new List<(int sender, Func<IGameState<RoleT, MoveT>, bool> claim)>();

            for (int sender = 0; sender < nPlayers; sender++)
            {
                if (IsClaim(move.ElementAt(sender), out var receivers, out var claim))
                {
                    foreach (var receiver in receivers) claims[receiver].Add((sender, claim));
                }
            }

            return claims;
        }
    }

    interface IGameState<RoleT, MoveT>
    {
        RoleT[] Allocation { get; }
        MoveT[][] History { get; }

        IGameState<RoleT, MoveT> AfterMove(IEnumerable<MoveT> move);
        int[] GetUtilities();
        
        bool IsEquivalentTo(IGameState<RoleT, MoveT> other)
        {
            if (History.Length != other.History.Length || !Allocation.SequenceEqual(other.Allocation)) return false;

            for (int i = 0; i < History.Length; i++)
            {
                if (!History[i].SequenceEqual(other.History[i])) return false;
            }

            return true;
        }
    }

    class GameStateEqualityComparer<RoleT, MoveT> : EqualityComparer<IGameState<RoleT, MoveT>>
    {
        public override bool Equals(IGameState<RoleT, MoveT> x, IGameState<RoleT, MoveT> y) => x.IsEquivalentTo(y);

        public override int GetHashCode(IGameState<RoleT, MoveT> obj)
        {
            return 0; // FIXME This is a terrible hash
        }
    }
}
