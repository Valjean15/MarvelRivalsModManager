namespace MarvelRivalManager.Library.Entities
{
    public class Delegates
    {
        public delegate ValueTask AsyncAction();
        public delegate ValueTask Log(string[] codes, PrintParams @params);
    }
    
    public record struct PrintParams(
        string Action,
        string Time = "",
        string Name = "",
        bool UndoLast = false
    );
}
