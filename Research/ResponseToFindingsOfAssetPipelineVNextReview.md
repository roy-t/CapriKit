I have worked through the findings you reported in C:\projects\csharp\CapriKit\research\AssetPipelineVNextReview.html. Read that first so that you are familiar with the issues and issue numbering. I have made substational changes to CapriKit.AssetPipelile and few changes to some tests and CapriKit.Concurrency to address most of the issues.

Below are what I worked on and the questions I have for you. Write a new report html report (call it continued or something like that) and answer my questions. In your new report only give a short one line answer if the problem was fixed satisfactory. If the problem still exists or has created a new problem, report it as normal.

---

I made changes to fix issues B1-B4, please verify. Note that for B4 I do not mind if one failed load kills the game, but I now added a way to handle that so that the game can at least throw a complete error message.

I made changes to fix issues H1, H2 and H4, please verify

For H3 "Hot reload can HotSwap an asset that Collect already disposed": I would like an example of how the HotReloadManager and Cache can work better together I think that will also help me with H5 The dedupe guard compares two different types, so it is always false. How to fix this can I use the cache as some sort of authority here?

M1: I've changed how assets are disposed, this can now only be done for a bundle at a time.  (see AssetManager.Unload). Of course in the tests nobody unloads the assets yet. But is this mechanism sound and thread-safe?

M2, M3, M4: I think I fixed these, please check

Keep M5 as a TODO in your next report. I need to look at that later.

M6, I've changed how unloading works, is this now fixed?

M7: Ignore that for now, users are supposed to initialize the asset manager with all transcoders on start-up


M8: I think I fxed this, but there is now a lot of locking going on in HotReloadManager, can we make this simpler or at least more explicit. Hot reloading is very rare so maybe its better to use the concurrent collection types more often?


L5: in which places am I missing `ConfigureAwait(false)` not that .FireAndForget sets `ConfigureAwait(false)`

I have not looked at S0 to S7
