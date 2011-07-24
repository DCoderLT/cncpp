using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCClasses.Helpers {
    public static class GenericExtensions {

        public static T At<T>(this Dictionary<String, T> Dict, int Index) {
            if (Index < 0) {
                throw new IndexOutOfRangeException();
            }
            return Dict.ElementAt(Index).Value;
        }
    }
}
