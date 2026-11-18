using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // Known issues Sprint 6, review of 2ae4cc8b (finding 4). The native UnitCommands.Run calls in progress, innermost last.
    // Review of fa533e97 (R19): only the innermost open Run on the ending command's own container decides. A pair command
    // that ends inside that Run, when it runs another command, was replaced by it; one that ends inside its own Run was
    // not, whatever outer Run on the same container is still open. Each Run opens its own token and closes exactly that token in a finally, so nothing depends on which pair owns
    // the actor when the Run ends, an inner Run never closes an outer one, an exception cannot leave a scope open, and a
    // token that is somehow left above the one being closed is removed with it.
    public sealed class MountedNativeRunScopes<TContainer, TCommand>
        where TContainer : class
        where TCommand : class
    {
        public sealed class Token
        {
            internal TContainer Container;
            internal TCommand Command;
        }

        private readonly List<Token> open = new List<Token>();

        public int Depth => open.Count;

        public Token Open(TContainer container, TCommand command)
        {
            var token = new Token { Container = container, Command = command };
            open.Add(token);
            return token;
        }

        public void Close(Token token)
        {
            var index = token == null ? -1 : open.LastIndexOf(token);
            if (index >= 0) open.RemoveRange(index, open.Count - index);
        }

        public bool EndedInsideReplacingRun(TContainer container, TCommand ended)
        {
            if (container == null) return false;
            for (var index = open.Count - 1; index >= 0; index--)
            {
                // The innermost matching token is the answer; no outer token is consulted after it.
                if (ReferenceEquals(open[index].Container, container)) return !ReferenceEquals(open[index].Command, ended);
            }
            return false;
        }

        // Mod disable and disposal: no Run can be in progress then, and nothing retained may outlive the session.
        public void Reset() => open.Clear();
    }
}
