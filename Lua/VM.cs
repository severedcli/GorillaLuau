using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

using GorillaLuau.Helpers;

namespace GorillaLuau.Lua
{
    public unsafe class VM
    {
        public static void runCode(string code)
        {
            if (Plugin.instance.isAllowed)
            {
                // Listen, I have no idea why. But, if you don't do this the entire game crashes.. (someone please fix)
                myLogger.Log($"Code: \n{code}");

                lua_State* state = Luau.luaL_newstate();
                if (state == null)
                {
                    myLogger.LogError("Failed to create Lua state.");
                    return;
                }

                Luau.luaL_openlibs(state);

                Wrapper.RegisterGameObjects();

                Luau.lua_register(state, print, "print");
                Luau.lua_register(state, Wrapper.GetObject, "GetObject");
                Luau.lua_register(state, Wrapper.GetComponent, "GetComponent");
                Luau.lua_register(state, Wrapper.GetProperty, "GetProperty");
                Luau.lua_register(state, Wrapper.GetType, "GetType");
                Luau.lua_register(state, Wrapper.CallFunction, "CallFunction");
                Luau.lua_register(state, Wrapper.SetProperty, "SetProperty");

                // Again, I have no clue why but if you don't do this the entire game crashes.. (probably something with lua itself)
                byte[] utf8Bytes = Encoding.UTF8.GetBytes(code);
                string utf8Code = Encoding.UTF8.GetString(utf8Bytes);

                nuint byteSize = 0;
                lua_CompileOptions compileOptions = new lua_CompileOptions();
                sbyte* compilePtr = Luau.luau_compile(utf8Code, (nuint)utf8Bytes.Length, &compileOptions, &byteSize);
                if (compilePtr == null)
                {
                    myLogger.LogError("Failed to compile code.");
                    Luau.lua_close(state);
                    return;
                }

                byte[] compileBytes = new byte[(int)byteSize];
                Marshal.Copy((IntPtr)compilePtr, compileBytes, 0, (int)byteSize);

                fixed (byte* compile = compileBytes)
                {
                    int loadStatus = Luau.luau_load(state, "console", (sbyte*)compile, (UIntPtr)compileBytes.Length, Luau.LUA_GLOBALSINDEX);
                    if (loadStatus != (int)Luau.lua_Status.LUA_OK)
                    {
                        string error = LuaToString(state, -1) ?? "Unknown error";
                        myLogger.LogError($"Failed to load: ({(Luau.lua_Status)loadStatus}): {error}.");

                        Luau.lua_pop(state, 1);
                        Luau.lua_close(state);
                        return;
                    }
                }

                int pcallStatus = Luau.lua_pcall(state, 0, 0, 0);
                if (pcallStatus != 0)
                {
                    string error = LuaToString(state, -1) ?? "Unknown error";
                    myLogger.LogError($"Lua runtime error: ({(Luau.lua_Status)pcallStatus}): {error}.");
                }

                Luau.lua_pop(state, 1);
                Luau.lua_close(state);

                ObjectRegistry.Clear();
            }
        }

        public static int print(lua_State* state)
        {
            int n = Luau.lua_gettop(state);
            StringBuilder sb = new StringBuilder();

            for (int i = 1; i <= n; i++)
            {
                int type = Luau.lua_type(state, i);
                switch (type)
                {
                    case 5: // LUA_TSTRING
                    case 4: // LUA_TVECTOR (optional)
                        sbyte* str = Luau.lua_tostring(state, i);
                        if (str != null) sb.Append(Marshal.PtrToStringAnsi((IntPtr)str));
                        break;
                    case 3: // LUA_TNUMBER
                        sb.Append(Luau.lua_tonumber(state, i));
                        break;
                    case 1: // LUA_TBOOLEAN
                        sb.Append(Luau.lua_toboolean(state, i) != 0 ? "true" : "false");
                        break;
                    case 0: // LUA_TNIL
                        sb.Append("nil");
                        break;
                    default:
                        sb.Append($"[type:{type}]");
                        break;
                }
                if (i < n) sb.Append("\t");
            }

            Debug.Log($"[Lua] {sb}");
            return 0;
        }

        public static unsafe string LuaToString(lua_State* state, int index)
        {
            sbyte* ptr = Luau.lua_tostring(state, index);
            if (ptr == null) return null;
            return Marshal.PtrToStringAnsi((IntPtr)ptr);
        }
    }
}
