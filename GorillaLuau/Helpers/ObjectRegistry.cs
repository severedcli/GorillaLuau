using System.Collections.Generic;
using UnityEngine;

namespace GorillaLuau.Helpers
{
    public class ObjectRegistry
    {
        private static Dictionary<int, Object> idToObject = new();
        private static Dictionary<Object, int> objectToId = new();
        private static int nextId = 1;

        public static int Register(Object obj)
        {
            if (obj == null)
                return 0;

            if (objectToId.TryGetValue(obj, out int id))
                return id;

            id = nextId++;
            idToObject[id] = obj;
            objectToId[obj] = id;
            return id;
        }

        public static Object Get(int id)
        {
            if (id == 0)
                return null;

            if (idToObject.TryGetValue(id, out var obj))
                return obj;

            return null;
        }

        public static void Clear()
        {
            idToObject.Clear();
            objectToId.Clear();
            nextId = 1;
        }
    }
}
