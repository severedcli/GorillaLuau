using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

using GorillaLuau.Helpers;

namespace GorillaLuau.Lua
{
    public unsafe class Wrapper
    {
        public static Dictionary<string, GameObject> GameObjectsRegistry = new();

        public static void RegisterGameObjects()
        {
            GameObjectsRegistry.Clear();
            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType<GameObject>())
            {
                if (!GameObjectsRegistry.ContainsKey(go.name))
                {
                    GameObjectsRegistry[go.name] = go;
                }
            }
        }
        public static int GetObject(lua_State* state)
        {
            string name = Marshal.PtrToStringAnsi((IntPtr)Luau.lua_tolstring(state, 1, null));
            if (GameObjectsRegistry.TryGetValue(name, out GameObject obj))
            {
                Helper.PushGameObject(state, obj);
            }
            else
            {
                Luau.lua_pushnil(state);
            }

            return 1;
        }

        public static int GetComponent(lua_State* state)
        {
            GameObject obj = Helper.CheckGameObject(state, 1);
            if (obj == null)
            {
                myLogger.LogError("`GetComponent` called on null object");
                Luau.lua_pushnil(state);
                return 1;
            }

            string typeName = Marshal.PtrToStringAnsi((nint)Luau.lua_tolstring(state, 2, null));
            Type type = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(typeName);
                if (type != null) break;
            }

            if (type == null)
            {
                myLogger.LogError($"Type `{typeName}` wasn't found.");
                Luau.lua_pushnil(state);
                return 1;
            }

            Component comp = obj.GetComponent(type);
            if (comp != null)
                Helper.PushComponent(state, comp);
            else
                Luau.lua_pushnil(state);

            return 1;
        }

        public static int GetProperty(lua_State* state)
        {
            object obj = Helper.CheckObject(state, 1);
            if (obj == null)
            {
                myLogger.LogError("`GetProperty` called on null object");
                Luau.lua_pushnil(state);
                return 1;
            }

            string propName = Marshal.PtrToStringAnsi((IntPtr)Luau.lua_tolstring(state, 2, null));
            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (prop != null && prop.CanRead)
            {
                try
                {
                    object value = prop.GetValue(prop.GetGetMethod().IsStatic ? null : obj);
                    Helper.PushValue(state, value);
                }
                catch (Exception e)
                {
                    myLogger.LogError($"Error getting property '{propName}': {e}");
                    Luau.lua_pushnil(state);
                }
            }
            else
            {
                var field = obj.GetType().GetField(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (field != null)
                {
                    try
                    {
                        object value = field.GetValue(field.IsStatic ? null : obj);
                        Helper.PushValue(state, value);
                    }
                    catch (Exception e)
                    {
                        myLogger.LogError($"Error getting field '{propName}': {e}");
                        Luau.lua_pushnil(state);
                    }
                }
                else
                {
                    myLogger.LogError($"Property or field '{propName}' not found or not readable in type '{obj.GetType().Name}'");
                    Luau.lua_pushnil(state);
                }
            }

            return 1;
        }

        public static int SetProperty(lua_State* state)
        {
            object obj = Helper.CheckObject(state, 1);
            if (obj == null)
            {
                myLogger.LogError("`SetProperty` called on null object");
                return 1;
            }

            string propName = Marshal.PtrToStringAnsi((IntPtr)Luau.lua_tolstring(state, 2, null));
            object value = Helper.ConvertLuaTableValue(state, 3, obj.GetType(), propName);
            if (value == null)
            {
                myLogger.LogError($"Failed to convert Lua value for property '{propName}'");
                return 1;
            }

            var prop = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (prop != null && prop.CanWrite)
            {
                try
                {
                    prop.SetValue(prop.GetSetMethod().IsStatic ? null : obj, value);
                }
                catch (Exception e)
                {
                    myLogger.LogError($"Error setting property `{propName}`: {e.Message}");
                }
            }
            else
            {
                var field = obj.GetType().GetField(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (field != null && !field.IsInitOnly && !field.IsLiteral)
                {
                    try
                    {
                        field.SetValue(field.IsStatic ? null : obj, value);
                    }
                    catch (Exception e)
                    {
                        myLogger.LogError($"Error setting field '{propName}': {e}");
                    }
                }
                else
                {
                    myLogger.LogError($"Property or field '{propName}' not found, not writable, or read-only in type '{obj.GetType().Name}'");
                }
            }

            return 1;
        }

        public static int GetType(lua_State* state)
        {
            string assemblyName = Marshal.PtrToStringAnsi((IntPtr)Luau.lua_tolstring(state, 1, null));
            string typeName = Marshal.PtrToStringAnsi((IntPtr)Luau.lua_tolstring(state, 2, null));
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == assemblyName)
                {
                    Type type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        IntPtr ptr = Marshal.GetIUnknownForObject(type);
                        Luau.lua_pushlightuserdatatagged(state, (void*)ptr, 0);
                        return 1;
                    }
                }
            }

            Luau.lua_pushnil(state);
            return 1;
        }

        public static int CallFunction(lua_State* state)
        {
            object obj = Helper.CheckObject(state, 1);
            string methodName = Marshal.PtrToStringAnsi((IntPtr)Luau.lua_tolstring(state, 2, null));
            if (obj == null && methodName == null)
            {
                myLogger.LogError("`CallFunction` called with null object and null method name");
                Luau.lua_pushnil(state);
                return 1;
            }

            Type type = null;
            if (obj == null)
            {
                type = typeof(GameObject);
            }
            else
            {
                type = obj.GetType();
            }

            int argCount = Luau.lua_gettop(state) - 2;
            object[] parameters = new object[argCount];
            Type[] parameterTypes = new Type[argCount];

            for (int i = 0; i < argCount; i++)
            {
                parameters[i] = Helper.ConvertLuaValue(state, 3 + i, null);
                parameterTypes[i] = parameters[i]?.GetType() ?? typeof(object);
            }

            MethodInfo method = null;
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            foreach (var m in methods)
            {
                if (m.Name == methodName)
                {
                    var paramInfos = m.GetParameters();

                    bool correctStaticness = (obj == null && m.IsStatic) || (obj != null && !m.IsStatic);
                    if (!correctStaticness) continue;

                    if (paramInfos.Length == argCount)
                    {
                        bool compatible = true;
                        for (int i = 0; i < argCount; i++)
                        {
                            if (parameters[i] != null)
                            {
                                if (paramInfos[i].ParameterType.IsEnum && parameters[i] is double)
                                {
                                    continue;
                                }

                                if (!paramInfos[i].ParameterType.IsInstanceOfType(parameters[i]))
                                {
                                    if (!Helper.CanConvert(parameters[i], paramInfos[i].ParameterType))
                                    {
                                        compatible = false;
                                        break;
                                    }
                                }
                            }
                            else if (paramInfos[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(paramInfos[i].ParameterType) == null)
                            {
                                compatible = false;
                                break;
                            }
                        }

                        if (compatible)
                        {
                            method = m;
                            break;
                        }
                    }
                }
            }

            if (method == null)
            {
                foreach (var m in methods)
                {
                    if (m.Name == methodName)
                    {
                        bool correctStatic = (obj == null && m.IsStatic) || (obj != null && !m.IsStatic);
                        if (!correctStatic) continue;

                        method = m;
                        break;
                    }
                }

                if (method == null)
                {
                    string typeName = obj != null ? obj.GetType().Name : type.Name;
                    myLogger.LogError($"Method '{methodName}' not found in type '{typeName}' (static: {obj == null})");
                    Luau.lua_pushnil(state);
                    return 1;
                }

                var paramInfos = method.GetParameters();
                object[] adjustedParams = new object[paramInfos.Length];

                for (int i = 0; i < paramInfos.Length; i++)
                {
                    if (i < parameters.Length && parameters[i] != null)
                    {
                        adjustedParams[i] = Helper.ConvertValue(parameters[i], paramInfos[i].ParameterType);
                    }
                    else if (paramInfos[i].HasDefaultValue)
                    {
                        adjustedParams[i] = paramInfos[i].DefaultValue;
                    }
                    else if (paramInfos[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(paramInfos[i].ParameterType) == null)
                    {
                        myLogger.LogError($"Method '{methodName}' requires parameter '{paramInfos[i].Name}' of type '{paramInfos[i].ParameterType.Name}' but none provided");
                        Luau.lua_pushnil(state);
                        return 1;
                    }
                    else
                    {
                        adjustedParams[i] = null;
                    }
                }

                parameters = adjustedParams;
            }
            else
            {
                var paramInfos = method.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i] != null)
                    {
                        parameters[i] = Helper.ConvertValue(parameters[i], paramInfos[i].ParameterType);
                    }
                }
            }

            try
            {
                object result = method.Invoke(obj, parameters);
                Helper.PushValue(state, result);
                return 1;
            }
            catch (Exception e)
            {
                string typeName = obj != null ? obj.GetType().Name : type.Name;
                myLogger.LogError($"Error calling method '{methodName}' on type '{typeName}': {e}");
                Luau.lua_pushnil(state);
                return 1;
            }
        }
    }
}
