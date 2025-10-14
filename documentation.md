## Overview
The `Wrapper` class provides Lua bindings for Unity objects, allowing Lua scripts to interact with `GameObject`s, `Component`s, and other objects in Unity. All functions assume a `lua_State*` parameter (`state`) representing the Lua environment.

## Methods

### GetObject(lua_State* state)
**Description:**  
Retrieves a `GameObject` by name from the internal registry and pushes it to Lua.

**Parameters:**  
- `state`: Pointer to the Lua state.

**Returns:**  
- `1` value pushed onto the Lua stack: the found `GameObject`, or `nil` if not found.

---

### GetComponent(lua_State* state)
**Description:**  
Retrieves a `Component` from a `GameObject`.

**Parameters:**  
- `state`: Lua state.  
- Lua stack:
  1. `GameObject` to query.
  2. `string` type name of the component.

**Returns:**  
- Pushes the `Component` onto the Lua stack, or `nil` if not found or the object is `null`.

---

### GetProperty(lua_State* state)
**Description:**  
Gets a property or field value from an object.

**Parameters:**  
- `state`: Lua state.  
- Lua stack:
  1. Object to query.
  2. `string` property/field name.

**Returns:**  
- Pushes the property or field value onto the Lua stack, or `nil` if not found or inaccessible.

---

### SetProperty(lua_State* state)
**Description:**  
Sets a property or field value on an object.

**Parameters:**  
- `state`: Lua state.  
- Lua stack:
  1. Object to modify.
  2. `string` property/field name.
  3. Value to set (converted from Lua).

**Returns:**  
- Always returns `1`, pushing nothing. Errors are logged if the property/field is not writable.

---

### GetType(lua_State* state)
**Description:**  
Fetches a `Type` object by assembly name and type name.

**Parameters:**  
- `state`: Lua state.  
- Lua stack:
  1. `string` assembly name.
  2. `string` type name.

**Returns:**  
- Pushes a light userdata representing the `Type`, or `nil` if not found.

---

### CallFunction(lua_State* state)
**Description:**  
Calls a method on an object or a static type from Lua. Automatically handles argument conversion.

**Parameters:**  
- `state`: Lua state.  
- Lua stack:
  1. Object (or `nil` for static calls).
  2. `string` method name.
  3+. Arguments for the method.

**Behavior:**  
- Finds a matching method with compatible parameter types.
- Converts Lua values to .NET types as needed.
- If no exact match is found, attempts partial conversion and fills missing parameters with default values.

**Returns:**  
- Pushes the return value of the method onto the Lua stack, or `nil` if an error occurs.

---

### Notes
- All functions log errors to `myLogger` when objects are null, types are not found, or property/method access fails.
- Helper functions in `GorillaLuau.Helpers` are used extensively for type conversion and pushing values to Lua.
