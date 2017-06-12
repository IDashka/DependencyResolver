using System.Collections.Generic;
using System.Linq;

namespace DependencyResolver.FileHelpers {
    public static class StringExtensions {
        public static bool StartsWithAny(this string text, IList<string> substringList) {
            return substringList.Any(text.StartsWith);
        }
    }
}