# CapriKit.DirectX11

This library is a light abstraction over DirectX 11. I chose DirectX 11 since it is a lot easier to work with than Vulkan and DirectX 12, while still providing me with enough power to make technology demos (and to hopefull actually finish a game for once).

The intent of this library is not to hide that it is DirectX 11, but I do not want users of this library to call DirectX11 functions themselves of work with Vortice specific structs. This way it is at least somewhat possible to switch the underlying framework (like Silk.NET) if needed or to analyze the code paths I need to mimic when moving to another technology entirely.
