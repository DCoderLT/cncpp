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

    public class CCCollection<T> : List<T> where T : AbstractHierarchy.AbstractTypeClass, new() {
        public T FindID(String ID) {
            return Find(o => o.ID.Equals(ID));
        }

        public T FindOrAllocate(String _ID) {
            var found = FindID(_ID);
            if (found == null) {
                found = new T() {
                    ID = _ID 
                };
            }
            return found;
        }
    }

    public class WeakRef<T> where T : class {
        private WeakReference _Value;
        public WeakRef(T val) {
            _Value = new WeakReference(val);
        }

        public T Value {
            get {
                return _Value.Target as T;
            }
        }

        internal void Empty() {
            _Value = new WeakReference(null);
        }
    }

}
