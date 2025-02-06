using System.Diagnostics;

namespace MarvelRivalManager.Library.Entities
{
    public class Delegates
    {
        public delegate ValueTask AsyncAction();
        public delegate ValueTask<T> AsyncActionWithTimer<T>(Stopwatch time);
        public delegate ValueTask Log(string[] codes, PrintParams @params);

        public static AsyncAction EmptyAsyncAction => () => ValueTask.CompletedTask;
        public static Log EmptyLog => (string[] codes, PrintParams @params) => ValueTask.CompletedTask;
    }
    
    public record struct PrintParams(
        string Action,
        string Time = "",
        string Name = "",
        bool UndoLast = false
    );
}
