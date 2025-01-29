using System.Linq;

namespace MarvelRivalManager.UI.ViewModels
{
    public class TagViewModel(string value)
    {
        public string Value { get; set; } = value;
        public string Text => string.Join("", Value.Take(9));
    }
}
