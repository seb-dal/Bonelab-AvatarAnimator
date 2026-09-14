# NOTES
- This file is here to answer (I hope) most of the question as to why thing have be done this way

- Each player is responsible for his avatar animation. When a player change his state he must notify the other players. (To avoid timing shenanigans and desync)
- The file "Unity/AvatarAnimatorDataContainer" is used by both Unity and this mod to not having to duplicate the current animator API.
- The animator API (Unity -> mod) is store as a json string to allow for multiple versions handling. 
- Use Dbg nullable to add/remove without cost of computation of string debug Logs.
Same as `if(Logger.DebugLogs) Logger.DbgInfo($"____")` but more compact and uniform.
And better that `Logger.DbgInfo(() => $"_____")` because lambda variables capture is costly. 
And `Logger.DbgInfo($"____")` with the "if(...)" inside the function mean that the string will get be created and discarded.

- Barcode and RigManager are not yet updated when using Hooking.OnSwitchAvatarPostfix. Do it later using a hook invoke by OnUpdate.
- ScannedDataFusion always call ScannedData() constructor without being called using base(). So the code had to be moved in the static functions "create" so it stop overriding my thing.
- When level change, Player Avatar data must be updated regardless if some of the data is not valid yet to avoid keeping ref to deleted object.

- `OtherPlayerState` has one constructor with both one and multiple elements because Fusion Message don't like it having multple constructor.

