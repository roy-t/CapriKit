# CapriKit.DirectX11

This library is a light abstraction over DirectX 11. I chose DirectX 11 since it is a lot easier to work with than Vulkan and DirectX 12, while still providing me with enough power to make technology demos (and to hopefull actually finish a game for once).

The intent of this library is not to hide that it is DirectX 11, but I do not want users of this library to call DirectX11 functions themselves of work with Vortice specific structs. This way it is at least somewhat possible to switch the underlying framework (like Silk.NET) if needed or to analyze the code paths I need to mimic when moving to another technology entirely.

Interfaces exposed by this library (like `IPixelShader`) hide the underlying native (DirectX specific) pointer by making it an internal field on a public inteface. This means that nothing outside of this library can create a valid implementation of this interface. So we need a base type that others can extend from and factories that let something like a content pipeline instantiate these interfaces.

## A Note on Threading

The `Device` class and the underlying `ID3D11Device` are free threaded and can be used from any thread. However, the `DeviceContext` classes are NOT thread safe. Instead each thread should own their own `DeferredDeviceContext` which allows you to do most interactions with the GPU. The `ImmediateDeviceContext` should only be used in the main render loop to resolve a deferred calls and to display the final image. Note that use of `async` can be tricky since there is no synchronization context so any async call can return on any thread pool thread. Use async/await only for preparing data. Using a `DeviceContext`, `ImmediateDeviceContext` `DeferredDeviceContext` in an async method should be considered a game breaking bug.
