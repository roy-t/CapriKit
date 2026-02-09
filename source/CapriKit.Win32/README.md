# CapriKit.Win32

A library to provide a single window Windows application with easy to use abstractions for mouse and keyboard.

Usage: run `Win32Application.Initialize()` to initialize a win32 application with a single window. Call `PumpMessages()` every frame, then use the `Window`, `Mouse`, and `Keyboard` fields on that class to work with the input devices and window.
