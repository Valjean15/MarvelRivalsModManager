namespace MarvelRivalManager.Library.Entities
{
    public class Delegates
    {
        public delegate ValueTask Print(string message);
        public delegate ValueTask PrintAndUndo(string message, bool undoLast);
    }
}
